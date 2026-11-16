using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Dialogs;

/// <summary>
/// Form gốc Thêm / Sửa chuyến xe (dùng cho formThemChuyen & formSuaChuyen).
/// </summary>
public class frmTripEditor : Form
{
    private readonly int? _tripId;
    private readonly ComboBox _cbRoute;
    private readonly ComboBox _cbVehicle;
    private readonly ComboBox _cbDriver;
    private readonly ComboBox _cbAssistant;
    private readonly DateTimePicker _dtpDeparture;
    private readonly ModernTextBox _txtPrice;
    private readonly ModernTextBox _txtSurcharge;
    private readonly ComboBox _cbStatus;
    private readonly ModernButton _btnSave;

    public frmTripEditor(int? tripId = null)
    {
        _tripId = tripId;
        Text = tripId.HasValue ? "Sửa thông tin chuyến xe" : "Thêm chuyến xe mới";
        Size = new Size(540, tripId.HasValue ? 590 : 540);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(20);
        DoubleBuffered = true;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder
        };

        card.Controls.Add(new Label
        {
            Text = Text,
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(22, 16)
        });

        int y = 58;
        card.Controls.Add(UiHelper.CreateFieldLabel("TUYẾN XE", 22, y));
        _cbRoute = UiHelper.CreateStyledComboBox(22, y + 20, 434);
        card.Controls.Add(_cbRoute);

        y += 64;
        card.Controls.Add(UiHelper.CreateFieldLabel("PHƯƠNG TIỆN (XE)", 22, y));
        _cbVehicle = UiHelper.CreateStyledComboBox(22, y + 20, 210);
        card.Controls.Add(_cbVehicle);

        card.Controls.Add(UiHelper.CreateFieldLabel("GIỜ KHỞI HÀNH", 246, y));
        _dtpDeparture = new DateTimePicker
        {
            Location = new Point(246, y + 20),
            Size = new Size(210, 34),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy HH:mm",
            Font = AppTheme.BodyFont,
            Value = DateTime.Today.AddDays(1).AddHours(8)
        };
        card.Controls.Add(_dtpDeparture);

        y += 64;
        card.Controls.Add(UiHelper.CreateFieldLabel("TÀI XẾ", 22, y));
        _cbDriver = UiHelper.CreateStyledComboBox(22, y + 20, 210);
        card.Controls.Add(_cbDriver);

        card.Controls.Add(UiHelper.CreateFieldLabel("PHỤ XE (TÙY CHỌN)", 246, y));
        _cbAssistant = UiHelper.CreateStyledComboBox(246, y + 20, 210);
        card.Controls.Add(_cbAssistant);

        y += 64;
        card.Controls.Add(UiHelper.CreateFieldLabel("GIÁ VÉ CƠ BẢN (VNĐ)", 22, y));
        _txtPrice = new ModernTextBox { Location = new Point(22, y + 20), Size = new Size(210, 40), Text = "280000" };
        card.Controls.Add(_txtPrice);

        card.Controls.Add(UiHelper.CreateFieldLabel("PHỤ THU (VNĐ)", 246, y));
        _txtSurcharge = new ModernTextBox { Location = new Point(246, y + 20), Size = new Size(210, 40), Text = "0" };
        card.Controls.Add(_txtSurcharge);

        y += 68;
        _cbStatus = UiHelper.CreateStyledComboBox(22, y + 20, 434);
        _cbStatus.Items.AddRange(new object[]
        {
            TripStatuses.Upcoming,
            TripStatuses.Running,
            TripStatuses.Completed,
            TripStatuses.Cancelled
        });
        _cbStatus.SelectedIndex = 0;

        if (tripId.HasValue)
        {
            card.Controls.Add(UiHelper.CreateFieldLabel("TRẠNG THÁI CHUYẾN", 22, y));
            card.Controls.Add(_cbStatus);
            y += 68;
        }

        _btnSave = new ModernButton
        {
            Text = tripId.HasValue ? "Lưu cập nhật" : "Thêm chuyến xe",
            Icon = IconChar.Check,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(22, y + 8),
            Size = new Size(434, 44)
        };
        _btnSave.Click += async (_, _) => await SaveAsync();
        card.Controls.Add(_btnSave);

        Controls.Add(card);
        Load += async (_, _) => await LoadOptionsAndDetailAsync();
    }

    private async Task LoadOptionsAndDetailAsync()
    {
        var optRes = await ApiService.GetAsync<TripFormOptionsDto>("api/admin/trips/form-options");
        if (!optRes.Success || optRes.Data == null)
        {
            UiHelper.ShowError(optRes.Message, this);
            return;
        }

        _cbRoute.DisplayMember = nameof(OptionItemDto.Name);
        _cbRoute.ValueMember = nameof(OptionItemDto.Id);
        _cbRoute.DataSource = optRes.Data.Routes;

        _cbVehicle.DisplayMember = nameof(VehicleOptionDto.LicensePlate);
        _cbVehicle.ValueMember = nameof(VehicleOptionDto.Id);
        _cbVehicle.DataSource = optRes.Data.Vehicles;

        _cbDriver.DisplayMember = nameof(StaffOptionDto.FullName);
        _cbDriver.ValueMember = nameof(StaffOptionDto.Id);
        _cbDriver.DataSource = optRes.Data.Drivers;

        var assistants = new List<StaffOptionDto> { new() { Id = 0, FullName = "-- Không chọn --" } };
        assistants.AddRange(optRes.Data.Assistants);
        _cbAssistant.DisplayMember = nameof(StaffOptionDto.FullName);
        _cbAssistant.ValueMember = nameof(StaffOptionDto.Id);
        _cbAssistant.DataSource = assistants;

        if (_tripId.HasValue)
        {
            var detailRes = await ApiService.GetAsync<AdminTripDetailDto>($"api/admin/trips/{_tripId.Value}");
            if (!detailRes.Success || detailRes.Data == null)
            {
                UiHelper.ShowError(detailRes.Message, this);
                return;
            }

            var d = detailRes.Data;
            _cbRoute.SelectedValue = d.RouteId;
            _cbVehicle.SelectedValue = d.VehicleId;
            _cbDriver.SelectedValue = d.DriverId;
            _cbAssistant.SelectedValue = d.AssistantId ?? 0;
            _dtpDeparture.Value = d.DepartureTime;
            _txtPrice.Text = ((long)d.Price).ToString();
            _txtSurcharge.Text = ((long)d.Surcharge).ToString();
            _cbStatus.SelectedItem = d.Status;
        }
    }

    private async Task SaveAsync()
    {
        if (_cbRoute.SelectedValue is not int routeId ||
            _cbVehicle.SelectedValue is not int vehicleId ||
            _cbDriver.SelectedValue is not int driverId)
        {
            UiHelper.ShowError("Vui lòng chọn đầy đủ Tuyến, Xe và Tài xế.", this);
            return;
        }

        if (!decimal.TryParse(_txtPrice.Text.Trim(), out decimal price) || price <= 0 ||
            !decimal.TryParse(_txtSurcharge.Text.Trim(), out decimal surcharge) || surcharge < 0)
        {
            UiHelper.ShowError("Giá vé hoặc phụ thu không hợp lệ.", this);
            return;
        }

        int? assistantId = (_cbAssistant.SelectedValue is int a && a > 0) ? a : null;

        await UiHelper.RunButtonActionAsync(_btnSave, "Đang lưu...", async () =>
        {
            if (_tripId.HasValue)
            {
                var res = await ApiService.PutAsync<object>($"api/admin/trips/{_tripId.Value}", new UpdateTripRequest
                {
                    RouteId = routeId,
                    VehicleId = vehicleId,
                    DriverId = driverId,
                    AssistantId = assistantId,
                    DepartureTime = _dtpDeparture.Value,
                    Price = price,
                    Surcharge = surcharge,
                    Status = _cbStatus.SelectedItem?.ToString() ?? TripStatuses.Upcoming
                });
                if (!res.Success) { UiHelper.ShowError(res.Message, this); return; }
            }
            else
            {
                var res = await ApiService.PostAsync<CreateTripResponseData>("api/admin/trips", new CreateTripRequest
                {
                    RouteId = routeId,
                    VehicleId = vehicleId,
                    DriverId = driverId,
                    AssistantId = assistantId,
                    DepartureTime = _dtpDeparture.Value,
                    Price = price,
                    Surcharge = surcharge
                });
                if (!res.Success) { UiHelper.ShowError(res.Message, this); return; }
            }

            DialogResult = DialogResult.OK;
            Close();
        }, this);
    }
}

public class formThemChuyen : frmTripEditor
{
    public formThemChuyen() : base(null) { }
}

public class formSuaChuyen : frmTripEditor
{
    public formSuaChuyen(int tripId) : base(tripId) { }
}
