using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using BusTicket.Core.DTOs;
using BusTicket.Core.Enums;
using BusTicket.Core.Models;
using BusTicket.Server.Services;

namespace BusTicket.Server.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        // 1. Đăng nhập
        group.MapPost("/login", (LoginRequest req, DataStore store, TokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.Json(ApiResponse<object>.Fail("Email và mật khẩu không được để trống"), statusCode: 400);

            var user = store.Db.Users.FirstOrDefault(u => u.Email.Equals(req.Email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user == null || !PasswordHasher.VerifyPassword(req.Password, user.PasswordHash))
                return Results.Json(ApiResponse<object>.Fail("Sai email hoặc mật khẩu"), statusCode: 401);

            if (user.Status == AccountStatuses.Locked)
                return Results.Json(ApiResponse<object>.Fail("Tài khoản của bạn đã bị khóa"), statusCode: 403);

            string token = tokenService.GenerateToken(user);
            var res = new LoginResponseData
            {
                Token = token,
                ExpiresAt = DateTime.Now.AddHours(24),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    Phone = user.Phone
                }
            };

            return Results.Ok(ApiResponse<LoginResponseData>.Ok(res, "Đăng nhập thành công"));
        });

        // 2. Đăng ký – Gửi mã xác thực email
        group.MapPost("/register/request-code", async (RegisterRequestCodeDto req, DataStore store, EmailService emailService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Password))
                return Results.Json(ApiResponse<object>.Fail("Vui lòng điền đầy đủ họ tên, email và mật khẩu"), statusCode: 400);

            if (store.Db.Users.Any(u => u.Email.Equals(req.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Results.Json(ApiResponse<object>.Fail("Email đã được đăng ký trong hệ thống"), statusCode: 409);

            string code = new Random().Next(100000, 999999).ToString();
            var ver = new EmailVerification
            {
                Id = store.Db.EmailVerifications.Count + 1,
                Email = req.Email.Trim(),
                Code = code,
                Type = "Register",
                ExpiresAt = DateTime.Now.AddSeconds(300),
                IsUsed = false
            };
            store.Db.EmailVerifications.Add(ver);
            store.SaveChanges();

            await emailService.SendOtpEmailAsync(req.Email.Trim(), code, "Register");

            return Results.Ok(ApiResponse<RequestCodeResponseData>.Ok(new RequestCodeResponseData
            {
                Email = req.Email.Trim(),
                ExpiresInSeconds = 300
            }, "Mã xác thực đã được gửi tới email"));
        });

        // 3. Đăng ký – Xác nhận mã
        group.MapPost("/register/verify-code", (VerifyCodeDto req, DataStore store, TokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Code))
                return Results.Json(ApiResponse<object>.Fail("Thiếu email hoặc mã xác thực"), statusCode: 400);

            var ver = store.Db.EmailVerifications
                .Where(v => v.Email.Equals(req.Email.Trim(), StringComparison.OrdinalIgnoreCase) && v.Type == "Register" && !v.IsUsed)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefault();

            if (ver == null || ver.Code != req.Code.Trim() || DateTime.Now > ver.ExpiresAt)
                return Results.Json(ApiResponse<object>.Fail("Mã xác thực không đúng hoặc đã hết hạn"), statusCode: 400);

            ver.TempToken = tokenService.GenerateTempToken();
            store.SaveChanges();

            return Results.Ok(ApiResponse<VerifyCodeResponseData>.Ok(new VerifyCodeResponseData
            {
                TempToken = ver.TempToken
            }, "Xác thực thành công"));
        });

        // 4. Đăng ký – Hoàn tất
        group.MapPost("/register/complete", (CompleteRegisterDto req, DataStore store) =>
        {
            if (string.IsNullOrWhiteSpace(req.TempToken) || string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Password))
                return Results.Json(ApiResponse<object>.Fail("Thiếu dữ liệu hoàn tất đăng ký"), statusCode: 400);

            var ver = store.Db.EmailVerifications.FirstOrDefault(v => v.TempToken == req.TempToken && !v.IsUsed && v.Type == "Register");
            if (ver == null || DateTime.Now > ver.ExpiresAt)
                return Results.Json(ApiResponse<object>.Fail("Mã tạm không hợp lệ hoặc đã hết hạn"), statusCode: 401);

            if (store.Db.Users.Any(u => u.Email.Equals(ver.Email, StringComparison.OrdinalIgnoreCase)))
                return Results.Json(ApiResponse<object>.Fail("Email đã được đăng ký"), statusCode: 409);

            var newUser = new User
            {
                Id = store.GetNextUserId(),
                FullName = req.FullName.Trim(),
                Email = ver.Email,
                PasswordHash = PasswordHasher.HashPassword(req.Password),
                Role = Roles.Customer,
                Status = AccountStatuses.Active,
                CreatedAt = DateTime.Now
            };

            ver.IsUsed = true;
            store.Db.Users.Add(newUser);
            store.SaveChanges();

            return Results.Json(ApiResponse<CompleteRegisterResponseData>.Ok(new CompleteRegisterResponseData
            {
                UserId = newUser.Id,
                Email = newUser.Email
            }, "Đăng ký thành công"), statusCode: 201);
        });

        // 5. Quên mật khẩu – Gửi mã xác thực
        group.MapPost("/forgot-password/request-code", async (ForgotPasswordRequestCodeDto req, DataStore store, EmailService emailService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.Json(ApiResponse<object>.Fail("Email không được để trống"), statusCode: 400);

            var user = store.Db.Users.FirstOrDefault(u => u.Email.Equals(req.Email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy tài khoản ứng với email"), statusCode: 404);

            string code = new Random().Next(100000, 999999).ToString();
            var ver = new EmailVerification
            {
                Id = store.Db.EmailVerifications.Count + 1,
                Email = req.Email.Trim(),
                Code = code,
                Type = "ForgotPassword",
                ExpiresAt = DateTime.Now.AddSeconds(300),
                IsUsed = false
            };
            store.Db.EmailVerifications.Add(ver);
            store.SaveChanges();

            await emailService.SendOtpEmailAsync(req.Email.Trim(), code, "ForgotPassword");

            return Results.Ok(ApiResponse<RequestCodeResponseData>.Ok(new RequestCodeResponseData
            {
                Email = req.Email.Trim(),
                ExpiresInSeconds = 300
            }, "Mã xác thực đã được gửi tới email"));
        });

        // 6. Quên mật khẩu – Xác nhận mã
        group.MapPost("/forgot-password/verify-code", (VerifyCodeDto req, DataStore store, TokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Code))
                return Results.Json(ApiResponse<object>.Fail("Thiếu email hoặc mã xác thực"), statusCode: 400);

            var ver = store.Db.EmailVerifications
                .Where(v => v.Email.Equals(req.Email.Trim(), StringComparison.OrdinalIgnoreCase) && v.Type == "ForgotPassword" && !v.IsUsed)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefault();

            if (ver == null || ver.Code != req.Code.Trim() || DateTime.Now > ver.ExpiresAt)
                return Results.Json(ApiResponse<object>.Fail("Mã xác thực không đúng hoặc đã hết hạn"), statusCode: 400);

            ver.TempToken = tokenService.GenerateTempToken();
            store.SaveChanges();

            return Results.Ok(ApiResponse<VerifyCodeResponseData>.Ok(new VerifyCodeResponseData
            {
                TempToken = ver.TempToken
            }, "Xác thực thành công"));
        });

        // 7. Quên mật khẩu – Đặt mật khẩu mới
        group.MapPost("/forgot-password/reset", (ResetPasswordDto req, DataStore store) =>
        {
            if (string.IsNullOrWhiteSpace(req.TempToken) || string.IsNullOrWhiteSpace(req.NewPassword))
                return Results.Json(ApiResponse<object>.Fail("Thiếu mã tạm hoặc mật khẩu mới"), statusCode: 400);

            var ver = store.Db.EmailVerifications.FirstOrDefault(v => v.TempToken == req.TempToken && !v.IsUsed && v.Type == "ForgotPassword");
            if (ver == null || DateTime.Now > ver.ExpiresAt)
                return Results.Json(ApiResponse<object>.Fail("Mã tạm không hợp lệ hoặc đã hết hạn"), statusCode: 401);

            var user = store.Db.Users.FirstOrDefault(u => u.Email.Equals(ver.Email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy tài khoản"), statusCode: 404);

            user.PasswordHash = PasswordHasher.HashPassword(req.NewPassword);
            ver.IsUsed = true;
            store.SaveChanges();

            return Results.Ok(ApiResponse<object?>.Ok(null, "Đặt lại mật khẩu thành công"));
        });

        // 8. Đăng xuất
        group.MapPost("/logout", (HttpRequest request, TokenService tokenService) =>
        {
            string? auth = request.Headers.Authorization;
            if (!tokenService.ValidateToken(auth, out _, out _, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            return Results.Ok(ApiResponse<object?>.Ok(null, "Đăng xuất thành công"));
        });
    }
}
