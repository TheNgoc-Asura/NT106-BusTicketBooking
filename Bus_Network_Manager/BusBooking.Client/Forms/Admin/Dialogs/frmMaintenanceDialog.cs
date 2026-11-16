using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Models;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Dialogs;

/// <summary>
/// Form Thêm lịch bảo trì xe (formBaoTri).
/// </summary>
public class formBaoTri : Form
{
    private readonly ComboBox _cbVehicle;
    private readonly DateTimePicker _dtpStart;
    private readonly DateTimePicker _dtpEnd;
    private readonly ModernTextBox _txtNote;
    private readonly ModernButton _btnSave;

    public formBaoTri()
    {
        Text = "Lên lịch bảo trì xe";
        Size = new Size(480, 440);
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
            Text = "Thêm lịch bảo trì xe",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(22, 16)
        });

        card.Controls.Add(UiHelper.CreateFieldLabel("CHỌN XE BẢO TRÌ", 22, 60));
        _cbVehicle = UiHelper.CreateStyledComboBox(22, 82, 376);

        card.Controls.Add(UiHelper.CreateFieldLabel("NGÀY BẮT ĐẦU", 22, 134));
        _dtpStart = new DateTimePicker
        {
            Location = new Point(22, 156),
            Size = new Size(180, 34),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = AppTheme.BodyFont,
            Value = DateTime.Today
        };

        card.Controls.Add(UiHelper.CreateFieldLabel("NGÀY KẾT THÚC", 218, 134));
        _dtpEnd = new DateTimePicker
        {
            Location = new Point(218, 156),
            Size = new Size(180, 34),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = AppTheme.BodyFont,
            Value = DateTime.Today.AddDays(2)
        };

        card.Controls.Add(UiHelper.CreateFieldLabel("GHI CHÚ NỘI DUNG BẢO TRÌ", 22, 206));
        _txtNote = new ModernTextBox
        {
            Location = new Point(22, 228),
            Size = new Size(376, 42),
            Icon = IconChar.Wrench,
            PlaceholderText = "vd: Bảo dưỡng định kỳ phanh, lốp, điều hòa"
        };

        _btnSave = new ModernButton
        {
            Text = "Lưu lịch bảo trì",
            Icon = IconChar.Check,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(22, 294),
            Size = new Size(376, 44)
        };
        _btnSave.Click += async (_, _) => await SaveAsync();

        card.Controls.AddRange(new Control[] { _cbVehicle, _dtpStart, _dtpEnd, _txtNote, _btnSave });
        Controls.Add(card);

        Load += async (_, _) => await LoadVehiclesAsync();
    }

    private async Task LoadVehiclesAsync()
    {
        var res = await ApiService.GetAsync<PagedResult<AdminVehicleDto>>("api/admin/vehicles?page=1&pageSize=100");
        if (!res.Success || res.Data == null)
        {
            UiHelper.ShowError(res.Message, this);
            return;
        }

        _cbVehicle.DisplayMember = nameof(AdminVehicleDto.LicensePlate);
        _cbVehicle.ValueMember = nameof(AdminVehicleDto.VehicleId);
        _cbVehicle.DataSource = res.Data.Items;
    }

    private async Task SaveAsync()
    {
        if (_cbVehicle.SelectedValue is not int vehicleId)
        {
            UiHelper.ShowError("Vui lòng chọn xe cần bảo trì.", this);
            return;
        }

        if (_dtpEnd.Value.Date < _dtpStart.Value.Date)
        {
            UiHelper.ShowError("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.", this);
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnSave, "Đang lưu...", async () =>
        {
            var res = await ApiService.PostAsync<object>("api/admin/maintenance", new CreateMaintenanceRequest
            {
                VehicleId = vehicleId,
                StartDate = _dtpStart.Value.ToString("yyyy-MM-dd"),
                EndDate = _dtpEnd.Value.ToString("yyyy-MM-dd"),
                Note = string.IsNullOrWhiteSpace(_txtNote.Text) ? null : _txtNote.Text.Trim()
            });

            if (!res.Success)
            {
                UiHelper.ShowError(res.Message, this);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }, this);
    }
}

public class frmMaintenanceDialog : formBaoTri
{
}
