using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using BusTicket.Core.DTOs;
using BusTicket.Server.Services;

namespace BusTicket.Server.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/account");

        // 9. Xem thông tin cá nhân
        group.MapGet("/profile", (HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out int userId, out _, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            var user = store.Db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy thông tin tài khoản"), statusCode: 404);

            var res = new ProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Results.Ok(ApiResponse<ProfileDto>.Ok(res, "Thành công"));
        });

        // 10. Cập nhật thông tin cá nhân
        group.MapPut("/profile", (UpdateProfileDto body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out int userId, out _, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (string.IsNullOrWhiteSpace(body.FullName))
                return Results.Json(ApiResponse<object>.Fail("Họ tên không được để trống"), statusCode: 400);

            var user = store.Db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy thông tin tài khoản"), statusCode: 404);

            user.FullName = body.FullName.Trim();
            if (body.Phone != null)
                user.Phone = body.Phone.Trim();

            store.SaveChanges();

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                phone = user.Phone
            }, "Cập nhật thông tin thành công"));
        });

        // 11. Đổi mật khẩu
        group.MapPost("/change-password", (ChangePasswordDto body, HttpRequest req, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(req.Headers.Authorization, out int userId, out _, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (string.IsNullOrWhiteSpace(body.OldPassword) || string.IsNullOrWhiteSpace(body.NewPassword))
                return Results.Json(ApiResponse<object>.Fail("Vui lòng điền mật khẩu cũ và mới"), statusCode: 400);

            var user = store.Db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy thông tin tài khoản"), statusCode: 404);

            if (!PasswordHasher.VerifyPassword(body.OldPassword, user.PasswordHash))
                return Results.Json(ApiResponse<object>.Fail("Mật khẩu cũ không đúng"), statusCode: 400);

            user.PasswordHash = PasswordHasher.HashPassword(body.NewPassword);
            store.SaveChanges();

            return Results.Ok(ApiResponse<object?>.Ok(null, "Đã thay đổi mật khẩu"));
        });
    }
}
