using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;

namespace BusBooking.Client.Controls;

/// <summary>
/// Bong bóng tin nhắn chat (tin của mình căn phải màu xanh, đối phương căn trái màu xám nhạt kèm thời gian).
/// </summary>
public class BubbleMessageControl : Panel
{
    public BubbleMessageControl(SupportMessageDto message, bool isOwnMessage, int containerWidth)
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Margin = new Padding(0, 0, 0, 8);

        int maxBubbleW = Math.Max(220, (int)(containerWidth * 0.68));
        var measured = TextRenderer.MeasureText(
            message.Content,
            AppTheme.BodyFont,
            new Size(maxBubbleW - 28, int.MaxValue),
            TextFormatFlags.WordBreak);

        int bubbleW = Math.Min(maxBubbleW, Math.Max(140, measured.Width + 30));
        int bubbleH = Math.Max(52, measured.Height + 36);

        Width = Math.Max(300, containerWidth - 28);
        Height = bubbleH + 4;

        var bubble = new RoundedCardPanel
        {
            CornerRadius = 14,
            BorderThickness = isOwnMessage ? 0 : 1,
            BorderColor = AppTheme.CardBorder,
            SurfaceColor = isOwnMessage ? AppTheme.Primary : Color.FromArgb(241, 245, 249),
            Size = new Size(bubbleW, bubbleH),
            Location = new Point(isOwnMessage ? Math.Max(8, Width - bubbleW - 8) : 8, 2),
            Padding = new Padding(12, 8, 12, 8)
        };

        var lblRoleTime = new Label
        {
            Text = $"{(isOwnMessage ? "Bạn" : message.SenderRole)} • {message.SentAt:HH:mm dd/MM}",
            Font = new Font("Segoe UI", 8f, FontStyle.Regular),
            ForeColor = isOwnMessage ? Color.FromArgb(219, 234, 254) : AppTheme.TextMuted,
            BackColor = bubble.SurfaceColor,
            AutoSize = true,
            Location = new Point(12, 7)
        };

        var lblContent = new Label
        {
            Text = message.Content,
            Font = AppTheme.BodyFont,
            ForeColor = isOwnMessage ? Color.White : AppTheme.TextPrimary,
            BackColor = bubble.SurfaceColor,
            Location = new Point(12, 25),
            Size = new Size(bubbleW - 24, bubbleH - 32)
        };

        bubble.Controls.Add(lblRoleTime);
        bubble.Controls.Add(lblContent);
        Controls.Add(bubble);
    }
}
