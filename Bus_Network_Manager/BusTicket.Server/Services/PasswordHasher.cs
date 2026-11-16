using System;
using System.Security.Cryptography;
using System.Text;

namespace BusTicket.Server.Services;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    public static bool VerifyPassword(string inputPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash))
            return false;

        string hashed = HashPassword(inputPassword);
        return string.Equals(hashed, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
