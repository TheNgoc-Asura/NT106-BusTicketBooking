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
/// Quản lý xe và Lịch bảo trì (formQuanLyXe).
/// </summary>
public class formQuanLyXe : Form
{
    private readonly ModernTextBox _txtPlate;
    private readonly ComboBox _cbStatus;
    private readonly ModernButton _btnSearch;
    private readonly ModernButton _btnAddVehicle;
    private readonly ModernButton _btnEditVehicle;
    private readonly ModernButton _btnAddMaintenance;

    private readonly DataGridView _dgvVehicles;
    private readonly DataGridView _dgvMaintenance;

    public formQuanLyXe()
    {
        Text = "Quản lý xe & Bảo trì";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = AppTheme.CanvasBg
        };
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 56f));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 44f));

        var vehicleCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(16)
        };

        var toolbar = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 54,
            Width = 1020,
            BackColor = AppTheme.CardSurface
        };

        _txtPlate = new ModernTextBox
        {
            Location = new Point(0, 6),
            Size = new Size(195, 40),
            Icon = IconChar.Search,
            PlaceholderText = "Biển số (vd: 51B)"
        };

        _cbStatus = UiHelper.CreateStyledComboBox(205, 9, 165);
        _cbStatus.Items.AddRange(new object[]
        {
            "Tất cả trạng thái",
            VehicleStatuses.Active,
            VehicleStatuses.Maintenance,
            VehicleStatuses.Inactive
        });
        _cbStatus.SelectedIndex = 0;

        _btnSearch = new ModernButton
        {
            Text = "Lọc xe",
            Icon = IconChar.Search,
            IconSize = 13,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Location = new Point(380, 7),
            Size = new Size(96, 38)
        };
        _btnSearch.Click += async (_, _) => await LoadAsync();

        _btnAddVehicle = new ModernButton
        {
            Text = "Thêm xe",
            Icon = IconChar.Plus,
            IconSize = 13,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Size = new Size(124, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 124, 7)
        };
        _btnAddVehicle.Click += async (_, _) =>
        {
            using var dlg = new formThemXe();
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK) await LoadAsync();
        };

        _btnEditVehicle = new ModernButton
        {
            Text = "Sửa xe",
            Icon = IconChar.PenToSquare,
            IconSize = 13,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Size = new Size(116, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 250, 7)
        };
        _btnEditVehicle.Click += async (_, _) => await EditSelectedAsync();

        toolbar.Controls.AddRange(new Control[] { _txtPlate, _cbStatus, _btnSearch, _btnEditVehicle, _btnAddVehicle });

        _dgvVehicles = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgvVehicles, "Status");
        _dgvVehicles.DoubleClick += async (_, _) => await EditSelectedAsync();

        vehicleCard.Controls.Add(_dgvVehicles);
        vehicleCard.Controls.Add(toolbar);

        var maintCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 0, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(16)
        };

        var maintHeader = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            Width = 1020,
            BackColor = AppTheme.CardSurface
        };
        maintHeader.Controls.Add(new Label
        {
            Text = "Lịch bảo trì phương tiện",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(0, 10)
        });

        _btnAddMaintenance = new ModernButton
        {
            Text = "Thêm lịch bảo trì",
            Icon = IconChar.Wrench,
            IconSize = 13,
            FillColor = AppTheme.WarningBg,
            HoverColor = Color.FromArgb(253, 230, 138),
            ForeColor = AppTheme.WarningFg,
            Size = new Size(170, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(maintHeader.Width - 170, 4)
        };
        _btnAddMaintenance.Click += async (_, _) =>
        {
            using var dlg = new formBaoTri();
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK) await LoadAsync();
        };
        maintHeader.Controls.Add(_btnAddMaintenance);

        _dgvMaintenance = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgvMaintenance);

        maintCard.Controls.Add(_dgvMaintenance);
        maintCard.Controls.Add(maintHeader);

        split.Controls.Add(vehicleCard, 0, 0);
        split.Controls.Add(maintCard, 0, 1);
        Controls.Add(split);
    }

    public async Task LoadAsync()
    {
        await UiHelper.RunButtonActionAsync(_btnSearch, "Đang tải...", async () =>
        {
            string url = "api/admin/vehicles?page=1&pageSize=50";
            if (!string.IsNullOrWhiteSpace(_txtPlate.Text))
                url += $"&licensePlate={Uri.EscapeDataString(_txtPlate.Text.Trim())}";
            if (_cbStatus.SelectedIndex > 0 && _cbStatus.SelectedItem is string st)
                url += $"&status={Uri.EscapeDataString(st)}";

            var vRes = await ApiService.GetAsync<PagedResult<AdminVehicleDto>>(url);
            if (vRes.Success && vRes.Data != null)
            {
                _dgvVehicles.DataSource = vRes.Data.Items;
                if (_dgvVehicles.Columns["VehicleId"] != null) _dgvVehicles.Columns["VehicleId"].Visible = false;
                if (_dgvVehicles.Columns["VehicleTypeId"] != null) _dgvVehicles.Columns["VehicleTypeId"].Visible = false;
                if (_dgvVehicles.Columns["LicensePlate"] != null) _dgvVehicles.Columns["LicensePlate"].HeaderText = "Biển số xe";
                if (_dgvVehicles.Columns["VehicleType"] != null) _dgvVehicles.Columns["VehicleType"].HeaderText = "Loại xe";
                if (_dgvVehicles.Columns["Status"] != null) _dgvVehicles.Columns["Status"].HeaderText = "Trạng thái";
            }

            var mRes = await ApiService.GetAsync<MaintenanceOverviewDto>("api/admin/maintenance");
            if (mRes.Success && mRes.Data != null)
            {
                _dgvMaintenance.DataSource = mRes.Data.Schedules;
                if (_dgvMaintenance.Columns["Id"] != null) _dgvMaintenance.Columns["Id"].Visible = false;
                if (_dgvMaintenance.Columns["VehicleId"] != null) _dgvMaintenance.Columns["VehicleId"].Visible = false;
                if (_dgvMaintenance.Columns["LicensePlate"] != null) _dgvMaintenance.Columns["LicensePlate"].HeaderText = "Biển số xe";
                if (_dgvMaintenance.Columns["StartDate"] != null) _dgvMaintenance.Columns["StartDate"].HeaderText = "Từ ngày";
                if (_dgvMaintenance.Columns["EndDate"] != null) _dgvMaintenance.Columns["EndDate"].HeaderText = "Đến ngày";
                if (_dgvMaintenance.Columns["Note"] != null) _dgvMaintenance.Columns["Note"].HeaderText = "Nội dung bảo trì";
            }
        }, FindForm());
    }

    private async Task EditSelectedAsync()
    {
        if (_dgvVehicles.CurrentRow?.DataBoundItem is not AdminVehicleDto vehicle)
        {
            UiHelper.ShowError("Vui lòng chọn một xe để sửa.", FindForm());
            return;
        }

        using var dlg = new formSuaXe(vehicle.VehicleId);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadAsync();
    }
}

public class AdminVehiclesView : formQuanLyXe
{
}
