using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using BusTicket.Core.DTOs;
using BusTicket.Server.Services;

namespace BusTicket.Server.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications");

        // 19. Xem danh sách thông báo
        group.MapGet("/", (bool? unreadOnly, int page, int pageSize, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out int userId, out _, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = store.Db.Notifications.Where(n => n.UserId == userId);
            if (unreadOnly == true)
            {
                query = query.Where(n => !n.IsRead);
            }

            int totalItems = query.Count();
            var items = query.OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Content = n.Content,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();

            var paged = new PagedResult<NotificationDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return Results.Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(paged, "Thành công"));
        });

        // 20. Đánh dấu đã đọc
        group.MapPut("/{id:int}/read", (int id, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out int userId, out _, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            var notif = store.Db.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId);
            if (notif == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy thông báo"), statusCode: 404);

            notif.IsRead = true;
            store.SaveChanges();

            return Results.Ok(ApiResponse<object>.Ok(new { id = notif.Id, isRead = true }, "Thành công"));
        });
    }
}
