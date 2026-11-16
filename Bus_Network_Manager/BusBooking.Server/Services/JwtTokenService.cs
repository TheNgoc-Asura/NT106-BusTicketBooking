using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusBooking.Server.Entities;
using Microsoft.IdentityModel.Tokens;

namespace BusBooking.Server.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
}

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        string secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Chưa cấu hình Jwt:SecretKey.");
        string issuer = _configuration["Jwt:Issuer"] ?? "BusBooking.Server";
        string audience = _configuration["Jwt:Audience"] ?? "BusBooking.Client";
        int expiryHours = _configuration.GetValue("Jwt:ExpiryHours", 24);

        var expiresAtLocal = DateTime.Now.AddHours(expiryHours);
        var expiresAtUtc = DateTime.UtcNow.AddHours(expiryHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("fullName", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: creds
        );

        string token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        return (token, expiresAtLocal);
    }
}
