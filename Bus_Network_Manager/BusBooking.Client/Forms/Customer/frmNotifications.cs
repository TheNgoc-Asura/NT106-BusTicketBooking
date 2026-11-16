using BusBooking.Client.Controls;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Customer;

/// <summary>
/// Form Thông báo của khách hàng với thẻ trạng thái chưa đọc và đồng bộ SignalR realtime.
/// </summary>
public class frmNotifications : Form
{
    private readonly ModernButton _btnRefresh;
    private readonly DoubleBufferedFlowLayoutPanel _flow;

    public event Action<int>? UnreadCountChanged;

    public frmNotifications()
    {
        Text = "Thông báo";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        var topBar = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Width = 900,
            BackColor = AppTheme.CanvasBg
        };

        var lblTitle = new Label
        {
            Text = "Trung tâm thông báo",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CanvasBg,
            AutoSize = true,
            Location = new Point(0, 8)
        };

        _btnRefresh = new ModernButton
        {
            Text = "Làm mới",
            Icon = IconChar.SyncAlt,
            IconSize = 14,
            FillColor = AppTheme.CardSurface,
            HoverColor = AppTheme.PrimarySoft,
            ForeColor = AppTheme.Primary,
            BorderColor = AppTheme.CardBorder,
            BorderWidth = 1,
            Size = new Size(124, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(topBar.Width - 124, 6)
        };
        _btnRefresh.Click += async (_, _) => await LoadAsync();
        topBar.Controls.AddRange(new Control[] { lblTitle, _btnRefresh });

        _flow = new DoubleBufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = AppTheme.CanvasBg,
            Padding = new Padding(0, 8, 8, 12)
        };
        _flow.Resize += (_, _) =>
        {
            int w = Math.Max(460, _flow.ClientSize.Width - 28);
            foreach (Control c in _flow.Controls) c.Width = w;
        };

        Controls.Add(_flow);
        Controls.Add(topBar);
    }

    public async Task LoadAsync()
    {
        var res = await ApiService.GetAsync<List<NotificationDto>>("api/notifications");
        if (!res.Success)
            return;

        RenderItems(res.Data ?? new List<NotificationDto>());
    }

    public void AddRealtimeNotification(NotificationDto dto)
    {
        var list = _flow.Tag as List<NotificationDto> ?? new List<NotificationDto>();
        if (list.All(n => n.Id != dto.Id))
        {
            list.Insert(0, dto);
            RenderItems(list);
        }
    }

    private void RenderItems(List<NotificationDto> items)
    {
        _flow.Tag = items;
        UnreadCountChanged?.Invoke(items.Count(n => !n.IsRead));

        _flow.SuspendLayout();
        _flow.Controls.Clear();

        int w = Math.Max(460, _flow.ClientSize.Width - 28);
        if (items.Count == 0)
        {
            var empty = new RoundedCardPanel { Size = new Size(w, 90), SurfaceColor = AppTheme.CardSurface };
            empty.Controls.Add(new Label
            {
                Text = "Hiện chưa có thông báo mới nào.",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextMuted,
                BackColor = AppTheme.CardSurface,
                AutoSize = true,
                Location = new Point(24, 34)
            });
            _flow.Controls.Add(empty);
        }
        else
        {
            foreach (var n in items)
            {
                _flow.Controls.Add(CreateNotificationCard(n, w));
            }
        }

        _flow.ResumeLayout();
    }

    private RoundedCardPanel CreateNotificationCard(NotificationDto notif, int width)
    {
        var card = new RoundedCardPanel
        {
            Size = new Size(width, 96),
            CornerRadius = 12,
            SurfaceColor = notif.IsRead ? AppTheme.CardSurface : Color.FromArgb(248, 250, 255),
            BorderColor = notif.IsRead ? AppTheme.CardBorder : AppTheme.Primary,
            LeftAccentColor = notif.IsRead ? Color.Transparent : AppTheme.Accent,
            Margin = new Padding(0, 0, 0, 10)
        };

        var icon = new IconPictureBox
        {
            IconChar = IconChar.Bell,
            IconSize = 20,
            IconColor = notif.IsRead ? AppTheme.TextMuted : AppTheme.Accent,
            BackColor = card.SurfaceColor,
            Size = new Size(34, 34),
            Location = new Point(18, 18),
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        var lblTitle = new Label
        {
            Text = notif.Title,
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = card.SurfaceColor,
            AutoSize = true,
            Location = new Point(60, 14)
        };

        var lblContent = new Label
        {
            Text = notif.Content,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.TextSecondary,
            BackColor = card.SurfaceColor,
            Location = new Point(60, 40),
            Size = new Size(Math.Max(200, width - 250), 42)
        };

        var lblTime = new Label
        {
            Text = notif.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = card.SurfaceColor,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(width - 145, 16)
        };

        card.Controls.AddRange(new Control[] { icon, lblTitle, lblContent, lblTime });

        if (!notif.IsRead)
        {
            var btnRead = new ModernButton
            {
                Text = "Đã đọc",
                Icon = IconChar.Check,
                IconSize = 12,
                FillColor = AppTheme.PrimarySoft,
                HoverColor = Color.FromArgb(219, 234, 254),
                ForeColor = AppTheme.Primary,
                Size = new Size(106, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(width - 126, 48)
            };
            btnRead.Click += async (_, _) =>
            {
                var res = await ApiService.PutAsync<MarkNotificationReadResponseData>($"api/notifications/{notif.Id}/read");
                if (res.Success)
                {
                    notif.IsRead = true;
                    if (_flow.Tag is List<NotificationDto> list) RenderItems(list);
                }
            };
            card.Controls.Add(btnRead);
        }

        return card;
    }
}
