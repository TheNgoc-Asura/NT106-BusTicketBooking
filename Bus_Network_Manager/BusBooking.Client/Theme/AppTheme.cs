using System.Drawing.Drawing2D;
using BusBooking.Shared.Enums;

namespace BusBooking.Client.Theme;

public static class AppTheme
{
    // Bảng màu SaaS & E-commerce hiện đại
    public static readonly Color CanvasBg = ColorTranslator.FromHtml("#F1F5F9");
    public static readonly Color CardSurface = ColorTranslator.FromHtml("#FFFFFF");
    public static readonly Color CardBorder = ColorTranslator.FromHtml("#E2E8F0");
    public static readonly Color SubtleBg = ColorTranslator.FromHtml("#F8FAFC");

    public static readonly Color Primary = ColorTranslator.FromHtml("#1D4ED8");
    public static readonly Color PrimaryDark = ColorTranslator.FromHtml("#0B4F6C");
    public static readonly Color PrimaryHover = ColorTranslator.FromHtml("#1E40AF");
    public static readonly Color PrimarySoft = ColorTranslator.FromHtml("#EFF6FF");

    public static readonly Color Accent = ColorTranslator.FromHtml("#F97316");
    public static readonly Color AccentHover = ColorTranslator.FromHtml("#EA580C");
    public static readonly Color AccentSoft = ColorTranslator.FromHtml("#FFF7ED");

    public static readonly Color MomoBrand = ColorTranslator.FromHtml("#A50064");

    public static readonly Color SidebarBg = ColorTranslator.FromHtml("#0F172A");
    public static readonly Color SidebarHover = ColorTranslator.FromHtml("#1E293B");
    public static readonly Color SidebarActive = ColorTranslator.FromHtml("#1D4ED8");
    public static readonly Color SidebarMuted = ColorTranslator.FromHtml("#94A3B8");

    public static readonly Color TextPrimary = ColorTranslator.FromHtml("#0F172A");
    public static readonly Color TextSecondary = ColorTranslator.FromHtml("#334155");
    public static readonly Color TextMuted = ColorTranslator.FromHtml("#64748B");

    public static readonly Color SuccessBg = ColorTranslator.FromHtml("#DCFCE7");
    public static readonly Color SuccessFg = ColorTranslator.FromHtml("#15803D");

    public static readonly Color WarningBg = ColorTranslator.FromHtml("#FEF3C7");
    public static readonly Color WarningFg = ColorTranslator.FromHtml("#B45309");

    public static readonly Color DangerBg = ColorTranslator.FromHtml("#FEE2E2");
    public static readonly Color DangerFg = ColorTranslator.FromHtml("#B91C1C");

    public static readonly Color InfoBg = ColorTranslator.FromHtml("#DBEAFE");
    public static readonly Color InfoFg = ColorTranslator.FromHtml("#1D4ED8");

    public static readonly Color NeutralBg = ColorTranslator.FromHtml("#E2E8F0");
    public static readonly Color NeutralFg = ColorTranslator.FromHtml("#475569");

    // Hệ thống Typography Segoe UI rõ cấp bậc
    public static readonly Font TitleFont = new("Segoe UI", 18f, FontStyle.Bold);
    public static readonly Font SectionFont = new("Segoe UI", 14f, FontStyle.Bold);
    public static readonly Font CardTitleFont = new("Segoe UI Semibold", 12f, FontStyle.Regular);
    public static readonly Font PriceFont = new("Segoe UI", 15f, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 10f, FontStyle.Regular);
    public static readonly Font BodyBoldFont = new("Segoe UI Semibold", 10f, FontStyle.Regular);
    public static readonly Font CaptionFont = new("Segoe UI", 9f, FontStyle.Regular);
    public static readonly Font CaptionBoldFont = new("Segoe UI Semibold", 9f, FontStyle.Regular);

    public const int CardRadius = 12;

    /// <summary>
    /// Tạo GraphicsPath hình chữ nhật bo góc mượt cho thẻ/nút/badge.
    /// </summary>
    public static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return path;

        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 1)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);

        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Ánh xạ trạng thái / kênh sang cặp màu nền - chữ chuẩn ngữ nghĩa.
    /// </summary>
    public static (Color Back, Color Fore) GetStatusColors(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return (NeutralBg, NeutralFg);

        return status.Trim() switch
        {
            TicketStatuses.Paid or AccountStatuses.Active or TripStatuses.Completed
                or SupportTicketStatuses.Resolved => (SuccessBg, SuccessFg),

            TicketStatuses.Confirmed or TripStatuses.Running or BookingChannels.Online
                or SupportTicketStatuses.Processing => (InfoBg, InfoFg),

            TicketStatuses.PendingPayment or SeatStatuses.Pending or VehicleStatuses.Maintenance
                or SupportTicketStatuses.Pending or SupportTicketStatuses.WaitingCustomer => (WarningBg, WarningFg),

            TicketStatuses.Cancelled or TripStatuses.Cancelled or AccountStatuses.Locked
                or VehicleStatuses.Inactive or RouteStatuses.Inactive => (DangerBg, DangerFg),

            TripStatuses.Upcoming or SeatStatuses.Available => (SuccessBg, SuccessFg),
            BookingChannels.Direct => (AccentSoft, AccentHover),
            SeatStatuses.Booked => (NeutralBg, NeutralFg),
            _ => (NeutralBg, NeutralFg)
        };
    }

    public static string FormatVnd(decimal amount) => $"{amount:N0} đ";

    public static Color Tint(Color color, double ratioToWhite = 0.88)
    {
        int r = (int)Math.Clamp(color.R + (255 - color.R) * ratioToWhite, 0, 255);
        int g = (int)Math.Clamp(color.G + (255 - color.G) * ratioToWhite, 0, 255);
        int b = (int)Math.Clamp(color.B + (255 - color.B) * ratioToWhite, 0, 255);
        return Color.FromArgb(r, g, b);
    }
}
