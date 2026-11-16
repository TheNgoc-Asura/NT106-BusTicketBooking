using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using BusTicket.Core.DTOs;
using BusTicket.Core.Enums;
using BusTicket.Server.Services;

namespace BusTicket.Server.Endpoints;

public static class CustomerTripEndpoints
{
    public static void MapCustomerTripEndpoints(this WebApplication app)
    {
        // 12. Tìm kiếm chuyến xe
        app.MapGet("/api/trips", (string? from, string? to, string? date, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            var query = store.Db.Trips.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
            {
                query = query.Where(t => t.DepartureTime.Date == parsedDate.Date);
            }

            var result = new List<TripDto>();

            foreach (var trip in query)
            {
                var route = store.Db.Routes.FirstOrDefault(r => r.RouteId == trip.RouteId);
                if (route == null) continue;

                if (!string.IsNullOrWhiteSpace(from) && !route.Origin.Contains(from.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(to) && !route.Destination.Contains(to.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                var vehicle = store.Db.Vehicles.FirstOrDefault(v => v.VehicleId == trip.VehicleId);
                var vType = vehicle != null ? store.Db.VehicleTypes.FirstOrDefault(vt => vt.Id == vehicle.VehicleTypeId) : null;

                var seats = store.Db.Seats.Where(s => s.TripId == trip.TripId).ToList();
                int totalSeats = seats.Count;
                int availableSeats = seats.Count(s => s.Status == SeatStatuses.Available);

                result.Add(new TripDto
                {
                    TripId = trip.TripId,
                    TripCode = trip.TripCode,
                    RouteName = $"{route.Origin} - {route.Destination}",
                    Origin = route.Origin,
                    Destination = route.Destination,
                    DepartureTime = trip.DepartureTime,
                    EstimatedArrival = trip.EstimatedArrival,
                    VehicleType = vType?.Name ?? "Tiêu chuẩn",
                    Price = trip.Price,
                    TotalSeats = totalSeats,
                    AvailableSeats = availableSeats
                });
            }

            return Results.Ok(ApiResponse<List<TripDto>>.Ok(result, "Thành công"));
        });

        // 13. Xem sơ đồ ghế của chuyến
        app.MapGet("/api/trips/{tripId:int}/seats", (int tripId, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out _, out _, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            var trip = store.Db.Trips.FirstOrDefault(t => t.TripId == tripId);
            if (trip == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy chuyến xe"), statusCode: 404);

            var vehicle = store.Db.Vehicles.FirstOrDefault(v => v.VehicleId == trip.VehicleId);
            var vType = vehicle != null ? store.Db.VehicleTypes.FirstOrDefault(vt => vt.Id == vehicle.VehicleTypeId) : null;

            var seats = store.Db.Seats.Where(s => s.TripId == tripId).Select(s => new SeatDto
            {
                SeatId = s.SeatId,
                SeatCode = s.SeatCode,
                Status = s.Status,
                Price = s.Price > 0 ? s.Price : trip.Price
            }).ToList();

            var pickups = store.Db.StopPoints.Where(p => p.RouteId == trip.RouteId && p.IsPickup).Select(p => new StopPointDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address
            }).ToList();

            var dropoffs = store.Db.StopPoints.Where(p => p.RouteId == trip.RouteId && !p.IsPickup).Select(p => new StopPointDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address
            }).ToList();

            var res = new TripSeatsResponseData
            {
                TripId = tripId,
                VehicleType = vType?.Name ?? "Tiêu chuẩn",
                Seats = seats,
                PickupPoints = pickups,
                DropoffPoints = dropoffs
            };

            return Results.Ok(ApiResponse<TripSeatsResponseData>.Ok(res, "Thành công"));
        });
    }
}
