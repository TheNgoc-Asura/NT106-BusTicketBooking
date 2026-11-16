using BusBooking.Server.Data;
using BusBooking.Server.Services;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Staff},{UserRoles.Support}")]
public class AdminDashboardController : ControllerBase
{
    private readonly BusBookingDbContext _db;
    private readonly ISeatLockService _seatLockService;

    public AdminDashboardController(BusBookingDbContext db, ISeatLockService seatLockService)
    {
        _db = db;
        _seatLockService = seatLockService;
    }

    // 23. GET /api/admin/dashboard/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        await _seatLockService.ReleaseExpiredLocksAsync(ct: ct);

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var now = DateTime.Now;

        var soldTodayQuery = _db.Tickets
            .AsNoTracking()
            .Where(t => (t.Status == TicketStatuses.Paid || t.Status == TicketStatuses.Confirmed)
                        && (t.PaidAt ?? t.CreatedAt) >= today
                        && (t.PaidAt ?? t.CreatedAt) < tomorrow);

        int ticketsToday = await soldTodayQuery.CountAsync(ct);
        decimal revenueToday = await soldTodayQuery.SumAsync(t => (decimal?)t.TotalAmount, ct) ?? 0m;

        int tripsToday = await _db.Trips
            .AsNoTracking()
            .CountAsync(tr => tr.Status != TripStatuses.Cancelled
                              && tr.DepartureTime >= today
                              && tr.DepartureTime < tomorrow, ct);

        var upcomingTrips = await _db.Trips
            .AsNoTracking()
            .Where(tr => tr.Status == TripStatuses.Upcoming && tr.DepartureTime >= now)
            .OrderBy(tr => tr.DepartureTime)
            .Take(10)
            .Select(tr => new UpcomingTripDto
            {
                TripId = tr.Id,
                TripCode = tr.TripCode,
                RouteName = tr.Route.Origin + " - " + tr.Route.Destination,
                DepartureTime = tr.DepartureTime,
                TotalSeats = tr.TripSeats.Count,
                BookedSeats = tr.TripSeats.Count(s => s.Status != SeatStatuses.Available)
            })
            .ToListAsync(ct);

        var data = new DashboardStatsDto
        {
            TicketsToday = ticketsToday,
            TripsToday = tripsToday,
            RevenueToday = revenueToday,
            UpcomingTrips = upcomingTrips
        };

        return Ok(ApiResponse<DashboardStatsDto>.Ok(data, "Thành công"));
    }

    // 24. GET /api/admin/dashboard/upcoming-trips
    [HttpGet("upcoming-trips")]
    public async Task<IActionResult> GetUpcomingTrips(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        await _seatLockService.ReleaseExpiredLocksAsync(ct: ct);

        var now = DateTime.Now;
        var query = _db.Trips
            .AsNoTracking()
            .Where(tr => tr.Status == TripStatuses.Upcoming && tr.DepartureTime >= now);

        int totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderBy(tr => tr.DepartureTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(tr => new UpcomingTripDto
            {
                TripId = tr.Id,
                TripCode = tr.TripCode,
                RouteName = tr.Route.Origin + " - " + tr.Route.Destination,
                DepartureTime = tr.DepartureTime,
                TotalSeats = tr.TripSeats.Count,
                BookedSeats = tr.TripSeats.Count(s => s.Status != SeatStatuses.Available)
            })
            .ToListAsync(ct);

        var paged = new PagedResult<UpcomingTripDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return Ok(ApiResponse<PagedResult<UpcomingTripDto>>.Ok(paged, "Thành công"));
    }
}
