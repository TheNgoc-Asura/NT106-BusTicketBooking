using BusBooking.Server.Data;
using BusBooking.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Services;

public interface ISeatLockService
{
    Task ReleaseExpiredLocksAsync(int? tripId = null, CancellationToken ct = default);
    Task<bool> TryAcquireSeatAsync(int tripId, int seatId, string targetStatus, DateTime? lockedUntil, DateTime now, CancellationToken ct = default);
}

public class SeatLockService : ISeatLockService
{
    private readonly BusBookingDbContext _db;

    public SeatLockService(BusBookingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Giải phóng các ghế đã hết hạn giữ chỗ (LockedUntil <= hiện tại) và hủy vé chờ thanh toán tương ứng.
    /// </summary>
    public async Task ReleaseExpiredLocksAsync(int? tripId = null, CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var query = _db.TripSeats
            .Where(s => s.Status == SeatStatuses.Pending && s.LockedUntil != null && s.LockedUntil <= now);

        if (tripId.HasValue)
        {
            query = query.Where(s => s.TripId == tripId.Value);
        }

        var expiredSeats = await query.ToListAsync(ct);
        if (expiredSeats.Count == 0)
            return;

        var seatIds = expiredSeats.Select(s => s.Id).ToList();
        var pendingTickets = await _db.Tickets
            .Where(t => seatIds.Contains(t.SeatId) && t.Status == TicketStatuses.PendingPayment)
            .ToListAsync(ct);

        foreach (var seat in expiredSeats)
        {
            seat.Status = SeatStatuses.Available;
            seat.LockedUntil = null;
        }

        foreach (var ticket in pendingTickets)
        {
            ticket.Status = TicketStatuses.Cancelled;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Khóa hoặc chiếm ghế nguyên tử trong transaction để chống đặt trùng cùng một ghế.
    /// </summary>
    public async Task<bool> TryAcquireSeatAsync(
        int tripId,
        int seatId,
        string targetStatus,
        DateTime? lockedUntil,
        DateTime now,
        CancellationToken ct = default)
    {
        int affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE TripSeats
            SET [Status] = {targetStatus}, [LockedUntil] = {lockedUntil}
            WHERE [Id] = {seatId}
              AND [TripId] = {tripId}
              AND (
                  [Status] = {SeatStatuses.Available}
                  OR ([Status] = {SeatStatuses.Pending} AND ([LockedUntil] IS NULL OR [LockedUntil] <= {now}))
              )", ct);

        return affectedRows > 0;
    }
}
