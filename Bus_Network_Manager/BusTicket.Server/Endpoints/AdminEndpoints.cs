using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using BusTicket.Core.DTOs;
using BusTicket.Core.Enums;
using BusTicket.Core.Models;
using BusTicket.Server.Services;
using Route = BusTicket.Core.Models.Route;

namespace BusTicket.Server.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin");

        // ---------------- 1. TỔNG QUAN (DASHBOARD) ----------------
        // 23. Thống kê tổng quan
        group.MapGet("/dashboard/stats", (HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff && role != Roles.Support)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var today = DateTime.Today;
            var ticketsToday = store.Db.Tickets.Where(t => t.BookedAt.Date == today && (t.Status == TicketStatuses.Paid || t.Status == TicketStatuses.Confirmed)).ToList();
            var tripsToday = store.Db.Trips.Where(t => t.DepartureTime.Date == today).ToList();
            decimal revenueToday = ticketsToday.Sum(t => t.TotalAmount);

            var upcoming = store.Db.Trips
                .Where(t => t.DepartureTime >= DateTime.Now)
                .OrderBy(t => t.DepartureTime)
                .Take(5)
                .Select(t =>
                {
                    var route = store.Db.Routes.FirstOrDefault(r => r.RouteId == t.RouteId);
                    var seats = store.Db.Seats.Where(s => s.TripId == t.TripId).ToList();
                    return new UpcomingTripDto
                    {
                        TripId = t.TripId,
                        TripCode = t.TripCode,
                        RouteName = route != null ? $"{route.Origin} - {route.Destination}" : "Tuyến",
                        DepartureTime = t.DepartureTime,
                        BookedSeats = seats.Count(s => s.Status == SeatStatuses.Booked || s.Status == SeatStatuses.Pending),
                        TotalSeats = seats.Count
                    };
                }).ToList();

            var res = new DashboardStatsDto
            {
                TicketsToday = ticketsToday.Count,
                TripsToday = tripsToday.Count,
                RevenueToday = revenueToday,
                UpcomingTrips = upcoming
            };

            return Results.Ok(ApiResponse<DashboardStatsDto>.Ok(res, "Thành công"));
        });

        // 24. Danh sách chuyến sắp khởi hành (phân trang)
        group.MapGet("/dashboard/upcoming-trips", (int page, int pageSize, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff && role != Roles.Support)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = store.Db.Trips.Where(t => t.DepartureTime >= DateTime.Now).OrderBy(t => t.DepartureTime);
            int totalItems = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).Select(t =>
            {
                var route = store.Db.Routes.FirstOrDefault(r => r.RouteId == t.RouteId);
                var seats = store.Db.Seats.Where(s => s.TripId == t.TripId).ToList();
                return new UpcomingTripDto
                {
                    TripId = t.TripId,
                    TripCode = t.TripCode,
                    RouteName = route != null ? $"{route.Origin} - {route.Destination}" : "Tuyến",
                    DepartureTime = t.DepartureTime,
                    BookedSeats = seats.Count(s => s.Status == SeatStatuses.Booked),
                    TotalSeats = seats.Count
                };
            }).ToList();

            return Results.Ok(ApiResponse<PagedResult<UpcomingTripDto>>.Ok(new PagedResult<UpcomingTripDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }, "Thành công"));
        });

        // ---------------- 2. QUẢN LÝ CHUYẾN XE ----------------
        // 25. Xem danh sách chuyến
        group.MapGet("/trips", (string? tripCode, int? routeId, string? status, int page, int pageSize, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = store.Db.Trips.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(tripCode))
                query = query.Where(t => t.TripCode.Contains(tripCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (routeId.HasValue && routeId.Value > 0)
                query = query.Where(t => t.RouteId == routeId.Value);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));

            int totalItems = query.Count();
            var trips = query.OrderByDescending(t => t.DepartureTime).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var items = trips.Select(t =>
            {
                var route = store.Db.Routes.FirstOrDefault(r => r.RouteId == t.RouteId);
                var vehicle = store.Db.Vehicles.FirstOrDefault(v => v.VehicleId == t.VehicleId);
                var driver = store.Db.Users.FirstOrDefault(u => u.Id == t.DriverId);
                var assistant = t.AssistantId.HasValue ? store.Db.Users.FirstOrDefault(u => u.Id == t.AssistantId.Value) : null;
                var seats = store.Db.Seats.Where(s => s.TripId == t.TripId).ToList();

                return new AdminTripDto
                {
                    TripId = t.TripId,
                    TripCode = t.TripCode,
                    RouteName = route != null ? $"{route.Origin} - {route.Destination}" : "",
                    DepartureTime = t.DepartureTime,
                    VehiclePlate = vehicle?.LicensePlate ?? "",
                    DriverName = driver?.FullName ?? "",
                    AssistantName = assistant?.FullName ?? "",
                    Price = t.Price,
                    BookedSeats = seats.Count(s => s.Status == SeatStatuses.Booked),
                    TotalSeats = seats.Count,
                    Status = t.Status
                };
            }).ToList();

            return Results.Ok(ApiResponse<PagedResult<AdminTripDto>>.Ok(new PagedResult<AdminTripDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }, "Thành công"));
        });

        // 26. Lấy dữ liệu cho form thêm chuyến
        group.MapGet("/trips/form-options", (HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var routes = store.Db.Routes.Where(r => r.Status == RouteStatuses.Active).Select(r => new OptionItemDto
            {
                Id = r.RouteId,
                Name = $"{r.Origin} - {r.Destination}"
            }).ToList();

            var vehicles = store.Db.Vehicles.Where(v => v.Status == VehicleStatuses.Active).Select(v => new VehicleOptionDto
            {
                Id = v.VehicleId,
                LicensePlate = v.LicensePlate
            }).ToList();

            var drivers = store.Db.Users.Where(u => u.Role == Roles.Staff && u.Status == AccountStatuses.Active).Select(u => new OptionItemDto
            {
                Id = u.Id,
                Name = u.FullName
            }).ToList();

            var assistants = store.Db.Users.Where(u => u.Role == Roles.Staff && u.Status == AccountStatuses.Active).Select(u => new OptionItemDto
            {
                Id = u.Id,
                Name = u.FullName
            }).ToList();

            var res = new TripFormOptionsDto
            {
                Routes = routes,
                Vehicles = vehicles,
                Drivers = drivers,
                Assistants = assistants
            };

            return Results.Ok(ApiResponse<TripFormOptionsDto>.Ok(res, "Thành công"));
        });

        // 27. Thêm chuyến
        group.MapPost("/trips", (CreateTripRequest body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (body.RouteId <= 0 || body.VehicleId <= 0 || body.DriverId <= 0 || body.Price <= 0)
                return Results.Json(ApiResponse<object>.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"), statusCode: 400);

            // Kiểm tra trùng lịch xe hoặc tài xế trong vòng 4 tiếng
            var conflict = store.Db.Trips.Any(t =>
                (t.VehicleId == body.VehicleId || t.DriverId == body.DriverId) &&
                t.Status != TripStatuses.Cancelled &&
                Math.Abs((t.DepartureTime - body.DepartureTime).TotalHours) < 4);

            if (conflict)
                return Results.Json(ApiResponse<object>.Fail("Xe hoặc tài xế bị trùng lịch chạy"), statusCode: 409);

            var route = store.Db.Routes.FirstOrDefault(r => r.RouteId == body.RouteId);
            int duration = route?.DurationMinutes ?? 360;
            var arrival = body.DepartureTime.AddMinutes(duration);

            var trip = new Trip
            {
                TripId = store.GetNextTripId(),
                TripCode = store.GenerateTripCode(),
                RouteId = body.RouteId,
                VehicleId = body.VehicleId,
                DriverId = body.DriverId,
                AssistantId = body.AssistantId,
                DepartureTime = body.DepartureTime,
                EstimatedArrival = arrival,
                Price = body.Price,
                Surcharge = body.Surcharge,
                Status = TripStatuses.Upcoming,
                CreatedAt = DateTime.Now
            };

            store.Db.Trips.Add(trip);

            // Sinh danh sách ghế tự động theo loại xe
            var vehicle = store.Db.Vehicles.FirstOrDefault(v => v.VehicleId == body.VehicleId);
            var vType = vehicle != null ? store.Db.VehicleTypes.FirstOrDefault(vt => vt.Id == vehicle.VehicleTypeId) : null;
            int seatCount = vType?.SeatCount ?? 40;

            for (int i = 1; i <= seatCount; i++)
            {
                string code = seatCount <= 9 ? $"VIP{i}" : (i <= seatCount / 2 ? $"A{i:D2}" : $"B{(i - seatCount / 2):D2}");
                store.Db.Seats.Add(new Seat
                {
                    SeatId = store.GetNextSeatId(),
                    TripId = trip.TripId,
                    SeatCode = code,
                    Status = SeatStatuses.Available,
                    Price = body.Price + body.Surcharge
                });
            }

            store.SaveChanges();

            return Results.Json(ApiResponse<object>.Ok(new
            {
                tripId = trip.TripId,
                tripCode = trip.TripCode
            }, "Thêm chuyến thành công"), statusCode: 201);
        });

        // 28. Xem chi tiết chuyến
        group.MapGet("/trips/{tripId:int}", (int tripId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var trip = store.Db.Trips.FirstOrDefault(t => t.TripId == tripId);
            if (trip == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy chuyến"), statusCode: 404);

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                tripId = trip.TripId,
                tripCode = trip.TripCode,
                routeId = trip.RouteId,
                departureTime = trip.DepartureTime,
                vehicleId = trip.VehicleId,
                driverId = trip.DriverId,
                assistantId = trip.AssistantId,
                price = trip.Price,
                surcharge = trip.Surcharge,
                status = trip.Status
            }, "Thành công"));
        });

        // 29. Sửa chuyến
        group.MapPut("/trips/{tripId:int}", (int tripId, UpdateTripRequest body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var trip = store.Db.Trips.FirstOrDefault(t => t.TripId == tripId);
            if (trip == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy chuyến"), statusCode: 404);

            // Kiểm tra trùng lịch xe/tài xế ngoại trừ chính chuyến này
            var conflict = store.Db.Trips.Any(t =>
                t.TripId != tripId &&
                (t.VehicleId == body.VehicleId || t.DriverId == body.DriverId) &&
                t.Status != TripStatuses.Cancelled &&
                Math.Abs((t.DepartureTime - body.DepartureTime).TotalHours) < 4);

            if (conflict)
                return Results.Json(ApiResponse<object>.Fail("Xe hoặc tài xế bị trùng lịch chạy"), statusCode: 409);

            trip.RouteId = body.RouteId;
            trip.VehicleId = body.VehicleId;
            trip.DriverId = body.DriverId;
            trip.AssistantId = body.AssistantId;
            trip.DepartureTime = body.DepartureTime;
            trip.Price = body.Price;
            trip.Surcharge = body.Surcharge;
            trip.Status = body.Status;

            store.SaveChanges();

            return Results.Ok(ApiResponse<object>.Ok(new { tripId = trip.TripId }, "Cập nhật chuyến thành công"));
        });

        // ---------------- 3. QUẢN LÝ TUYẾN XE ----------------
        // 30. Xem danh sách tuyến
        group.MapGet("/routes", (string? routeCode, string? status, int page, int pageSize, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = store.Db.Routes.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(routeCode))
                query = query.Where(r => r.RouteCode.Contains(routeCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));

            int totalItems = query.Count();
            var routes = query.OrderBy(r => r.RouteCode).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var items = routes.Select(r => new AdminRouteDto
            {
                RouteId = r.RouteId,
                RouteCode = r.RouteCode,
                Origin = r.Origin,
                Destination = r.Destination,
                Status = r.Status,
                UpcomingTrips = store.Db.Trips.Count(t => t.RouteId == r.RouteId && t.DepartureTime >= DateTime.Now && t.Status != TripStatuses.Cancelled)
            }).ToList();

            return Results.Ok(ApiResponse<PagedResult<AdminRouteDto>>.Ok(new PagedResult<AdminRouteDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }, "Thành công"));
        });

        // 31. Thêm tuyến
        group.MapPost("/routes", (CreateRouteRequest body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (string.IsNullOrWhiteSpace(body.Origin) || string.IsNullOrWhiteSpace(body.Destination))
                return Results.Json(ApiResponse<object>.Fail("Điểm đi và điểm đến không được để trống"), statusCode: 400);

            if (store.Db.Routes.Any(r => r.Origin.Equals(body.Origin.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                         r.Destination.Equals(body.Destination.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Results.Json(ApiResponse<object>.Fail("Tuyến đường này đã tồn tại"), statusCode: 409);

            var route = new Route
            {
                RouteId = store.GetNextRouteId(),
                RouteCode = store.GenerateRouteCode(),
                Origin = body.Origin.Trim(),
                Destination = body.Destination.Trim(),
                DistanceKm = body.DistanceKm ?? 0,
                DurationMinutes = body.DurationMinutes ?? 0,
                Status = RouteStatuses.Active,
                CreatedAt = DateTime.Now
            };

            store.Db.Routes.Add(route);

            // Add pickup points
            int idx = 1;
            foreach (var p in body.PickupPoints)
            {
                store.Db.StopPoints.Add(new RouteStopPoint
                {
                    Id = store.GetNextStopPointId(),
                    RouteId = route.RouteId,
                    Name = p.Name,
                    Address = p.Address,
                    IsPickup = true,
                    OrderIndex = idx++
                });
            }

            // Add dropoff points
            idx = 1;
            foreach (var d in body.DropoffPoints)
            {
                store.Db.StopPoints.Add(new RouteStopPoint
                {
                    Id = store.GetNextStopPointId(),
                    RouteId = route.RouteId,
                    Name = d.Name,
                    Address = d.Address,
                    IsPickup = false,
                    OrderIndex = idx++
                });
            }

            store.SaveChanges();

            return Results.Json(ApiResponse<object>.Ok(new
            {
                routeId = route.RouteId,
                routeCode = route.RouteCode
            }, "Thêm tuyến thành công"), statusCode: 201);
        });

        // 32. Xem chi tiết tuyến
        group.MapGet("/routes/{routeId:int}", (int routeId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var route = store.Db.Routes.FirstOrDefault(r => r.RouteId == routeId);
            if (route == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy tuyến"), statusCode: 404);

            var pickups = store.Db.StopPoints.Where(p => p.RouteId == routeId && p.IsPickup).OrderBy(p => p.OrderIndex).Select(p => new StopPointDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address
            }).ToList();

            var dropoffs = store.Db.StopPoints.Where(p => p.RouteId == routeId && !p.IsPickup).OrderBy(p => p.OrderIndex).Select(p => new StopPointDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address
            }).ToList();

            var res = new RouteDetailDto
            {
                RouteId = route.RouteId,
                RouteCode = route.RouteCode,
                Origin = route.Origin,
                Destination = route.Destination,
                DistanceKm = route.DistanceKm,
                DurationMinutes = route.DurationMinutes,
                Status = route.Status,
                PickupPoints = pickups,
                DropoffPoints = dropoffs
            };

            return Results.Ok(ApiResponse<RouteDetailDto>.Ok(res, "Thành công"));
        });

        // 33. Sửa tuyến
        group.MapPut("/routes/{routeId:int}", (int routeId, UpdateRouteRequest body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var route = store.Db.Routes.FirstOrDefault(r => r.RouteId == routeId);
            if (route == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy tuyến"), statusCode: 404);

            route.Origin = body.Origin.Trim();
            route.Destination = body.Destination.Trim();
            route.DistanceKm = body.DistanceKm ?? route.DistanceKm;
            route.DurationMinutes = body.DurationMinutes ?? route.DurationMinutes;
            route.Status = body.Status;

            store.SaveChanges();

            return Results.Ok(ApiResponse<object>.Ok(new { routeId = route.RouteId }, "Cập nhật tuyến thành công"));
        });

        // ---------------- 4. QUẢN LÝ XE VÀ BẢO TRÌ ----------------
        // 34. Xem danh sách xe
        group.MapGet("/vehicles", (string? licensePlate, string? status, int page, int pageSize, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = store.Db.Vehicles.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(licensePlate))
                query = query.Where(v => v.LicensePlate.Contains(licensePlate.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(v => v.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));

            int totalItems = query.Count();
            var list = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var items = list.Select(v =>
            {
                var vt = store.Db.VehicleTypes.FirstOrDefault(t => t.Id == v.VehicleTypeId);
                return new AdminVehicleDto
                {
                    VehicleId = v.VehicleId,
                    LicensePlate = v.LicensePlate,
                    VehicleTypeId = v.VehicleTypeId,
                    VehicleType = vt?.Name ?? "",
                    Status = v.Status
                };
            }).ToList();

            return Results.Ok(ApiResponse<PagedResult<AdminVehicleDto>>.Ok(new PagedResult<AdminVehicleDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }, "Thành công"));
        });

        // 35. Xem danh sách xe và lịch bảo trì
        group.MapGet("/maintenance", (HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var vehicles = store.Db.Vehicles.Select(v => new AdminVehicleDto
            {
                VehicleId = v.VehicleId,
                LicensePlate = v.LicensePlate,
                VehicleTypeId = v.VehicleTypeId,
                VehicleType = store.Db.VehicleTypes.FirstOrDefault(t => t.Id == v.VehicleTypeId)?.Name ?? "",
                Status = v.Status
            }).ToList();

            var schedules = store.Db.Maintenances.Select(m =>
            {
                var v = store.Db.Vehicles.FirstOrDefault(x => x.VehicleId == m.VehicleId);
                return new MaintenanceScheduleDto
                {
                    Id = m.Id,
                    VehicleId = m.VehicleId,
                    LicensePlate = v?.LicensePlate ?? "",
                    StartDate = m.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = m.EndDate.ToString("yyyy-MM-dd"),
                    Note = m.Note
                };
            }).ToList();

            var res = new MaintenanceResponseData
            {
                Vehicles = vehicles,
                Schedules = schedules
            };

            return Results.Ok(ApiResponse<MaintenanceResponseData>.Ok(res, "Thành công"));
        });

        // 36. Xem danh sách loại xe
        group.MapGet("/vehicle-types", (HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var list = store.Db.VehicleTypes.Select(vt => new VehicleTypeDto
            {
                Id = vt.Id,
                Name = vt.Name,
                SeatCount = vt.SeatCount
            }).ToList();

            return Results.Ok(ApiResponse<List<VehicleTypeDto>>.Ok(list, "Thành công"));
        });

        // 37. Thêm xe
        group.MapPost("/vehicles", (CreateVehicleRequest body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (string.IsNullOrWhiteSpace(body.LicensePlate) || body.VehicleTypeId <= 0)
                return Results.Json(ApiResponse<object>.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"), statusCode: 400);

            if (store.Db.Vehicles.Any(v => v.LicensePlate.Equals(body.LicensePlate.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Results.Json(ApiResponse<object>.Fail("Biển số xe đã tồn tại"), statusCode: 409);

            var vehicle = new Vehicle
            {
                VehicleId = store.GetNextVehicleId(),
                LicensePlate = body.LicensePlate.Trim(),
                VehicleTypeId = body.VehicleTypeId,
                Status = VehicleStatuses.Active,
                CreatedAt = DateTime.Now
            };

            store.Db.Vehicles.Add(vehicle);
            store.SaveChanges();

            return Results.Json(ApiResponse<object>.Ok(new { vehicleId = vehicle.VehicleId }, "Thêm xe thành công"), statusCode: 201);
        });

        // 38. Xem chi tiết xe
        group.MapGet("/vehicles/{vehicleId:int}", (int vehicleId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var v = store.Db.Vehicles.FirstOrDefault(x => x.VehicleId == vehicleId);
            if (v == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy xe"), statusCode: 404);

            var vt = store.Db.VehicleTypes.FirstOrDefault(t => t.Id == v.VehicleTypeId);

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                vehicleId = v.VehicleId,
                licensePlate = v.LicensePlate,
                vehicleTypeId = v.VehicleTypeId,
                vehicleType = vt?.Name ?? "",
                status = v.Status
            }, "Thành công"));
        });

        // 39. Sửa xe
        group.MapPut("/vehicles/{vehicleId:int}", (int vehicleId, CreateVehicleRequest body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var v = store.Db.Vehicles.FirstOrDefault(x => x.VehicleId == vehicleId);
            if (v == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy xe"), statusCode: 404);

            if (store.Db.Vehicles.Any(x => x.VehicleId != vehicleId && x.LicensePlate.Equals(body.LicensePlate.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Results.Json(ApiResponse<object>.Fail("Biển số xe đã tồn tại"), statusCode: 409);

            v.LicensePlate = body.LicensePlate.Trim();
            v.VehicleTypeId = body.VehicleTypeId;
            store.SaveChanges();

            return Results.Ok(ApiResponse<object>.Ok(new { vehicleId = v.VehicleId }, "Cập nhật xe thành công"));
        });

        // 40. Thêm lịch bảo trì
        group.MapPost("/maintenance", (CreateMaintenanceRequest body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var v = store.Db.Vehicles.FirstOrDefault(x => x.VehicleId == body.VehicleId);
            if (v == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy xe"), statusCode: 404);

            if (!DateTime.TryParse(body.StartDate, out var start) || !DateTime.TryParse(body.EndDate, out var end))
                return Results.Json(ApiResponse<object>.Fail("Định dạng ngày không hợp lệ"), statusCode: 400);

            var m = new VehicleMaintenance
            {
                Id = store.GetNextMaintenanceId(),
                VehicleId = body.VehicleId,
                StartDate = start,
                EndDate = end,
                Note = body.Note ?? "",
                CreatedAt = DateTime.Now
            };

            v.Status = VehicleStatuses.Maintenance;
            store.Db.Maintenances.Add(m);
            store.SaveChanges();

            return Results.Json(ApiResponse<object>.Ok(new { id = m.Id }, "Thêm lịch bảo trì thành công"), statusCode: 201);
        });

        // ---------------- 5. QUẢN LÝ GHẾ ----------------
        // 41. Xem sơ đồ ghế của chuyến
        group.MapGet("/trips/{tripId:int}/seats", (int tripId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var trip = store.Db.Trips.FirstOrDefault(t => t.TripId == tripId);
            if (trip == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy chuyến"), statusCode: 404);

            var route = store.Db.Routes.FirstOrDefault(r => r.RouteId == trip.RouteId);
            var seats = store.Db.Seats.Where(s => s.TripId == tripId).Select(s => new SeatDto
            {
                SeatId = s.SeatId,
                SeatCode = s.SeatCode,
                Status = s.Status,
                Price = s.Price
            }).ToList();

            var pickups = store.Db.StopPoints.Where(p => p.RouteId == trip.RouteId && p.IsPickup).Select(p => new StopPointDto
            {
                Id = p.Id,
                Name = p.Name
            }).ToList();

            var dropoffs = store.Db.StopPoints.Where(p => p.RouteId == trip.RouteId && !p.IsPickup).Select(p => new StopPointDto
            {
                Id = p.Id,
                Name = p.Name
            }).ToList();

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                trip = new
                {
                    tripId = trip.TripId,
                    tripCode = trip.TripCode,
                    routeName = route != null ? $"{route.Origin} - {route.Destination}" : "",
                    departureTime = trip.DepartureTime
                },
                seats = seats,
                pickupPoints = pickups,
                dropoffPoints = dropoffs
            }, "Thành công"));
        });

        // ---------------- 6. QUẢN LÝ VÉ ----------------
        // 42. Xem danh sách vé
        group.MapGet("/tickets", (string? ticketCode, int? tripId, string? status, int page, int pageSize, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = store.Db.Tickets.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(ticketCode))
                query = query.Where(t => t.TicketCode.Contains(ticketCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (tripId.HasValue && tripId.Value > 0)
                query = query.Where(t => t.TripId == tripId.Value);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));

            int totalItems = query.Count();
            var tickets = query.OrderByDescending(t => t.BookedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var items = tickets.Select(t =>
            {
                var trip = store.Db.Trips.FirstOrDefault(tr => tr.TripId == t.TripId);
                var seat = store.Db.Seats.FirstOrDefault(s => s.SeatId == t.SeatId);
                return new AdminTicketDto
                {
                    TicketId = t.TicketId,
                    TicketCode = t.TicketCode,
                    TripCode = trip?.TripCode ?? "",
                    PassengerName = t.PassengerName,
                    SeatCode = seat?.SeatCode ?? "",
                    TotalAmount = t.TotalAmount,
                    Channel = t.Channel,
                    Status = t.Status
                };
            }).ToList();

            return Results.Ok(ApiResponse<PagedResult<AdminTicketDto>>.Ok(new PagedResult<AdminTicketDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }, "Thành công"));
        });

        // 43. Xem chi tiết vé
        group.MapGet("/tickets/{ticketId:int}", (int ticketId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var ticket = store.Db.Tickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy vé"), statusCode: 404);

            var trip = store.Db.Trips.FirstOrDefault(tr => tr.TripId == ticket.TripId);
            var route = trip != null ? store.Db.Routes.FirstOrDefault(r => r.RouteId == trip.RouteId) : null;
            var seat = store.Db.Seats.FirstOrDefault(s => s.SeatId == ticket.SeatId);
            var pickup = store.Db.StopPoints.FirstOrDefault(p => p.Id == ticket.PickupPointId);
            var dropoff = store.Db.StopPoints.FirstOrDefault(p => p.Id == ticket.DropoffPointId);

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                ticketId = ticket.TicketId,
                ticketCode = ticket.TicketCode,
                status = ticket.Status,
                channel = ticket.Channel,
                trip = new
                {
                    tripCode = trip?.TripCode ?? "",
                    routeName = route != null ? $"{route.Origin} - {route.Destination}" : "",
                    departureTime = trip?.DepartureTime ?? DateTime.Now
                },
                seatCode = seat?.SeatCode ?? "",
                passenger = new
                {
                    name = ticket.PassengerName,
                    phone = ticket.PassengerPhone
                },
                pickupPoint = pickup?.Name ?? "",
                dropoffPoint = dropoff?.Name ?? "",
                totalAmount = ticket.TotalAmount
            }, "Thành công"));
        });

        // 44. Xác nhận vé
        group.MapPut("/tickets/{ticketId:int}/confirm", (int ticketId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var ticket = store.Db.Tickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy vé"), statusCode: 404);

            if (ticket.Status == TicketStatuses.Cancelled)
                return Results.Json(ApiResponse<object>.Fail("Không thể xác nhận vé đã bị hủy"), statusCode: 409);

            ticket.Status = TicketStatuses.Confirmed;
            ticket.ConfirmedAt = DateTime.Now;
            store.SaveChanges();

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                ticketId = ticket.TicketId,
                status = ticket.Status
            }, "Xác nhận vé thành công"));
        });

        // 45. Đặt vé mới (tại quầy)
        group.MapPost("/tickets", (CounterBookingRequest body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var trip = store.Db.Trips.FirstOrDefault(t => t.TripId == body.TripId);
            if (trip == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy chuyến"), statusCode: 404);

            var seat = store.Db.Seats.FirstOrDefault(s => s.SeatId == body.SeatId && s.TripId == body.TripId);
            if (seat == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy ghế"), statusCode: 404);

            if (seat.Status == SeatStatuses.Booked)
                return Results.Json(ApiResponse<object>.Fail("Ghế không còn trống"), statusCode: 409);

            seat.Status = SeatStatuses.Booked;
            string ticketCode = store.GenerateTicketCode();

            var ticket = new Ticket
            {
                TicketId = store.GetNextTicketId(),
                TicketCode = ticketCode,
                TripId = body.TripId,
                SeatId = body.SeatId,
                CustomerId = null,
                PassengerName = body.PassengerName.Trim(),
                PassengerPhone = body.PassengerPhone.Trim(),
                PassengerEmail = body.PassengerEmail?.Trim(),
                PickupPointId = body.PickupPointId,
                DropoffPointId = body.DropoffPointId,
                TotalAmount = trip.Price + trip.Surcharge,
                Channel = BookingChannels.Direct,
                Status = TicketStatuses.Confirmed,
                BookedAt = DateTime.Now,
                PaidAt = DateTime.Now,
                ConfirmedAt = DateTime.Now
            };

            store.Db.Tickets.Add(ticket);
            store.SaveChanges();

            return Results.Json(ApiResponse<object>.Ok(new
            {
                ticketId = ticket.TicketId,
                ticketCode = ticket.TicketCode
            }, "Tạo vé thành công"), statusCode: 201);
        });

        // ---------------- 7. QUẢN LÝ QUYỀN TRUY CẬP ----------------
        // 46. Xem tài khoản nhân viên và quyền
        group.MapGet("/staff-accounts", (HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var accounts = store.Db.Users.Where(u => u.Role == Roles.Staff || u.Role == Roles.Support).Select(u => new StaffAccountDto
            {
                AccountId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                Status = u.Status
            }).ToList();

            var perms = new List<RolePermissionDto>
            {
                new RolePermissionDto { Role = Roles.Staff, Functions = new List<string> { "Quản lý chuyến xe", "Quản lý tuyến xe", "Quản lý xe", "Quản lý vé" } },
                new RolePermissionDto { Role = Roles.Support, Functions = new List<string> { "Hỗ trợ khách hàng", "Tra cứu khách hàng" } }
            };

            var res = new StaffAccountsResponseData
            {
                Accounts = accounts,
                Permissions = perms
            };

            return Results.Ok(ApiResponse<StaffAccountsResponseData>.Ok(res, "Thành công"));
        });

        // 47. Khóa tài khoản nhân viên
        group.MapPut("/staff-accounts/{accountId:int}/lock", (int accountId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var account = store.Db.Users.FirstOrDefault(u => u.Id == accountId);
            if (account == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy tài khoản"), statusCode: 404);

            account.Status = account.Status == AccountStatuses.Locked ? AccountStatuses.Active : AccountStatuses.Locked;
            store.SaveChanges();

            string msg = account.Status == AccountStatuses.Locked ? "Đã khóa tài khoản nhân viên" : "Đã mở khóa tài khoản nhân viên";
            return Results.Ok(ApiResponse<object>.Ok(new { accountId = account.Id, status = account.Status }, msg));
        });

        // ---------------- 8. TRA CỨU KHÁCH HÀNG ----------------
        // 48. Tìm kiếm khách hàng
        group.MapGet("/customers", (string? fullName, string? email, string? phone, int page, int pageSize, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff && role != Roles.Support)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = store.Db.Users.Where(u => u.Role == Roles.Customer).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(fullName))
                query = query.Where(u => u.FullName.Contains(fullName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(u => u.Email.Contains(email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(phone))
                query = query.Where(u => u.Phone.Contains(phone.Trim(), StringComparison.OrdinalIgnoreCase));

            int totalItems = query.Count();
            var list = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var items = list.Select(u => new CustomerItemDto
            {
                CustomerId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                TicketCount = store.Db.Tickets.Count(t => t.CustomerId == u.Id)
            }).ToList();

            return Results.Ok(ApiResponse<PagedResult<CustomerItemDto>>.Ok(new PagedResult<CustomerItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }, "Thành công"));
        });

        // 49. Xem thông tin khách hàng
        group.MapGet("/customers/{customerId:int}", (int customerId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Staff && role != Roles.Support)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var user = store.Db.Users.FirstOrDefault(u => u.Id == customerId);
            if (user == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy khách hàng"), statusCode: 404);

            var tickets = store.Db.Tickets.Where(t => t.CustomerId == customerId).Select(t =>
            {
                var trip = store.Db.Trips.FirstOrDefault(tr => tr.TripId == t.TripId);
                var route = trip != null ? store.Db.Routes.FirstOrDefault(r => r.RouteId == trip.RouteId) : null;
                var seat = store.Db.Seats.FirstOrDefault(s => s.SeatId == t.SeatId);
                return new MyTicketDto
                {
                    TicketId = t.TicketId,
                    TicketCode = t.TicketCode,
                    RouteName = route != null ? $"{route.Origin} - {route.Destination}" : "",
                    DepartureTime = trip?.DepartureTime ?? DateTime.Now,
                    SeatCode = seat?.SeatCode ?? "",
                    TotalAmount = t.TotalAmount,
                    Status = t.Status
                };
            }).ToList();

            var res = new CustomerDetailData
            {
                CustomerId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Tickets = tickets
            };

            return Results.Ok(ApiResponse<CustomerDetailData>.Ok(res, "Thành công"));
        });

        // ---------------- 9. HỖ TRỢ KHÁCH HÀNG ----------------
        // 50. Xem danh sách yêu cầu hỗ trợ
        group.MapGet("/support/tickets", (string? status, string? title, int? handlerId, int page, int pageSize, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Support)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var allTickets = store.Db.SupportTickets.ToList();
            var stats = new SupportStatisticsDto
            {
                Total = allTickets.Count,
                Pending = allTickets.Count(t => t.Status == SupportTicketStatuses.Pending),
                Processing = allTickets.Count(t => t.Status == SupportTicketStatuses.Processing),
                WaitingCustomer = allTickets.Count(t => t.Status == SupportTicketStatuses.WaitingCustomer),
                Resolved = allTickets.Count(t => t.Status == SupportTicketStatuses.Resolved)
            };

            var query = allTickets.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(t => t.Title.Contains(title.Trim(), StringComparison.OrdinalIgnoreCase));
            if (handlerId.HasValue && handlerId.Value > 0)
                query = query.Where(t => t.HandlerId == handlerId.Value);

            int totalItems = query.Count();
            var list = query.OrderByDescending(t => t.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var items = list.Select(t =>
            {
                var cust = store.Db.Users.FirstOrDefault(u => u.Id == t.CustomerId);
                var handler = t.HandlerId.HasValue ? store.Db.Users.FirstOrDefault(u => u.Id == t.HandlerId.Value) : null;
                var lastMsg = store.Db.SupportMessages.Where(m => m.TicketId == t.TicketId).OrderByDescending(m => m.SentAt).FirstOrDefault();

                return new SupportTicketSummaryDto
                {
                    TicketId = t.TicketId,
                    Title = t.Title,
                    CustomerName = cust?.FullName ?? "Khách hàng",
                    Status = t.Status,
                    HandlerName = handler?.FullName,
                    LastMessageAt = lastMsg?.SentAt ?? t.UpdatedAt
                };
            }).ToList();

            var res = new AdminSupportTicketListResponseData
            {
                Statistics = stats,
                Tickets = new PagedResult<SupportTicketSummaryDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            };

            return Results.Ok(ApiResponse<AdminSupportTicketListResponseData>.Ok(res, "Thành công"));
        });

        // 51. Xem nội dung hỗ trợ (tin nhắn của ticket)
        group.MapGet("/support/tickets/{ticketId:int}/messages", (int ticketId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Support)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var ticket = store.Db.SupportTickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy ticket"), statusCode: 404);

            var messages = store.Db.SupportMessages
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.SentAt)
                .Select(m => new SupportMessageDto
                {
                    Id = m.Id,
                    TicketId = m.TicketId,
                    SenderId = m.SenderId,
                    SenderRole = m.SenderRole,
                    Content = m.Content,
                    SentAt = m.SentAt
                }).ToList();

            return Results.Ok(ApiResponse<List<SupportMessageDto>>.Ok(messages, "Thành công"));
        });

        // 52. Nhận xử lý hoặc đánh dấu đã xử lý
        group.MapPut("/support/tickets/{ticketId:int}/status", (int ticketId, UpdateTicketStatusRequest body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out int userId, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin && role != Roles.Support)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            var ticket = store.Db.SupportTickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy ticket"), statusCode: 404);

            ticket.Status = body.Status;
            ticket.HandlerId = userId;
            ticket.UpdatedAt = DateTime.Now;
            store.SaveChanges();

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                ticketId = ticket.TicketId,
                status = ticket.Status,
                handlerId = ticket.HandlerId
            }, "Cập nhật trạng thái thành công"));
        });

        // ---------------- 10. DOANH THU ----------------
        // 53. Xem doanh thu
        group.MapGet("/revenue", (string? fromDate, string? toDate, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (role != Roles.Admin)
                return Results.Json(ApiResponse<object>.Fail("Không đủ quyền"), statusCode: 403);

            DateTime start = DateTime.TryParse(fromDate, out var s) ? s.Date : DateTime.Today.AddDays(-7);
            DateTime end = DateTime.TryParse(toDate, out var e) ? e.Date : DateTime.Today;

            var validTickets = store.Db.Tickets
                .Where(t => t.BookedAt.Date >= start && t.BookedAt.Date <= end &&
                           (t.Status == TicketStatuses.Paid || t.Status == TicketStatuses.Confirmed))
                .ToList();

            decimal total = validTickets.Sum(t => t.TotalAmount);
            decimal online = validTickets.Where(t => t.Channel == BookingChannels.Online).Sum(t => t.TotalAmount);
            decimal offline = validTickets.Where(t => t.Channel == BookingChannels.Direct).Sum(t => t.TotalAmount);

            var grouped = validTickets
                .GroupBy(t => t.BookedAt.ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .Select(g => new DailyRevenueDto
                {
                    Date = g.Key,
                    Online = g.Where(x => x.Channel == BookingChannels.Online).Sum(x => x.TotalAmount),
                    Offline = g.Where(x => x.Channel == BookingChannels.Direct).Sum(x => x.TotalAmount),
                    Total = g.Sum(x => x.TotalAmount)
                }).ToList();

            var res = new RevenueReportDto
            {
                FromDate = start.ToString("yyyy-MM-dd"),
                ToDate = end.ToString("yyyy-MM-dd"),
                TotalRevenue = total,
                OnlineRevenue = online,
                OfflineRevenue = offline,
                Details = grouped
            };

            return Results.Ok(ApiResponse<RevenueReportDto>.Ok(res, "Thành công"));
        });
    }
}
