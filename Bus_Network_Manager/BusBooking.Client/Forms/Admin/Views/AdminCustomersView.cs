using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Models;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Views;

/// <summary>
/// Tra cứu khách hàng và lịch sử vé của khách (formTraCuuKhachHang).
/// </summary>
public class formTraCuuKhachHang : Form
{
    private readonly ModernTextBox _txtName;
    private readonly ModernTextBox _txtEmail;
    private readonly ModernTextBox _txtPhone;
    private readonly ModernButton _btnSearch;
    private readonly DataGridView _dgvCustomers;
    private readonly DataGridView _dgvCustomerTickets;
    private readonly Label _lblDetailHeader;
    private readonly ModernButton _btnPrev;
    private readonly ModernButton _btnNext;
    private readonly Label _lblPage;

    private int _page = 1;
    private int _totalPages = 1;

    public formTraCuuKhachHang()
    {
        Text = "Tra cứu khách hàng";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.CanvasBg
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

        var leftCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(16)
        };

        var searchBar = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Width = 540,
            BackColor = AppTheme.CardSurface
        };

        _txtName = new ModernTextBox { Location = new Point(0, 8), Size = new Size(130, 38), Icon = IconChar.User, PlaceholderText = "Họ tên" };
        _txtEmail = new ModernTextBox { Location = new Point(138, 8), Size = new Size(140, 38), Icon = IconChar.IdCard, PlaceholderText = "Email" };
        _txtPhone = new ModernTextBox { Location = new Point(286, 8), Size = new Size(116, 38), Icon = IconChar.Headset, PlaceholderText = "SĐT" };
        _btnSearch = new ModernButton
        {
            Text = "Tìm",
            Icon = IconChar.Search,
            IconSize = 13,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Location = new Point(410, 8),
            Size = new Size(76, 38)
        };
        _btnSearch.Click += async (_, _) => { _page = 1; await LoadAsync(); };
        searchBar.Controls.AddRange(new Control[] { _txtName, _txtEmail, _txtPhone, _btnSearch });

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

        _dgvCustomers = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgvCustomers);
        _dgvCustomers.SelectionChanged += async (_, _) =>
        {
            if (_dgvCustomers.CurrentRow?.DataBoundItem is AdminCustomerDto c)
                await LoadCustomerDetailAsync(c.CustomerId);
        };

        leftCard.Controls.Add(_dgvCustomers);
        leftCard.Controls.Add(pager);
        leftCard.Controls.Add(searchBar);

        var rightCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 0, 0, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(16)
        };

        _lblDetailHeader = new Label
        {
            Text = "Chọn khách hàng để xem lịch sử vé",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            Dock = DockStyle.Top,
            Height = 46
        };

        _dgvCustomerTickets = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgvCustomerTickets, "Status");

        rightCard.Controls.Add(_dgvCustomerTickets);
        rightCard.Controls.Add(_lblDetailHeader);

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightCard, 1, 0);
        Controls.Add(split);
    }

    public async Task LoadAsync()
    {
        await UiHelper.RunButtonActionAsync(_btnSearch, "Tìm...", async () =>
        {
            string url = $"api/admin/customers?page={_page}&pageSize=15";
            if (!string.IsNullOrWhiteSpace(_txtName.Text))
                url += $"&fullName={Uri.EscapeDataString(_txtName.Text.Trim())}";
            if (!string.IsNullOrWhiteSpace(_txtEmail.Text))
                url += $"&email={Uri.EscapeDataString(_txtEmail.Text.Trim())}";
            if (!string.IsNullOrWhiteSpace(_txtPhone.Text))
                url += $"&phone={Uri.EscapeDataString(_txtPhone.Text.Trim())}";

            var res = await ApiService.GetAsync<PagedResult<AdminCustomerDto>>(url);
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            _totalPages = Math.Max(1, res.Data.TotalPages);
            _lblPage.Text = $"Trang {res.Data.Page}/{_totalPages} ({res.Data.TotalItems} khách)";
            _btnPrev.Enabled = _page > 1;
            _btnNext.Enabled = _page < _totalPages;

            _dgvCustomers.DataSource = res.Data.Items;
            if (_dgvCustomers.Columns["CustomerId"] != null) _dgvCustomers.Columns["CustomerId"].Visible = false;
            if (_dgvCustomers.Columns["FullName"] != null) _dgvCustomers.Columns["FullName"].HeaderText = "Họ tên";
            if (_dgvCustomers.Columns["Email"] != null) _dgvCustomers.Columns["Email"].HeaderText = "Email";
            if (_dgvCustomers.Columns["Phone"] != null) _dgvCustomers.Columns["Phone"].HeaderText = "Số điện thoại";
            if (_dgvCustomers.Columns["TicketCount"] != null) _dgvCustomers.Columns["TicketCount"].HeaderText = "Số vé";

            if (res.Data.Items.Count > 0)
            {
                await LoadCustomerDetailAsync(res.Data.Items[0].CustomerId);
            }
            else
            {
                _lblDetailHeader.Text = "Không tìm thấy khách hàng";
                _dgvCustomerTickets.DataSource = new List<CustomerTicketSummaryDto>();
            }
        }, FindForm());
    }

    private async Task LoadCustomerDetailAsync(int customerId)
    {
        var res = await ApiService.GetAsync<AdminCustomerDetailDto>($"api/admin/customers/{customerId}");
        if (!res.Success || res.Data == null)
            return;

        var d = res.Data;
        _lblDetailHeader.Text = $"Vé của: {d.FullName}  •  {d.Phone ?? d.Email}";
        _dgvCustomerTickets.DataSource = d.Tickets;
        if (_dgvCustomerTickets.Columns["TicketId"] != null) _dgvCustomerTickets.Columns["TicketId"].Visible = false;
        if (_dgvCustomerTickets.Columns["TicketCode"] != null) _dgvCustomerTickets.Columns["TicketCode"].HeaderText = "Mã vé";
        if (_dgvCustomerTickets.Columns["RouteName"] != null) _dgvCustomerTickets.Columns["RouteName"].HeaderText = "Tuyến xe";
        if (_dgvCustomerTickets.Columns["DepartureTime"] != null)
        {
            _dgvCustomerTickets.Columns["DepartureTime"].HeaderText = "Khởi hành";
            _dgvCustomerTickets.Columns["DepartureTime"].DefaultCellStyle.Format = "HH:mm dd/MM/yyyy";
        }
        if (_dgvCustomerTickets.Columns["Status"] != null) _dgvCustomerTickets.Columns["Status"].HeaderText = "Trạng thái";
    }
}

public class AdminCustomersView : formTraCuuKhachHang
{
}
