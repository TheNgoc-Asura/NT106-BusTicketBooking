using System.Drawing.Drawing2D;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using FontAwesome.Sharp;

namespace BusBooking.Client.Controls;

/// <summary>
/// Nút chọn ghế/giường bo góc với icon ghế và hiệu ứng hover/chọn mượt mà.
/// </summary>
public class SeatButton : Control
{
    private static readonly Dictionary<(bool IsSleeper, int State), Bitmap> IconCache = new();

    private bool _isSleeper;
    private bool _hovered;
    private bool _isSelected;

    public TripSeatDto Seat { get; private set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            Invalidate();
        }
    }

    public bool IsSelectable => Seat.Status == SeatStatuses.Available;

    public SeatButton(TripSeatDto seat, bool isSleeper)
    {
        Seat = seat;
        _isSleeper = isSleeper;
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Size = new Size(76, 62);
        Margin = new Padding(6);
        Font = AppTheme.CaptionBoldFont;
        Cursor = IsSelectable ? Cursors.Hand : Cursors.No;
    }

    public void UpdateSeat(TripSeatDto seat, bool isSleeper)
    {
        Seat = seat;
        _isSleeper = isSleeper;
        if (!IsSelectable) _isSelected = false;
        Cursor = IsSelectable ? Cursors.Hand : Cursors.No;
        Invalidate();
    }

    private static Bitmap GetStateBitmap(bool isSleeper, int state, Color color)
    {
        var key = (isSleeper, state);
        if (!IconCache.TryGetValue(key, out var bmp))
        {
            IconChar icon = isSleeper ? IconChar.Bed : IconChar.Chair;
            bmp = icon.ToBitmap(color, 18);
            IconCache[key] = bmp;
        }
        return bmp;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = AppTheme.CreateRoundedRect(rect, 10);

        Color fill;
        Color border;
        Color textColor;
        Bitmap? iconBmp;

        if (_isSelected && IsSelectable)
        {
            fill = AppTheme.Primary;
            border = AppTheme.PrimaryDark;
            textColor = Color.White;
            iconBmp = GetStateBitmap(_isSleeper, 1, Color.White);
        }
        else if (Seat.Status == SeatStatuses.Available)
        {
            fill = _hovered ? AppTheme.PrimarySoft : AppTheme.CardSurface;
            border = _hovered ? AppTheme.Primary : Color.FromArgb(203, 213, 225);
            textColor = AppTheme.TextPrimary;
            iconBmp = GetStateBitmap(_isSleeper, 0, AppTheme.Primary);
        }
        else if (Seat.Status == SeatStatuses.Pending)
        {
            fill = AppTheme.WarningBg;
            border = Color.FromArgb(245, 158, 11);
            textColor = AppTheme.WarningFg;
            iconBmp = GetStateBitmap(_isSleeper, 2, AppTheme.WarningFg);
        }
        else
        {
            fill = AppTheme.NeutralBg;
            border = Color.FromArgb(203, 213, 225);
            textColor = AppTheme.NeutralFg;
            iconBmp = GetStateBitmap(_isSleeper, 3, AppTheme.NeutralFg);
        }

        using (var fillBrush = new SolidBrush(fill))
        {
            e.Graphics.FillPath(fillBrush, path);
        }

        using (var pen = new Pen(border, _isSelected || _hovered ? 2f : 1.3f))
        {
            e.Graphics.DrawPath(pen, path);
        }

        // Gối đầu ghế trên cùng để tạo hình dáng ghế/giường trực quan
        var headrestRect = new Rectangle(Width / 2 - 14, 5, 28, 4);
        using (var headBrush = new SolidBrush(_isSelected ? Color.FromArgb(180, Color.White) : border))
        {
            e.Graphics.FillRectangle(headBrush, headrestRect);
        }

        if (iconBmp != null)
        {
            e.Graphics.DrawImage(iconBmp, (Width - iconBmp.Width) / 2, 13);
        }

        var textRect = new Rectangle(4, 34, Width - 8, 22);
        TextRenderer.DrawText(
            e.Graphics,
            Seat.SeatCode,
            Font,
            textRect,
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
