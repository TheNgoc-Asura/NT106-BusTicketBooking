using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Views;

/// <summary>
/// Form Nội dung hỗ trợ (formNoiDungHoTro) - Khung chat Bubble realtime qua SignalRService, có thể nhúng hoặc mở riêng.
/// </summary>
public class formNoiDungHoTro : Form
{
    private readonly Label _lblChatHeader;
    private readonly ModernButton _btnClaim;
    private readonly ModernButton _btnResolve;
    private readonly DoubleBufferedFlowLayoutPanel _flowMessages;
    private readonly ModernTextBox _txtReply;
    private readonly ModernButton _btnSend;

    private readonly HashSet<int> _renderedMessageIds = new();
    private int _ticketId;

    public event Action? TicketStatusUpdated;

    public formNoiDungHoTro(SupportTicketSummaryDto? initialTicket = null, bool standaloneDialog = false)
    {
        Text = initialTicket != null ? $"Hỗ trợ #{initialTicket.TicketId} - {initialTicket.CustomerName}" : "Nội dung hỗ trợ";
        BackColor = AppTheme.CardSurface;
        Font = AppTheme.BodyFont;
        DoubleBuffered = true;

        if (standaloneDialog)
        {
            Size = new Size(660, 620);
            StartPosition = FormStartPosition.CenterParent;
        }

        var chatCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(0)
        };

        var chatHeader = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 68,
            Width = 600,
            BackColor = AppTheme.CardSurface,
            Padding = new Padding(16, 12, 16, 12)
        };
        chatHeader.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.CardBorder);
            e.Graphics.DrawLine(pen, 0, chatHeader.Height - 1, chatHeader.Width, chatHeader.Height - 1);
        };

        _lblChatHeader = new Label
        {
            Text = "Chọn yêu cầu bên trái để trò chuyện",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoEllipsis = true,
            Location = new Point(16, 20),
            Size = new Size(250, 26)
        };

        _btnResolve = new ModernButton
        {
            Text = "Đã xử lý",
            Icon = IconChar.Check,
            IconSize = 12,
            FillColor = AppTheme.SuccessBg,
            HoverColor = Color.FromArgb(187, 247, 208),
            ForeColor = AppTheme.SuccessFg,
            Size = new Size(106, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(chatHeader.Width - 118, 16)
        };
        _btnResolve.Click += async (_, _) => await UpdateStatusAsync(SupportTicketStatuses.Resolved);

        _btnClaim = new ModernButton
        {
            Text = "Nhận xử lý",
            Icon = IconChar.Headset,
            IconSize = 12,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Size = new Size(116, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(chatHeader.Width - 242, 16)
        };
        _btnClaim.Click += async (_, _) => await UpdateStatusAsync(SupportTicketStatuses.Processing);

        chatHeader.Resize += (_, _) => _lblChatHeader.Width = Math.Max(100, _btnClaim.Left - 24);
        chatHeader.Controls.AddRange(new Control[] { _lblChatHeader, _btnClaim, _btnResolve });

        var composer = new DoubleBufferedPanel
        {
            Dock = DockStyle.Bottom,
            Height = 68,
            BackColor = AppTheme.CardSurface,
            Padding = new Padding(14)
        };
        composer.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.CardBorder);
            e.Graphics.DrawLine(pen, 0, 0, composer.Width, 0);
        };

        _btnSend = new ModernButton
        {
            Text = "Gửi",
            Icon = IconChar.PaperPlane,
            IconSize = 13,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Size = new Size(100, 40),
            Dock = DockStyle.Right
        };
        _btnSend.Click += async (_, _) => await SendReplyAsync();

        var composerGap = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = AppTheme.CardSurface };

        _txtReply = new ModernTextBox
        {
            Dock = DockStyle.Fill,
            Icon = IconChar.Pen,
            PlaceholderText = "Nhập phản hồi hỗ trợ cho khách hàng..."
        };
        _txtReply.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendReplyAsync();
            }
        };

        composer.Controls.Add(_txtReply);
        composer.Controls.Add(composerGap);
        composer.Controls.Add(_btnSend);

        _flowMessages = new DoubleBufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(12)
        };

        chatCard.Controls.Add(_flowMessages);
        chatCard.Controls.Add(composer);
        chatCard.Controls.Add(chatHeader);
        Controls.Add(chatCard);

        if (standaloneDialog)
        {
            SignalRService.Instance.MessageReceived += OnStandaloneMessage;
            FormClosed += (_, _) => SignalRService.Instance.MessageReceived -= OnStandaloneMessage;
            if (initialTicket != null)
            {
                Load += async (_, _) => await LoadTicketAsync(initialTicket);
            }
        }
    }

    private void OnStandaloneMessage(SupportMessageDto msg)
        => SignalRService.SafeInvoke(this, () => AppendRealtimeMessage(msg));

    public async Task LoadTicketAsync(SupportTicketSummaryDto ticket)
    {
        _ticketId = ticket.TicketId;
        _lblChatHeader.Text = $"#{ticket.TicketId} • {ticket.CustomerName}";

        var res = await ApiService.GetAsync<List<SupportMessageDto>>($"api/admin/support/tickets/{ticket.TicketId}/messages");
        if (!res.Success || res.Data == null)
            return;

        _renderedMessageIds.Clear();
        _flowMessages.SuspendLayout();
        _flowMessages.Controls.Clear();
        foreach (var msg in res.Data)
        {
            AppendBubble(msg);
        }
        _flowMessages.ResumeLayout();
        ScrollToBottom();
    }

    public void AppendRealtimeMessage(SupportMessageDto msg)
    {
        if (msg.TicketId != _ticketId)
            return;

        AppendBubble(msg);
        ScrollToBottom();
    }

    private void AppendBubble(SupportMessageDto msg)
    {
        if (!_renderedMessageIds.Add(msg.Id))
            return;

        bool isOwn = msg.SenderRole != UserRoles.Customer;
        _flowMessages.Controls.Add(new BubbleMessageControl(msg, isOwn, Math.Max(360, _flowMessages.ClientSize.Width)));
    }

    private async Task SendReplyAsync()
    {
        if (_ticketId <= 0)
        {
            UiHelper.ShowError("Vui lòng chọn ticket hỗ trợ bên trái.", FindForm());
            return;
        }

        string text = _txtReply.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!SignalRService.Instance.IsConnected)
        {
            await SignalRService.Instance.StartAsync();
            if (!SignalRService.Instance.IsConnected)
            {
                UiHelper.ShowError("Kết nối SignalR chưa sẵn sàng để gửi tin nhắn realtime.", FindForm());
                return;
            }
        }

        await UiHelper.RunButtonActionAsync(_btnSend, "Gửi...", async () =>
        {
            await SignalRService.Instance.SendMessageAsync(_ticketId, text);
            _txtReply.Text = string.Empty;
            _txtReply.FocusInput();
        }, FindForm());
    }

    private async Task UpdateStatusAsync(string status)
    {
        if (_ticketId <= 0)
            return;

        var res = await ApiService.PutAsync<UpdateSupportTicketStatusResponseData>(
            $"api/admin/support/tickets/{_ticketId}/status",
            new UpdateSupportTicketStatusRequest { Status = status });

        if (!res.Success)
        {
            UiHelper.ShowError(res.Message, FindForm());
            return;
        }

        TicketStatusUpdated?.Invoke();
    }

    private void ScrollToBottom()
    {
        if (_flowMessages.Controls.Count > 0)
        {
            _flowMessages.ScrollControlIntoView(_flowMessages.Controls[^1]);
        }
    }
}

/// <summary>
/// Hỗ trợ khách hàng (formHoTroKhachHang) gồm thống kê trạng thái, lọc ticket và nhúng formNoiDungHoTro realtime.
/// </summary>
public class formHoTroKhachHang : Form
{
    private readonly BadgeLabel _statTotal;
    private readonly BadgeLabel _statPending;
    private readonly BadgeLabel _statProcessing;
    private readonly BadgeLabel _statResolved;

    private readonly ComboBox _cbStatus;
    private readonly ModernTextBox _txtTitle;
    private readonly ModernButton _btnFilter;
    private readonly DataGridView _dgvTickets;
    private readonly formNoiDungHoTro _chatPanel;

    private int _selectedTicketId;
    private bool _suppressSelectionChanged;

    public formHoTroKhachHang()
    {
        Text = "Hỗ trợ khách hàng";
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
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));

        var queueCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(16)
        };

        var statsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = AppTheme.CardSurface
        };
        _statTotal = new BadgeLabel();
        _statPending = new BadgeLabel();
        _statProcessing = new BadgeLabel();
        _statResolved = new BadgeLabel();
        _statTotal.SetColors("Tổng: 0", AppTheme.PrimarySoft, AppTheme.Primary);
        _statPending.SetColors("Chờ: 0", AppTheme.WarningBg, AppTheme.WarningFg);
        _statProcessing.SetColors("Đang xử lý: 0", AppTheme.InfoBg, AppTheme.InfoFg);
        _statResolved.SetColors("Đã xong: 0", AppTheme.SuccessBg, AppTheme.SuccessFg);
        statsFlow.Controls.AddRange(new Control[] { _statTotal, _statPending, _statProcessing, _statResolved });

        var filterBar = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = AppTheme.CardSurface
        };
        _txtTitle = new ModernTextBox
        {
            Location = new Point(0, 8),
            Size = new Size(185, 38),
            Icon = IconChar.Search,
            PlaceholderText = "Tìm tiêu đề..."
        };
        _cbStatus = UiHelper.CreateStyledComboBox(193, 10, 155);
        _cbStatus.Items.AddRange(new object[]
        {
            "Tất cả",
            SupportTicketStatuses.Pending,
            SupportTicketStatuses.Processing,
            SupportTicketStatuses.WaitingCustomer,
            SupportTicketStatuses.Resolved
        });
        _cbStatus.SelectedIndex = 0;

        _btnFilter = new ModernButton
        {
            Text = "Lọc",
            Icon = IconChar.Search,
            IconSize = 13,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Location = new Point(356, 8),
            Size = new Size(82, 38)
        };
        _btnFilter.Click += async (_, _) => await LoadAsync();
        filterBar.Controls.AddRange(new Control[] { _txtTitle, _cbStatus, _btnFilter });

        _dgvTickets = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgvTickets, "Status");
        _dgvTickets.SelectionChanged += async (_, _) =>
        {
            if (!_suppressSelectionChanged && _dgvTickets.CurrentRow?.DataBoundItem is SupportTicketSummaryDto ticket)
            {
                _selectedTicketId = ticket.TicketId;
                await _chatPanel.LoadTicketAsync(ticket);
            }
        };
        _dgvTickets.DoubleClick += async (_, _) =>
        {
            if (_dgvTickets.CurrentRow?.DataBoundItem is SupportTicketSummaryDto ticket)
            {
                using var popup = new formNoiDungHoTro(ticket, standaloneDialog: true);
                popup.TicketStatusUpdated += () => _ = LoadAsync();
                popup.ShowDialog(FindForm());
                await LoadAsync();
            }
        };

        queueCard.Controls.Add(_dgvTickets);
        queueCard.Controls.Add(filterBar);
        queueCard.Controls.Add(statsFlow);

        _chatPanel = new formNoiDungHoTro
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 0, 0, 0)
        };
        _chatPanel.Show();
        _chatPanel.TicketStatusUpdated += async () => await LoadAsync();

        split.Controls.Add(queueCard, 0, 0);
        split.Controls.Add(_chatPanel, 1, 0);
        Controls.Add(split);
    }

    public async Task LoadAsync()
    {
        await UiHelper.RunButtonActionAsync(_btnFilter, "Đang tải...", async () =>
        {
            string url = "api/admin/support/tickets?page=1&pageSize=50";
            if (!string.IsNullOrWhiteSpace(_txtTitle.Text))
                url += $"&title={Uri.EscapeDataString(_txtTitle.Text.Trim())}";
            if (_cbStatus.SelectedIndex > 0 && _cbStatus.SelectedItem is string st)
                url += $"&status={Uri.EscapeDataString(st)}";

            var res = await ApiService.GetAsync<AdminSupportTicketListResponseData>(url);
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            var stData = res.Data.Statistics;
            _statTotal.SetColors($"Tổng: {stData.Total}", AppTheme.PrimarySoft, AppTheme.Primary);
            _statPending.SetColors($"Chờ: {stData.Pending}", AppTheme.WarningBg, AppTheme.WarningFg);
            _statProcessing.SetColors($"Đang xử lý: {stData.Processing}", AppTheme.InfoBg, AppTheme.InfoFg);
            _statResolved.SetColors($"Đã xong: {stData.Resolved}", AppTheme.SuccessBg, AppTheme.SuccessFg);

            _suppressSelectionChanged = true;
            _dgvTickets.DataSource = res.Data.Tickets.Items;
            if (_dgvTickets.Columns["TicketId"] != null) _dgvTickets.Columns["TicketId"].Visible = false;
            if (_dgvTickets.Columns["Title"] != null) _dgvTickets.Columns["Title"].HeaderText = "Tiêu đề";
            if (_dgvTickets.Columns["CustomerName"] != null) _dgvTickets.Columns["CustomerName"].HeaderText = "Khách hàng";
            if (_dgvTickets.Columns["Status"] != null) _dgvTickets.Columns["Status"].HeaderText = "Trạng thái";
            if (_dgvTickets.Columns["HandlerName"] != null) _dgvTickets.Columns["HandlerName"].HeaderText = "Người xử lý";
            if (_dgvTickets.Columns["LastMessageAt"] != null)
            {
                _dgvTickets.Columns["LastMessageAt"].HeaderText = "Cập nhật";
                _dgvTickets.Columns["LastMessageAt"].DefaultCellStyle.Format = "HH:mm dd/MM";
            }
            _suppressSelectionChanged = false;

            if (res.Data.Tickets.Items.Count > 0)
            {
                var target = res.Data.Tickets.Items.FirstOrDefault(t => t.TicketId == _selectedTicketId)
                             ?? res.Data.Tickets.Items[0];
                _selectedTicketId = target.TicketId;
                await _chatPanel.LoadTicketAsync(target);
            }
        }, FindForm());
    }

    public void AppendRealtimeMessage(SupportMessageDto msg) => _chatPanel.AppendRealtimeMessage(msg);
}

public class AdminSupportView : formHoTroKhachHang
{
}
