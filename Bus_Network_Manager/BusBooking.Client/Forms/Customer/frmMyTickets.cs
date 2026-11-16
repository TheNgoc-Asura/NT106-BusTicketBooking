using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Customer;

/// <summary>
/// Form Vé của tôi - Xem danh sách vé đã đặt và mở chi tiết vé.
/// </summary>
public class frmMyTickets : Form
{
    private readonly ComboBox _cbStatus;
    private readonly ModernButton _btnRefresh;
    private readonly ModernButton _btnDetail;
    private readonly DataGridView _dgvTickets;

    public frmMyTickets()
    {
        Text = "Vé của tôi";
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
            Padding = new Padding(20)
        };

        var header = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Width = 900,
            BackColor = AppTheme.CardSurface
        };

        var lblTitle = new Label
        {
            Text = "Danh sách vé đã đặt",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(0, 10)
        };

        _cbStatus = UiHelper.CreateStyledComboBox(240, 10, 190);
        _cbStatus.Items.AddRange(new object[]
        {
            "Tất cả trạng thái",
            TicketStatuses.PendingPayment,
            TicketStatuses.Paid,
            TicketStatuses.Confirmed,
            TicketStatuses.Cancelled
        });
        _cbStatus.SelectedIndex = 0;
        _cbStatus.SelectedIndexChanged += async (_, _) => await LoadAsync();

        _btnRefresh = new ModernButton
        {
            Text = "Làm mới",
            Icon = IconChar.SyncAlt,
            IconSize = 14,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Size = new Size(118, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(header.Width - 284, 8)
        };
        _btnRefresh.Click += async (_, _) => await LoadAsync();

        _btnDetail = new ModernButton
        {
            Text = "Xem chi tiết vé",
            Icon = IconChar.TicketAlt,
            IconSize = 14,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Size = new Size(156, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(header.Width - 156, 8)
        };
        _btnDetail.Click += async (_, _) => await OpenSelectedDetailAsync();

        header.Controls.AddRange(new Control[] { lblTitle, _cbStatus, _btnRefresh, _btnDetail });

        _dgvTickets = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgvTickets, "Status");
        _dgvTickets.DoubleClick += async (_, _) => await OpenSelectedDetailAsync();

        card.Controls.Add(_dgvTickets);
        card.Controls.Add(header);
        Controls.Add(card);
    }

    public async Task LoadAsync()
    {
        await UiHelper.RunButtonActionAsync(_btnRefresh, "Đang tải...", async () =>
        {
            string url = "api/tickets/my?page=1&pageSize=50";
            if (_cbStatus.SelectedIndex > 0 && _cbStatus.SelectedItem is string s)
            {
                url += $"&status={Uri.EscapeDataString(s)}";
            }

            var res = await ApiService.GetAsync<PagedResult<MyTicketDto>>(url);
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            _dgvTickets.DataSource = res.Data.Items;
            if (_dgvTickets.Columns["TicketId"] != null) _dgvTickets.Columns["TicketId"].Visible = false;
            if (_dgvTickets.Columns["TicketCode"] != null) _dgvTickets.Columns["TicketCode"].HeaderText = "Mã vé";
            if (_dgvTickets.Columns["RouteName"] != null) _dgvTickets.Columns["RouteName"].HeaderText = "Tuyến xe";
            if (_dgvTickets.Columns["DepartureTime"] != null)
            {
                _dgvTickets.Columns["DepartureTime"].HeaderText = "Khởi hành";
                _dgvTickets.Columns["DepartureTime"].DefaultCellStyle.Format = "HH:mm dd/MM/yyyy";
            }
            if (_dgvTickets.Columns["SeatCode"] != null) _dgvTickets.Columns["SeatCode"].HeaderText = "Ghế";
            if (_dgvTickets.Columns["TotalAmount"] != null)
            {
                _dgvTickets.Columns["TotalAmount"].HeaderText = "Tổng tiền";
                _dgvTickets.Columns["TotalAmount"].DefaultCellStyle.Format = "N0";
            }
            if (_dgvTickets.Columns["Status"] != null) _dgvTickets.Columns["Status"].HeaderText = "Trạng thái";
        }, FindForm());
    }

    private async Task OpenSelectedDetailAsync()
    {
        if (_dgvTickets.CurrentRow?.DataBoundItem is not MyTicketDto ticket)
        {
            UiHelper.ShowError("Vui lòng chọn một vé trong danh sách.", FindForm());
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnDetail, "Đang mở...", async () =>
        {
            var res = await ApiService.GetAsync<TicketDetailDto>($"api/tickets/{ticket.TicketId}");
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            using var dlg = new frmTicketDetail(res.Data);
            dlg.ShowDialog(FindForm());
        }, FindForm());
    }
}
