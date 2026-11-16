using System.Drawing.Drawing2D;
using BusBooking.Client.Theme;
using FontAwesome.Sharp;

namespace BusBooking.Client.Controls;

public class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
    }
}

public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
{
    public DoubleBufferedFlowLayoutPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
    }
}

/// <summary>
/// Thẻ trắng bo góc 12px chuẩn SaaS kèm viền mảnh #E2E8F0 và tùy chọn dải màu nhấn bên trái.
/// </summary>
public class RoundedCardPanel : Panel
{
    private int _cornerRadius = AppTheme.CardRadius;
    private Color _surfaceColor = AppTheme.CardSurface;
    private Color _borderColor = AppTheme.CardBorder;
    private int _borderThickness = 1;
    private Color _leftAccentColor = Color.Transparent;

    public RoundedCardPanel()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Padding = new Padding(16);
    }

    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Math.Max(0, value); Invalidate(); }
    }

    public Color SurfaceColor
    {
        get => _surfaceColor;
        set { _surfaceColor = value; Invalidate(); }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    public int BorderThickness
    {
        get => _borderThickness;
        set { _borderThickness = Math.Max(0, value); Invalidate(); }
    }

    public Color LeftAccentColor
    {
        get => _leftAccentColor;
        set { _leftAccentColor = value; Invalidate(); }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));

        using var path = AppTheme.CreateRoundedRect(rect, _cornerRadius);
        using (var bgBrush = new SolidBrush(_surfaceColor))
        {
            e.Graphics.FillPath(bgBrush, path);
        }

        if (_leftAccentColor != Color.Transparent && _leftAccentColor.A > 0)
        {
            var prevClip = e.Graphics.Clip;
            e.Graphics.SetClip(path);
            using var accentBrush = new SolidBrush(_leftAccentColor);
            e.Graphics.FillRectangle(accentBrush, new Rectangle(0, 0, 5, Height));
            e.Graphics.Clip = prevClip;
        }

        if (_borderThickness > 0 && _borderColor.A > 0)
        {
            using var pen = new Pen(_borderColor, _borderThickness);
            e.Graphics.DrawPath(pen, path);
        }
    }
}

/// <summary>
/// Nút bấm bo góc hiện đại có hover mượt, hỗ trợ icon FontAwesome.Sharp và trạng thái Loading.
/// </summary>
public class ModernButton : Button
{
    private bool _hovered;
    private bool _pressed;
    private IconChar? _icon;
    private int _iconSize = 15;
    private Bitmap? _cachedIconBitmap;
    private Color _cachedIconColor;
    private string? _savedText;
    private int _badgeCount;

    public int CornerRadius { get; set; } = 10;
    public Color FillColor { get; set; } = AppTheme.Primary;
    public Color HoverColor { get; set; } = AppTheme.PrimaryHover;
    public Color BorderColor { get; set; } = Color.Transparent;
    public int BorderWidth { get; set; }

    public int BadgeCount
    {
        get => _badgeCount;
        set
        {
            int normalized = Math.Max(0, value);
            if (_badgeCount == normalized) return;
            _badgeCount = normalized;
            Invalidate();
        }
    }

    public IconChar? Icon
    {
        get => _icon;
        set
        {
            if (_icon == value) return;
            _icon = value;
            RebuildIconCache();
            Invalidate();
        }
    }

    public int IconSize
    {
        get => _iconSize;
        set
        {
            if (_iconSize == value) return;
            _iconSize = value;
            RebuildIconCache();
            Invalidate();
        }
    }

    public ModernButton()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        ForeColor = Color.White;
        Font = AppTheme.BodyBoldFont;
        Cursor = Cursors.Hand;
        Size = new Size(140, 40);
    }

    public void SetLoading(bool loading, string loadingText = "Đang xử lý...")
    {
        if (loading)
        {
            _savedText ??= Text;
            Text = loadingText;
            Enabled = false;
        }
        else
        {
            if (_savedText != null)
            {
                Text = _savedText;
                _savedText = null;
            }
            Enabled = true;
        }
    }

    private void RebuildIconCache()
    {
        _cachedIconBitmap?.Dispose();
        _cachedIconBitmap = null;

        if (_icon.HasValue)
        {
            _cachedIconColor = ForeColor;
            _cachedIconBitmap = _icon.Value.ToBitmap(ForeColor, _iconSize);
        }
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        if (_icon.HasValue && _cachedIconColor != ForeColor)
        {
            RebuildIconCache();
        }
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
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = AppTheme.CreateRoundedRect(rect, CornerRadius);

        Color activeFill = !Enabled
            ? Color.FromArgb(203, 213, 225)
            : _pressed
                ? ControlPaint.Dark(HoverColor, 0.08f)
                : _hovered
                    ? HoverColor
                    : FillColor;

        using (var fillBrush = new SolidBrush(activeFill))
        {
            pevent.Graphics.FillPath(fillBrush, path);
        }

        if (BorderWidth > 0 && BorderColor != Color.Transparent)
        {
            using var pen = new Pen(BorderColor, BorderWidth);
            pevent.Graphics.DrawPath(pen, path);
        }

        Color textColor = Enabled ? ForeColor : Color.FromArgb(100, 116, 139);
        if (_icon.HasValue && (_cachedIconBitmap == null || _cachedIconColor != textColor))
        {
            _cachedIconBitmap?.Dispose();
            _cachedIconColor = textColor;
            _cachedIconBitmap = _icon.Value.ToBitmap(textColor, _iconSize);
        }

        var textSize = TextRenderer.MeasureText(pevent.Graphics, Text, Font, Size.Empty, TextFormatFlags.NoPadding);
        int gap = (_cachedIconBitmap != null && !string.IsNullOrEmpty(Text)) ? 7 : 0;
        int totalWidth = (_cachedIconBitmap?.Width ?? 0) + gap + textSize.Width;

        bool isLeftAligned = TextAlign is ContentAlignment.MiddleLeft
            or ContentAlignment.TopLeft
            or ContentAlignment.BottomLeft;
        int startX = isLeftAligned
            ? 16
            : Math.Max(8, (Width - totalWidth) / 2);

        if (_cachedIconBitmap != null)
        {
            int iconY = (Height - _cachedIconBitmap.Height) / 2;
            pevent.Graphics.DrawImage(_cachedIconBitmap, startX, iconY);
            startX += _cachedIconBitmap.Width + gap;
        }

        var textRect = new Rectangle(startX, 0, Math.Max(1, Width - startX - (_badgeCount > 0 ? 44 : 6)), Height);
        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            textRect,
            textColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        if (_badgeCount > 0)
        {
            string badgeText = _badgeCount > 99 ? "99+" : _badgeCount.ToString();
            var bSize = TextRenderer.MeasureText(pevent.Graphics, badgeText, AppTheme.CaptionBoldFont, Size.Empty, TextFormatFlags.NoPadding);
            int pillW = Math.Max(22, bSize.Width + 12);
            int pillH = 20;
            var pillRect = new Rectangle(Width - pillW - 12, (Height - pillH) / 2, pillW, pillH);
            using var pillPath = AppTheme.CreateRoundedRect(pillRect, pillH / 2);
            using var pillBrush = new SolidBrush(AppTheme.Accent);
            pevent.Graphics.FillPath(pillBrush, pillPath);

            TextRenderer.DrawText(
                pevent.Graphics,
                badgeText,
                AppTheme.CaptionBoldFont,
                pillRect,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cachedIconBitmap?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Ô nhập liệu bo góc hiện đại kèm icon bên trái và hiệu ứng viền xanh khi focus.
/// </summary>
public class ModernTextBox : RoundedCardPanel
{
    private readonly TextBox _inner;
    private readonly IconPictureBox _iconBox;

    public ModernTextBox()
    {
        CornerRadius = 10;
        BorderColor = Color.FromArgb(203, 213, 225);
        SurfaceColor = Color.White;
        Padding = new Padding(10, 6, 10, 6);
        Size = new Size(260, 42);

        _iconBox = new IconPictureBox
        {
            Dock = DockStyle.Left,
            Width = 24,
            IconSize = 15,
            IconColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            Visible = false,
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        var innerHolder = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(2, 4, 2, 0)
        };

        _inner = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = Color.White
        };

        _inner.GotFocus += (_, _) =>
        {
            BorderColor = AppTheme.Primary;
            BorderThickness = 2;
        };
        _inner.LostFocus += (_, _) =>
        {
            BorderColor = Color.FromArgb(203, 213, 225);
            BorderThickness = 1;
        };
        _inner.TextChanged += (s, e) => TextChanged?.Invoke(this, e);
        _inner.KeyDown += (s, e) => KeyDown?.Invoke(this, e);

        innerHolder.Controls.Add(_inner);
        Controls.Add(innerHolder);
        Controls.Add(_iconBox);
    }

    public new event EventHandler? TextChanged;
    public new event KeyEventHandler? KeyDown;

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text
    {
        get => _inner.Text;
        set => _inner.Text = value ?? string.Empty;
    }

    public string PlaceholderText
    {
        get => _inner.PlaceholderText;
        set => _inner.PlaceholderText = value;
    }

    public bool UseSystemPasswordChar
    {
        get => _inner.UseSystemPasswordChar;
        set => _inner.UseSystemPasswordChar = value;
    }

    public bool ReadOnly
    {
        get => _inner.ReadOnly;
        set
        {
            _inner.ReadOnly = value;
            SurfaceColor = value ? AppTheme.SubtleBg : Color.White;
            _inner.BackColor = SurfaceColor;
            _iconBox.BackColor = SurfaceColor;
            if (_inner.Parent != null) _inner.Parent.BackColor = SurfaceColor;
        }
    }

    public IconChar? Icon
    {
        get => _iconBox.Visible ? _iconBox.IconChar : null;
        set
        {
            if (value.HasValue)
            {
                _iconBox.IconChar = value.Value;
                _iconBox.Visible = true;
            }
            else
            {
                _iconBox.Visible = false;
            }
        }
    }

    public void FocusInput() => _inner.Focus();
}

/// <summary>
/// Nhãn badge bo tròn theo màu ngữ nghĩa (trạng thái vé, ghế trống, kênh bán).
/// </summary>
public class BadgeLabel : Control
{
    private Color _badgeBackColor = AppTheme.InfoBg;

    public Color BadgeBackColor
    {
        get => _badgeBackColor;
        set { _badgeBackColor = value; Invalidate(); }
    }

    public BadgeLabel()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        ForeColor = AppTheme.InfoFg;
        Font = AppTheme.CaptionBoldFont;
        Size = new Size(110, 26);
    }

    public void SetSemanticStatus(string statusText)
    {
        Text = statusText;
        var (back, fore) = AppTheme.GetStatusColors(statusText);
        _badgeBackColor = back;
        ForeColor = fore;
        UpdatePreferredWidth();
        Invalidate();
    }

    public void SetColors(string text, Color back, Color fore)
    {
        Text = text;
        _badgeBackColor = back;
        ForeColor = fore;
        UpdatePreferredWidth();
        Invalidate();
    }

    private void UpdatePreferredWidth()
    {
        var size = TextRenderer.MeasureText(Text, Font);
        Width = Math.Max(76, size.Width + 20);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = AppTheme.CreateRoundedRect(rect, Height / 2);
        using var brush = new SolidBrush(_badgeBackColor);
        e.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            rect,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

/// <summary>
/// Thẻ KPI bo góc kèm dải màu nhấn bên trái và icon FontAwesome.Sharp.
/// </summary>
public class KpiCard : RoundedCardPanel
{
    private readonly Label _lblTitle;
    private readonly Label _lblValue;
    private readonly Label _lblSub;
    private readonly IconPictureBox _iconBox;
    private readonly RoundedCardPanel _iconWrap;

    public KpiCard(string title, string value, string subtitle, IconChar icon, Color accentColor)
    {
        LeftAccentColor = accentColor;
        Size = new Size(255, 112);
        Padding = new Padding(18, 14, 16, 12);

        var pastel = AppTheme.Tint(accentColor, 0.88);
        _iconWrap = new RoundedCardPanel
        {
            Size = new Size(42, 42),
            CornerRadius = 10,
            SurfaceColor = pastel,
            BorderThickness = 0,
            Padding = new Padding(0)
        };
        _iconBox = new IconPictureBox
        {
            Dock = DockStyle.Fill,
            IconChar = icon,
            IconColor = accentColor,
            IconSize = 20,
            BackColor = pastel,
            SizeMode = PictureBoxSizeMode.CenterImage
        };
        _iconWrap.Controls.Add(_iconBox);

        _lblTitle = new Label
        {
            Text = title,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(18, 14)
        };

        _lblValue = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = false,
            AutoEllipsis = true,
            Size = new Size(185, 34),
            Location = new Point(18, 39)
        };

        _lblSub = new Label
        {
            Text = subtitle,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = false,
            AutoEllipsis = true,
            Size = new Size(210, 22),
            Location = new Point(18, 79)
        };

        Controls.Add(_lblTitle);
        Controls.Add(_lblValue);
        Controls.Add(_lblSub);
        Controls.Add(_iconWrap);

        Resize += (_, _) => PositionIcon();
        PositionIcon();
    }

    private void PositionIcon()
    {
        _iconWrap.Location = new Point(Math.Max(140, Width - _iconWrap.Width - 16), 14);
        _lblValue.Width = Math.Max(100, _iconWrap.Left - 24);
        _lblSub.Width = Math.Max(120, Width - 34);
    }

    public void UpdateMetric(string value, string? subtitle = null)
    {
        _lblValue.Text = value;
        if (subtitle != null)
            _lblSub.Text = subtitle;
    }
}
