using BusBooking.Client.Controls;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Customer;

/// <summary>
/// Màn hình chính Khách hàng (Mục 4.1) với thanh điều hướng bên trái hiện đại, badge đếm thông báo chưa đọc và SignalR realtime.
/// </summary>
public class frmMainCustomer : Form
{
    private readonly DoubleBufferedPanel _pnlContent;
    private readonly FlowLayoutPanel _menuFlow;
    private readonly Label _lblPageTitle;
    private readonly Label _lblSidebarUser;
    private readonly BadgeLabel _wsBadge;
    private readonly List<ModernButton> _menuButtons = new();

    private readonly frmSearchTrips _searchForm;
    private readonly frmMyTickets _ticketsForm;
    private readonly frmNotifications _notificationsForm;
    private readonly frmCustomerSupport _supportForm;
    private readonly frmCustomerProfile _profileForm;
    private readonly ModernButton _navNotifications;

    public frmMainCustomer()
    {
        Text = "BusBooking - Hệ thống Đặt vé xe trực tuyến";
        Size = new Size(1280, 820);
        MinimumSize = new Size(1080, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        DoubleBuffered = true;

        // 1. Left Navigation Sidebar (#0F172A)
        var pnlSidebar = new DoubleBufferedPanel
        {
            Dock = DockStyle.Left,
            Width = 252,
            BackColor = AppTheme.SidebarBg,
            Padding = new Padding(14, 18, 14, 16)
        };

        var brandPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = AppTheme.SidebarBg
        };
        brandPanel.Controls.Add(new IconPictureBox
        {
            IconChar = IconChar.Bus,
            IconSize = 24,
            IconColor = AppTheme.Accent,
            BackColor = AppTheme.SidebarBg,
            Location = new Point(6, 10),
            Size = new Size(34, 34),
            SizeMode = PictureBoxSizeMode.CenterImage
        });
        brandPanel.Controls.Add(new Label
        {
            Text = "BusBooking",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = AppTheme.SidebarBg,
            AutoSize = true,
            Location = new Point(48, 6)
        });
        brandPanel.Controls.Add(new Label
        {
            Text = "CỔNG KHÁCH HÀNG",
            Font = new Font("Segoe UI Semibold", 8f),
            ForeColor = AppTheme.SidebarMuted,
            BackColor = AppTheme.SidebarBg,
            AutoSize = true,
            Location = new Point(50, 36)
        });

        // User Card ở đáy Sidebar
        var bottomUserPanel = new RoundedCardPanel
        {
            Dock = DockStyle.Bottom,
            Height = 104,
            CornerRadius = 12,
            SurfaceColor = AppTheme.SidebarHover,
            BorderThickness = 0,
            Padding = new Padding(12)
        };
        _lblSidebarUser = new Label
        {
            Text = SessionManager.CurrentUser?.FullName ?? "Khách hàng",
            Font = AppTheme.BodyBoldFont,
            ForeColor = Color.White,
            BackColor = AppTheme.SidebarHover,
            AutoEllipsis = true,
            Location = new Point(12, 10),
            Size = new Size(200, 22)
        };
        bottomUserPanel.Controls.Add(_lblSidebarUser);
        bottomUserPanel.Controls.Add(new Label
        {
            Text = SessionManager.CurrentUser?.Email ?? string.Empty,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.SidebarMuted,
            BackColor = AppTheme.SidebarHover,
            AutoEllipsis = true,
            Location = new Point(12, 34),
            Size = new Size(200, 18)
        });

        var btnLogout = new ModernButton
        {
            Text = "Đăng xuất",
            Icon = IconChar.SignOutAlt,
            IconSize = 13,
            FillColor = Color.FromArgb(127, 29, 29),
            HoverColor = Color.FromArgb(185, 28, 28),
            ForeColor = Color.White,
            CornerRadius = 8,
            Location = new Point(12, 60),
            Size = new Size(200, 34)
        };
        btnLogout.Click += async (_, _) => await LogoutAsync();
        bottomUserPanel.Controls.Add(btnLogout);

        _menuFlow = new DoubleBufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = AppTheme.SidebarBg,
            Padding = new Padding(0, 8, 0, 8)
        };

        pnlSidebar.Controls.Add(_menuFlow);
        pnlSidebar.Controls.Add(bottomUserPanel);
        pnlSidebar.Controls.Add(brandPanel);

        // 2. Top Bar
        var pnlTop = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 64,
            Width = 1000,
            BackColor = AppTheme.CardSurface,
            Padding = new Padding(24, 14, 24, 14)
        };
        pnlTop.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.CardBorder);
            e.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
        };

        _lblPageTitle = new Label
        {
            Text = "Tìm chuyến xe",
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(24, 15)
        };

        _wsBadge = new BadgeLabel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 130, 19)
        };
        UpdateRealtimeBadge(SignalRService.Instance.IsConnected);

        pnlTop.Controls.AddRange(new Control[] { _lblPageTitle, _wsBadge });
        pnlTop.Resize += (_, _) =>
        {
            _wsBadge.Location = new Point(pnlTop.Width - _wsBadge.Width - 24, 19);
        };

        // 3. Content Host + Child Forms (embedded seamlessly as TopLevel = false)
        _pnlContent = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CanvasBg
        };

        _searchForm = PrepareChildForm(new frmSearchTrips());
        _ticketsForm = PrepareChildForm(new frmMyTickets());
        _notificationsForm = PrepareChildForm(new frmNotifications());
        _supportForm = PrepareChildForm(new frmCustomerSupport());
        _profileForm = PrepareChildForm(new frmCustomerProfile());

        _pnlContent.Controls.AddRange(new Control[]
        {
            _searchForm, _ticketsForm, _notificationsForm, _supportForm, _profileForm
        });

        var navSearch = AddMenuItem("Tìm chuyến xe", IconChar.Search, async () =>
        {
            ShowPage(_searchForm, "Tìm chuyến xe trực tuyến");
            await _searchForm.SearchAsync();
        });
        var navTickets = AddMenuItem("Vé của tôi", IconChar.TicketAlt, async () =>
        {
            ShowPage(_ticketsForm, "Quản lý vé đã đặt");
            await _ticketsForm.LoadAsync();
        });
        _navNotifications = AddMenuItem("Thông báo", IconChar.Bell, async () =>
        {
            ShowPage(_notificationsForm, "Thông báo của tôi");
            await _notificationsForm.LoadAsync();
        });
        AddMenuItem("Hỗ trợ trực tuyến", IconChar.Headset, async () =>
        {
            ShowPage(_supportForm, "Chat hỗ trợ trực tuyến");
            await _supportForm.LoadAsync();
        });
        AddMenuItem("Tài khoản cá nhân", IconChar.User, async () =>
        {
            ShowPage(_profileForm, "Thông tin tài khoản & bảo mật");
            await _profileForm.LoadAsync();
        });

        var mainArea = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = AppTheme.CanvasBg };
        mainArea.Controls.Add(_pnlContent);
        mainArea.Controls.Add(pnlTop);

        Controls.Add(mainArea);
        Controls.Add(pnlSidebar);

        _searchForm.BookingCompleted += () => _ = _ticketsForm.LoadAsync();
        _notificationsForm.UnreadCountChanged += unread => _navNotifications.BadgeCount = unread;
        _profileForm.ProfileUpdated += () =>
        {
            _lblSidebarUser.Text = SessionManager.CurrentUser?.FullName ?? "Khách hàng";
        };

        SignalRService.Instance.ConnectionStateChanged += OnConnectionChanged;
        SignalRService.Instance.NotificationReceived += OnNotificationReceived;
        SignalRService.Instance.MessageReceived += OnMessageReceived;
        SignalRService.Instance.PaymentConfirmed += OnPaymentConfirmed;

        FormClosed += (_, _) =>
        {
            SignalRService.Instance.ConnectionStateChanged -= OnConnectionChanged;
            SignalRService.Instance.NotificationReceived -= OnNotificationReceived;
            SignalRService.Instance.MessageReceived -= OnMessageReceived;
            SignalRService.Instance.PaymentConfirmed -= OnPaymentConfirmed;
        };

        SetActiveMenu(navSearch);
        ShowPage(_searchForm, "Tìm chuyến xe trực tuyến");

        Load += async (_, _) =>
        {
            await _searchForm.SearchAsync();
            await _notificationsForm.LoadAsync();
        };
    }

    private static TForm PrepareChildForm<TForm>(TForm form) where TForm : Form
    {
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        return form;
    }

    private ModernButton AddMenuItem(string text, IconChar icon, Func<Task> onClick)
    {
        var btn = new ModernButton
        {
            Text = "  " + text,
            Icon = icon,
            IconSize = 15,
            FillColor = AppTheme.SidebarBg,
            HoverColor = AppTheme.SidebarHover,
            ForeColor = AppTheme.SidebarMuted,
            CornerRadius = 10,
            Size = new Size(224, 44),
            Margin = new Padding(0, 0, 0, 6),
            TextAlign = ContentAlignment.MiddleLeft
        };
        btn.Click += async (_, _) =>
        {
            SetActiveMenu(btn);
            await onClick();
        };
        _menuButtons.Add(btn);
        _menuFlow.Controls.Add(btn);
        return btn;
    }

    private void SetActiveMenu(ModernButton active)
    {
        foreach (var btn in _menuButtons)
        {
            bool selected = btn == active;
            btn.FillColor = selected ? AppTheme.SidebarActive : AppTheme.SidebarBg;
            btn.HoverColor = selected ? AppTheme.SidebarActive : AppTheme.SidebarHover;
            btn.ForeColor = selected ? Color.White : AppTheme.SidebarMuted;
            btn.Invalidate();
        }
    }

    private void ShowPage(Form target, string title)
    {
        _lblPageTitle.Text = title;
        foreach (Control c in _pnlContent.Controls)
        {
            c.Visible = c == target;
        }
        target.Show();
        target.BringToFront();
    }

    private void OnConnectionChanged(bool connected)
        => SignalRService.SafeInvoke(this, () => UpdateRealtimeBadge(connected));

    private void OnNotificationReceived(NotificationDto notif)
        => SignalRService.SafeInvoke(this, () => _notificationsForm.AddRealtimeNotification(notif));

    private void OnMessageReceived(SupportMessageDto msg)
        => SignalRService.SafeInvoke(this, () => _supportForm.AppendIncomingMessage(msg));

    private void OnPaymentConfirmed(PaymentConfirmedEventDto evt)
        => SignalRService.SafeInvoke(this, () => _ = _ticketsForm.LoadAsync());

    private void UpdateRealtimeBadge(bool connected)
    {
        if (connected)
            _wsBadge.SetColors("● SignalR Live", AppTheme.SuccessBg, AppTheme.SuccessFg);
        else
            _wsBadge.SetColors("○ Offline", AppTheme.NeutralBg, AppTheme.NeutralFg);
    }

    private async Task LogoutAsync()
    {
        await SignalRService.Instance.StopAsync();
        await ApiService.LogoutAsync();
        SessionManager.Clear();
        Close();
    }
}
