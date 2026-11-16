using System.ComponentModel.DataAnnotations;
using System.Data;
using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Server.Services;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers.Admin;

[ApiController]
[Route("api/admin/tickets")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Staff}")]
public class AdminTicketsController : ControllerBase
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    private readonly BusBookingDbContext _db;
    private readonly ISeatLockService _seatLockService;

    public AdminTicketsController(BusBookingDbContext db, ISeatLockService seatLockService)
    {
        _db = db;
        _seatLockService = seatLockService;
    }

    // 42. GET /api/admin/tickets
    [HttpGet]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string? ticketCode,
        [FromQuery] int? tripId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        await _seatLockService.ReleaseExpiredLocksAsync(tripId, ct);

        var query = _db.Tickets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ticketCode))
        {
            string codeFilter = ticketCode.Trim();
            query = query.Where(tk => tk.TicketCode.Contains(codeFilter));
        }

        if (tripId.HasValue && tripId.Value > 0)
        {
            query = query.Where(tk => tk.TripId == tripId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            string statusFilter = status.Trim();
            query = query.Where(tk => tk.Status == statusFilter);
        }

        int totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(tk => tk.CreatedAt)
            .ThenByDescending(tk => tk.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(tk => new AdminTicketDto
            {
                TicketId = tk.Id,
                TicketCode = tk.TicketCode,
                TripCode = tk.Trip.TripCode,
                PassengerName = tk.PassengerName,
                SeatCode = tk.Seat.SeatCode,
                TotalAmount = tk.TotalAmount,
                Channel = tk.Channel,
                Status = tk.Status
            })
            .ToListAsync(ct);

        var paged = new PagedResult<AdminTicketDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return Ok(ApiResponse<PagedResult<AdminTicketDto>>.Ok(paged, "Thành công"));
    }

    // 43. GET /api/admin/tickets/{ticketId}
    [HttpGet("{ticketId:int}")]
    public async Task<IActionResult> GetTicketDetail(int ticketId, CancellationToken ct)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Where(tk => tk.Id == ticketId)
            .Select(tk => new AdminTicketDetailDto
            {
                TicketId = tk.Id,
                TicketCode = tk.TicketCode,
                Status = tk.Status,
                Channel = tk.Channel,
                Trip = new TicketTripSummaryDto
                {
                    TripId = tk.Trip.Id,
                    TripCode = tk.Trip.TripCode,
                    RouteName = tk.Trip.Route.Origin + " - " + tk.Trip.Route.Destination,
                    DepartureTime = tk.Trip.DepartureTime,
                    VehiclePlate = tk.Trip.Vehicle.LicensePlate
                },
                SeatCode = tk.Seat.SeatCode,
                Passenger = new PassengerInfoDto
                {
                    Name = tk.PassengerName,
                    Phone = tk.PassengerPhone
                },
                PickupPoint = tk.PickupPoint.Name,
                DropoffPoint = tk.DropoffPoint.Name,
                TotalAmount = tk.TotalAmount
            })
            .FirstOrDefaultAsync(ct);

        if (ticket == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy vé"));

        return Ok(ApiResponse<AdminTicketDetailDto>.Ok(ticket, "Thành công"));
    }

    // 44. PUT /api/admin/tickets/{ticketId}/confirm
    [HttpPut("{ticketId:int}/confirm")]
    public async Task<IActionResult> ConfirmTicket(int ticketId, CancellationToken ct)
    {
        var ticket = await _db.Tickets
            .Include(tk => tk.Seat)
            .FirstOrDefaultAsync(tk => tk.Id == ticketId, ct);

        if (ticket == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy vé"));

        if (ticket.Status != TicketStatuses.Paid)
            return Conflict(ApiResponse.Fail("Trạng thái hiện tại của vé không cho phép xác nhận"));

        ticket.Status = TicketStatuses.Confirmed;
        if (ticket.Seat != null)
        {
            ticket.Seat.Status = SeatStatuses.Booked;
            ticket.Seat.LockedUntil = null;
        }

        await _db.SaveChangesAsync(ct);

        var data = new ConfirmTicketResponseData
        {
            TicketId = ticket.Id,
            Status = ticket.Status
        };

        return Ok(ApiResponse<ConfirmTicketResponseData>.Ok(data, "Xác nhận vé thành công"));
    }

    // 45. POST /api/admin/tickets (Đặt vé trực tiếp tại quầy)
    [HttpPost]
    public async Task<IActionResult> CreateCounterTicket([FromBody] CounterBookingRequest request, CancellationToken ct)
    {
        if (request.TripId <= 0 ||
            request.SeatId <= 0 ||
            request.PickupPointId <= 0 ||
            request.DropoffPointId <= 0 ||
            string.IsNullOrWhiteSpace(request.PassengerName) ||
            string.IsNullOrWhiteSpace(request.PassengerPhone))
        {
            return BadRequest(ApiResponse.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"));
        }

        string? normalizedEmail = null;
        if (!string.IsNullOrWhiteSpace(request.PassengerEmail))
        {
            normalizedEmail = request.PassengerEmail.Trim();
            if (!EmailValidator.IsValid(normalizedEmail))
                return BadRequest(ApiResponse.Fail("Định dạng email hành khách không hợp lệ"));
        }

        var trip = await _db.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(tr => tr.Id == request.TripId, ct);

        if (trip == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy chuyến xe"));

        if (trip.Status != TripStatuses.Upcoming)
            return Conflict(ApiResponse.Fail("Chuyến xe hiện không còn mở đặt vé"));

        bool seatExists = await _db.TripSeats
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.SeatId && s.TripId == request.TripId, ct);
        if (!seatExists)
            return NotFound(ApiResponse.Fail("Không tìm thấy ghế trên chuyến xe này"));

        bool validPickup = await _db.RoutePoints.AsNoTracking().AnyAsync(
            p => p.Id == request.PickupPointId && p.RouteId == trip.RouteId && p.Type == RoutePointTypes.Pickup, ct);
        bool validDropoff = await _db.RoutePoints.AsNoTracking().AnyAsync(
            p => p.Id == request.DropoffPointId && p.RouteId == trip.RouteId && p.Type == RoutePointTypes.Dropoff, ct);

        if (!validPickup || !validDropoff)
            return BadRequest(ApiResponse.Fail("Điểm đón hoặc điểm trả không hợp lệ cho tuyến xe"));

        int? linkedCustomerId = null;
        if (normalizedEmail != null)
        {
            linkedCustomerId = await _db.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRoles.Customer && u.Email.ToLower() == normalizedEmail.ToLower())
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(ct);
        }

        await _seatLockService.ReleaseExpiredLocksAsync(request.TripId, ct);

        var now = DateTime.Now;
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        bool acquired = await _seatLockService.TryAcquireSeatAsync(
            request.TripId, request.SeatId, SeatStatuses.Booked, lockedUntil: null, now, ct);

        if (!acquired)
        {
            await transaction.RollbackAsync(ct);
            return Conflict(ApiResponse.Fail("Ghế không còn trống"));
        }

        string ticketCode = $"VE{now:yyyyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var ticket = new Ticket
        {
            TicketCode = ticketCode,
            TripId = trip.Id,
            SeatId = request.SeatId,
            CustomerId = linkedCustomerId,
            PassengerName = request.PassengerName.Trim(),
            PassengerPhone = request.PassengerPhone.Trim(),
            PassengerEmail = normalizedEmail,
            PickupPointId = request.PickupPointId,
            DropoffPointId = request.DropoffPointId,
            TotalAmount = trip.Price + trip.Surcharge,
            Channel = BookingChannels.Direct,
            Status = TicketStatuses.Confirmed,
            PaymentMethod = PaymentMethods.Cash,
            PaidAt = now,
            CreatedAt = now
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var data = new CreateTicketResponseData
        {
            TicketId = ticket.Id,
            TicketCode = ticket.TicketCode
        };

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<CreateTicketResponseData>.Ok(data, "Tạo vé thành công"));
    }
}
