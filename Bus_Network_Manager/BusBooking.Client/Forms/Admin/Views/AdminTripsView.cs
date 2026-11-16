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
/// Quản lý chuyến xe (formQuanLyChuyenXe) với bộ lọc mã chuyến, tuyến, trạng thái và phân trang.
/// </summary>
public class formQuanLyChuyenXe : Form
{
    private readonly ModernTextBox _txtCode;
    private readonly ComboBox _cbRoute;
    private readonly ComboBox _cbStatus;
    private readonly ModernButton _btnSearch;
    private readonly ModernButton _btnAdd;
    private readonly ModernButton _btnEdit;
    private readonly ModernButton _btnViewSeats;
    private readonly DataGridView _dgv;
    private readonly ModernButton _btnPrev;
    private readonly ModernButton _btnNext;
    private readonly Label _lblPage;

    private int _page = 1;
    private int _totalPages = 1;

    public event Action<int>? ViewTripSeatsRequested;

    public formQuanLyChuyenXe()
    {
        Text = "Quản lý chuyến xe";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(18)
        };

        var toolbar = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Width = 1040,
            BackColor = AppTheme.CardSurface
        };

        _txtCode = new ModernTextBox
        {
            Location = new Point(0, 8),
            Size = new Size(135, 40),
            Icon = IconChar.Search,
            PlaceholderText = "Mã chuyến"
        };

        _cbRoute = UiHelper.CreateStyledComboBox(143, 11, 165);

        _cbStatus = UiHelper.CreateStyledComboBox(316, 11, 145);
        _cbStatus.Items.AddRange(new object[]
        {
            "Tất cả trạng thái",
            TripStatuses.Upcoming,
            TripStatuses.Running,
            TripStatuses.Completed,
            TripStatuses.Cancelled
        });
        _cbStatus.SelectedIndex = 0;

        _btnSearch = new ModernButton
        {
            Text = "Lọc",
            Icon = IconChar.Search,
            IconSize = 13,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Location = new Point(469, 9),
            Size = new Size(72, 38)
        };
        _btnSearch.Click += async (_, _) => { _page = 1; await LoadAsync(); };

        _btnAdd = new ModernButton
        {
            Text = "Thêm chuyến",
            Icon = IconChar.Plus,
            IconSize = 13,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Size = new Size(130, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 130, 9)
        };
        _btnAdd.Click += async (_, _) =>
        {
            using var dlg = new formThemChuyen();
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK) await LoadAsync();
        };

        _btnEdit = new ModernButton
        {
            Text = "Sửa chuyến",
            Icon = IconChar.PenToSquare,
            IconSize = 13,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Size = new Size(118, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 256, 9)
        };
        _btnEdit.Click += async (_, _) => await EditSelectedAsync();

        _btnViewSeats = new ModernButton
        {
            Text = "Sơ đồ ghế",
            Icon = IconChar.Chair,
            IconSize = 13,
            FillColor = AppTheme.SubtleBg,
            HoverColor = AppTheme.CardBorder,
            ForeColor = AppTheme.TextSecondary,
            Size = new Size(110, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 374, 9)
        };
        _btnViewSeats.Click += (_, _) =>
        {
            if (_dgv.CurrentRow?.DataBoundItem is AdminTripDto trip)
                ViewTripSeatsRequested?.Invoke(trip.TripId);
        };

        toolbar.Controls.AddRange(new Control[]
        {
            _txtCode, _cbRoute, _cbStatus, _btnSearch, _btnViewSeats, _btnEdit, _btnAdd
        });

        var pager = new DoubleBufferedPanel { Dock = DockStyle.Bottom, Height = 44, BackColor = AppTheme.CardSurface };
        _btnPrev = new ModernButton
        {
            Text = "Trước",
            Icon = IconChar.ChevronLeft,
            IconSize = 12,
            FillColor = AppTheme.SubtleBg,
            HoverColor = AppTheme.CardBorder,
            ForeColor = AppTheme.TextSecondary,
            Size = new Size(88, 32),
            Location = new Point(0, 8)
        };
        _btnNext = new ModernButton
        {
            Text = "Sau",
            FillColor = AppTheme.SubtleBg,
            HoverColor = AppTheme.CardBorder,
            ForeColor = AppTheme.TextSecondary,
            Size = new Size(76, 32),
            Location = new Point(96, 8)
        };
        _lblPage = new Label
        {
            Text = "Trang 1/1",
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(186, 15)
        };
        _btnPrev.Click += async (_, _) => { if (_page > 1) { _page--; await LoadAsync(); } };
        _btnNext.Click += async (_, _) => { if (_page < _totalPages) { _page++; await LoadAsync(); } };
        pager.Controls.AddRange(new Control[] { _btnPrev, _btnNext, _lblPage });

        _dgv = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgv, "Status");
        _dgv.DoubleClick += async (_, _) => await EditSelectedAsync();

        card.Controls.Add(_dgv);
        card.Controls.Add(pager);
        card.Controls.Add(toolbar);
        Controls.Add(card);
    }

    public async Task LoadAsync()
    {
        if (_cbRoute.Items.Count == 0)
        {
            var optRes = await ApiService.GetAsync<TripFormOptionsDto>("api/admin/trips/form-options");
            if (optRes.Success && optRes.Data != null)
            {
                var routeOpts = new List<OptionItemDto> { new() { Id = 0, Name = "Tất cả tuyến" } };
                routeOpts.AddRange(optRes.Data.Routes);
                _cbRoute.DisplayMember = nameof(OptionItemDto.Name);
                _cbRoute.ValueMember = nameof(OptionItemDto.Id);
                _cbRoute.DataSource = routeOpts;
            }
        }

        await UiHelper.RunButtonActionAsync(_btnSearch, "Đang lọc...", async () =>
        {
            string url = $"api/admin/trips?page={_page}&pageSize=15";
            if (!string.IsNullOrWhiteSpace(_txtCode.Text))
                url += $"&tripCode={Uri.EscapeDataString(_txtCode.Text.Trim())}";
            if (_cbRoute.SelectedValue is int rId && rId > 0)
                url += $"&routeId={rId}";
            if (_cbStatus.SelectedIndex > 0 && _cbStatus.SelectedItem is string st)
                url += $"&status={Uri.EscapeDataString(st)}";

            var res = await ApiService.GetAsync<PagedResult<AdminTripDto>>(url);
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            _totalPages = Math.Max(1, res.Data.TotalPages);
            _lblPage.Text = $"Trang {res.Data.Page}/{_totalPages} ({res.Data.TotalItems} chuyến)";
            _btnPrev.Enabled = _page > 1;
            _btnNext.Enabled = _page < _totalPages;

            _dgv.DataSource = res.Data.Items;
            if (_dgv.Columns["TripId"] != null) _dgv.Columns["TripId"].Visible = false;
            if (_dgv.Columns["TripCode"] != null) _dgv.Columns["TripCode"].HeaderText = "Mã chuyến";
            if (_dgv.Columns["RouteName"] != null) _dgv.Columns["RouteName"].HeaderText = "Tuyến xe";
            if (_dgv.Columns["DepartureTime"] != null)
            {
                _dgv.Columns["DepartureTime"].HeaderText = "Khởi hành";
                _dgv.Columns["DepartureTime"].DefaultCellStyle.Format = "HH:mm dd/MM/yyyy";
            }
            if (_dgv.Columns["VehiclePlate"] != null) _dgv.Columns["VehiclePlate"].HeaderText = "Biển số";
            if (_dgv.Columns["DriverName"] != null) _dgv.Columns["DriverName"].HeaderText = "Tài xế";
            if (_dgv.Columns["AssistantName"] != null) _dgv.Columns["AssistantName"].HeaderText = "Phụ xe";
            if (_dgv.Columns["Price"] != null)
            {
                _dgv.Columns["Price"].HeaderText = "Giá vé";
                _dgv.Columns["Price"].DefaultCellStyle.Format = "N0";
            }
            if (_dgv.Columns["BookedSeats"] != null) _dgv.Columns["BookedSeats"].HeaderText = "Đã đặt";
            if (_dgv.Columns["TotalSeats"] != null) _dgv.Columns["TotalSeats"].HeaderText = "Tổng ghế";
            if (_dgv.Columns["Status"] != null) _dgv.Columns["Status"].HeaderText = "Trạng thái";
        }, FindForm());
    }

    private async Task EditSelectedAsync()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not AdminTripDto trip)
        {
            UiHelper.ShowError("Vui lòng chọn một chuyến xe để sửa.", FindForm());
            return;
        }

        using var dlg = new formSuaChuyen(trip.TripId);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadAsync();
    }
}

public class AdminTripsView : formQuanLyChuyenXe
{
}
