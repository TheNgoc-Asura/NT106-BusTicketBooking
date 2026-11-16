using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BusBooking.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out int userId)
    {
        string? raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(raw, out userId) && userId > 0;
    }
}
