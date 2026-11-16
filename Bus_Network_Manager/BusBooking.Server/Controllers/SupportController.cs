using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Server.Extensions;
using BusBooking.Server.Hubs;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers;

[ApiController]
[Route("api/support")]
[Authorize(Roles = UserRoles.Customer)]
public class SupportController : ControllerBase
{
    private readonly BusBookingDbContext _db;
    private readonly IHubContext<SupportHub> _hubContext;

    public SupportController(BusBookingDbContext db, IHubContext<SupportHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    // 21. GET /api/support/messages
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (!User.TryGetUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var ticket = await _db.SupportTickets
            .AsNoTracking()
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(t => t.CustomerId == userId, ct);

        if (ticket == null)
        {
            return Ok(ApiResponse<PagedResult<SupportMessageDto>>.Ok(new PagedResult<SupportMessageDto>
            {
                Items = new List<SupportMessageDto>(),
                Page = page,
                PageSize = pageSize,
                TotalItems = 0
            }, "Thành công"));
        }

        var query = _db.SupportMessages
            .AsNoTracking()
            .Where(m => m.SupportTicketId == ticket.Id);

        int totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderBy(m => m.SentAt)
            .ThenBy(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new SupportMessageDto
            {
                Id = m.Id,
                TicketId = m.SupportTicketId,
                SenderId = m.SenderId,
                SenderRole = m.SenderRole,
                Content = m.Content,
                SentAt = m.SentAt
            })
            .ToListAsync(ct);

        var paged = new PagedResult<SupportMessageDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return Ok(ApiResponse<PagedResult<SupportMessageDto>>.Ok(paged, "Thành công"));
    }

    // 22. POST /api/support/messages
    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] CreateSupportMessageRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(ApiResponse.Fail("Nội dung tin nhắn không được để trống"));

        var now = DateTime.Now;
        var ticket = await _db.SupportTickets
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(t => t.CustomerId == userId, ct);

        if (ticket == null)
        {
            ticket = new SupportTicket
            {
                CustomerId = userId,
                Title = "Yêu cầu hỗ trợ trực tuyến",
                Status = SupportTicketStatuses.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.SupportTickets.Add(ticket);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            ticket.Status = SupportTicketStatuses.Pending;
            ticket.UpdatedAt = now;
        }

        var message = new SupportMessage
        {
            SupportTicketId = ticket.Id,
            SenderId = userId,
            SenderRole = UserRoles.Customer,
            Content = request.Content.Trim(),
            SentAt = now
        };

        _db.SupportMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        await _hubContext.Clients.Group("staff_group").SendAsync("ReceiveMessage", new SupportMessageDto
        {
            Id = message.Id,
            TicketId = ticket.Id,
            SenderId = message.SenderId,
            SenderRole = message.SenderRole,
            Content = message.Content,
            SentAt = message.SentAt
        }, ct);

        var data = new SendSupportMessageResponseData
        {
            Id = message.Id,
            TicketId = ticket.Id,
            Content = message.Content,
            SentAt = message.SentAt
        };

        return StatusCode(StatusCodes.Status201Created, ApiResponse<SendSupportMessageResponseData>.Ok(data, "Gửi tin nhắn thành công"));
    }
}
