namespace BusBooking.Shared.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseData
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto User { get; set; } = new();
}

public class UserInfoDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class RegisterRequestCodeRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RequestCodeResponseData
{
    public string Email { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; } = 300;
}

public class VerifyCodeRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class VerifyCodeResponseData
{
    public string TempToken { get; set; } = string.Empty;
}

public class CompleteRegisterRequest
{
    public string TempToken { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CompleteRegisterResponseData
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordRequestCodeRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string TempToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
