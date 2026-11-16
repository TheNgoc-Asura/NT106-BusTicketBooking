using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusTicket.Core.Models;

namespace BusTicket.Server.Services;

public class TokenService
{
    private readonly string _secretKey;

    public TokenService(IConfiguration config)
    {
        _secretKey = config["Jwt:SecretKey"] ?? "BusBookingSecretKey_NT106_R12_Group07_VerySecureKey2026!";
    }

    public string GenerateToken(User user, int expiryHours = 24)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var expiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours).ToUnixTimeSeconds();
        
        var payload = new
        {
            id = user.Id,
            email = user.Email,
            role = user.Role,
            fullName = user.FullName,
            exp = expiresAt
        };

        string headerJson = JsonSerializer.Serialize(header);
        string payloadJson = JsonSerializer.Serialize(payload);

        string encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        string encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        string unsignedToken = $"{encodedHeader}.{encodedPayload}";

        string signature = CreateSignature(unsignedToken, _secretKey);
        return $"{unsignedToken}.{signature}";
    }

    public bool ValidateToken(string? authHeader, out int userId, out string role, out string email)
    {
        userId = 0;
        role = string.Empty;
        email = string.Empty;

        if (string.IsNullOrWhiteSpace(authHeader))
            return false;

        string token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader.Substring(7).Trim()
            : authHeader.Trim();

        var parts = token.Split('.');
        if (parts.Length != 3)
            return false;

        string unsignedToken = $"{parts[0]}.{parts[1]}";
        string expectedSignature = CreateSignature(unsignedToken, _secretKey);
        if (parts[2] != expectedSignature)
            return false;

        try
        {
            byte[] payloadBytes = Base64UrlDecode(parts[1]);
            string payloadJson = Encoding.UTF8.GetString(payloadBytes);
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("exp", out var expProp))
            {
                long exp = expProp.GetInt64();
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                    return false;
            }

            if (root.TryGetProperty("id", out var idProp))
                userId = idProp.GetInt32();

            if (root.TryGetProperty("role", out var roleProp))
                role = roleProp.GetString() ?? string.Empty;

            if (root.TryGetProperty("email", out var emailProp))
                email = emailProp.GetString() ?? string.Empty;

            return userId > 0 && !string.IsNullOrEmpty(role);
        }
        catch
        {
            return false;
        }
    }

    public string GenerateTempToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string CreateSignature(string input, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string incoming = input.Replace('-', '+').Replace('_', '/');
        switch (incoming.Length % 4)
        {
            case 2: incoming += "=="; break;
            case 3: incoming += "="; break;
        }
        return Convert.FromBase64String(incoming);
    }
}
