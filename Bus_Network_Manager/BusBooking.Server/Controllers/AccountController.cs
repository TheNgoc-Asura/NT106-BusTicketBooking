using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using BusBooking.Server.Data;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private const int MinPasswordLength = 6;
    private static readonly Regex PhoneRegex = new(@"^[0-9+\-\s]{8,15}$", RegexOptions.Compiled);

    private readonly BusBookingDbContext _db;

    public AccountController(BusBookingDbContext db)
    {
        _db = db;
    }

    // 9. GET /api/account/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy thông tin tài khoản"));

        if (user.Status == AccountStatuses.Locked)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Tài khoản của bạn đã bị khóa"));

        var data = new ProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };

        return Ok(ApiResponse<ProfileDto>.Ok(data, "Thành công"));
    }

    // 10. PUT /api/account/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(ApiResponse.Fail("Họ tên không được để trống"));

        string? trimmedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        if (trimmedPhone != null && !PhoneRegex.IsMatch(trimmedPhone))
            return BadRequest(ApiResponse.Fail("Số điện thoại không đúng định dạng"));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy thông tin tài khoản"));

        if (user.Status == AccountStatuses.Locked)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Tài khoản của bạn đã bị khóa"));

        user.FullName = request.FullName.Trim();
        if (request.Phone != null)
            user.Phone = trimmedPhone;

        await _db.SaveChangesAsync(ct);

        var data = new UpdateProfileResponseData
        {
            Id = user.Id,
            FullName = user.FullName,
            Phone = user.Phone
        };

        return Ok(ApiResponse<UpdateProfileResponseData>.Ok(data, "Cập nhật thông tin thành công"));
    }

    // 11. POST /api/account/change-password
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(ApiResponse.Fail("Vui lòng điền mật khẩu hiện tại và mật khẩu mới"));

        if (request.NewPassword.Length < MinPasswordLength)
            return BadRequest(ApiResponse.Fail($"Mật khẩu mới phải có ít nhất {MinPasswordLength} ký tự"));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy thông tin tài khoản"));

        if (user.Status == AccountStatuses.Locked)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Tài khoản của bạn đã bị khóa"));

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return BadRequest(ApiResponse.Fail("Mật khẩu cũ không đúng"));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(null, "Đã thay đổi mật khẩu"));
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        string? rawId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(rawId, out userId) && userId > 0;
    }
}
