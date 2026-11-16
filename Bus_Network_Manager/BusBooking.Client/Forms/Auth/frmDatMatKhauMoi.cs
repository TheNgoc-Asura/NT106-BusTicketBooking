using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Auth;

/// <summary>
/// Form Đặt mật khẩu mới sau khi xác thực OTP thành công (luồng Quên mật khẩu).
/// </summary>
public class frmDatMatKhauMoi : Form
{
    private readonly string _tempToken;
    private readonly ModernTextBox _txtNewPassword;
    private readonly ModernTextBox _txtConfirmPassword;
    private readonly ModernButton _btnConfirm;

    public frmDatMatKhauMoi(string email, string tempToken)
    {
        _tempToken = tempToken;

        Text = "Đặt mật khẩu mới";
        Size = new Size(460, 390);
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
            Text = "Thiết lập mật khẩu mới",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(24, 20)
        });

        card.Controls.Add(new Label
        {
            Text = $"Tài khoản: {email}",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.PrimaryDark,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(24, 52)
        });

        card.Controls.Add(UiHelper.CreateFieldLabel("MẬT KHẨU MỚI (TỐI THIỂU 6 KÝ TỰ)", 24, 88));
        _txtNewPassword = new ModernTextBox
        {
            Location = new Point(24, 110),
            Size = new Size(348, 42),
            Icon = IconChar.Lock,
            PlaceholderText = "Nhập mật khẩu mới...",
            UseSystemPasswordChar = true
        };

        card.Controls.Add(UiHelper.CreateFieldLabel("NHẬP LẠI MẬT KHẨU MỚI", 24, 166));
        _txtConfirmPassword = new ModernTextBox
        {
            Location = new Point(24, 188),
            Size = new Size(348, 42),
            Icon = IconChar.Key,
            PlaceholderText = "Xác nhận lại mật khẩu...",
            UseSystemPasswordChar = true
        };

        _btnConfirm = new ModernButton
        {
            Text = "Cập nhật mật khẩu",
            Icon = IconChar.Check,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(24, 250),
            Size = new Size(348, 44)
        };
        _btnConfirm.Click += async (_, _) => await ResetPasswordAsync();

        card.Controls.AddRange(new Control[] { _txtNewPassword, _txtConfirmPassword, _btnConfirm });
        Controls.Add(card);
    }

    private async Task ResetPasswordAsync()
    {
        if (_txtNewPassword.Text.Length < 6)
        {
            UiHelper.ShowError("Mật khẩu mới phải có ít nhất 6 ký tự.", this);
            return;
        }

        if (_txtNewPassword.Text != _txtConfirmPassword.Text)
        {
            UiHelper.ShowError("Mật khẩu xác nhận không khớp.", this);
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnConfirm, "Đang cập nhật...", async () =>
        {
            var res = await ApiService.PostAsync<object>("api/auth/forgot-password/reset", new ResetPasswordRequest
            {
                TempToken = _tempToken,
                NewPassword = _txtNewPassword.Text
            });

            if (!res.Success)
            {
                UiHelper.ShowError(res.Message, this);
                return;
            }

            UiHelper.ShowInfo("Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.", this);
            DialogResult = DialogResult.OK;
            Close();
        }, this);
    }
}
