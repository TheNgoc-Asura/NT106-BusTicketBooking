using BusBooking.Client.Controls;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;

namespace BusBooking.Client.Forms.Customer;

public class frmTicketDetail : Form
{
    public frmTicketDetail(AdminTicketDetailDto detail) : this(new TicketDetailDto
    {
        TicketId = detail.TicketId,
        TicketCode = detail.TicketCode,
        Status = detail.Status,
        Trip = detail.Trip,
        SeatCode = detail.SeatCode,
        Passenger = detail.Passenger,
        PickupPoint = detail.PickupPoint,
        DropoffPoint = detail.DropoffPoint,
        TotalAmount = detail.TotalAmount,
        PaymentMethod = $"Kênh: {detail.Channel}"
    })
    {
    }

    public frmTicketDetail(TicketDetailDto detail)
    {
        Text = $"Chi tiết vé điện tử - {detail.TicketCode}";
        Size = new Size(560, 585);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22);
        DoubleBuffered = true;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 14,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            LeftAccentColor = AppTheme.Primary,
            Padding = new Padding(24)
        };

        var lblCode = new Label
        {
            Text = $"VÉ XE: {detail.TicketCode}",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.PrimaryDark,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(26, 20)
        };

        var badge = new BadgeLabel { Location = new Point(340, 22) };
        badge.SetSemanticStatus(detail.Status);

        var picQr = new PictureBox
        {
            Location = new Point(346, 70),
            Size = new Size(124, 124),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White,
            Image = frmPaymentQR.GenerateQrBitmap($"TICKET:{detail.TicketCode}|SEAT:{detail.SeatCode}|TRIP:{detail.Trip.TripCode}")
        };

        int y = 68;
        void AddInfo(string label, string value, Color? valueColor = null)
        {
            var l1 = new Label
            {
                Text = label,
                Font = AppTheme.CaptionFont,
                ForeColor = AppTheme.TextMuted,
                BackColor = AppTheme.CardSurface,
                AutoSize = true,
                Location = new Point(26, y)
            };
            var l2 = new Label
            {
                Text = value,
                Font = AppTheme.BodyBoldFont,
                ForeColor = valueColor ?? AppTheme.TextPrimary,
                BackColor = AppTheme.CardSurface,
                Location = new Point(26, y + 18),
                Size = new Size(305, 22)
            };
            card.Controls.Add(l1);
            card.Controls.Add(l2);
            y += 46;
        }

        AddInfo("Tuyến xe & Mã chuyến", $"{detail.Trip.RouteName} ({detail.Trip.TripCode})");
        AddInfo("Giờ khởi hành", detail.Trip.DepartureTime.ToString("HH:mm - dd/MM/yyyy"));
        AddInfo("Biển số xe & Vị trí ghế", $"{detail.Trip.VehiclePlate}  •  Ghế {detail.SeatCode}", AppTheme.Primary);
        AddInfo("Hành khách", $"{detail.Passenger.Name} ({detail.Passenger.Phone})");
        AddInfo("Điểm đón", detail.PickupPoint);
        AddInfo("Điểm trả", detail.DropoffPoint);
        AddInfo("Tổng tiền & Thanh toán", $"{AppTheme.FormatVnd(detail.TotalAmount)} ({detail.PaymentMethod})", AppTheme.Accent);

        var btnClose = new ModernButton
        {
            Text = "Đóng",
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Location = new Point(350, 424),
            Size = new Size(120, 38)
        };
        btnClose.Click += (_, _) => Close();

        card.Controls.AddRange(new Control[] { lblCode, badge, picQr, btnClose });
        Controls.Add(card);
    }
}
