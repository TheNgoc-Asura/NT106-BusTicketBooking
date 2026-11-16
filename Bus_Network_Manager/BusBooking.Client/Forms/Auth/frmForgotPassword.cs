using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Auth;

/// <summary>
/// Form Quên mật khẩu: Nhập Email -> Xác nhận OTP 6 số -> Chuyển sang frmDatMatKhauMoi.
/// </summary>
public class frmForgotPassword : Form
{
    private readonly ModernTextBox _txtEmail;
    private readonly ModernTextBox _txtOtp;
    private readonly ModernButton _btnSendOtp;
    private readonly ModernButton _btnVerifyOtp;
    private readonly Label _lblInfo;

    public frmForgotPassword()
    {
        Text = "Quên mật khẩu";
        Size = new Size(490, 415);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22);
        DoubleBuffered = true;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 14,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder
        };

        card.Controls.Add(new Label
        {
            Text = "Khôi phục mật khẩu",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(26, 20)
        });

        _lblInfo = new Label
        {
            Text = "Bước 1: Nhập Email tài khoản để nhận mã xác thực 6 số",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(26, 52)
        };

        card.Controls.Add(UiHelper.CreateFieldLabel("EMAIL TÀI KHOẢN", 26, 88));
        _txtEmail = new ModernTextBox
        {
            Location = new Point(26, 110),
            Size = new Size(374, 42),
            Icon = IconChar.User,
            PlaceholderText = "email@example.com"
        };

        _btnSendOtp = new ModernButton
        {
            Text = "Gửi mã OTP",
            Icon = IconChar.PaperPlane,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Location = new Point(26, 164),
            Size = new Size(374, 40)
        };
        _btnSendOtp.Click += async (_, _) => await SendOtpAsync();

        card.Controls.Add(UiHelper.CreateFieldLabel("MÃ XÁC THỰC OTP (6 SỐ)", 26, 222));
        _txtOtp = new ModernTextBox
        {
            Location = new Point(26, 244),
            Size = new Size(168, 42),
            Icon = IconChar.Key,
            PlaceholderText = "123456"
        };

        _btnVerifyOtp = new ModernButton
        {
            Text = "Xác nhận OTP",
            Icon = IconChar.Check,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(206, 244),
            Size = new Size(194, 42),
            Enabled = false
        };
        _btnVerifyOtp.Click += async (_, _) => await VerifyOtpAsync();

        card.Controls.AddRange(new Control[]
        {
            _lblInfo, _txtEmail, _btnSendOtp, _txtOtp, _btnVerifyOtp
        });

        Controls.Add(card);
    }

    private async Task SendOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtEmail.Text))
        {
            UiHelper.ShowError("Vui lòng nhập email.", this);
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnSendOtp, "Đang gửi...", async () =>
        {
            var res = await ApiService.PostAsync<RequestCodeResponseData>("api/auth/forgot-password/request-code",
                new ForgotPasswordRequestCodeRequest { Email = _txtEmail.Text.Trim() });

            if (!res.Success)
            {
                UiHelper.ShowError(res.Message, this);
                return;
            }

            _btnVerifyOtp.Enabled = true;
            _lblInfo.Text = "Bước 2: Nhập mã OTP đã gửi về email để tiếp tục.";
            _lblInfo.ForeColor = AppTheme.SuccessFg;
            _txtOtp.FocusInput();
        }, this);
    }

    private async Task VerifyOtpAsync()
    {
        string code = _txtOtp.Text.Trim();
        if (code.Length != 6)
        {
            UiHelper.ShowError("Vui lòng nhập đủ mã OTP 6 chữ số.", this);
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnVerifyOtp, "Đang xác thực...", async () =>
        {
            string email = _txtEmail.Text.Trim();
            var verifyRes = await ApiService.PostAsync<VerifyCodeResponseData>("api/auth/forgot-password/verify-code",
                new VerifyCodeRequest
                {
                    Email = email,
                    Code = code
                });

            if (!verifyRes.Success || verifyRes.Data == null)
            {
                UiHelper.ShowError(verifyRes.Message, this);
                return;
            }

            using var resetForm = new frmDatMatKhauMoi(email, verifyRes.Data.TempToken);
            if (resetForm.ShowDialog(this) == DialogResult.OK)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }, this);
    }
}
