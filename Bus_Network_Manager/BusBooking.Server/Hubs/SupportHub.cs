using System.Security.Claims;
using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Hubs;

[Authorize]
public class SupportHub : Hub
{
    private readonly BusBookingDbContext _db;
    private readonly ILogger<SupportHub> _logger;

    public SupportHub(BusBookingDbContext db, ILogger<SupportHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("sub")?.Value;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? UserRoles.Customer;

        if (int.TryParse(userIdClaim, out int userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            if (role == UserRoles.Admin || role == UserRoles.Support)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "staff_group");
            }

            _logger.LogInformation("SignalR connected: UserId={UserId}, Role={Role}", userId, role);
        }

        await base.OnConnectedAsync();
    }

    public async Task SendMessage(int ticketId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("sub")?.Value;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? UserRoles.Customer;

        if (!int.TryParse(userIdClaim, out int userId))
            return;

        // Chỉ Customer, Support hoặc Admin được phép trao đổi trên kênh hỗ trợ
        bool isCustomer = role == UserRoles.Customer;
        bool isSupportAgent = role == UserRoles.Support || role == UserRoles.Admin;
        if (!isCustomer && !isSupportAgent)
            return;

        var ticket = ticketId > 0
            ? await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId)
            : null;

        if (ticket == null)
        {
            if (!isCustomer)
                return;

            // Tái sử dụng ticket đang mở gần nhất của khách nếu có, tránh tạo trùng lặp
            ticket = await _db.SupportTickets
                .Where(t => t.CustomerId == userId && t.Status != SupportTicketStatuses.Resolved)
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefaultAsync();

            if (ticket == null)
            {
                ticket = new SupportTicket
                {
                    CustomerId = userId,
                    Title = "Yêu cầu hỗ trợ trực tuyến",
                    Status = SupportTicketStatuses.Pending,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _db.SupportTickets.Add(ticket);
                await _db.SaveChangesAsync();
            }
        }
        else if (isCustomer && ticket.CustomerId != userId)
        {
            // Ngăn khách hàng gửi tin nhắn vào ticket của người khác
            return;
        }

        var message = new SupportMessage
        {
            SupportTicketId = ticket.Id,
            SenderId = userId,
            SenderRole = role,
            Content = content.Trim(),
            SentAt = DateTime.Now
        };

        _db.SupportMessages.Add(message);
        ticket.UpdatedAt = message.SentAt;

        if (isCustomer)
        {
            ticket.Status = SupportTicketStatuses.Pending;
        }
        else
        {
            ticket.Status = SupportTicketStatuses.WaitingCustomer;
            ticket.HandlerId = userId;
        }

        await _db.SaveChangesAsync();

        var dto = new SupportMessageDto
        {
            Id = message.Id,
            TicketId = ticket.Id,
            SenderId = message.SenderId,
            SenderRole = message.SenderRole,
            Content = message.Content,
            SentAt = message.SentAt
        };

        // Đẩy tin nhắn đúng cho khách hàng chủ ticket và nhóm nhân viên hỗ trợ
        await Clients.Group($"user_{ticket.CustomerId}").SendAsync("ReceiveMessage", dto);
        await Clients.Group("staff_group").SendAsync("ReceiveMessage", dto);
    }
}
