using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using BusTicket.Core.Enums;
using BusTicket.Core.Models;
using BusTicket.Server.Services;

namespace BusTicket.Server.Hubs;

public class SupportHub : Hub
{
    private readonly DataStore _store;
    private readonly TokenService _tokenService;
    private readonly ILogger<SupportHub> _logger;

    public SupportHub(DataStore store, TokenService tokenService, ILogger<SupportHub> logger)
    {
        _store = store;
        _tokenService = tokenService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        string? token = httpContext?.Request.Query["access_token"];

        if (!string.IsNullOrEmpty(token) && _tokenService.ValidateToken(token, out int userId, out string role, out _))
        {
            Context.Items["UserId"] = userId;
            Context.Items["Role"] = role;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            if (role == Roles.Support || role == Roles.Admin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "staff_group");
            }
            _logger.LogInformation("SignalR connected: User {UserId} ({Role})", userId, role);
        }

        await base.OnConnectedAsync();
    }

    public async Task SendMessage(int ticketId, string content)
    {
        int userId = Context.Items.TryGetValue("UserId", out var u) && u is int uid ? uid : 0;
        string role = Context.Items.TryGetValue("Role", out var r) && r is string srole ? srole : Roles.Customer;

        var ticket = _store.Db.SupportTickets.FirstOrDefault(t => t.TicketId == ticketId);
        if (ticket == null)
            return;

        var msg = new SupportMessage
        {
            Id = _store.GetNextSupportMessageId(),
            TicketId = ticketId,
            SenderId = userId,
            SenderRole = role,
            Content = content,
            SentAt = DateTime.Now
        };

        _store.Db.SupportMessages.Add(msg);
        ticket.UpdatedAt = DateTime.Now;

        if (role == Roles.Customer)
        {
            ticket.Status = SupportTicketStatuses.Pending;
        }
        else
        {
            ticket.Status = SupportTicketStatuses.WaitingCustomer;
            ticket.HandlerId = userId;
        }

        _store.SaveChanges();

        var messageDto = new
        {
            id = msg.Id,
            ticketId = msg.TicketId,
            senderId = msg.SenderId,
            senderRole = msg.SenderRole,
            content = msg.Content,
            sentAt = msg.SentAt.ToString("yyyy-MM-ddTHH:mm:ss")
        };

        // Push to specific ticket group or users
        await Clients.Group($"user_{ticket.CustomerId}").SendAsync("ReceiveMessage", messageDto);
        await Clients.Group("staff_group").SendAsync("ReceiveMessage", messageDto);
        await Clients.All.SendAsync("ReceiveMessage", messageDto);
    }
}
