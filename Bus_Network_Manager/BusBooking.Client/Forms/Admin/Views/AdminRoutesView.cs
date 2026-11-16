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
/// Quản lý tuyến xe (formQuanLyTuyenXe).
/// </summary>
public class formQuanLyTuyenXe : Form
{
    private readonly ModernTextBox _txtCode;
    private readonly ComboBox _cbStatus;
    private readonly ModernButton _btnSearch;
    private readonly ModernButton _btnAdd;
    private readonly ModernButton _btnEdit;
    private readonly DataGridView _dgv;
    private readonly ModernButton _btnPrev;
    private readonly ModernButton _btnNext;
    private readonly Label _lblPage;

    private int _page = 1;
    private int _totalPages = 1;

    public formQuanLyTuyenXe()
    {
        Text = "Quản lý tuyến xe";
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
            Width = 1020,
            BackColor = AppTheme.CardSurface
        };

        _txtCode = new ModernTextBox
        {
            Location = new Point(0, 8),
            Size = new Size(195, 40),
            Icon = IconChar.Search,
            PlaceholderText = "Mã tuyến (vd: T001)"
        };

        _cbStatus = UiHelper.CreateStyledComboBox(205, 11, 175);
        _cbStatus.Items.AddRange(new object[] { "Tất cả trạng thái", RouteStatuses.Active, RouteStatuses.Inactive });
        _cbStatus.SelectedIndex = 0;

        _btnSearch = new ModernButton
        {
            Text = "Lọc",
            Icon = IconChar.Search,
            IconSize = 13,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Location = new Point(390, 9),
            Size = new Size(88, 38)
        };
        _btnSearch.Click += async (_, _) => { _page = 1; await LoadAsync(); };

        _btnAdd = new ModernButton
        {
            Text = "Thêm tuyến",
            Icon = IconChar.Plus,
            IconSize = 13,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Size = new Size(140, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 140, 9)
        };
        _btnAdd.Click += async (_, _) =>
        {
            using var dlg = new formThemTuyen();
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK) await LoadAsync();
        };

        _btnEdit = new ModernButton
        {
            Text = "Sửa tuyến",
            Icon = IconChar.PenToSquare,
            IconSize = 13,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Size = new Size(130, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 280, 9)
        };
        _btnEdit.Click += async (_, _) => await EditSelectedAsync();

        toolbar.Controls.AddRange(new Control[] { _txtCode, _cbStatus, _btnSearch, _btnEdit, _btnAdd });

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
        await UiHelper.RunButtonActionAsync(_btnSearch, "Đang lọc...", async () =>
        {
            string url = $"api/admin/routes?page={_page}&pageSize=15";
            if (!string.IsNullOrWhiteSpace(_txtCode.Text))
                url += $"&routeCode={Uri.EscapeDataString(_txtCode.Text.Trim())}";
            if (_cbStatus.SelectedIndex > 0 && _cbStatus.SelectedItem is string st)
                url += $"&status={Uri.EscapeDataString(st)}";

            var res = await ApiService.GetAsync<PagedResult<AdminRouteDto>>(url);
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            _totalPages = Math.Max(1, res.Data.TotalPages);
            _lblPage.Text = $"Trang {res.Data.Page}/{_totalPages} ({res.Data.TotalItems} tuyến)";
            _btnPrev.Enabled = _page > 1;
            _btnNext.Enabled = _page < _totalPages;

            _dgv.DataSource = res.Data.Items;
            if (_dgv.Columns["RouteId"] != null) _dgv.Columns["RouteId"].Visible = false;
            if (_dgv.Columns["RouteCode"] != null) _dgv.Columns["RouteCode"].HeaderText = "Mã tuyến";
            if (_dgv.Columns["Origin"] != null) _dgv.Columns["Origin"].HeaderText = "Điểm đi";
            if (_dgv.Columns["Destination"] != null) _dgv.Columns["Destination"].HeaderText = "Điểm đến";
            if (_dgv.Columns["Status"] != null) _dgv.Columns["Status"].HeaderText = "Trạng thái";
            if (_dgv.Columns["UpcomingTrips"] != null) _dgv.Columns["UpcomingTrips"].HeaderText = "Chuyến sắp chạy";
        }, FindForm());
    }

    private async Task EditSelectedAsync()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not AdminRouteDto route)
        {
            UiHelper.ShowError("Vui lòng chọn một tuyến xe để sửa.", FindForm());
            return;
        }

        using var dlg = new formSuaTuyen(route.RouteId);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadAsync();
    }
}

public class AdminRoutesView : formQuanLyTuyenXe
{
}
