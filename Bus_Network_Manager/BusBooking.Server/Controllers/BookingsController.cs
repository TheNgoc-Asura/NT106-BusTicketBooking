using System.ComponentModel.DataAnnotations;
using System.Data;
using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Server.Extensions;
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

namespace BusBooking.Server.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize(Roles = UserRoles.Customer)]
public class BookingsController : ControllerBase
{
    private const int SeatLockMinutes = 10;
    private static readonly EmailAddressAttribute EmailValidator = new();

    private readonly BusBookingDbContext _db;
    private readonly IMomoService _momoService;
    private readonly ISeatLockService _seatLockService;
    private readonly IHubContext<SupportHub> _hubContext;

    public BookingsController(
        BusBookingDbContext db,
        IMomoService momoService,
        ISeatLockService seatLockService,
        IHubContext<SupportHub> hubContext)
    {
        _db = db;
        _momoService = momoService;
        _seatLockService = seatLockService;
        _hubContext = hubContext;
    }

    // 14. POST /api/bookings
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        if (request.TripId <= 0 ||
            request.SeatId <= 0 ||
            request.PickupPointId <= 0 ||
            request.DropoffPointId <= 0 ||
            string.IsNullOrWhiteSpace(request.PassengerName) ||
            string.IsNullOrWhiteSpace(request.PassengerPhone))
        {
            return BadRequest(ApiResponse.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"));
        }

        if (!string.IsNullOrWhiteSpace(request.PassengerEmail) && !EmailValidator.IsValid(request.PassengerEmail.Trim()))
            return BadRequest(ApiResponse.Fail("Định dạng email hành khách không hợp lệ"));

        var customer = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (customer == null)
            return Unauthorized(ApiResponse.Fail("Token không hợp lệ"));
        if (customer.Status == AccountStatuses.Locked)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Tài khoản của bạn đã bị khóa"));

        var trip = await _db.Trips
            .Include(t => t.Route)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy chuyến xe"));

        if (trip.Status != TripStatuses.Upcoming)
            return Conflict(ApiResponse.Fail("Chuyến xe hiện không còn mở đặt vé"));

        bool seatExists = await _db.TripSeats.AnyAsync(s => s.Id == request.SeatId && s.TripId == request.TripId, ct);
        if (!seatExists)
            return NotFound(ApiResponse.Fail("Không tìm thấy ghế trên chuyến xe này"));

        bool validPickup = await _db.RoutePoints.AnyAsync(
            p => p.Id == request.PickupPointId && p.RouteId == trip.RouteId && p.Type == RoutePointTypes.Pickup, ct);
        bool validDropoff = await _db.RoutePoints.AnyAsync(
            p => p.Id == request.DropoffPointId && p.RouteId == trip.RouteId && p.Type == RoutePointTypes.Dropoff, ct);

        if (!validPickup || !validDropoff)
            return NotFound(ApiResponse.Fail("Không tìm thấy điểm đón hoặc điểm trả hợp lệ cho tuyến xe"));

        await _seatLockService.ReleaseExpiredLocksAsync(request.TripId, ct);

        var now = DateTime.Now;
        var lockUntil = now.AddMinutes(SeatLockMinutes);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        // Khóa ghế nguyên tử ngay trong transaction để chống 2 khách đặt trùng cùng 1 ghế
        bool acquired = await _seatLockService.TryAcquireSeatAsync(
            request.TripId, request.SeatId, SeatStatuses.Pending, lockUntil, now, ct);

        if (!acquired)
        {
            await transaction.RollbackAsync(ct);
            return Conflict(ApiResponse.Fail("Ghế đã có người đặt hoặc đang được giữ"));
        }

        string ticketCode = $"VE{now:yyyyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        decimal totalAmount = trip.Price + trip.Surcharge;

        string ipnUrl = $"{Request.Scheme}://{Request.Host}/api/payments/momo/ipn";
        string redirectUrl = $"{Request.Scheme}://{Request.Host}/api/bookings/0/payment-status";

        var (momoSuccess, paymentInfo, momoError) = await _momoService.CreatePaymentAsync(
            ticketCode,
            (long)totalAmount,
            $"Thanh toan ve {ticketCode}",
            redirectUrl,
            ipnUrl,
            ct);

        if (!momoSuccess || paymentInfo == null)
        {
            await transaction.RollbackAsync(ct);
            return StatusCode(StatusCodes.Status502BadGateway, ApiResponse.Fail(momoError));
        }

        var ticket = new Ticket
        {
            TicketCode = ticketCode,
            TripId = trip.Id,
            SeatId = request.SeatId,
            CustomerId = userId,
            PassengerName = request.PassengerName.Trim(),
            PassengerPhone = request.PassengerPhone.Trim(),
            PassengerEmail = string.IsNullOrWhiteSpace(request.PassengerEmail)
                ? customer.Email
                : request.PassengerEmail.Trim(),
            PickupPointId = request.PickupPointId,
            DropoffPointId = request.DropoffPointId,
            TotalAmount = totalAmount,
            Channel = BookingChannels.Online,
            Status = TicketStatuses.PendingPayment,
            PaymentMethod = PaymentMethods.MoMo,
            CreatedAt = now
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await NotificationHelper.SaveAndPushAsync(_db, _hubContext, new[]
        {
            new Notification
            {
                UserId = userId,
                Type = NotificationTypes.BookingCreated,
                Title = "Đặt vé thành công",
                Content = $"Vé {ticket.TicketCode} ({trip.Route.Origin} - {trip.Route.Destination}) đã được tạo, vui lòng hoàn tất thanh toán MoMo trong {SeatLockMinutes} phút.",
                IsRead = false,
                CreatedAt = DateTime.Now
            }
        }, ct);

        var data = new BookingResponseData
        {
            TicketId = ticket.Id,
            TicketCode = ticket.TicketCode,
            Status = ticket.Status,
            TotalAmount = ticket.TotalAmount,
            Payment = paymentInfo
        };

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<BookingResponseData>.Ok(data, "Tạo vé thành công, vui lòng thanh toán"));
    }

    // 16. GET /api/bookings/{ticketId}/payment-status
    [HttpGet("{ticketId:int}/payment-status")]
    public async Task<IActionResult> GetPaymentStatus(int ticketId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        var ticket = await _db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy vé"));

        if (ticket.CustomerId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Vé không thuộc về người dùng hiện tại"));

        var data = new PaymentStatusResponseData
        {
            TicketId = ticket.Id,
            TicketCode = ticket.TicketCode,
            Status = ticket.Status,
            PaidAt = ticket.PaidAt
        };

        return Ok(ApiResponse<PaymentStatusResponseData>.Ok(data, "Thành công"));
    }
}
