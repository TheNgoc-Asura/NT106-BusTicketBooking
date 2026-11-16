using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Server.Hubs;
using BusBooking.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace BusBooking.Server.Helpers;

public static class NotificationHelper
{
    /// <summary>
    /// Lưu danh sách thông báo vào CSDL và đẩy realtime qua SignalR tới từng người dùng.
    /// </summary>
    public static async Task SaveAndPushAsync(
        BusBookingDbContext db,
        IHubContext<SupportHub> hubContext,
        IEnumerable<Notification> notifications,
        CancellationToken ct = default)
    {
        var list = notifications.ToList();
        if (list.Count == 0)
            return;

        db.Notifications.AddRange(list);
        await db.SaveChangesAsync(ct);

        foreach (var n in list)
        {
            await hubContext.Clients.Group($"user_{n.UserId}").SendAsync("ReceiveNotification", new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Content = n.Content,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }, ct);
        }
    }
}
