using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Server.Services;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const int OtpExpirySeconds = 300;
    private const int TempTokenExpiryMinutes = 10;
    private const int MinPasswordLength = 6;
    private static readonly EmailAddressAttribute EmailValidator = new();

    private readonly BusBookingDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly ITokenRevocationService _revocationService;

    public AuthController(
        BusBookingDbContext db,
        IEmailService emailService,
        ITokenService tokenService,
        ITokenRevocationService revocationService)
    {
        _db = db;
        _emailService = emailService;
        _tokenService = tokenService;
        _revocationService = revocationService;
    }

    // 1. POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ApiResponse.Fail("Email và mật khẩu không được để trống"));

        string email = NormalizeEmail(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(ApiResponse.Fail("Sai email hoặc mật khẩu"));

        if (user.Status == AccountStatuses.Locked)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Tài khoản của bạn đã bị khóa"));

        var (token, expiresAt) = _tokenService.GenerateAccessToken(user);

        var data = new LoginResponseData
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Phone = user.Phone
            }
        };

        return Ok(ApiResponse<LoginResponseData>.Ok(data, "Đăng nhập thành công"));
    }

    // 2. POST /api/auth/register/request-code
    [HttpPost("register/request-code")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterRequestCode([FromBody] RegisterRequestCodeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse.Fail("Vui lòng điền đầy đủ họ tên, email và mật khẩu"));
        }

        string email = NormalizeEmail(request.Email);
        if (!EmailValidator.IsValid(email))
            return BadRequest(ApiResponse.Fail("Định dạng email không hợp lệ"));

        if (request.Password.Length < MinPasswordLength)
            return BadRequest(ApiResponse.Fail($"Mật khẩu phải có ít nhất {MinPasswordLength} ký tự"));

        bool exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
            return Conflict(ApiResponse.Fail("Email đã được đăng ký trong hệ thống"));

        return await IssueOtpAsync(email, OtpPurposes.Register, ct);
    }

    // 3. POST /api/auth/register/verify-code
    [HttpPost("register/verify-code")]
    [AllowAnonymous]
    public Task<IActionResult> RegisterVerifyCode([FromBody] VerifyCodeRequest request, CancellationToken ct)
        => VerifyOtpAsync(request, OtpPurposes.Register, ct);

    // 4. POST /api/auth/register/complete
    [HttpPost("register/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterComplete([FromBody] CompleteRegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TempToken) ||
            string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"));
        }

        if (request.Password.Length < MinPasswordLength)
            return BadRequest(ApiResponse.Fail($"Mật khẩu phải có ít nhất {MinPasswordLength} ký tự"));

        string tempToken = request.TempToken.Trim();
        var otp = await _db.OtpCodes.FirstOrDefaultAsync(
            o => o.TempToken == tempToken && o.Purpose == OtpPurposes.Register && o.IsVerified && !o.IsUsed,
            ct);

        if (otp == null || DateTime.Now > otp.ExpiresAt)
            return Unauthorized(ApiResponse.Fail("Mã tạm không hợp lệ hoặc đã hết hạn"));

        bool emailTaken = await _db.Users.AnyAsync(u => u.Email == otp.Email, ct);
        if (emailTaken)
            return Conflict(ApiResponse.Fail("Email đã được đăng ký"));

        var newUser = new User
        {
            FullName = request.FullName.Trim(),
            Email = otp.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRoles.Customer,
            Status = AccountStatuses.Active,
            CreatedAt = DateTime.Now
        };

        otp.IsUsed = true;
        _db.Users.Add(newUser);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Conflict(ApiResponse.Fail("Email đã được đăng ký"));
        }

        var data = new CompleteRegisterResponseData
        {
            UserId = newUser.Id,
            Email = newUser.Email
        };

        return StatusCode(StatusCodes.Status201Created, ApiResponse<CompleteRegisterResponseData>.Ok(data, "Đăng ký thành công"));
    }

    // 5. POST /api/auth/forgot-password/request-code
    [HttpPost("forgot-password/request-code")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordRequestCode([FromBody] ForgotPasswordRequestCodeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(ApiResponse.Fail("Email không được để trống"));

        string email = NormalizeEmail(request.Email);
        if (!EmailValidator.IsValid(email))
            return BadRequest(ApiResponse.Fail("Định dạng email không hợp lệ"));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản ứng với email"));

        if (user.Status == AccountStatuses.Locked)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Tài khoản của bạn đã bị khóa"));

        return await IssueOtpAsync(email, OtpPurposes.ForgotPassword, ct);
    }

    // 6. POST /api/auth/forgot-password/verify-code
    [HttpPost("forgot-password/verify-code")]
    [AllowAnonymous]
    public Task<IActionResult> ForgotPasswordVerifyCode([FromBody] VerifyCodeRequest request, CancellationToken ct)
        => VerifyOtpAsync(request, OtpPurposes.ForgotPassword, ct);

    // 7. POST /api/auth/forgot-password/reset
    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordReset([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TempToken) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(ApiResponse.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"));

        if (request.NewPassword.Length < MinPasswordLength)
            return BadRequest(ApiResponse.Fail($"Mật khẩu mới phải có ít nhất {MinPasswordLength} ký tự"));

        string tempToken = request.TempToken.Trim();
        var otp = await _db.OtpCodes.FirstOrDefaultAsync(
            o => o.TempToken == tempToken && o.Purpose == OtpPurposes.ForgotPassword && o.IsVerified && !o.IsUsed,
            ct);

        if (otp == null || DateTime.Now > otp.ExpiresAt)
            return Unauthorized(ApiResponse.Fail("Mã tạm không hợp lệ hoặc đã hết hạn"));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == otp.Email, ct);
        if (user == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản"));

        if (user.Status == AccountStatuses.Locked)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Tài khoản của bạn đã bị khóa"));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        otp.IsUsed = true;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(null, "Đặt lại mật khẩu thành công"));
    }

    // 8. POST /api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        string? jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        string? expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        DateTime expiresAtUtc = DateTime.UtcNow.AddHours(24);
        if (long.TryParse(expClaim, out long expUnix))
        {
            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        }

        if (!string.IsNullOrWhiteSpace(jti))
        {
            _revocationService.RevokeToken(jti, expiresAtUtc);
        }

        return Ok(ApiResponse.Ok(null, "Đăng xuất thành công"));
    }

    private async Task<IActionResult> IssueOtpAsync(string email, string purpose, CancellationToken ct)
    {
        string code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        bool sent = await _emailService.SendOtpEmailAsync(email, code, purpose, ct);
        if (!sent)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse.Fail("Không gửi được email qua máy chủ SMTP"));
        }

        var oldOtps = await _db.OtpCodes
            .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync(ct);

        foreach (var old in oldOtps)
        {
            old.IsUsed = true;
        }

        var otp = new OtpCode
        {
            Email = email,
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.Now.AddSeconds(OtpExpirySeconds),
            IsVerified = false,
            IsUsed = false,
            CreatedAt = DateTime.Now
        };

        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync(ct);

        var data = new RequestCodeResponseData
        {
            Email = email,
            ExpiresInSeconds = OtpExpirySeconds
        };

        return Ok(ApiResponse<RequestCodeResponseData>.Ok(data, "Mã xác thực đã được gửi tới email"));
    }

    private async Task<IActionResult> VerifyOtpAsync(VerifyCodeRequest request, string purpose, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse.Fail("Thiếu email hoặc mã xác thực"));

        string email = NormalizeEmail(request.Email);
        string code = request.Code.Trim();

        var otp = await _db.OtpCodes
            .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed && !o.IsVerified)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .FirstOrDefaultAsync(ct);

        if (otp == null || otp.Code != code || DateTime.Now > otp.ExpiresAt)
            return BadRequest(ApiResponse.Fail("Mã xác thực không đúng hoặc đã hết hạn"));

        string tempToken = Guid.NewGuid().ToString("N");
        otp.TempToken = tempToken;
        otp.IsVerified = true;
        otp.ExpiresAt = DateTime.Now.AddMinutes(TempTokenExpiryMinutes);

        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<VerifyCodeResponseData>.Ok(
            new VerifyCodeResponseData { TempToken = tempToken },
            "Xác thực thành công"));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
