using System.Drawing.Drawing2D;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;

namespace BusBooking.Client.Controls;

/// <summary>
/// Sơ đồ cabin xe trực quan (vẽ khung xe + vô lăng tài xế, tự tách 2 tầng cho xe giường nằm 40 chỗ).
/// Dùng chung cho cả Khách hàng (frmSeatSelection) và Quản trị (AdminSeatsView).
/// </summary>
public class BusChassisControl : DoubleBufferedPanel
{
    private readonly Panel _pnlLegend;
    private readonly DoubleBufferedPanel _pnlDecksHost;
    private readonly List<SeatButton> _seatButtons = new();
    private int? _selectedSeatId;

    public event EventHandler<TripSeatDto>? SeatSelected;

    public bool AllowSelectingBooked { get; set; }

    public BusChassisControl()
    {
        BackColor = AppTheme.CardSurface;
        Padding = new Padding(12);

        _pnlLegend = BuildLegendBar();
        _pnlLegend.Dock = DockStyle.Top;

        _pnlDecksHost = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppTheme.CardSurface,
            Padding = new Padding(8, 12, 8, 8)
        };

        Controls.Add(_pnlDecksHost);
        Controls.Add(_pnlLegend);
    }

    public void BindSeats(IEnumerable<TripSeatDto> seats, string vehicleTypeName, int? selectedSeatId = null)
    {
        _selectedSeatId = selectedSeatId;
        _seatButtons.Clear();
        _pnlDecksHost.SuspendLayout();
        _pnlDecksHost.Controls.Clear();

        var seatList = seats.OrderBy(s => s.SeatCode, StringComparer.OrdinalIgnoreCase).ToList();
        bool isSleeper = seatList.Count >= 20
                         || vehicleTypeName.Contains("Giường", StringComparison.OrdinalIgnoreCase)
                         || seatList.Any(s => s.SeatCode.StartsWith("B", StringComparison.OrdinalIgnoreCase));

        var chassisCard = new ChassisFramePanel
        {
            Location = new Point(6, 6),
            Padding = new Padding(18, 56, 18, 18)
        };

        if (isSleeper)
        {
            var lowerSeats = seatList.Where(s => !s.SeatCode.StartsWith("B", StringComparison.OrdinalIgnoreCase)).ToList();
            var upperSeats = seatList.Where(s => s.SeatCode.StartsWith("B", StringComparison.OrdinalIgnoreCase)).ToList();

            if (upperSeats.Count == 0 && lowerSeats.Count > 12)
            {
                int half = lowerSeats.Count / 2;
                upperSeats = lowerSeats.Skip(half).ToList();
                lowerSeats = lowerSeats.Take(half).ToList();
            }

            var deckLower = BuildDeckContainer("TẦNG DƯỚI", lowerSeats, isSleeper: true, width: 266);
            deckLower.Location = new Point(16, 60);

            var deckUpper = BuildDeckContainer("TẦNG TRÊN", upperSeats, isSleeper: true, width: 266);
            deckUpper.Location = new Point(deckLower.Right + 14, 60);

            int maxDeckH = Math.Max(deckLower.Height, deckUpper.Height);
            deckLower.Height = maxDeckH;
            deckUpper.Height = maxDeckH;

            chassisCard.Controls.Add(deckLower);
            chassisCard.Controls.Add(deckUpper);
            chassisCard.Size = new Size(deckUpper.Right + 16, deckUpper.Bottom + 18);
        }
        else
        {
            var singleDeck = BuildDeckContainer("KHOANG VIP", seatList, isSleeper: false, width: 320);
            singleDeck.Location = new Point(120, 60);
            chassisCard.Controls.Add(singleDeck);
            chassisCard.Size = new Size(560, singleDeck.Bottom + 20);
        }

        _pnlDecksHost.Controls.Add(chassisCard);
        _pnlDecksHost.ResumeLayout();
    }

    public void SetSelectedSeat(int? seatId)
    {
        _selectedSeatId = seatId;
        foreach (var btn in _seatButtons)
        {
            btn.IsSelected = seatId.HasValue && btn.Seat.SeatId == seatId.Value;
        }
    }

    private RoundedCardPanel BuildDeckContainer(string title, List<TripSeatDto> seats, bool isSleeper, int width)
    {
        int cols = 3;
        int rows = Math.Max(1, (int)Math.Ceiling(seats.Count / (double)cols));
        int btnW = width >= 300 ? 84 : 74;
        int btnH = 54;
        int flowH = rows * (btnH + 8) + 12;

        var container = new RoundedCardPanel
        {
            CornerRadius = 12,
            SurfaceColor = AppTheme.SubtleBg,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(10, 8, 10, 8),
            Size = new Size(width, flowH + 46)
        };

        var lblHeader = new Label
        {
            Text = title,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.PrimaryDark,
            BackColor = AppTheme.SubtleBg,
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var flow = new DoubleBufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SubtleBg,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(6, 4, 2, 4)
        };

        foreach (var seat in seats)
        {
            var btn = new SeatButton(seat, isSleeper)
            {
                Size = new Size(btnW, btnH),
                Margin = new Padding(4),
                IsSelected = _selectedSeatId.HasValue && _selectedSeatId.Value == seat.SeatId
            };

            btn.Click += (_, _) =>
            {
                if (!btn.IsSelectable && !AllowSelectingBooked)
                    return;

                SetSelectedSeat(btn.Seat.SeatId);
                SeatSelected?.Invoke(this, btn.Seat);
            };

            _seatButtons.Add(btn);
            flow.Controls.Add(btn);
        }

        container.Controls.Add(flow);
        container.Controls.Add(lblHeader);
        return container;
    }

    private static Panel BuildLegendBar()
    {
        var pnl = new FlowLayoutPanel
        {
            Height = 36,
            BackColor = AppTheme.CardSurface,
            Padding = new Padding(4, 4, 4, 4)
        };

        pnl.Controls.Add(CreateLegendItem("Trống", Color.White, Color.FromArgb(203, 213, 225)));
        pnl.Controls.Add(CreateLegendItem("Đang chọn", AppTheme.Primary, AppTheme.PrimaryDark));
        pnl.Controls.Add(CreateLegendItem("Chờ xác nhận", AppTheme.WarningBg, Color.FromArgb(245, 158, 11)));
        pnl.Controls.Add(CreateLegendItem("Đã đặt", AppTheme.NeutralBg, Color.FromArgb(203, 213, 225)));
        return pnl;
    }

    private static Control CreateLegendItem(string label, Color fill, Color border)
    {
        var wrap = new Panel { Size = new Size(124, 26), BackColor = AppTheme.CardSurface, Margin = new Padding(0, 0, 8, 0) };
        var box = new RoundedCardPanel
        {
            Size = new Size(18, 18),
            CornerRadius = 5,
            SurfaceColor = fill,
            BorderColor = border,
            BorderThickness = 2,
            Location = new Point(2, 4),
            Padding = new Padding(0)
        };
        var lbl = new Label
        {
            Text = label,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(26, 4)
        };
        wrap.Controls.Add(box);
        wrap.Controls.Add(lbl);
        return wrap;
    }

    /// <summary>
    /// Khung thân xe mô phỏng có kính chắn gió và vô lăng tài xế ở góc trên trái.
    /// </summary>
    private sealed class ChassisFramePanel : RoundedCardPanel
    {
        public ChassisFramePanel()
        {
            CornerRadius = 22;
            SurfaceColor = Color.White;
            BorderColor = Color.FromArgb(203, 213, 225);
            BorderThickness = 2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Vạch ngăn buồng lái phía đầu xe
            using (var dashPen = new Pen(AppTheme.CardBorder, 1.5f) { DashStyle = DashStyle.Dash })
            {
                e.Graphics.DrawLine(dashPen, 20, 50, Width - 20, 50);
            }

            // Vẽ vô lăng tài xế vector sắc nét ở bên trái đầu xe
            var wheelRect = new Rectangle(24, 12, 28, 28);
            using (var wheelPen = new Pen(AppTheme.PrimaryDark, 3f))
            {
                e.Graphics.DrawEllipse(wheelPen, wheelRect);
                e.Graphics.DrawLine(wheelPen, wheelRect.Left + 4, wheelRect.Top + 14, wheelRect.Right - 4, wheelRect.Top + 14);
                e.Graphics.DrawLine(wheelPen, wheelRect.Left + 14, wheelRect.Top + 14, wheelRect.Left + 14, wheelRect.Bottom - 3);
            }

            TextRenderer.DrawText(
                e.Graphics,
                "Khoang tài xế",
                AppTheme.CaptionBoldFont,
                new Point(60, 17),
                AppTheme.TextMuted);

            TextRenderer.DrawText(
                e.Graphics,
                "Cửa lên xe →",
                AppTheme.CaptionFont,
                new Point(Math.Max(180, Width - 110), 17),
                AppTheme.TextMuted);
        }
    }
}
