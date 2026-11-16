using System.Data;
using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Server.Helpers;
using BusBooking.Server.Hubs;
using BusBooking.Server.Services;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers.Admin;

[ApiController]
[Route("api/admin/trips")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Staff}")]
public class AdminTripsController : ControllerBase
{
    private static readonly HashSet<string> ValidTripStatuses = new(StringComparer.Ordinal)
    {
        TripStatuses.Upcoming,
        TripStatuses.Running,
        TripStatuses.Completed,
        TripStatuses.Cancelled
    };

    private readonly BusBookingDbContext _db;
    private readonly ISeatLockService _seatLockService;
    private readonly IHubContext<SupportHub> _hubContext;

    public AdminTripsController(
        BusBookingDbContext db,
        ISeatLockService seatLockService,
        IHubContext<SupportHub> hubContext)
    {
        _db = db;
        _seatLockService = seatLockService;
        _hubContext = hubContext;
    }

    // 25. GET /api/admin/trips
    [HttpGet]
    public async Task<IActionResult> GetTrips(
        [FromQuery] string? tripCode,
        [FromQuery] int? routeId,
        [FromQuery] string? status,
        [FromQuery] DateTime? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        await _seatLockService.ReleaseExpiredLocksAsync(ct: ct);

        var query = _db.Trips.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(tripCode))
        {
            string codeFilter = tripCode.Trim();
            query = query.Where(tr => tr.TripCode.Contains(codeFilter));
        }

        if (routeId.HasValue && routeId.Value > 0)
        {
            query = query.Where(tr => tr.RouteId == routeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            string statusFilter = status.Trim();
            query = query.Where(tr => tr.Status == statusFilter);
        }

        if (date.HasValue)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(tr => tr.DepartureTime >= start && tr.DepartureTime < end);
        }

        int totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(tr => tr.DepartureTime)
            .ThenByDescending(tr => tr.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(tr => new AdminTripDto
            {
                TripId = tr.Id,
                TripCode = tr.TripCode,
                RouteName = tr.Route.Origin + " - " + tr.Route.Destination,
                DepartureTime = tr.DepartureTime,
                VehiclePlate = tr.Vehicle.LicensePlate,
                DriverName = tr.Driver.FullName,
                AssistantName = tr.Assistant != null ? tr.Assistant.FullName : null,
                Price = tr.Price,
                BookedSeats = tr.TripSeats.Count(s => s.Status != SeatStatuses.Available),
                TotalSeats = tr.TripSeats.Count,
                Status = tr.Status
            })
            .ToListAsync(ct);

        var paged = new PagedResult<AdminTripDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return Ok(ApiResponse<PagedResult<AdminTripDto>>.Ok(paged, "Thành công"));
    }

    // 26. GET /api/admin/trips/form-options
    [HttpGet("form-options")]
    public async Task<IActionResult> GetFormOptions(CancellationToken ct)
    {
        var routes = await _db.Routes
            .AsNoTracking()
            .Where(r => r.Status == RouteStatuses.Active)
            .OrderBy(r => r.Origin)
            .ThenBy(r => r.Destination)
            .Select(r => new OptionItemDto
            {
                Id = r.Id,
                Name = r.Origin + " - " + r.Destination
            })
            .ToListAsync(ct);

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.Status == VehicleStatuses.Active)
            .OrderBy(v => v.LicensePlate)
            .Select(v => new VehicleOptionDto
            {
                Id = v.Id,
                LicensePlate = v.LicensePlate
            })
            .ToListAsync(ct);

        var staffUsers = await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRoles.Staff && u.Status == AccountStatuses.Active)
            .OrderBy(u => u.FullName)
            .Select(u => new StaffOptionDto
            {
                Id = u.Id,
                FullName = u.FullName
            })
            .ToListAsync(ct);

        var data = new TripFormOptionsDto
        {
            Routes = routes,
            Vehicles = vehicles,
            Drivers = staffUsers,
            Assistants = staffUsers
        };

        return Ok(ApiResponse<TripFormOptionsDto>.Ok(data, "Thành công"));
    }

    // 27. POST /api/admin/trips
    [HttpPost]
    public async Task<IActionResult> CreateTrip([FromBody] CreateTripRequest request, CancellationToken ct)
    {
        if (request.RouteId <= 0 ||
            request.VehicleId <= 0 ||
            request.DriverId <= 0 ||
            request.DepartureTime == default ||
            request.Price <= 0 ||
            request.Surcharge < 0 ||
            (request.AssistantId.HasValue && request.AssistantId.Value <= 0))
        {
            return BadRequest(ApiResponse.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"));
        }

        if (request.AssistantId.HasValue && request.AssistantId.Value == request.DriverId)
            return BadRequest(ApiResponse.Fail("Tài xế và phụ xe không thể là cùng một người"));

        var (errorResult, route, vehicle) = await ValidateTripResourcesAsync(
            null, request, isCancelledTarget: false, ct);
        if (errorResult != null || route == null || vehicle == null)
            return errorResult!;

        int durationMinutes = route.DurationMinutes is > 0 ? route.DurationMinutes.Value : 180;
        var estimatedArrival = request.DepartureTime.AddMinutes(durationMinutes);

        var conflictError = await CheckScheduleConflictAsync(
            null, request.VehicleId, request.DriverId, request.AssistantId,
            request.DepartureTime, estimatedArrival, ct);
        if (conflictError != null)
            return conflictError;

        string tripCode = await GenerateNextTripCodeAsync(ct);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        var trip = new Trip
        {
            TripCode = tripCode,
            RouteId = route.Id,
            VehicleId = vehicle.Id,
            DriverId = request.DriverId,
            AssistantId = request.AssistantId,
            DepartureTime = request.DepartureTime,
            EstimatedArrival = estimatedArrival,
            Price = request.Price,
            Surcharge = request.Surcharge,
            Status = TripStatuses.Upcoming,
            CreatedAt = DateTime.Now
        };

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync(ct);

        var seats = DbSeeder.GenerateSeatsForTrip(trip.Id, vehicle.VehicleType.SeatCount);
        _db.TripSeats.AddRange(seats);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var data = new CreateTripResponseData
        {
            TripId = trip.Id,
            TripCode = trip.TripCode
        };

        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreateTripResponseData>.Ok(data, "Thêm chuyến thành công"));
    }

    // 28. GET /api/admin/trips/{tripId}
    [HttpGet("{tripId:int}")]
    public async Task<IActionResult> GetTripDetail(int tripId, CancellationToken ct)
    {
        var trip = await _db.Trips
            .AsNoTracking()
            .Where(tr => tr.Id == tripId)
            .Select(tr => new AdminTripDetailDto
            {
                TripId = tr.Id,
                TripCode = tr.TripCode,
                RouteId = tr.RouteId,
                DepartureTime = tr.DepartureTime,
                VehicleId = tr.VehicleId,
                DriverId = tr.DriverId,
                AssistantId = tr.AssistantId,
                Price = tr.Price,
                Surcharge = tr.Surcharge,
                Status = tr.Status
            })
            .FirstOrDefaultAsync(ct);

        if (trip == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy chuyến xe"));

        return Ok(ApiResponse<AdminTripDetailDto>.Ok(trip, "Thành công"));
    }

    // 29. PUT /api/admin/trips/{tripId}
    [HttpPut("{tripId:int}")]
    public async Task<IActionResult> UpdateTrip(int tripId, [FromBody] UpdateTripRequest request, CancellationToken ct)
    {
        if (request.RouteId <= 0 ||
            request.VehicleId <= 0 ||
            request.DriverId <= 0 ||
            request.DepartureTime == default ||
            request.Price <= 0 ||
            request.Surcharge < 0 ||
            string.IsNullOrWhiteSpace(request.Status) ||
            (request.AssistantId.HasValue && request.AssistantId.Value <= 0))
        {
            return BadRequest(ApiResponse.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"));
        }

        string targetStatus = request.Status.Trim();
        if (!ValidTripStatuses.Contains(targetStatus))
            return BadRequest(ApiResponse.Fail("Trạng thái chuyến xe không hợp lệ"));

        if (request.AssistantId.HasValue && request.AssistantId.Value == request.DriverId)
            return BadRequest(ApiResponse.Fail("Tài xế và phụ xe không thể là cùng một người"));

        var trip = await _db.Trips.FirstOrDefaultAsync(tr => tr.Id == tripId, ct);
        if (trip == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy chuyến xe"));

        bool isCancelledTarget = targetStatus == TripStatuses.Cancelled;

        var (errorResult, route, vehicle) = await ValidateTripResourcesAsync(
            trip, request, isCancelledTarget, ct);
        if (errorResult != null || route == null || vehicle == null)
            return errorResult!;

        bool vehicleChanged = trip.VehicleId != vehicle.Id;
        if (vehicleChanged)
        {
            bool hasAnyTickets = await _db.Tickets.AnyAsync(tk => tk.TripId == trip.Id, ct);
            if (hasAnyTickets)
                return Conflict(ApiResponse.Fail("Không thể thay đổi xe khi chuyến đã phát sinh vé gắn với sơ đồ ghế"));
        }

        int durationMinutes = route.DurationMinutes is > 0 ? route.DurationMinutes.Value : 180;
        var estimatedArrival = request.DepartureTime.AddMinutes(durationMinutes);

        if (!isCancelledTarget)
        {
            var conflictError = await CheckScheduleConflictAsync(
                trip.Id, request.VehicleId, request.DriverId, request.AssistantId,
                request.DepartureTime, estimatedArrival, ct);
            if (conflictError != null)
                return conflictError;
        }

        DateTime oldDeparture = trip.DepartureTime;
        string oldStatus = trip.Status;

        trip.RouteId = route.Id;
        trip.VehicleId = vehicle.Id;
        trip.DriverId = request.DriverId;
        trip.AssistantId = request.AssistantId;
        trip.DepartureTime = request.DepartureTime;
        trip.EstimatedArrival = estimatedArrival;
        trip.Price = request.Price;
        trip.Surcharge = request.Surcharge;
        trip.Status = targetStatus;

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        if (vehicleChanged)
        {
            var oldSeats = await _db.TripSeats.Where(s => s.TripId == trip.Id).ToListAsync(ct);
            _db.TripSeats.RemoveRange(oldSeats);
            _db.TripSeats.AddRange(DbSeeder.GenerateSeatsForTrip(trip.Id, vehicle.VehicleType.SeatCount));
        }

        List<Notification> pendingNotifications = new();

        if (oldStatus != TripStatuses.Cancelled && targetStatus == TripStatuses.Cancelled)
        {
            var activeTickets = await _db.Tickets
                .Where(tk => tk.TripId == trip.Id && tk.Status != TicketStatuses.Cancelled)
                .ToListAsync(ct);

            foreach (var tk in activeTickets)
            {
                tk.Status = TicketStatuses.Cancelled;
            }

            var seats = await _db.TripSeats.Where(s => s.TripId == trip.Id).ToListAsync(ct);
            foreach (var s in seats)
            {
                s.Status = SeatStatuses.Available;
                s.LockedUntil = null;
            }

            var customerIds = activeTickets
                .Where(tk => tk.CustomerId.HasValue)
                .Select(tk => tk.CustomerId!.Value)
                .Distinct();

            var now = DateTime.Now;
            foreach (int customerId in customerIds)
            {
                pendingNotifications.Add(new Notification
                {
                    UserId = customerId,
                    Type = NotificationTypes.TripCancelled,
                    Title = "Chuyến xe đã bị hủy",
                    Content = $"Chuyến xe {trip.TripCode} ({route.Origin} - {route.Destination}) khởi hành lúc {trip.DepartureTime:dd/MM/yyyy HH:mm} đã bị hủy.",
                    IsRead = false,
                    CreatedAt = now
                });
            }
        }
        else if (!isCancelledTarget && oldDeparture != trip.DepartureTime)
        {
            var customerIds = await _db.Tickets
                .AsNoTracking()
                .Where(tk => tk.TripId == trip.Id && tk.Status != TicketStatuses.Cancelled && tk.CustomerId.HasValue)
                .Select(tk => tk.CustomerId!.Value)
                .Distinct()
                .ToListAsync(ct);

            var now = DateTime.Now;
            foreach (int customerId in customerIds)
            {
                pendingNotifications.Add(new Notification
                {
                    UserId = customerId,
                    Type = NotificationTypes.TripUpdated,
                    Title = "Thay đổi giờ khởi hành",
                    Content = $"Chuyến xe {trip.TripCode} ({route.Origin} - {route.Destination}) đã cập nhật giờ khởi hành sang {trip.DepartureTime:dd/MM/yyyy HH:mm}.",
                    IsRead = false,
                    CreatedAt = now
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        if (pendingNotifications.Count > 0)
        {
            await NotificationHelper.SaveAndPushAsync(_db, _hubContext, pendingNotifications, ct);
        }

        return Ok(ApiResponse<object>.Ok(new { tripId = trip.Id }, "Cập nhật chuyến thành công"));
    }

    // 41. GET /api/admin/trips/{tripId}/seats
    [HttpGet("{tripId:int}/seats")]
    public async Task<IActionResult> GetTripSeats(int tripId, CancellationToken ct)
    {
        var trip = await _db.Trips
            .AsNoTracking()
            .Include(tr => tr.Route)
            .FirstOrDefaultAsync(tr => tr.Id == tripId, ct);

        if (trip == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy chuyến xe"));

        await _seatLockService.ReleaseExpiredLocksAsync(tripId, ct);

        decimal seatPrice = trip.Price + trip.Surcharge;

        var seats = await _db.TripSeats
            .AsNoTracking()
            .Where(s => s.TripId == tripId)
            .OrderBy(s => s.SeatCode)
            .Select(s => new TripSeatDto
            {
                SeatId = s.Id,
                SeatCode = s.SeatCode,
                Status = s.Status,
                Price = seatPrice
            })
            .ToListAsync(ct);

        var points = await _db.RoutePoints
            .AsNoTracking()
            .Where(p => p.RouteId == trip.RouteId)
            .OrderBy(p => p.OrderIndex)
            .ThenBy(p => p.Id)
            .Select(p => new RoutePointDto
            {
                Id = p.Id,
                Type = p.Type,
                Name = p.Name,
                Address = p.Address
            })
            .ToListAsync(ct);

        var data = new AdminTripSeatsResponseData
        {
            Trip = new AdminTripHeaderDto
            {
                TripId = trip.Id,
                TripCode = trip.TripCode,
                RouteName = trip.Route.Origin + " - " + trip.Route.Destination,
                DepartureTime = trip.DepartureTime
            },
            Seats = seats,
            PickupPoints = points.Where(p => p.Type == RoutePointTypes.Pickup).ToList(),
            DropoffPoints = points.Where(p => p.Type == RoutePointTypes.Dropoff).ToList()
        };

        return Ok(ApiResponse<AdminTripSeatsResponseData>.Ok(data, "Thành công"));
    }

    private async Task<(IActionResult? Error, Entities.Route? Route, Vehicle? Vehicle)> ValidateTripResourcesAsync(
        Trip? existingTrip,
        CreateTripRequest request,
        bool isCancelledTarget,
        CancellationToken ct)
    {
        var route = await _db.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RouteId, ct);
        if (route == null)
            return (BadRequest(ApiResponse.Fail("Không tìm thấy tuyến xe")), null, null);

        if (!isCancelledTarget && route.Status != RouteStatuses.Active && (existingTrip == null || existingTrip.RouteId != route.Id))
            return (Conflict(ApiResponse.Fail("Tuyến xe hiện đã ngừng hoạt động")), null, null);

        var vehicle = await _db.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleType)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);
        if (vehicle == null)
            return (BadRequest(ApiResponse.Fail("Không tìm thấy xe")), null, null);

        if (!isCancelledTarget && vehicle.Status != VehicleStatuses.Active)
            return (Conflict(ApiResponse.Fail("Xe hiện không ở trạng thái hoạt động")), null, null);

        var driver = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.DriverId, ct);
        if (driver == null || driver.Role != UserRoles.Staff)
            return (BadRequest(ApiResponse.Fail("Tài xế không tồn tại hoặc không thuộc nhóm nhân viên")), null, null);

        if (!isCancelledTarget && driver.Status != AccountStatuses.Active)
            return (Conflict(ApiResponse.Fail("Tài khoản tài xế hiện đang bị khóa")), null, null);

        if (request.AssistantId.HasValue)
        {
            var assistant = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.AssistantId.Value, ct);
            if (assistant == null || assistant.Role != UserRoles.Staff)
                return (BadRequest(ApiResponse.Fail("Phụ xe không tồn tại hoặc không thuộc nhóm nhân viên")), null, null);

            if (!isCancelledTarget && assistant.Status != AccountStatuses.Active)
                return (Conflict(ApiResponse.Fail("Tài khoản phụ xe hiện đang bị khóa")), null, null);
        }

        return (null, route, vehicle);
    }

    private async Task<IActionResult?> CheckScheduleConflictAsync(
        int? excludeTripId,
        int vehicleId,
        int driverId,
        int? assistantId,
        DateTime departureTime,
        DateTime estimatedArrival,
        CancellationToken ct)
    {
        var departureDate = departureTime.Date;
        bool hasMaintenanceConflict = await _db.MaintenanceSchedules
            .AsNoTracking()
            .AnyAsync(m => m.VehicleId == vehicleId
                           && m.StartDate < estimatedArrival
                           && m.EndDate >= departureDate, ct);

        if (hasMaintenanceConflict)
            return Conflict(ApiResponse.Fail("Xe đang có lịch bảo trì trùng với thời gian chạy"));

        var overlappingTrips = await _db.Trips
            .AsNoTracking()
            .Where(tr => tr.Status != TripStatuses.Cancelled
                         && (!excludeTripId.HasValue || tr.Id != excludeTripId.Value)
                         && tr.DepartureTime < estimatedArrival
                         && tr.EstimatedArrival > departureTime)
            .Select(tr => new { tr.TripCode, tr.VehicleId, tr.DriverId, tr.AssistantId })
            .ToListAsync(ct);

        var vehicleConflict = overlappingTrips.FirstOrDefault(t => t.VehicleId == vehicleId);
        if (vehicleConflict != null)
            return Conflict(ApiResponse.Fail($"Xe bị trùng lịch với chuyến {vehicleConflict.TripCode}"));

        var driverConflict = overlappingTrips.FirstOrDefault(t => t.DriverId == driverId || t.AssistantId == driverId);
        if (driverConflict != null)
            return Conflict(ApiResponse.Fail($"Tài xế bị trùng lịch với chuyến {driverConflict.TripCode}"));

        if (assistantId.HasValue)
        {
            var assistantConflict = overlappingTrips.FirstOrDefault(
                t => t.DriverId == assistantId.Value || t.AssistantId == assistantId.Value);
            if (assistantConflict != null)
                return Conflict(ApiResponse.Fail($"Phụ xe bị trùng lịch với chuyến {assistantConflict.TripCode}"));
        }

        return null;
    }

    private async Task<string> GenerateNextTripCodeAsync(CancellationToken ct)
    {
        int nextSeq = (await _db.Trips.MaxAsync(tr => (int?)tr.Id, ct) ?? 0) + 1;
        string code = $"CH{nextSeq:D4}";
        while (await _db.Trips.AnyAsync(tr => tr.TripCode == code, ct))
        {
            nextSeq++;
            code = $"CH{nextSeq:D4}";
        }
        return code;
    }
}
