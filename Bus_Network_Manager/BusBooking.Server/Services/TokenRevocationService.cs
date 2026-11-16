using Microsoft.Extensions.Caching.Memory;

namespace BusBooking.Server.Services;

public interface ITokenRevocationService
{
    void RevokeToken(string jti, DateTime expiresAtUtc);
    bool IsTokenRevoked(string jti);
}

public class MemoryTokenRevocationService : ITokenRevocationService
{
    private readonly IMemoryCache _cache;
    private const string Prefix = "revoked_jwt:";

    public MemoryTokenRevocationService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void RevokeToken(string jti, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return;

        var ttl = expiresAtUtc - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
            ttl = TimeSpan.FromMinutes(5);

        _cache.Set(Prefix + jti, true, ttl);
    }

    public bool IsTokenRevoked(string jti)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return false;

        return _cache.TryGetValue(Prefix + jti, out _);
    }
}
