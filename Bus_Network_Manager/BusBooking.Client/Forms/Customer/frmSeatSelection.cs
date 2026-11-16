using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Customer;

public class frmSeatSelection : Form
{
    private readonly TripSearchItemDto _trip;
    private readonly BusChassisControl _chassis;
    private readonly ComboBox _cbPickup;
    private readonly ComboBox _cbDropoff;
    private readonly ModernTextBox _txtName;
    private readonly ModernTextBox _txtPhone;
    private readonly ModernTextBox _txtEmail;
    private readonly Label _lblSelectedSeatSummary;
    private readonly Label _lblTotalFare;
    private readonly ModernButton _btnBook;

    private TripSeatDto? _selectedSeat;

    public bool BookingSucceeded { get; private set; }

    public frmSeatSelection(TripSearchItemDto trip)
    {
        _trip = trip;

        Text = $"Chọn ghế - {trip.RouteName} ({trip.TripCode})";
        var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 800);
        int initW = Math.Min(1120, Math.Max(980, workArea.Width - 60));
        int initH = Math.Min(760, Math.Max(580, workArea.Height - 40));
        Size = new Size(initW, initH);
        MinimumSize = new Size(960, Math.Min(580, Math.Max(520, workArea.Height - 60)));
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        DoubleBuffered = true;

        // Top trip summary header
        var pnlTop = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = AppTheme.CardSurface,
            Padding = new Padding(24, 12, 24, 12)
        };
        pnlTop.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.CardBorder);
            e.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
        };

        var lblRoute = new Label
        {
            Text = $"{trip.Origin}  →  {trip.Destination}   ({trip.TripCode})",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.PrimaryDark,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(24, 12)
        };

        var lblMeta = new Label
        {
            Text = $"Khởi hành: {trip.DepartureTime:HH:mm dd/MM/yyyy}   •   Loại xe: {trip.VehicleType}   •   Giá vé: {AppTheme.FormatVnd(trip.Price)}",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(24, 42)
        };

        var btnReload = new ModernButton
        {
            Text = "Làm mới ghế",
            Icon = IconChar.SyncAlt,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Size = new Size(136, 36),
            Location = new Point(Math.Max(300, initW - 176), 18)
        };
        btnReload.Click += async (_, _) => await LoadSeatsAsync();
        pnlTop.Resize += (_, _) =>
        {
            btnReload.Location = new Point(Math.Max(300, pnlTop.ClientSize.Width - btnReload.Width - 24), 18);
        };
        pnlTop.Controls.AddRange(new Control[] { lblRoute, lblMeta, btnReload });

        // Bottom fixed checkout bar
        var pnlBottom = new DoubleBufferedPanel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            BackColor = AppTheme.CardSurface,
            Padding = new Padding(24, 0, 0, 0)
        };
        pnlBottom.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.CardBorder);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        _lblSelectedSeatSummary = new Label
        {
            Text = "Vui lòng chọn 1 ghế trống trên sơ đồ bên trái",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.CardSurface,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var pnlBottomRight = new DoubleBufferedPanel
        {
            Dock = DockStyle.Right,
            Width = 480,
            BackColor = AppTheme.CardSurface
        };

        var lblTotalCaption = new Label
        {
            Text = "Tổng thanh toán:",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            TextAlign = ContentAlignment.MiddleRight,
            Size = new Size(200, 18),
            Location = new Point(0, 14)
        };

        _lblTotalFare = new Label
        {
            Text = "0 đ",
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            ForeColor = AppTheme.Accent,
            BackColor = AppTheme.CardSurface,
            TextAlign = ContentAlignment.MiddleRight,
            Size = new Size(200, 34),
            Location = new Point(0, 32)
        };

        _btnBook = new ModernButton
        {
            Text = "Đặt vé & Thanh toán MoMo",
            Icon = IconChar.Qrcode,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Size = new Size(246, 46),
            Location = new Point(214, 15),
            Enabled = false
        };
        _btnBook.Click += async (_, _) => await CreateBookingAsync();

        pnlBottomRight.Controls.AddRange(new Control[]
        {
            lblTotalCaption, _lblTotalFare, _btnBook
        });

        pnlBottom.Controls.Add(_lblSelectedSeatSummary);
        pnlBottom.Controls.Add(pnlBottomRight);

        // Main Split Body
        var pnlBody = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CanvasBg,
            Padding = new Padding(20, 16, 20, 16)
        };

        var pnlRightCard = new RoundedCardPanel
        {
            Dock = DockStyle.Right,
            Width = 385,
            AutoScroll = true,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(20)
        };

        var lblFormTitle = new Label
        {
            Text = "Thông tin hành khách & Điểm đón/trả",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(20, 18)
        };

        int y = 56;
        pnlRightCard.Controls.Add(lblFormTitle);
        pnlRightCard.Controls.Add(UiHelper.CreateFieldLabel("ĐIỂM ĐÓN", 20, y));
        _cbPickup = UiHelper.CreateStyledComboBox(20, y + 20, 342);
        pnlRightCard.Controls.Add(_cbPickup);

        y += 66;
        pnlRightCard.Controls.Add(UiHelper.CreateFieldLabel("ĐIỂM TRẢ", 20, y));
        _cbDropoff = UiHelper.CreateStyledComboBox(20, y + 20, 342);
        pnlRightCard.Controls.Add(_cbDropoff);

        y += 66;
        pnlRightCard.Controls.Add(UiHelper.CreateFieldLabel("HỌ TÊN HÀNH KHÁCH", 20, y));
        _txtName = new ModernTextBox
        {
            Location = new Point(20, y + 20),
            Size = new Size(342, 40),
            Icon = IconChar.User,
            Text = SessionManager.CurrentUser?.FullName ?? string.Empty
        };
        pnlRightCard.Controls.Add(_txtName);

        y += 72;
        pnlRightCard.Controls.Add(UiHelper.CreateFieldLabel("SỐ ĐIỆN THOẠI", 20, y));
        _txtPhone = new ModernTextBox
        {
            Location = new Point(20, y + 20),
            Size = new Size(342, 40),
            Icon = IconChar.Headset,
            PlaceholderText = "090xxxxxxx",
            Text = SessionManager.CurrentUser?.Phone ?? string.Empty
        };
        pnlRightCard.Controls.Add(_txtPhone);

        y += 72;
        pnlRightCard.Controls.Add(UiHelper.CreateFieldLabel("EMAIL NHẬN VÉ ĐIỆN TỬ", 20, y));
        _txtEmail = new ModernTextBox
        {
            Location = new Point(20, y + 20),
            Size = new Size(342, 40),
            Icon = IconChar.IdCard,
            Text = SessionManager.CurrentUser?.Email ?? string.Empty
        };
        pnlRightCard.Controls.Add(_txtEmail);

        var leftSeatCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Margin = new Padding(0, 0, 16, 0),
            Padding = new Padding(14)
        };

        var spacer = new Panel { Dock = DockStyle.Right, Width = 16, BackColor = AppTheme.CanvasBg };

        _chassis = new BusChassisControl { Dock = DockStyle.Fill };
        _chassis.SeatSelected += (_, seat) =>
        {
            _selectedSeat = seat;
            _lblSelectedSeatSummary.Text = $"Ghế đang chọn: {seat.SeatCode}";
            _lblSelectedSeatSummary.ForeColor = AppTheme.PrimaryDark;
            _lblTotalFare.Text = AppTheme.FormatVnd(seat.Price > 0 ? seat.Price : _trip.Price);
            _btnBook.Enabled = true;
        };

        leftSeatCard.Controls.Add(_chassis);

        pnlBody.Controls.Add(leftSeatCard);
        pnlBody.Controls.Add(spacer);
        pnlBody.Controls.Add(pnlRightCard);

        Controls.Add(pnlBody);
        Controls.Add(pnlBottom);
        Controls.Add(pnlTop);

        Load += async (_, _) => await LoadSeatsAsync();
    }

    private async Task LoadSeatsAsync()
    {
        var res = await ApiService.GetAsync<TripSeatsResponseData>($"api/trips/{_trip.TripId}/seats");
        if (!res.Success || res.Data == null)
        {
            UiHelper.ShowError(res.Message, this);
            return;
        }

        _selectedSeat = null;
        _lblSelectedSeatSummary.Text = "Vui lòng chọn 1 ghế trống trên sơ đồ bên trái";
        _lblSelectedSeatSummary.ForeColor = AppTheme.TextSecondary;
        _lblTotalFare.Text = "0 đ";
        _btnBook.Enabled = false;

        _chassis.BindSeats(res.Data.Seats, res.Data.VehicleType);

        _cbPickup.DisplayMember = nameof(RoutePointDto.Name);
        _cbPickup.ValueMember = nameof(RoutePointDto.Id);
        _cbPickup.DataSource = res.Data.PickupPoints;

        _cbDropoff.DisplayMember = nameof(RoutePointDto.Name);
        _cbDropoff.ValueMember = nameof(RoutePointDto.Id);
        _cbDropoff.DataSource = res.Data.DropoffPoints;
    }

    private async Task CreateBookingAsync()
    {
        if (_selectedSeat == null)
        {
            UiHelper.ShowError("Vui lòng chọn một ghế trống.", this);
            return;
        }

        if (_cbPickup.SelectedItem is not RoutePointDto pickup || _cbDropoff.SelectedItem is not RoutePointDto dropoff)
        {
            UiHelper.ShowError("Vui lòng chọn điểm đón và điểm trả.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtName.Text) || string.IsNullOrWhiteSpace(_txtPhone.Text))
        {
            UiHelper.ShowError("Vui lòng nhập họ tên và số điện thoại hành khách.", this);
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnBook, "Đang tạo vé MoMo...", async () =>
        {
            var res = await ApiService.PostAsync<BookingResponseData>("api/bookings", new CreateBookingRequest
            {
                TripId = _trip.TripId,
                SeatId = _selectedSeat.SeatId,
                PassengerName = _txtName.Text.Trim(),
                PassengerPhone = _txtPhone.Text.Trim(),
                PassengerEmail = string.IsNullOrWhiteSpace(_txtEmail.Text) ? null : _txtEmail.Text.Trim(),
                PickupPointId = pickup.Id,
                DropoffPointId = dropoff.Id
            });

            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, this);
                await LoadSeatsAsync();
                return;
            }

            using var payForm = new frmPaymentQR(res.Data, _trip, _selectedSeat.SeatCode, _txtName.Text.Trim());
            payForm.ShowDialog(this);
            BookingSucceeded = true;

            if (payForm.PaymentCompleted)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                await LoadSeatsAsync();
            }
        }, this);
    }
}
