using BusBooking.Client.Controls;
using BusBooking.Client.Forms.Admin.Views;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin;

/// <summary>
/// Màn hình chính Quản trị (frmMainAdmin): ẩn/hiện menu theo đúng phân quyền Admin, Staff, Support (Mục 4.2 & 5.11).
/// </summary>
public class frmMainAdmin : Form
{
    private readonly DoubleBufferedPanel _pnlContent;
    private readonly FlowLayoutPanel _menuFlow;
    private readonly Label _lblPageTitle;
    private readonly BadgeLabel _wsBadge;
    private readonly List<ModernButton> _menuButtons = new();

    private readonly formTongQuan _dashboardForm;
    private readonly formQuanLyChuyenXe _tripsForm;
    private readonly formQuanLyTuyenXe _routesForm;
    private readonly formQuanLyXe _vehiclesForm;
    private readonly formGhe _seatsForm;
    private readonly formQuanLyVe _ticketsForm;
    private readonly formQuanLyTruyCap _staffForm;
    private readonly formTraCuuKhachHang _customersForm;
    private readonly formHoTroKhachHang _supportForm;
    private readonly formDoanhThu _revenueForm;

    public frmMainAdmin()
    {
        string role = SessionManager.CurrentUser?.Role ?? UserRoles.Staff;

        Text = $"BusBooking Admin Panel - [{role}]";
        Size = new Size(1360, 840);
        MinimumSize = new Size(1180, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        DoubleBuffered = true;

        var pnlSidebar = new DoubleBufferedPanel
        {
            Dock = DockStyle.Left,
            Width = 256,
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
            Text = "ADMIN & OPERATIONS",
            Font = new Font("Segoe UI Semibold", 8f),
            ForeColor = AppTheme.SidebarMuted,
            BackColor = AppTheme.SidebarBg,
            AutoSize = true,
            Location = new Point(50, 36)
        });

        var bottomUserPanel = new RoundedCardPanel
        {
            Dock = DockStyle.Bottom,
            Height = 104,
            CornerRadius = 12,
            SurfaceColor = AppTheme.SidebarHover,
            BorderThickness = 0,
            Padding = new Padding(12)
        };
        bottomUserPanel.Controls.Add(new Label
        {
            Text = SessionManager.CurrentUser?.FullName ?? "Nhân viên",
            Font = AppTheme.BodyBoldFont,
            ForeColor = Color.White,
            BackColor = AppTheme.SidebarHover,
            AutoEllipsis = true,
            Location = new Point(12, 10),
            Size = new Size(204, 22)
        });
        bottomUserPanel.Controls.Add(new Label
        {
            Text = $"Vai trò: {role}",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.SidebarMuted,
            BackColor = AppTheme.SidebarHover,
            AutoSize = true,
            Location = new Point(12, 34)
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
            Size = new Size(204, 34)
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

        var pnlTop = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 64,
            Width = 1040,
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
            Text = "Tổng quan hệ thống",
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(24, 15)
        };

        _wsBadge = new BadgeLabel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 220, 19)
        };
        UpdateRealtimeBadge(SignalRService.Instance.IsConnected);

        var roleBadge = new BadgeLabel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 110, 19)
        };
        roleBadge.SetColors(role, AppTheme.PrimarySoft, AppTheme.Primary);

        pnlTop.Controls.AddRange(new Control[] { _lblPageTitle, _wsBadge, roleBadge });
        pnlTop.Resize += (_, _) =>
        {
            roleBadge.Location = new Point(pnlTop.Width - roleBadge.Width - 22, 19);
            _wsBadge.Location = new Point(roleBadge.Left - _wsBadge.Width - 10, 19);
        };

        _pnlContent = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CanvasBg
        };

        _dashboardForm = PrepareChildForm(new formTongQuan());
        _tripsForm = PrepareChildForm(new formQuanLyChuyenXe());
        _routesForm = PrepareChildForm(new formQuanLyTuyenXe());
        _vehiclesForm = PrepareChildForm(new formQuanLyXe());
        _seatsForm = PrepareChildForm(new formGhe());
        _ticketsForm = PrepareChildForm(new formQuanLyVe());
        _staffForm = PrepareChildForm(new formQuanLyTruyCap());
        _customersForm = PrepareChildForm(new formTraCuuKhachHang());
        _supportForm = PrepareChildForm(new formHoTroKhachHang());
        _revenueForm = PrepareChildForm(new formDoanhThu());

        _pnlContent.Controls.AddRange(new Control[]
        {
            _dashboardForm, _tripsForm, _routesForm, _vehiclesForm, _seatsForm,
            _ticketsForm, _staffForm, _customersForm, _supportForm, _revenueForm
        });

        var navDashboard = AddMenuItem("Tổng quan", IconChar.TachometerAlt, async () =>
        {
            ShowPage(_dashboardForm, "Tổng quan hệ thống");
            await _dashboardForm.LoadAsync();
        });

        ModernButton? navSeats = null;
        if (role == UserRoles.Admin || role == UserRoles.Staff)
        {
            AddMenuItem("Quản lý chuyến xe", IconChar.Bus, async () =>
            {
                ShowPage(_tripsForm, "Quản lý chuyến xe");
                await _tripsForm.LoadAsync();
            });
            AddMenuItem("Quản lý tuyến xe", IconChar.Route, async () =>
            {
                ShowPage(_routesForm, "Quản lý tuyến xe");
                await _routesForm.LoadAsync();
            });
            AddMenuItem("Xe & Bảo trì", IconChar.Wrench, async () =>
            {
                ShowPage(_vehiclesForm, "Quản lý xe & lịch bảo trì");
                await _vehiclesForm.LoadAsync();
            });
            navSeats = AddMenuItem("Sơ đồ ghế", IconChar.Chair, async () =>
            {
                ShowPage(_seatsForm, "Quản lý sơ đồ ghế & bán vé nhanh");
                await _seatsForm.LoadAsync();
            });
            AddMenuItem("Quản lý vé", IconChar.TicketAlt, async () =>
            {
                ShowPage(_ticketsForm, "Quản lý vé & Đặt vé tại quầy");
                await _ticketsForm.LoadAsync();
            });
        }

        AddMenuItem("Tra cứu khách hàng", IconChar.Users, async () =>
        {
            ShowPage(_customersForm, "Tra cứu thông tin khách hàng");
            await _customersForm.LoadAsync();
        });

        if (role == UserRoles.Admin || role == UserRoles.Support)
        {
            AddMenuItem("Hỗ trợ khách hàng", IconChar.Headset, async () =>
            {
                ShowPage(_supportForm, "Trung tâm hỗ trợ khách hàng");
                await _supportForm.LoadAsync();
            });
        }

        if (role == UserRoles.Admin)
        {
            AddMenuItem("Quyền truy cập", IconChar.ShieldAlt, async () =>
            {
                ShowPage(_staffForm, "Quản lý tài khoản & phân quyền");
                await _staffForm.LoadAsync();
            });
            AddMenuItem("Báo cáo doanh thu", IconChar.ChartBar, async () =>
            {
                ShowPage(_revenueForm, "Báo cáo doanh thu Online & Trực tiếp");
                await _revenueForm.LoadAsync();
            });
        }

        _tripsForm.ViewTripSeatsRequested += async tripId =>
        {
            if (navSeats != null) SetActiveMenu(navSeats);
            ShowPage(_seatsForm, "Quản lý sơ đồ ghế & bán vé nhanh");
            await _seatsForm.LoadAsync(tripId);
        };

        var mainArea = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = AppTheme.CanvasBg };
        mainArea.Controls.Add(_pnlContent);
        mainArea.Controls.Add(pnlTop);

        Controls.Add(mainArea);
        Controls.Add(pnlSidebar);

        SignalRService.Instance.ConnectionStateChanged += OnConnectionChanged;
        SignalRService.Instance.MessageReceived += OnSupportMessage;

        FormClosed += (_, _) =>
        {
            SignalRService.Instance.ConnectionStateChanged -= OnConnectionChanged;
            SignalRService.Instance.MessageReceived -= OnSupportMessage;
        };

        SetActiveMenu(navDashboard);
        ShowPage(_dashboardForm, "Tổng quan hệ thống");
        Load += async (_, _) => await _dashboardForm.LoadAsync();
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
            Size = new Size(226, 44),
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

    private void OnSupportMessage(SupportMessageDto msg)
        => SignalRService.SafeInvoke(this, () => _supportForm.AppendRealtimeMessage(msg));

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
