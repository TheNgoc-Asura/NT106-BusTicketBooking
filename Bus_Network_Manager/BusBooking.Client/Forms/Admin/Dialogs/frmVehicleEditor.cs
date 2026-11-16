using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Dialogs;

/// <summary>
/// Form gốc Thêm / Sửa xe (dùng cho formThemXe & formSuaXe).
/// </summary>
public class frmVehicleEditor : Form
{
    private readonly int? _vehicleId;
    private readonly ModernTextBox _txtPlate;
    private readonly ComboBox _cbType;
    private readonly ComboBox _cbStatus;
    private readonly ModernButton _btnSave;

    public frmVehicleEditor(int? vehicleId = null)
    {
        _vehicleId = vehicleId;
        Text = vehicleId.HasValue ? "Cập nhật thông tin xe" : "Thêm xe mới";
        Size = new Size(460, vehicleId.HasValue ? 410 : 350);
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

        card.Controls.Add(UiHelper.CreateFieldLabel("BIỂN SỐ XE", 22, 60));
        _txtPlate = new ModernTextBox
        {
            Location = new Point(22, 82),
            Size = new Size(356, 40),
            Icon = IconChar.Bus,
            PlaceholderText = "vd: 51B-345.67"
        };
        card.Controls.Add(_txtPlate);

        card.Controls.Add(UiHelper.CreateFieldLabel("LOẠI XE", 22, 136));
        _cbType = UiHelper.CreateStyledComboBox(22, 158, 356);
        card.Controls.Add(_cbType);

        int nextY = 212;
        _cbStatus = UiHelper.CreateStyledComboBox(22, nextY + 22, 356);
        _cbStatus.Items.AddRange(new object[]
        {
            VehicleStatuses.Active,
            VehicleStatuses.Maintenance,
            VehicleStatuses.Inactive
        });
        _cbStatus.SelectedIndex = 0;

        if (vehicleId.HasValue)
        {
            card.Controls.Add(UiHelper.CreateFieldLabel("TRẠNG THÁI XE", 22, nextY));
            card.Controls.Add(_cbStatus);
            nextY += 68;
        }

        _btnSave = new ModernButton
        {
            Text = vehicleId.HasValue ? "Lưu thay đổi" : "Thêm xe",
            Icon = IconChar.Check,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(22, nextY + 6),
            Size = new Size(356, 42)
        };
        _btnSave.Click += async (_, _) => await SaveAsync();
        card.Controls.Add(_btnSave);

        Controls.Add(card);
        Load += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var typeRes = await ApiService.GetAsync<List<VehicleTypeDto>>("api/admin/vehicle-types");
        if (!typeRes.Success || typeRes.Data == null)
        {
            UiHelper.ShowError(typeRes.Message, this);
            return;
        }

        _cbType.DisplayMember = nameof(VehicleTypeDto.Name);
        _cbType.ValueMember = nameof(VehicleTypeDto.Id);
        _cbType.DataSource = typeRes.Data;

        if (_vehicleId.HasValue)
        {
            var detailRes = await ApiService.GetAsync<AdminVehicleDto>($"api/admin/vehicles/{_vehicleId.Value}");
            if (!detailRes.Success || detailRes.Data == null)
            {
                UiHelper.ShowError(detailRes.Message, this);
                return;
            }

            _txtPlate.Text = detailRes.Data.LicensePlate;
            _cbType.SelectedValue = detailRes.Data.VehicleTypeId;
            _cbStatus.SelectedItem = detailRes.Data.Status;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtPlate.Text) || _cbType.SelectedValue is not int typeId)
        {
            UiHelper.ShowError("Vui lòng nhập biển số và chọn loại xe.", this);
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnSave, "Đang lưu...", async () =>
        {
            if (_vehicleId.HasValue)
            {
                var res = await ApiService.PutAsync<object>($"api/admin/vehicles/{_vehicleId.Value}", new UpdateVehicleRequest
                {
                    LicensePlate = _txtPlate.Text.Trim(),
                    VehicleTypeId = typeId,
                    Status = _cbStatus.SelectedItem?.ToString()
                });
                if (!res.Success) { UiHelper.ShowError(res.Message, this); return; }
            }
            else
            {
                var res = await ApiService.PostAsync<object>("api/admin/vehicles", new CreateVehicleRequest
                {
                    LicensePlate = _txtPlate.Text.Trim(),
                    VehicleTypeId = typeId
                });
                if (!res.Success) { UiHelper.ShowError(res.Message, this); return; }
            }

            DialogResult = DialogResult.OK;
            Close();
        }, this);
    }
}

public class formThemXe : frmVehicleEditor
{
    public formThemXe() : base(null) { }
}

public class formSuaXe : frmVehicleEditor
{
    public formSuaXe(int vehicleId) : base(vehicleId) { }
}
