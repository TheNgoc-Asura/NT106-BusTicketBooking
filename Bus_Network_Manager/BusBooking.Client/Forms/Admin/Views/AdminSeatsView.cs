using BusBooking.Client.Controls;
using BusBooking.Client.Forms.Admin.Dialogs;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Views;

/// <summary>
/// Quản lý sơ đồ ghế theo chuyến (formGhe) - Nhấn ghế trống để mở formDatVeMoi.
/// </summary>
public class formGhe : Form
{
    private readonly ComboBox _cbTrip;
    private readonly ModernButton _btnRefresh;
    private readonly BusChassisControl _chassis;
    private readonly Label _lblTripMeta;
    private bool _suppressTripSelect;
    private int _currentTripId;

    public formGhe()
    {
        Text = "Quản lý sơ đồ ghế";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        var topCard = new RoundedCardPanel
        {
            Dock = DockStyle.Top,
            Height = 84,
            Width = 1020,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(18, 12, 18, 12)
        };

        topCard.Controls.Add(UiHelper.CreateFieldLabel("CHỌN CHUYẾN XE ĐỂ XEM SƠ ĐỒ GHẾ (NHẤN GHẾ TRỐNG ĐỂ ĐẶT VÉ TẠI QUẦY)", 18, 12));
        _cbTrip = UiHelper.CreateStyledComboBox(18, 36, 460);
        _cbTrip.SelectedIndexChanged += async (_, _) =>
        {
            if (!_suppressTripSelect && _cbTrip.SelectedItem is TripSelectOption opt)
            {
                await LoadTripSeatsAsync(opt.TripId);
            }
        };

        _lblTripMeta = new Label
        {
            Text = string.Empty,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.PrimaryDark,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(494, 42)
        };

        _btnRefresh = new ModernButton
        {
            Text = "Làm mới",
            Icon = IconChar.SyncAlt,
            IconSize = 13,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Size = new Size(124, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(topCard.Width - 144, 32)
        };
        _btnRefresh.Click += async (_, _) => await LoadAsync(_currentTripId > 0 ? _currentTripId : null);

        topCard.Controls.AddRange(new Control[] { _cbTrip, _lblTripMeta, _btnRefresh });

        var chassisCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(14)
        };

        _chassis = new BusChassisControl { Dock = DockStyle.Fill };
        _chassis.SeatSelected += async (_, seat) =>
        {
            if (_currentTripId <= 0 || seat.Status != SeatStatuses.Available)
                return;

            using var dlg = new formDatVeMoi(_currentTripId, seat.SeatId);
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            {
                await LoadTripSeatsAsync(_currentTripId);
            }
        };

        chassisCard.Controls.Add(_chassis);

        var spacer = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = AppTheme.CanvasBg };
        Controls.Add(chassisCard);
        Controls.Add(spacer);
        Controls.Add(topCard);
    }

    public async Task LoadAsync(int? focusTripId = null)
    {
        var res = await ApiService.GetAsync<PagedResult<AdminTripDto>>("api/admin/trips?page=1&pageSize=100");
        if (!res.Success || res.Data == null)
        {
            UiHelper.ShowError(res.Message, FindForm());
            return;
        }

        var items = res.Data.Items
            .Select(t => new TripSelectOption(t.TripId, $"{t.TripCode} | {t.RouteName} ({t.DepartureTime:HH:mm dd/MM}) [{t.Status}]"))
            .ToList();

        _suppressTripSelect = true;
        _cbTrip.DisplayMember = nameof(TripSelectOption.Display);
        _cbTrip.ValueMember = nameof(TripSelectOption.TripId);
        _cbTrip.DataSource = items;

        int targetId = focusTripId ?? (_currentTripId > 0 ? _currentTripId : items.FirstOrDefault()?.TripId ?? 0);
        if (targetId > 0 && items.Any(i => i.TripId == targetId))
        {
            _cbTrip.SelectedValue = targetId;
        }
        _suppressTripSelect = false;

        if (_cbTrip.SelectedItem is TripSelectOption selected)
        {
            await LoadTripSeatsAsync(selected.TripId);
        }
    }

    private async Task LoadTripSeatsAsync(int tripId)
    {
        _currentTripId = tripId;
        var res = await ApiService.GetAsync<AdminTripSeatsResponseData>($"api/admin/trips/{tripId}/seats");
        if (!res.Success || res.Data == null)
        {
            UiHelper.ShowError(res.Message, FindForm());
            return;
        }

        int emptyCount = res.Data.Seats.Count(s => s.Status == SeatStatuses.Available);
        _lblTripMeta.Text = $"Trống {emptyCount}/{res.Data.Seats.Count} ghế • {res.Data.Trip.DepartureTime:HH:mm dd/MM/yyyy}";
        string vehicleGuess = res.Data.Seats.Count > 12 ? "Giường nằm" : "Limousine VIP";
        _chassis.BindSeats(res.Data.Seats, vehicleGuess);
    }

    private sealed record TripSelectOption(int TripId, string Display);
}

public class AdminSeatsView : formGhe
{
}
