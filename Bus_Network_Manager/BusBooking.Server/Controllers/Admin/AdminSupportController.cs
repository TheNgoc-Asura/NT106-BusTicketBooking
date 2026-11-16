using BusBooking.Server.Data;
using BusBooking.Server.Extensions;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers.Admin;

[ApiController]
[Route("api/admin/support")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Support}")]
public class AdminSupportController : ControllerBase
{
    private readonly BusBookingDbContext _db;

    public AdminSupportController(BusBookingDbContext db)
    {
        _db = db;
    }

    // 50. GET /api/admin/support/tickets
    [HttpGet("tickets")]
    public async Task<IActionResult> GetSupportTickets(
        [FromQuery] string? status,
        [FromQuery] string? title,
        [FromQuery] int? handlerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var statusCounts = await _db.SupportTickets
            .AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var stats = new SupportStatisticsDto
        {
            Total = statusCounts.Sum(x => x.Count),
            Pending = statusCounts.FirstOrDefault(x => x.Status == SupportTicketStatuses.Pending)?.Count ?? 0,
            Processing = statusCounts.FirstOrDefault(x => x.Status == SupportTicketStatuses.Processing)?.Count ?? 0,
            WaitingCustomer = statusCounts.FirstOrDefault(x => x.Status == SupportTicketStatuses.WaitingCustomer)?.Count ?? 0,
            Resolved = statusCounts.FirstOrDefault(x => x.Status == SupportTicketStatuses.Resolved)?.Count ?? 0
        };

        var query = _db.SupportTickets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            string statusFilter = status.Trim();
            query = query.Where(t => t.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            string titleFilter = title.Trim();
            query = query.Where(t => t.Title.Contains(titleFilter));
        }

        if (handlerId.HasValue && handlerId.Value > 0)
        {
            query = query.Where(t => t.HandlerId == handlerId.Value);
        }

        int totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new SupportTicketSummaryDto
            {
                TicketId = t.Id,
                Title = t.Title,
                CustomerName = t.Customer.FullName,
                Status = t.Status,
                HandlerName = t.Handler != null ? t.Handler.FullName : null,
                LastMessageAt = t.UpdatedAt
            })
            .ToListAsync(ct);

        var data = new AdminSupportTicketListResponseData
        {
            Statistics = stats,
            Tickets = new PagedResult<SupportTicketSummaryDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }
        };

        return Ok(ApiResponse<AdminSupportTicketListResponseData>.Ok(data, "Thành công"));
    }

    // 51. GET /api/admin/support/tickets/{ticketId}/messages
    [HttpGet("tickets/{ticketId:int}/messages")]
    public async Task<IActionResult> GetTicketMessages(int ticketId, CancellationToken ct)
    {
        bool exists = await _db.SupportTickets.AsNoTracking().AnyAsync(t => t.Id == ticketId, ct);
        if (!exists)
            return NotFound(ApiResponse.Fail("Không tìm thấy ticket hỗ trợ"));

        var messages = await _db.SupportMessages
            .AsNoTracking()
            .Where(m => m.SupportTicketId == ticketId)
            .OrderBy(m => m.SentAt)
            .ThenBy(m => m.Id)
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

        return Ok(ApiResponse<List<SupportMessageDto>>.Ok(messages, "Thành công"));
    }

    // 52. PUT /api/admin/support/tickets/{ticketId}/status
    [HttpPut("tickets/{ticketId:int}/status")]
    public async Task<IActionResult> UpdateTicketStatus(
        int ticketId,
        [FromBody] UpdateSupportTicketStatusRequest request,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        string targetStatus = request.Status?.Trim() ?? string.Empty;
        if (targetStatus != SupportTicketStatuses.Processing && targetStatus != SupportTicketStatuses.Resolved)
            return BadRequest(ApiResponse.Fail("Trạng thái mới chỉ hỗ trợ 'Đang xử lý' hoặc 'Đã xử lý'"));

        var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy ticket hỗ trợ"));

        ticket.Status = targetStatus;
        ticket.HandlerId = userId;
        ticket.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);

        var data = new UpdateSupportTicketStatusResponseData
        {
            TicketId = ticket.Id,
            Status = ticket.Status,
            HandlerId = userId
        };

        return Ok(ApiResponse<UpdateSupportTicketStatusResponseData>.Ok(data, "Cập nhật trạng thái thành công"));
    }
}
