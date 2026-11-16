using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using BusTicket.Core.DTOs;
using BusTicket.Core.Enums;
using BusTicket.Core.Models;
using BusTicket.Server.Hubs;
using BusTicket.Server.Services;

namespace BusTicket.Server.Endpoints;

public static class SupportEndpoints
{
    public static void MapSupportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/support");

        // 21. Xem lịch sử tin nhắn hỗ trợ (Customer)
        group.MapGet("/messages", (int page, int pageSize, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out int userId, out _, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var ticket = store.Db.SupportTickets.FirstOrDefault(t => t.CustomerId == userId);
            if (ticket == null)
            {
                var empty = new PagedResult<SupportMessageDto>
                {
                    Items = new List<SupportMessageDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = 0
                };
                return Results.Ok(ApiResponse<PagedResult<SupportMessageDto>>.Ok(empty, "Thành công"));
            }

            var query = store.Db.SupportMessages.Where(m => m.TicketId == ticket.TicketId);
            int totalItems = query.Count();
            var items = query.OrderBy(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new SupportMessageDto
                {
                    Id = m.Id,
                    TicketId = m.TicketId,
                    SenderId = m.SenderId,
                    SenderRole = m.SenderRole,
                    Content = m.Content,
                    SentAt = m.SentAt
                }).ToList();

            var paged = new PagedResult<SupportMessageDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return Results.Ok(ApiResponse<PagedResult<SupportMessageDto>>.Ok(paged, "Thành công"));
        });

        // 22. Gửi tin nhắn hỗ trợ (Customer)
        group.MapPost("/messages", async (CreateSupportMessageDto body, HttpRequest req, DataStore store, TokenService tokenService, IHubContext<SupportHub> hubContext) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out int userId, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (string.IsNullOrWhiteSpace(body.Content))
                return Results.Json(ApiResponse<object>.Fail("Nội dung tin nhắn không được để trống"), statusCode: 400);

            var ticket = store.Db.SupportTickets.FirstOrDefault(t => t.CustomerId == userId);
            if (ticket == null)
            {
                ticket = new SupportTicket
                {
                    TicketId = store.GetNextSupportTicketId(),
                    CustomerId = userId,
                    Title = "Yêu cầu hỗ trợ trực tuyến",
                    Status = SupportTicketStatuses.Pending,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                store.Db.SupportTickets.Add(ticket);
            }
            else
            {
                ticket.Status = SupportTicketStatuses.Pending;
                ticket.UpdatedAt = DateTime.Now;
            }

            var msg = new SupportMessage
            {
                Id = store.GetNextSupportMessageId(),
                TicketId = ticket.TicketId,
                SenderId = userId,
                SenderRole = Roles.Customer,
                Content = body.Content.Trim(),
                SentAt = DateTime.Now
            };

            store.Db.SupportMessages.Add(msg);
            store.SaveChanges();

            var msgDto = new
            {
                id = msg.Id,
                ticketId = msg.TicketId,
                senderId = msg.SenderId,
                senderRole = msg.SenderRole,
                content = msg.Content,
                sentAt = msg.SentAt.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            // Broadcast to Staff group
            await hubContext.Clients.Group("staff_group").SendAsync("ReceiveMessage", msgDto);

            return Results.Json(ApiResponse<object>.Ok(new
            {
                id = msg.Id,
                ticketId = msg.TicketId,
                content = msg.Content,
                sentAt = msg.SentAt
            }, "Gửi tin nhắn thành công"), statusCode: 201);
        });
    }
}
