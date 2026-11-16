using BusBooking.Client.Controls;
using BusBooking.Client.Forms.Admin.Dialogs;
using BusBooking.Client.Forms.Customer;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Views;

/// <summary>
/// Quản lý vé (formQuanLyVe) - Tra cứu theo mã vé, chuyến, trạng thái, xem chi tiết, xác nhận vé & đặt vé tại quầy.
/// </summary>
public class formQuanLyVe : Form
{
    private readonly ModernTextBox _txtCode;
    private readonly ComboBox _cbTrip;
    private readonly ComboBox _cbStatus;
    private readonly ModernButton _btnSearch;
    private readonly ModernButton _btnDetail;
    private readonly ModernButton _btnConfirm;
    private readonly ModernButton _btnCounterBook;
    private readonly DataGridView _dgv;
    private readonly ModernButton _btnPrev;
    private readonly ModernButton _btnNext;
    private readonly Label _lblPage;

    private int _page = 1;
    private int _totalPages = 1;

    public formQuanLyVe()
    {
        Text = "Quản lý vé";
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
            PlaceholderText = "Mã vé"
        };

        _cbTrip = UiHelper.CreateStyledComboBox(143, 11, 160);

        _cbStatus = UiHelper.CreateStyledComboBox(311, 11, 145);
        _cbStatus.Items.AddRange(new object[]
        {
            "Tất cả trạng thái",
            TicketStatuses.PendingPayment,
            TicketStatuses.Paid,
            TicketStatuses.Confirmed,
            TicketStatuses.Cancelled
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
            Location = new Point(464, 9),
            Size = new Size(72, 38)
        };
        _btnSearch.Click += async (_, _) => { _page = 1; await LoadAsync(); };

        _btnCounterBook = new ModernButton
        {
            Text = "Đặt vé mới",
            Icon = IconChar.Plus,
            IconSize = 13,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Size = new Size(124, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 124, 9)
        };
        _btnCounterBook.Click += async (_, _) =>
        {
            using var dlg = new formDatVeMoi();
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK) await LoadAsync();
        };

        _btnConfirm = new ModernButton
        {
            Text = "Xác nhận vé",
            Icon = IconChar.CheckCircle,
            IconSize = 13,
            FillColor = AppTheme.SuccessBg,
            HoverColor = Color.FromArgb(187, 247, 208),
            ForeColor = AppTheme.SuccessFg,
            Size = new Size(120, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 252, 9)
        };
        _btnConfirm.Click += async (_, _) => await ConfirmSelectedAsync();

        _btnDetail = new ModernButton
        {
            Text = "Chi tiết",
            Icon = IconChar.TicketAlt,
            IconSize = 13,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Size = new Size(94, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 354, 9)
        };
        _btnDetail.Click += async (_, _) => await ShowDetailAsync();

        toolbar.Controls.AddRange(new Control[]
        {
            _txtCode, _cbTrip, _cbStatus, _btnSearch, _btnDetail, _btnConfirm, _btnCounterBook
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
        ModernGridHelper.ApplyModernStyle(_dgv, "Status", "Channel");
        _dgv.DoubleClick += async (_, _) => await ShowDetailAsync();

        card.Controls.Add(_dgv);
        card.Controls.Add(pager);
        card.Controls.Add(toolbar);
        Controls.Add(card);
    }

    public async Task LoadAsync()
    {
        if (_cbTrip.Items.Count == 0)
        {
            var tRes = await ApiService.GetAsync<PagedResult<AdminTripDto>>("api/admin/trips?page=1&pageSize=100");
            if (tRes.Success && tRes.Data != null)
            {
                var opts = new List<TripFilterItem> { new(0, "Tất cả chuyến") };
                opts.AddRange(tRes.Data.Items.Select(t => new TripFilterItem(t.TripId, $"{t.TripCode} - {t.RouteName}")));
                _cbTrip.DisplayMember = nameof(TripFilterItem.Label);
                _cbTrip.ValueMember = nameof(TripFilterItem.TripId);
                _cbTrip.DataSource = opts;
            }
        }

        await UiHelper.RunButtonActionAsync(_btnSearch, "Đang lọc...", async () =>
        {
            string url = $"api/admin/tickets?page={_page}&pageSize=15";
            if (!string.IsNullOrWhiteSpace(_txtCode.Text))
                url += $"&ticketCode={Uri.EscapeDataString(_txtCode.Text.Trim())}";
            if (_cbTrip.SelectedValue is int tId && tId > 0)
                url += $"&tripId={tId}";
            if (_cbStatus.SelectedIndex > 0 && _cbStatus.SelectedItem is string st)
                url += $"&status={Uri.EscapeDataString(st)}";

            var res = await ApiService.GetAsync<PagedResult<AdminTicketDto>>(url);
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            _totalPages = Math.Max(1, res.Data.TotalPages);
            _lblPage.Text = $"Trang {res.Data.Page}/{_totalPages} ({res.Data.TotalItems} vé)";
            _btnPrev.Enabled = _page > 1;
            _btnNext.Enabled = _page < _totalPages;

            _dgv.DataSource = res.Data.Items;
            if (_dgv.Columns["TicketId"] != null) _dgv.Columns["TicketId"].Visible = false;
            if (_dgv.Columns["TicketCode"] != null) _dgv.Columns["TicketCode"].HeaderText = "Mã vé";
            if (_dgv.Columns["TripCode"] != null) _dgv.Columns["TripCode"].HeaderText = "Chuyến";
            if (_dgv.Columns["PassengerName"] != null) _dgv.Columns["PassengerName"].HeaderText = "Hành khách";
            if (_dgv.Columns["SeatCode"] != null) _dgv.Columns["SeatCode"].HeaderText = "Ghế";
            if (_dgv.Columns["TotalAmount"] != null)
            {
                _dgv.Columns["TotalAmount"].HeaderText = "Tổng tiền";
                _dgv.Columns["TotalAmount"].DefaultCellStyle.Format = "N0";
            }
            if (_dgv.Columns["Channel"] != null) _dgv.Columns["Channel"].HeaderText = "Kênh bán";
            if (_dgv.Columns["Status"] != null) _dgv.Columns["Status"].HeaderText = "Trạng thái";
        }, FindForm());
    }

    private async Task ShowDetailAsync()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not AdminTicketDto ticket)
        {
            UiHelper.ShowError("Vui lòng chọn một vé.", FindForm());
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnDetail, "Đang mở...", async () =>
        {
            var res = await ApiService.GetAsync<AdminTicketDetailDto>($"api/admin/tickets/{ticket.TicketId}");
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            using var dlg = new frmTicketDetail(res.Data);
            dlg.ShowDialog(FindForm());
        }, FindForm());
    }

    private async Task ConfirmSelectedAsync()
    {
        if (_dgv.CurrentRow?.DataBoundItem is not AdminTicketDto ticket)
        {
            UiHelper.ShowError("Vui lòng chọn vé cần xác nhận.", FindForm());
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnConfirm, "Đang xử lý...", async () =>
        {
            var res = await ApiService.PutAsync<ConfirmTicketResponseData>($"api/admin/tickets/{ticket.TicketId}/confirm");
            if (!res.Success)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            UiHelper.ShowInfo($"Đã xác nhận vé {ticket.TicketCode} thành công.", FindForm());
            await LoadAsync();
        }, FindForm());
    }

    private sealed record TripFilterItem(int TripId, string Label);
}

public class AdminTicketsView : formQuanLyVe
{
}
