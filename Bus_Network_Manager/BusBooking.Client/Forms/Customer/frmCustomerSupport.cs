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
/// Form Hỗ trợ trực tuyến - Khung Chat Bubble thời gian thực qua SignalRService.
/// </summary>
public class frmCustomerSupport : Form
{
    private readonly DoubleBufferedFlowLayoutPanel _flowMessages;
    private readonly ModernTextBox _txtMessage;
    private readonly ModernButton _btnSend;
    private readonly HashSet<int> _renderedIds = new();
    private int _currentTicketId;

    public frmCustomerSupport()
    {
        Text = "Hỗ trợ trực tuyến";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 14,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(0)
        };

        var pnlHeader = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 64,
            Width = 900,
            BackColor = AppTheme.CardSurface,
            Padding = new Padding(20, 12, 20, 12)
        };
        pnlHeader.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.CardBorder);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var icon = new IconPictureBox
        {
            IconChar = IconChar.Headset,
            IconSize = 22,
            IconColor = AppTheme.Primary,
            BackColor = AppTheme.CardSurface,
            Size = new Size(34, 34),
            Location = new Point(20, 15),
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        var lblTitle = new Label
        {
            Text = "Hỗ trợ trực tuyến với nhà xe",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(62, 12)
        };

        var lblSub = new Label
        {
            Text = "Trao đổi trực tiếp với bộ phận CSKH qua SignalR WebSocket",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(62, 36)
        };

        var btnReload = new ModernButton
        {
            Text = "Tải lại",
            Icon = IconChar.SyncAlt,
            IconSize = 13,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Size = new Size(104, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlHeader.Width - 124, 15)
        };
        btnReload.Click += async (_, _) => await LoadAsync();
        pnlHeader.Controls.AddRange(new Control[] { icon, lblTitle, lblSub, btnReload });

        var pnlBottom = new DoubleBufferedPanel
        {
            Dock = DockStyle.Bottom,
            Height = 74,
            BackColor = AppTheme.CardSurface,
            Padding = new Padding(18, 15, 18, 15)
        };
        pnlBottom.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.CardBorder);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        _btnSend = new ModernButton
        {
            Text = "Gửi",
            Icon = IconChar.PaperPlane,
            IconSize = 14,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Size = new Size(116, 42),
            Dock = DockStyle.Right
        };
        _btnSend.Click += async (_, _) => await SendAsync();

        var spacer = new Panel { Dock = DockStyle.Right, Width = 12, BackColor = AppTheme.CardSurface };

        _txtMessage = new ModernTextBox
        {
            Dock = DockStyle.Fill,
            Icon = IconChar.Pen,
            PlaceholderText = "Nhập nội dung câu hỏi hoặc yêu cầu hỗ trợ..."
        };
        _txtMessage.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendAsync();
            }
        };

        pnlBottom.Controls.Add(_txtMessage);
        pnlBottom.Controls.Add(spacer);
        pnlBottom.Controls.Add(_btnSend);

        _flowMessages = new DoubleBufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(14)
        };

        card.Controls.Add(_flowMessages);
        card.Controls.Add(pnlBottom);
        card.Controls.Add(pnlHeader);
        Controls.Add(card);
    }

    public async Task LoadAsync()
    {
        var res = await ApiService.GetAsync<PagedResult<SupportMessageDto>>("api/support/messages?page=1&pageSize=100");
        if (!res.Success || res.Data == null)
            return;

        _renderedIds.Clear();
        _flowMessages.SuspendLayout();
        _flowMessages.Controls.Clear();

        foreach (var msg in res.Data.Items)
        {
            _currentTicketId = msg.TicketId;
            AppendBubbleInternal(msg);
        }

        _flowMessages.ResumeLayout();
        ScrollToBottom();
    }

    public void AppendIncomingMessage(SupportMessageDto msg)
    {
        if (_currentTicketId != 0 && msg.TicketId != _currentTicketId)
            return;

        _currentTicketId = msg.TicketId;
        AppendBubbleInternal(msg);
        ScrollToBottom();
    }

    private void AppendBubbleInternal(SupportMessageDto msg)
    {
        if (!_renderedIds.Add(msg.Id))
            return;

        bool isOwn = msg.SenderRole == UserRoles.Customer;
        var bubble = new BubbleMessageControl(msg, isOwn, Math.Max(420, _flowMessages.ClientSize.Width));
        _flowMessages.Controls.Add(bubble);
    }

    private async Task SendAsync()
    {
        string text = _txtMessage.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        await UiHelper.RunButtonActionAsync(_btnSend, "Gửi...", async () =>
        {
            bool sentViaHub = false;
            if (SignalRService.Instance.IsConnected)
            {
                try
                {
                    sentViaHub = await SignalRService.Instance.SendMessageAsync(_currentTicketId, text);
                }
                catch
                {
                    sentViaHub = false;
                }
            }

            if (!sentViaHub)
            {
                var res = await ApiService.PostAsync<SendSupportMessageResponseData>("api/support/messages",
                    new CreateSupportMessageRequest { Content = text });
                if (!res.Success || res.Data == null)
                {
                    UiHelper.ShowError(res.Message, FindForm());
                    return;
                }

                _currentTicketId = res.Data.TicketId;
                AppendBubbleInternal(new SupportMessageDto
                {
                    Id = res.Data.Id,
                    TicketId = res.Data.TicketId,
                    SenderId = SessionManager.CurrentUser?.Id ?? 0,
                    SenderRole = UserRoles.Customer,
                    Content = res.Data.Content,
                    SentAt = res.Data.SentAt
                });
                ScrollToBottom();
            }

            _txtMessage.Text = string.Empty;
            _txtMessage.FocusInput();
        }, FindForm());
    }

    private void ScrollToBottom()
    {
        if (_flowMessages.Controls.Count > 0)
        {
            _flowMessages.ScrollControlIntoView(_flowMessages.Controls[^1]);
        }
    }
}
