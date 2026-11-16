using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Controls;

/// <summary>
/// Thẻ chuyến xe hiện đại thay thế DataGridView cho khách hàng (kiểu Vexere/FUTA).
/// </summary>
public class TripCardItem : RoundedCardPanel
{
    public TripSearchItemDto Trip { get; }
    public event EventHandler<TripSearchItemDto>? SelectClicked;

    private readonly ModernButton _btnSelect;

    public TripCardItem(TripSearchItemDto trip)
    {
        Trip = trip;
        Height = 128;
        Width = 880;
        CornerRadius = 12;
        SurfaceColor = AppTheme.CardSurface;
        BorderColor = AppTheme.CardBorder;
        Margin = new Padding(0, 0, 0, 12);
        Padding = new Padding(20, 16, 20, 16);

        // Giờ khởi hành -> Giờ đến dự kiến
        var lblDeparture = new Label
        {
            Text = trip.DepartureTime.ToString("HH:mm"),
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(22, 18)
        };

        var arrowIcon = new IconPictureBox
        {
            IconChar = IconChar.Bus,
            IconSize = 18,
            IconColor = AppTheme.Primary,
            BackColor = AppTheme.CardSurface,
            Size = new Size(24, 24),
            Location = new Point(116, 25),
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        var span = trip.EstimatedArrival - trip.DepartureTime;
        string durationText = span.TotalMinutes > 0
            ? $"{(int)span.TotalHours}g {span.Minutes:D2}p"
            : "Dự kiến";

        var lblDuration = new Label
        {
            Text = $"--- {durationText} --->",
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(144, 29)
        };

        var lblArrival = new Label
        {
            Text = trip.EstimatedArrival.ToString("HH:mm"),
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(252, 18)
        };

        // Tuyến đường + Mã chuyến + Loại xe
        var lblRoute = new Label
        {
            Text = $"{trip.Origin}  →  {trip.Destination}",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.PrimaryDark,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(22, 62)
        };

        var lblDate = new Label
        {
            Text = $"Ngày {trip.DepartureTime:dd/MM/yyyy}",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(22, 92)
        };

        var codeBadge = new BadgeLabel
        {
            Location = new Point(140, 88),
            Height = 24
        };
        codeBadge.SetColors(trip.TripCode, AppTheme.PrimarySoft, AppTheme.Primary);

        var vehicleIcon = new IconPictureBox
        {
            IconChar = trip.TotalSeats <= 12 ? IconChar.Couch : IconChar.Bed,
            IconSize = 15,
            IconColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            Size = new Size(20, 20),
            Location = new Point(230, 90),
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        var lblVehicle = new Label
        {
            Text = trip.VehicleType,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(254, 92)
        };

        // Cột phải: Số chỗ trống + Giá vé cam + Nút Chọn chỗ
        var seatsBadge = new BadgeLabel
        {
            Height = 26,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        if (trip.AvailableSeats > 0)
        {
            seatsBadge.SetColors($"Còn {trip.AvailableSeats} chỗ trống", AppTheme.SuccessBg, AppTheme.SuccessFg);
        }
        else
        {
            seatsBadge.SetColors("Hết chỗ", AppTheme.DangerBg, AppTheme.DangerFg);
        }

        var lblPrice = new Label
        {
            Text = AppTheme.FormatVnd(trip.Price),
            Font = AppTheme.PriceFont,
            ForeColor = AppTheme.Accent,
            BackColor = AppTheme.CardSurface,
            AutoSize = false,
            Size = new Size(190, 32),
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _btnSelect = new ModernButton
        {
            Text = trip.AvailableSeats > 0 ? "Chọn chỗ" : "Hết vé",
            Icon = IconChar.Chair,
            IconSize = 14,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Enabled = trip.AvailableSeats > 0,
            Size = new Size(138, 38),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnSelect.Click += (_, _) => SelectClicked?.Invoke(this, Trip);

        Controls.AddRange(new Control[]
        {
            lblDeparture, arrowIcon, lblDuration, lblArrival,
            lblRoute, lblDate, codeBadge, vehicleIcon, lblVehicle,
            seatsBadge, lblPrice, _btnSelect
        });

        void LayoutRight()
        {
            lblPrice.Location = new Point(Width - lblPrice.Width - 22, 16);
            seatsBadge.Location = new Point(Width - seatsBadge.Width - 22, 50);
            _btnSelect.Location = new Point(Width - _btnSelect.Width - 22, Height - _btnSelect.Height - 16);
        }

        Resize += (_, _) => LayoutRight();
        LayoutRight();

        // Hiệu ứng hover nhẹ trên toàn card
        void AttachHover(Control c)
        {
            c.MouseEnter += (_, _) => { BorderColor = AppTheme.Primary; BorderThickness = 1; };
            c.MouseLeave += (_, _) =>
            {
                if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
                {
                    BorderColor = AppTheme.CardBorder;
                }
            };
        }
        AttachHover(this);
        foreach (Control c in Controls) AttachHover(c);
    }
}
