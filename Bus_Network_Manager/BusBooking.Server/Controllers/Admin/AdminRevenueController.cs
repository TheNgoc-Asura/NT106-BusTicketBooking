using System.Globalization;
using BusBooking.Server.Data;
using BusBooking.Server.Helpers;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers.Admin;

[ApiController]
[Route("api/admin/revenue")]
[Authorize(Roles = UserRoles.Admin)]
public class AdminRevenueController : ControllerBase
{
    private readonly BusBookingDbContext _db;

    public AdminRevenueController(BusBookingDbContext db)
    {
        _db = db;
    }

    // 53. GET /api/admin/revenue
    [HttpGet]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        CancellationToken ct = default)
    {
        DateTime endDate;
        DateTime startDate;

        if (string.IsNullOrWhiteSpace(toDate))
        {
            endDate = DateTime.Today;
        }
        else if (!InputParseHelper.TryParseDate(toDate, allowGeneralFallback: false, out endDate))
        {
            return BadRequest(ApiResponse.Fail("Định dạng toDate không hợp lệ (yyyy-MM-dd)"));
        }

        if (string.IsNullOrWhiteSpace(fromDate))
        {
            startDate = endDate.AddDays(-29);
        }
        else if (!InputParseHelper.TryParseDate(fromDate, allowGeneralFallback: false, out startDate))
        {
            return BadRequest(ApiResponse.Fail("Định dạng fromDate không hợp lệ (yyyy-MM-dd)"));
        }

        if (startDate > endDate)
            return BadRequest(ApiResponse.Fail("fromDate không được lớn hơn toDate"));

        var endExclusive = endDate.AddDays(1);

        var rows = await _db.Tickets
            .AsNoTracking()
            .Where(t => (t.Status == TicketStatuses.Paid || t.Status == TicketStatuses.Confirmed)
                        && (t.PaidAt ?? t.CreatedAt) >= startDate
                        && (t.PaidAt ?? t.CreatedAt) < endExclusive)
            .Select(t => new
            {
                EffectiveDate = (t.PaidAt ?? t.CreatedAt).Date,
                t.Channel,
                t.TotalAmount
            })
            .ToListAsync(ct);

        var details = rows
            .GroupBy(r => r.EffectiveDate)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                decimal online = g.Where(x => x.Channel == BookingChannels.Online).Sum(x => x.TotalAmount);
                decimal offline = g.Where(x => x.Channel == BookingChannels.Direct).Sum(x => x.TotalAmount);
                return new DailyRevenueDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Online = online,
                    Offline = offline,
                    Total = online + offline
                };
            })
            .ToList();

        decimal onlineTotal = details.Sum(d => d.Online);
        decimal offlineTotal = details.Sum(d => d.Offline);

        var data = new RevenueReportDto
        {
            FromDate = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ToDate = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TotalRevenue = onlineTotal + offlineTotal,
            OnlineRevenue = onlineTotal,
            OfflineRevenue = offlineTotal,
            Details = details
        };

        return Ok(ApiResponse<RevenueReportDto>.Ok(data, "Thành công"));
    }
}
