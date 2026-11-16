using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Auth;

public class frmRegister : Form
{
    private readonly ModernTextBox _txtFullName;
    private readonly ModernTextBox _txtEmail;
    private readonly ModernTextBox _txtPassword;
    private readonly ModernTextBox _txtOtp;
    private readonly ModernButton _btnSendOtp;
    private readonly ModernButton _btnComplete;
    private readonly Label _lblStepInfo;

    public frmRegister()
    {
        Text = "Đăng ký tài khoản mới";
        Size = new Size(520, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(24);
        DoubleBuffered = true;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 14,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder
        };

        var lblTitle = new Label
        {
            Text = "Tạo tài khoản hành khách",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(26, 22)
        };

        _lblStepInfo = new Label
        {
            Text = "Bước 1: Nhập thông tin để nhận mã xác thực 6 số qua Email",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(26, 54)
        };

        var lblName = UiHelper.CreateFieldLabel("HỌ VÀ TÊN", 26, 88);
        _txtFullName = new ModernTextBox
        {
            Location = new Point(26, 110),
            Size = new Size(400, 40),
            Icon = IconChar.User,
            PlaceholderText = "Nguyễn Văn A"
        };

        var lblEmail = UiHelper.CreateFieldLabel("EMAIL ĐĂNG KÝ", 26, 160);
        _txtEmail = new ModernTextBox
        {
            Location = new Point(26, 182),
            Size = new Size(400, 40),
            Icon = IconChar.IdCard,
            PlaceholderText = "email@example.com"
        };

        var lblPass = UiHelper.CreateFieldLabel("MẬT KHẨU (TỐI THIỂU 6 KÝ TỰ)", 26, 232);
        _txtPassword = new ModernTextBox
        {
            Location = new Point(26, 254),
            Size = new Size(400, 40),
            Icon = IconChar.Lock,
            PlaceholderText = "Nhập mật khẩu...",
            UseSystemPasswordChar = true
        };

        _btnSendOtp = new ModernButton
        {
            Text = "Gửi mã xác thực OTP",
            Icon = IconChar.PaperPlane,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Location = new Point(26, 308),
            Size = new Size(400, 40)
        };
        _btnSendOtp.Click += async (_, _) => await RequestOtpAsync();

        var lblOtp = UiHelper.CreateFieldLabel("MÃ XÁC THỰC 6 SỐ (OTP)", 26, 362);
        _txtOtp = new ModernTextBox
        {
            Location = new Point(26, 384),
            Size = new Size(190, 42),
            Icon = IconChar.Key,
            PlaceholderText = "123456"
        };

        _btnComplete = new ModernButton
        {
            Text = "Xác nhận & Đăng ký",
            Icon = IconChar.Check,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(226, 384),
            Size = new Size(200, 42),
            Enabled = false
        };
        _btnComplete.Click += async (_, _) => await VerifyAndCompleteAsync();

        card.Controls.AddRange(new Control[]
        {
            lblTitle, _lblStepInfo,
            lblName, _txtFullName,
            lblEmail, _txtEmail,
            lblPass, _txtPassword,
            _btnSendOtp,
            lblOtp, _txtOtp, _btnComplete
        });

        Controls.Add(card);
    }

    private async Task RequestOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtFullName.Text) ||
            string.IsNullOrWhiteSpace(_txtEmail.Text) ||
            _txtPassword.Text.Length < 6)
        {
            UiHelper.ShowError("Vui lòng nhập họ tên, email hợp lệ và mật khẩu tối thiểu 6 ký tự.", this);
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnSendOtp, "Đang gửi OTP...", async () =>
        {
            var res = await ApiService.PostAsync<RequestCodeResponseData>("api/auth/register/request-code",
                new RegisterRequestCodeRequest
                {
                    FullName = _txtFullName.Text.Trim(),
                    Email = _txtEmail.Text.Trim(),
                    Password = _txtPassword.Text
                });

            if (!res.Success)
            {
                UiHelper.ShowError(res.Message, this);
                return;
            }

            _btnComplete.Enabled = true;
            _lblStepInfo.Text = $"Bước 2: Mã OTP đã gửi tới {_txtEmail.Text.Trim()} (hiệu lực {res.Data?.ExpiresInSeconds ?? 300}s)";
            _lblStepInfo.ForeColor = AppTheme.SuccessFg;
            _txtOtp.FocusInput();
        }, this);
    }

    private async Task VerifyAndCompleteAsync()
    {
        string code = _txtOtp.Text.Trim();
        if (code.Length != 6)
        {
            UiHelper.ShowError("Vui lòng nhập đủ mã OTP 6 chữ số.", this);
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnComplete, "Đang hoàn tất...", async () =>
        {
            var verifyRes = await ApiService.PostAsync<VerifyCodeResponseData>("api/auth/register/verify-code",
                new VerifyCodeRequest
                {
                    Email = _txtEmail.Text.Trim(),
                    Code = code
                });

            if (!verifyRes.Success || verifyRes.Data == null)
            {
                UiHelper.ShowError(verifyRes.Message, this);
                return;
            }

            var completeRes = await ApiService.PostAsync<CompleteRegisterResponseData>("api/auth/register/complete",
                new CompleteRegisterRequest
                {
                    TempToken = verifyRes.Data.TempToken,
                    FullName = _txtFullName.Text.Trim(),
                    Password = _txtPassword.Text
                });

            if (!completeRes.Success)
            {
                UiHelper.ShowError(completeRes.Message, this);
                return;
            }

            UiHelper.ShowInfo("Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.", this);
            DialogResult = DialogResult.OK;
            Close();
        }, this);
    }
}
