using System.Globalization;
using BusBooking.Server.Data;
using BusBooking.Server.Services;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize(Roles = UserRoles.Customer)]
public class TripsController : ControllerBase
{
    private readonly BusBookingDbContext _db;
    private readonly ISeatLockService _seatLockService;

    public TripsController(BusBookingDbContext db, ISeatLockService seatLockService)
    {
        _db = db;
        _seatLockService = seatLockService;
    }

    // 12. GET /api/trips?from=...&to=...&date=yyyy-MM-dd
    [HttpGet]
    public async Task<IActionResult> SearchTrips(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? date,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(date))
            return BadRequest(ApiResponse.Fail("Vui lòng nhập đầy đủ điểm đi (from), điểm đến (to) và ngày khởi hành (date)"));

        if (!DateTime.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return BadRequest(ApiResponse.Fail("Ngày khởi hành không đúng định dạng yyyy-MM-dd"));

        await _seatLockService.ReleaseExpiredLocksAsync(null, ct);

        string fromKeyword = from.Trim();
        string toKeyword = to.Trim();
        var dayStart = parsedDate.Date;
        var dayEnd = dayStart.AddDays(1);

        var trips = await _db.Trips
            .AsNoTracking()
            .Where(t => t.Status == TripStatuses.Upcoming &&
                        t.Route.Status == RouteStatuses.Active &&
                        t.DepartureTime >= dayStart &&
                        t.DepartureTime < dayEnd &&
                        t.Route.Origin.Contains(fromKeyword) &&
                        t.Route.Destination.Contains(toKeyword))
            .OrderBy(t => t.DepartureTime)
            .Select(t => new TripSearchItemDto
            {
                TripId = t.Id,
                TripCode = t.TripCode,
                RouteName = t.Route.Origin + " - " + t.Route.Destination,
                Origin = t.Route.Origin,
                Destination = t.Route.Destination,
                DepartureTime = t.DepartureTime,
                EstimatedArrival = t.EstimatedArrival,
                VehicleType = t.Vehicle.VehicleType.Name,
                Price = t.Price + t.Surcharge,
                TotalSeats = t.TripSeats.Count,
                AvailableSeats = t.TripSeats.Count(s => s.Status == SeatStatuses.Available)
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<List<TripSearchItemDto>>.Ok(trips, "Thành công"));
    }

    // 13. GET /api/trips/{tripId}/seats
    [HttpGet("{tripId:int}/seats")]
    public async Task<IActionResult> GetTripSeats(int tripId, CancellationToken ct)
    {
        var trip = await _db.Trips
            .AsNoTracking()
            .Include(t => t.Vehicle)
                .ThenInclude(v => v.VehicleType)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct);

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

        var data = new TripSeatsResponseData
        {
            TripId = trip.Id,
            VehicleType = trip.Vehicle.VehicleType.Name,
            Seats = seats,
            PickupPoints = points.Where(p => p.Type == RoutePointTypes.Pickup).ToList(),
            DropoffPoints = points.Where(p => p.Type == RoutePointTypes.Dropoff).ToList()
        };

        return Ok(ApiResponse<TripSeatsResponseData>.Ok(data, "Thành công"));
    }
}
