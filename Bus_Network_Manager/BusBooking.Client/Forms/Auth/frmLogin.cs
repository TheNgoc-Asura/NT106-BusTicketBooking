using System.Drawing.Drawing2D;
using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Auth;

public class frmLogin : Form
{
    private readonly ModernTextBox _txtEmail;
    private readonly ModernTextBox _txtPassword;
    private readonly ModernTextBox _txtServerUrl;
    private readonly ModernButton _btnLogin;
    private readonly Label _lblError;

    public frmLogin()
    {
        Text = "BusBooking - Đăng nhập hệ thống";
        Size = new Size(940, 590);
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        DoubleBuffered = true;

        // Cột trái: Brand Hero Gradient (#0B4F6C -> #1D4ED8)
        var pnlHero = new GradientHeroPanel
        {
            Dock = DockStyle.Left,
            Width = 390,
            Padding = new Padding(34, 42, 34, 30)
        };

        var logoWrap = new RoundedCardPanel
        {
            Size = new Size(56, 56),
            CornerRadius = 14,
            SurfaceColor = Color.FromArgb(30, 64, 175),
            BorderThickness = 0,
            Location = new Point(34, 42),
            Padding = new Padding(0)
        };
        logoWrap.Controls.Add(new IconPictureBox
        {
            Dock = DockStyle.Fill,
            IconChar = IconChar.Bus,
            IconSize = 26,
            IconColor = AppTheme.Accent,
            BackColor = Color.FromArgb(30, 64, 175),
            SizeMode = PictureBoxSizeMode.CenterImage
        });

        var lblBrand = new Label
        {
            Text = "BusBooking",
            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(104, 46)
        };

        var lblTag = new Label
        {
            Text = "Nền tảng đặt vé xe khách & điều hành trực tuyến",
            Font = AppTheme.BodyFont,
            ForeColor = Color.FromArgb(203, 213, 225),
            BackColor = Color.Transparent,
            Location = new Point(34, 114),
            Size = new Size(320, 46)
        };

        var lblBullet1 = CreateHeroFeature("• Tìm chuyến trực quan, chọn giường theo tầng", 180);
        var lblBullet2 = CreateHeroFeature("• Thanh toán MoMo QR & nhận hóa đơn tức thì", 214);
        var lblBullet3 = CreateHeroFeature("• Chat hỗ trợ khách hàng realtime qua SignalR", 248);

        var lblQuickDemo = new Label
        {
            Text = "ĐIỀN NHANH TÀI KHOẢN MẪU (NT106 DEMO)",
            Font = new Font("Segoe UI Semibold", 8.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(34, 392)
        };

        var pnlDemoButtons = new FlowLayoutPanel
        {
            Location = new Point(32, 418),
            Size = new Size(330, 96),
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        pnlDemoButtons.Controls.Add(CreateDemoPill("Khách hàng", "nguyenvana@gmail.com"));
        pnlDemoButtons.Controls.Add(CreateDemoPill("Admin", "admin@nhaxe.vn"));
        pnlDemoButtons.Controls.Add(CreateDemoPill("Staff", "staff@nhaxe.vn"));
        pnlDemoButtons.Controls.Add(CreateDemoPill("Support", "support@nhaxe.vn"));

        pnlHero.Controls.AddRange(new Control[]
        {
            logoWrap, lblBrand, lblTag,
            lblBullet1, lblBullet2, lblBullet3,
            lblQuickDemo, pnlDemoButtons
        });

        // Cột phải: Card đăng nhập trắng bo góc 12px
        var pnlRight = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CanvasBg,
            Padding = new Padding(44, 34, 44, 34)
        };

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 14,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(30)
        };

        var lblTitle = new Label
        {
            Text = "Chào mừng trở lại",
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(32, 28)
        };

        var lblSub = new Label
        {
            Text = "Đăng nhập để đặt vé hoặc truy cập bảng điều khiển quản trị",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(34, 64)
        };

        var lblEmail = UiHelper.CreateFieldLabel("EMAIL", 34, 104);
        _txtEmail = new ModernTextBox
        {
            Location = new Point(34, 126),
            Size = new Size(392, 42),
            Icon = IconChar.User,
            PlaceholderText = "vd: nguyenvana@gmail.com"
        };

        var lblPass = UiHelper.CreateFieldLabel("MẬT KHẨU", 34, 182);
        _txtPassword = new ModernTextBox
        {
            Location = new Point(34, 204),
            Size = new Size(392, 42),
            Icon = IconChar.Lock,
            PlaceholderText = "Nhập mật khẩu...",
            UseSystemPasswordChar = true
        };
        _txtPassword.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await HandleLoginAsync();
            }
        };

        var lblServer = UiHelper.CreateFieldLabel("SERVER API URL", 34, 260);
        _txtServerUrl = new ModernTextBox
        {
            Location = new Point(34, 282),
            Size = new Size(392, 38),
            Icon = IconChar.Route,
            Text = ApiClient.BaseUrl
        };

        _lblError = new Label
        {
            Text = string.Empty,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.DangerFg,
            BackColor = AppTheme.CardSurface,
            Location = new Point(34, 328),
            Size = new Size(392, 24),
            Visible = false
        };

        _btnLogin = new ModernButton
        {
            Text = "Đăng nhập",
            Icon = IconChar.Key,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Location = new Point(34, 360),
            Size = new Size(392, 46)
        };
        _btnLogin.Click += async (_, _) => await HandleLoginAsync();

        var lnkForgot = new LinkLabel
        {
            Text = "Quên mật khẩu?",
            Font = AppTheme.CaptionBoldFont,
            LinkColor = AppTheme.Primary,
            ActiveLinkColor = AppTheme.PrimaryHover,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(34, 422)
        };
        lnkForgot.Click += (_, _) =>
        {
            using var frm = new frmForgotPassword();
            frm.ShowDialog(this);
        };

        var btnRegister = new ModernButton
        {
            Text = "Tạo tài khoản mới",
            Icon = IconChar.UserPlus,
            FillColor = AppTheme.AccentSoft,
            HoverColor = Color.FromArgb(255, 237, 213),
            ForeColor = AppTheme.AccentHover,
            BorderColor = AppTheme.Accent,
            BorderWidth = 1,
            Location = new Point(236, 414),
            Size = new Size(190, 38)
        };
        btnRegister.Click += (_, _) =>
        {
            using var frm = new frmRegister();
            frm.ShowDialog(this);
        };

        card.Controls.AddRange(new Control[]
        {
            lblTitle, lblSub,
            lblEmail, _txtEmail,
            lblPass, _txtPassword,
            lblServer, _txtServerUrl,
            _lblError, _btnLogin,
            lnkForgot, btnRegister
        });

        pnlRight.Controls.Add(card);
        Controls.Add(pnlRight);
        Controls.Add(pnlHero);
    }

    private Label CreateHeroFeature(string text, int y)
    {
        return new Label
        {
            Text = text,
            Font = AppTheme.BodyFont,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(34, y)
        };
    }

    private ModernButton CreateDemoPill(string label, string email)
    {
        var btn = new ModernButton
        {
            Text = label,
            FillColor = Color.FromArgb(30, 58, 138),
            HoverColor = Color.FromArgb(37, 99, 235),
            CornerRadius = 8,
            Size = new Size(148, 34),
            Margin = new Padding(0, 0, 10, 8),
            Font = AppTheme.CaptionBoldFont
        };
        btn.Click += (_, _) =>
        {
            _txtEmail.Text = email;
            _txtPassword.Text = "Matkhau@123";
            _lblError.Visible = false;
        };
        return btn;
    }

    private async Task HandleLoginAsync()
    {
        _lblError.Visible = false;
        string serverUrl = _txtServerUrl.Text.Trim();
        if (!string.IsNullOrWhiteSpace(serverUrl))
        {
            ApiService.BaseUrl = serverUrl;
        }

        string email = _txtEmail.Text.Trim();
        string password = _txtPassword.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowInlineError("Vui lòng nhập đầy đủ Email và Mật khẩu.");
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnLogin, "Đang xác thực...", async () =>
        {
            var res = await ApiService.PostAsync<LoginResponseData>("api/auth/login", new LoginRequest
            {
                Email = email,
                Password = password
            });

            if (!res.Success || res.Data == null)
            {
                ShowInlineError(res.Message);
                return;
            }

            SessionManager.SetSession(res.Data.Token, res.Data.User);
            _ = SignalRService.Instance.StartAsync();

            DialogResult = DialogResult.OK;
            Close();
        }, this);
    }

    private void ShowInlineError(string message)
    {
        _lblError.Text = message;
        _lblError.Visible = true;
    }

    private sealed class GradientHeroPanel : DoubleBufferedPanel
    {
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new LinearGradientBrush(
                ClientRectangle,
                AppTheme.PrimaryDark,
                AppTheme.Primary,
                LinearGradientMode.ForwardDiagonal);
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }
}
