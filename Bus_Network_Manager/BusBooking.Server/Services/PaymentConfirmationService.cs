using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Server.Hubs;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Services;

public enum IpnProcessOutcome
{
    Success,
    InvalidSignatureOrData,
    TicketNotFound
}

public interface IPaymentConfirmationService
{
    Task<(IpnProcessOutcome Outcome, string Message)> ProcessMomoIpnAsync(MomoIpnRequest ipn, CancellationToken ct = default);
}

public class PaymentConfirmationService : IPaymentConfirmationService
{
    private readonly BusBookingDbContext _db;
    private readonly IMomoService _momoService;
    private readonly IEmailService _emailService;
    private readonly IHubContext<SupportHub> _hubContext;
    private readonly ILogger<PaymentConfirmationService> _logger;

    public PaymentConfirmationService(
        BusBookingDbContext db,
        IMomoService momoService,
        IEmailService emailService,
        IHubContext<SupportHub> hubContext,
        ILogger<PaymentConfirmationService> logger)
    {
        _db = db;
        _momoService = momoService;
        _emailService = emailService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<(IpnProcessOutcome Outcome, string Message)> ProcessMomoIpnAsync(MomoIpnRequest ipn, CancellationToken ct = default)
    {
        if (!_momoService.VerifyIpnSignature(ipn))
        {
            return (IpnProcessOutcome.InvalidSignatureOrData, "Chữ ký HMAC SHA256 hoặc partnerCode không hợp lệ");
        }

        var ticket = await _db.Tickets
            .Include(t => t.Seat)
            .Include(t => t.Trip)
                .ThenInclude(tr => tr.Route)
            .FirstOrDefaultAsync(t => t.TicketCode == ipn.OrderId, ct);

        if (ticket == null)
        {
            return (IpnProcessOutcome.TicketNotFound, "Không tìm thấy vé ứng với orderId");
        }

        if ((long)ticket.TotalAmount != ipn.Amount)
        {
            return (IpnProcessOutcome.InvalidSignatureOrData, "Số tiền thanh toán không khớp với giá trị vé");
        }

        // Idempotent: nếu vé đã thanh toán hoặc đã xác nhận rồi thì trả về thành công mà không xử lý lặp
        if (ticket.Status == TicketStatuses.Paid || ticket.Status == TicketStatuses.Confirmed)
        {
            return (IpnProcessOutcome.Success, "Giao dịch đã được ghi nhận trước đó");
        }

        if (ipn.ResultCode == 0)
        {
            if (ticket.Status == TicketStatuses.Cancelled)
            {
                return (IpnProcessOutcome.InvalidSignatureOrData, "Vé đã bị hủy trước khi nhận IPN");
            }

            ticket.Status = TicketStatuses.Paid;
            ticket.PaymentMethod = PaymentMethods.MoMo;
            ticket.PaymentTransId = ipn.TransId;
            ticket.PaidAt = DateTime.Now;

            if (ticket.Seat != null)
            {
                ticket.Seat.Status = SeatStatuses.Booked;
                ticket.Seat.LockedUntil = null;
            }

            Notification? notif = null;
            if (ticket.CustomerId.HasValue)
            {
                notif = new Notification
                {
                    UserId = ticket.CustomerId.Value,
                    Type = NotificationTypes.PaymentConfirmed,
                    Title = "Thanh toán thành công",
                    Content = $"Vé {ticket.TicketCode} ({ticket.Trip.Route.Origin} - {ticket.Trip.Route.Destination}) đã được thanh toán qua MoMo.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                _db.Notifications.Add(notif);
            }

            await _db.SaveChangesAsync(ct);

            if (ticket.CustomerId.HasValue)
            {
                var group = _hubContext.Clients.Group($"user_{ticket.CustomerId.Value}");

                await group.SendAsync("PaymentConfirmed", new PaymentConfirmedEventDto
                {
                    TicketId = ticket.Id,
                    TicketCode = ticket.TicketCode,
                    Status = ticket.Status
                }, ct);

                if (notif != null)
                {
                    await group.SendAsync("ReceiveNotification", new NotificationDto
                    {
                        Id = notif.Id,
                        Type = notif.Type,
                        Title = notif.Title,
                        Content = notif.Content,
                        IsRead = notif.IsRead,
                        CreatedAt = notif.CreatedAt
                    }, ct);
                }
            }

            if (!string.IsNullOrWhiteSpace(ticket.PassengerEmail))
            {
                string routeName = $"{ticket.Trip.Route.Origin} - {ticket.Trip.Route.Destination}";
                string subject = $"[BusBooking] Hóa đơn vé điện tử {ticket.TicketCode}";
                string body = $"""
                    Kính gửi Quý khách {ticket.PassengerName},

                    Vé xe của Quý khách đã được thanh toán thành công qua ví điện tử MoMo:
                    - Mã vé: {ticket.TicketCode}
                    - Tuyến xe: {routeName}
                    - Chuyến: {ticket.Trip.TripCode} | Khởi hành: {ticket.Trip.DepartureTime:dd/MM/yyyy HH:mm}
                    - Vị trí ghế: {ticket.Seat?.SeatCode}
                    - Tổng tiền: {ticket.TotalAmount:N0} VNĐ
                    - Mã giao dịch MoMo: {ipn.TransId}

                    Chúc Quý khách có chuyến đi an toàn và thoải mái!
                    BusBooking System - Nhóm 07 (NT106.R12)
                    """;

                bool emailSent = await _emailService.SendEmailAsync(ticket.PassengerEmail, subject, body, isHtml: false, ct);
                if (!emailSent)
                {
                    _logger.LogWarning("Không gửi được email hóa đơn cho vé {TicketCode} tới {Email}", ticket.TicketCode, ticket.PassengerEmail);
                }
            }

            return (IpnProcessOutcome.Success, "Đã xác nhận thanh toán thành công");
        }
        else
        {
            ticket.Status = TicketStatuses.Cancelled;
            if (ticket.Seat != null)
            {
                ticket.Seat.Status = SeatStatuses.Available;
                ticket.Seat.LockedUntil = null;
            }

            await _db.SaveChangesAsync(ct);
            return (IpnProcessOutcome.Success, "Giao dịch thất bại, đã hủy vé và nhả ghế");
        }
    }
}
