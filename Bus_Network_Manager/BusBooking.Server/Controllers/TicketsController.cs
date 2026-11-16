using BusBooking.Server.Data;
using BusBooking.Server.Extensions;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize(Roles = UserRoles.Customer)]
public class TicketsController : ControllerBase
{
    private readonly BusBookingDbContext _db;

    public TicketsController(BusBookingDbContext db)
    {
        _db = db;
    }

    // 17. GET /api/tickets/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (!User.TryGetUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Tickets
            .AsNoTracking()
            .Where(t => t.CustomerId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            string statusFilter = status.Trim();
            query = query.Where(t => t.Status == statusFilter);
        }

        int totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new MyTicketDto
            {
                TicketId = t.Id,
                TicketCode = t.TicketCode,
                RouteName = t.Trip.Route.Origin + " - " + t.Trip.Route.Destination,
                DepartureTime = t.Trip.DepartureTime,
                SeatCode = t.Seat.SeatCode,
                TotalAmount = t.TotalAmount,
                Status = t.Status
            })
            .ToListAsync(ct);

        var paged = new PagedResult<MyTicketDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return Ok(ApiResponse<PagedResult<MyTicketDto>>.Ok(paged, "Thành công"));
    }

    // 18. GET /api/tickets/{ticketId}
    [HttpGet("{ticketId:int}")]
    public async Task<IActionResult> GetTicketDetail(int ticketId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Trip)
                .ThenInclude(tr => tr.Route)
            .Include(t => t.Trip)
                .ThenInclude(tr => tr.Vehicle)
            .Include(t => t.Seat)
            .Include(t => t.PickupPoint)
            .Include(t => t.DropoffPoint)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy vé"));

        if (ticket.CustomerId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Vé không thuộc về người dùng hiện tại"));

        var data = new TicketDetailDto
        {
            TicketId = ticket.Id,
            TicketCode = ticket.TicketCode,
            Status = ticket.Status,
            Trip = new TicketTripSummaryDto
            {
                TripId = ticket.Trip.Id,
                TripCode = ticket.Trip.TripCode,
                RouteName = $"{ticket.Trip.Route.Origin} - {ticket.Trip.Route.Destination}",
                DepartureTime = ticket.Trip.DepartureTime,
                VehiclePlate = ticket.Trip.Vehicle.LicensePlate
            },
            SeatCode = ticket.Seat.SeatCode,
            Passenger = new PassengerInfoDto
            {
                Name = ticket.PassengerName,
                Phone = ticket.PassengerPhone
            },
            PickupPoint = ticket.PickupPoint.Name,
            DropoffPoint = ticket.DropoffPoint.Name,
            TotalAmount = ticket.TotalAmount,
            PaymentMethod = ticket.PaymentMethod ?? PaymentMethods.MoMo,
            PaidAt = ticket.PaidAt
        };

        return Ok(ApiResponse<TicketDetailDto>.Ok(data, "Thành công"));
    }
}
