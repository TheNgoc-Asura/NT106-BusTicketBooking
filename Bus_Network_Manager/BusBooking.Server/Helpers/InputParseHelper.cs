using System.Globalization;

namespace BusBooking.Server.Helpers;

public static class InputParseHelper
{
    /// <summary>
    /// Parse chuỗi ngày dạng yyyy-MM-dd (có thể tùy chọn fallback ISO date).
    /// </summary>
    public static bool TryParseDate(string? input, bool allowGeneralFallback, out DateTime parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string trimmed = input.Trim();
        if (DateTime.TryParseExact(
            trimmed,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsed))
        {
            parsed = parsed.Date;
            return true;
        }

        if (allowGeneralFallback && DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            parsed = parsed.Date;
            return true;
        }

        return false;
    }
}
