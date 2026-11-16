using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Dialogs;

/// <summary>
/// Đặt vé mới tại quầy (formDatVeMoi) với sơ đồ chọn ghế trực quan + thông tin hành khách (Kênh Trực tiếp).
/// </summary>
public class formDatVeMoi : Form
{
    private readonly int? _presetTripId;
    private readonly int? _presetSeatId;

    private readonly BusChassisControl _chassis;
    private readonly ComboBox _cbTrip;
    private readonly ComboBox _cbSeat;
    private readonly ComboBox _cbPickup;
    private readonly ComboBox _cbDropoff;
    private readonly ModernTextBox _txtName;
    private readonly ModernTextBox _txtPhone;
    private readonly ModernTextBox _txtEmail;
    private readonly ModernButton _btnCreate;
    private bool _suppressTripEvent;
    private bool _suppressSeatEvent;

    public formDatVeMoi(int? presetTripId = null, int? presetSeatId = null)
    {
        _presetTripId = presetTripId;
        _presetSeatId = presetSeatId;

        Text = "Đặt vé mới tại quầy (Trực tiếp)";
        Size = new Size(1060, 720);
        MinimumSize = new Size(980, 660);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(18);
        DoubleBuffered = true;

        var rightCard = new RoundedCardPanel
        {
            Dock = DockStyle.Right,
            Width = 380,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder
        };

        rightCard.Controls.Add(new Label
        {
            Text = "Thông tin xuất vé tại quầy",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(20, 16)
        });

        int y = 56;
        rightCard.Controls.Add(UiHelper.CreateFieldLabel("CHỌN CHUYẾN SẮP KHỞI HÀNH", 20, y));
        _cbTrip = UiHelper.CreateStyledComboBox(20, y + 20, 336);
        _cbTrip.SelectedIndexChanged += async (_, _) =>
        {
            if (!_suppressTripEvent && _cbTrip.SelectedItem is TripSelectItem item)
                await LoadTripSeatsAsync(item.TripId);
        };
        rightCard.Controls.Add(_cbTrip);

        y += 62;
        rightCard.Controls.Add(UiHelper.CreateFieldLabel("GHẾ ĐANG CHỌN (HOẶC NHẤN TRÊN SƠ ĐỒ)", 20, y));
        _cbSeat = UiHelper.CreateStyledComboBox(20, y + 20, 336);
        _cbSeat.SelectedIndexChanged += (_, _) =>
        {
            if (!_suppressSeatEvent && _cbSeat.SelectedItem is SeatSelectItem item)
            {
                _suppressSeatEvent = true;
                _chassis.SetSelectedSeat(item.SeatId);
                _suppressSeatEvent = false;
            }
        };
        rightCard.Controls.Add(_cbSeat);

        y += 62;
        rightCard.Controls.Add(UiHelper.CreateFieldLabel("ĐIỂM ĐÓN", 20, y));
        _cbPickup = UiHelper.CreateStyledComboBox(20, y + 20, 336);
        rightCard.Controls.Add(_cbPickup);

        y += 62;
        rightCard.Controls.Add(UiHelper.CreateFieldLabel("ĐIỂM TRẢ", 20, y));
        _cbDropoff = UiHelper.CreateStyledComboBox(20, y + 20, 336);
        rightCard.Controls.Add(_cbDropoff);

        y += 62;
        rightCard.Controls.Add(UiHelper.CreateFieldLabel("HỌ TÊN HÀNH KHÁCH", 20, y));
        _txtName = new ModernTextBox { Location = new Point(20, y + 20), Size = new Size(336, 38), Icon = IconChar.User };
        rightCard.Controls.Add(_txtName);

        y += 66;
        rightCard.Controls.Add(UiHelper.CreateFieldLabel("SỐ ĐIỆN THOẠI", 20, y));
        _txtPhone = new ModernTextBox { Location = new Point(20, y + 20), Size = new Size(336, 38), Icon = IconChar.Headset };
        rightCard.Controls.Add(_txtPhone);

        y += 66;
        rightCard.Controls.Add(UiHelper.CreateFieldLabel("EMAIL (TÙY CHỌN)", 20, y));
        _txtEmail = new ModernTextBox { Location = new Point(20, y + 20), Size = new Size(336, 38), Icon = IconChar.IdCard };
        rightCard.Controls.Add(_txtEmail);

        _btnCreate = new ModernButton
        {
            Text = "Xuất vé tại quầy (Tiền mặt)",
            Icon = IconChar.TicketAlt,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(20, y + 76),
            Size = new Size(336, 44)
        };
        _btnCreate.Click += async (_, _) => await CreateTicketAsync();
        rightCard.Controls.Add(_btnCreate);

        var leftCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(12)
        };
        _chassis = new BusChassisControl { Dock = DockStyle.Fill };
        _chassis.SeatSelected += (_, seat) =>
        {
            if (_suppressSeatEvent) return;
            _suppressSeatEvent = true;
            _cbSeat.SelectedValue = seat.SeatId;
            _suppressSeatEvent = false;
        };
        leftCard.Controls.Add(_chassis);

        var spacer = new Panel { Dock = DockStyle.Right, Width = 14, BackColor = AppTheme.CanvasBg };
        Controls.Add(leftCard);
        Controls.Add(spacer);
        Controls.Add(rightCard);

        Load += async (_, _) => await LoadTripsAsync();
    }

    private async Task LoadTripsAsync()
    {
        var res = await ApiService.GetAsync<PagedResult<AdminTripDto>>(
            $"api/admin/trips?status={Uri.EscapeDataString(TripStatuses.Upcoming)}&page=1&pageSize=100");
        if (!res.Success || res.Data == null)
        {
            UiHelper.ShowError(res.Message, this);
            return;
        }

        var items = res.Data.Items
            .Select(t => new TripSelectItem(t.TripId, $"{t.TripCode} | {t.RouteName} ({t.DepartureTime:HH:mm dd/MM})"))
            .ToList();

        _suppressTripEvent = true;
        _cbTrip.DisplayMember = nameof(TripSelectItem.Display);
        _cbTrip.ValueMember = nameof(TripSelectItem.TripId);
        _cbTrip.DataSource = items;
        if (_presetTripId.HasValue)
        {
            _cbTrip.SelectedValue = _presetTripId.Value;
        }
        _suppressTripEvent = false;

        if (_cbTrip.SelectedItem is TripSelectItem selected)
            await LoadTripSeatsAsync(selected.TripId);
    }

    private async Task LoadTripSeatsAsync(int tripId)
    {
        var res = await ApiService.GetAsync<AdminTripSeatsResponseData>($"api/admin/trips/{tripId}/seats");
        if (!res.Success || res.Data == null)
            return;

        string vehicleGuess = res.Data.Seats.Count > 12 ? "Giường nằm" : "Limousine VIP";
        _chassis.BindSeats(res.Data.Seats, vehicleGuess, _presetSeatId);

        var availableSeats = res.Data.Seats
            .Where(s => s.Status == SeatStatuses.Available)
            .Select(s => new SeatSelectItem(s.SeatId, $"{s.SeatCode} ({AppTheme.FormatVnd(s.Price)})"))
            .ToList();

        _suppressSeatEvent = true;
        _cbSeat.DisplayMember = nameof(SeatSelectItem.Display);
        _cbSeat.ValueMember = nameof(SeatSelectItem.SeatId);
        _cbSeat.DataSource = availableSeats;

        if (_presetSeatId.HasValue && availableSeats.Any(s => s.SeatId == _presetSeatId.Value))
        {
            _cbSeat.SelectedValue = _presetSeatId.Value;
            _chassis.SetSelectedSeat(_presetSeatId.Value);
        }
        else if (availableSeats.Count > 0)
        {
            _chassis.SetSelectedSeat(availableSeats[0].SeatId);
        }
        _suppressSeatEvent = false;

        _cbPickup.DisplayMember = nameof(RoutePointDto.Name);
        _cbPickup.ValueMember = nameof(RoutePointDto.Id);
        _cbPickup.DataSource = res.Data.PickupPoints;

        _cbDropoff.DisplayMember = nameof(RoutePointDto.Name);
        _cbDropoff.ValueMember = nameof(RoutePointDto.Id);
        _cbDropoff.DataSource = res.Data.DropoffPoints;
    }

    private async Task CreateTicketAsync()
    {
        if (_cbTrip.SelectedItem is not TripSelectItem trip ||
            _cbSeat.SelectedItem is not SeatSelectItem seat ||
            _cbPickup.SelectedItem is not RoutePointDto pickup ||
            _cbDropoff.SelectedItem is not RoutePointDto dropoff)
        {
            UiHelper.ShowError("Vui lòng chọn đầy đủ chuyến xe, ghế trống và điểm đón/trả.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtName.Text) || string.IsNullOrWhiteSpace(_txtPhone.Text))
        {
            UiHelper.ShowError("Vui lòng nhập họ tên và số điện thoại hành khách.", this);
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnCreate, "Đang tạo vé...", async () =>
        {
            var res = await ApiService.PostAsync<CreateTicketResponseData>("api/admin/tickets", new CounterBookingRequest
            {
                TripId = trip.TripId,
                SeatId = seat.SeatId,
                PassengerName = _txtName.Text.Trim(),
                PassengerPhone = _txtPhone.Text.Trim(),
                PassengerEmail = string.IsNullOrWhiteSpace(_txtEmail.Text) ? null : _txtEmail.Text.Trim(),
                PickupPointId = pickup.Id,
                DropoffPointId = dropoff.Id
            });

            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, this);
                await LoadTripSeatsAsync(trip.TripId);
                return;
            }

            UiHelper.ShowInfo($"Xuất vé tại quầy thành công!\r\nMã vé: {res.Data.TicketCode}", this);
            DialogResult = DialogResult.OK;
            Close();
        }, this);
    }

    private sealed record TripSelectItem(int TripId, string Display);
    private sealed record SeatSelectItem(int SeatId, string Display);
}

public class frmCounterBooking : formDatVeMoi
{
    public frmCounterBooking(int? presetTripId = null, int? presetSeatId = null) : base(presetTripId, presetSeatId) { }
}
