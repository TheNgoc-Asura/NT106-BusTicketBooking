using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Customer;

/// <summary>
/// Form Tài khoản cá nhân - Cập nhật hồ sơ và đổi mật khẩu.
/// </summary>
public class frmCustomerProfile : Form
{
    private readonly ModernTextBox _txtFullName;
    private readonly ModernTextBox _txtEmail;
    private readonly ModernTextBox _txtPhone;
    private readonly ModernButton _btnSaveProfile;

    private readonly ModernTextBox _txtOldPass;
    private readonly ModernTextBox _txtNewPass;
    private readonly ModernButton _btnChangePass;

    public event Action? ProfileUpdated;

    public frmCustomerProfile()
    {
        Text = "Tài khoản cá nhân";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 395,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.CanvasBg
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        var cardProfile = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            CornerRadius = 14,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Size = new Size(440, 395)
        };

        cardProfile.Controls.Add(new Label
        {
            Text = "Thông tin cá nhân",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(24, 20)
        });

        cardProfile.Controls.Add(UiHelper.CreateFieldLabel("EMAIL ĐĂNG NHẬP", 24, 66));
        _txtEmail = new ModernTextBox
        {
            Location = new Point(24, 88),
            Size = new Size(392, 40),
            Icon = IconChar.IdCard,
            ReadOnly = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        cardProfile.Controls.Add(UiHelper.CreateFieldLabel("HỌ VÀ TÊN", 24, 140));
        _txtFullName = new ModernTextBox
        {
            Location = new Point(24, 162),
            Size = new Size(392, 40),
            Icon = IconChar.User,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        cardProfile.Controls.Add(UiHelper.CreateFieldLabel("SỐ ĐIỆN THOẠI", 24, 214));
        _txtPhone = new ModernTextBox
        {
            Location = new Point(24, 236),
            Size = new Size(392, 40),
            Icon = IconChar.Headset,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _btnSaveProfile = new ModernButton
        {
            Text = "Lưu thay đổi",
            Icon = IconChar.Check,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Location = new Point(24, 300),
            Size = new Size(176, 42)
        };
        _btnSaveProfile.Click += async (_, _) => await SaveProfileAsync();

        cardProfile.Controls.AddRange(new Control[] { _txtEmail, _txtFullName, _txtPhone, _btnSaveProfile });

        var cardPass = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(12, 0, 0, 0),
            CornerRadius = 14,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Size = new Size(440, 395)
        };

        cardPass.Controls.Add(new Label
        {
            Text = "Đổi mật khẩu bảo mật",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(24, 20)
        });

        cardPass.Controls.Add(UiHelper.CreateFieldLabel("MẬT KHẨU HIỆN TẠI", 24, 66));
        _txtOldPass = new ModernTextBox
        {
            Location = new Point(24, 88),
            Size = new Size(392, 40),
            Icon = IconChar.Lock,
            UseSystemPasswordChar = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        cardPass.Controls.Add(UiHelper.CreateFieldLabel("MẬT KHẨU MỚI (TỐI THIỂU 6 KÝ TỰ)", 24, 140));
        _txtNewPass = new ModernTextBox
        {
            Location = new Point(24, 162),
            Size = new Size(392, 40),
            Icon = IconChar.Key,
            UseSystemPasswordChar = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _btnChangePass = new ModernButton
        {
            Text = "Cập nhật mật khẩu",
            Icon = IconChar.ShieldAlt,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(24, 232),
            Size = new Size(200, 42)
        };
        _btnChangePass.Click += async (_, _) => await ChangePasswordAsync();

        cardPass.Controls.AddRange(new Control[] { _txtOldPass, _txtNewPass, _btnChangePass });

        grid.Controls.Add(cardProfile, 0, 0);
        grid.Controls.Add(cardPass, 1, 0);
        Controls.Add(grid);
    }

    public async Task LoadAsync()
    {
        var res = await ApiService.GetAsync<ProfileDto>("api/account/profile");
        if (!res.Success || res.Data == null)
            return;

        _txtEmail.Text = res.Data.Email;
        _txtFullName.Text = res.Data.FullName;
        _txtPhone.Text = res.Data.Phone ?? string.Empty;
    }

    private async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtFullName.Text))
        {
            UiHelper.ShowError("Họ tên không được để trống.", FindForm());
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnSaveProfile, "Đang lưu...", async () =>
        {
            var res = await ApiService.PutAsync<UpdateProfileResponseData>("api/account/profile",
                new UpdateProfileRequest
                {
                    FullName = _txtFullName.Text.Trim(),
                    Phone = string.IsNullOrWhiteSpace(_txtPhone.Text) ? null : _txtPhone.Text.Trim()
                });

            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            SessionManager.UpdateUserInfo(res.Data.FullName, res.Data.Phone);
            ProfileUpdated?.Invoke();
            UiHelper.ShowInfo("Cập nhật thông tin cá nhân thành công!", FindForm());
        }, FindForm());
    }

    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrEmpty(_txtOldPass.Text) || _txtNewPass.Text.Length < 6)
        {
            UiHelper.ShowError("Vui lòng nhập mật khẩu cũ và mật khẩu mới tối thiểu 6 ký tự.", FindForm());
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnChangePass, "Đang đổi...", async () =>
        {
            var res = await ApiService.PostAsync<object>("api/account/change-password",
                new ChangePasswordRequest
                {
                    OldPassword = _txtOldPass.Text,
                    NewPassword = _txtNewPass.Text
                });

            if (!res.Success)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            _txtOldPass.Text = string.Empty;
            _txtNewPass.Text = string.Empty;
            UiHelper.ShowInfo("Đổi mật khẩu thành công!", FindForm());
        }, FindForm());
    }
}
