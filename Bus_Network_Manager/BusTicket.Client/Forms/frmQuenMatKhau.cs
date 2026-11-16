using System;
using System.Drawing;
using System.Windows.Forms;
using BusTicket.Client.Services;
using BusTicket.Core.DTOs;

namespace BusTicket.Client.Forms;

public class frmQuenMatKhau : Form
{
    private TextBox txtEmail = null!;
    private TextBox txtCode = null!;
    private TextBox txtNewPassword = null!;
    private Button btnSendCode = null!;
    private Button btnVerify = null!;
    private Button btnReset = null!;
    private string? _tempToken;

    public frmQuenMatKhau()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "Khôi phục mật khẩu - NT106.R12";
        this.Size = new Size(480, 480);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(41, 128, 185) };
        var lblTitle = new Label
        {
            Text = "QUÊN MẬT KHẨU",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlHeader.Controls.Add(lblTitle);
        this.Controls.Add(pnlHeader);

        int top = 85;
        int left = 40;
        int width = 380;

        this.Controls.Add(new Label { Text = "Email tài khoản:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        top += 22;
        txtEmail = new TextBox { Location = new Point(left, top), Width = width, Font = new Font("Segoe UI", 10) };
        this.Controls.Add(txtEmail);
        top += 35;

        btnSendCode = new Button
        {
            Text = "1. Gửi mã OTP xác thực qua Email",
            Location = new Point(left, top),
            Width = width,
            Height = 35,
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSendCode.Click += async (s, e) => await HandleSendCodeAsync();
        this.Controls.Add(btnSendCode);
        top += 45;

        this.Controls.Add(new Label { Text = "Mã xác thực 6 số (OTP):", Location = new Point(left, top), AutoSize = true });
        top += 22;
        txtCode = new TextBox { Location = new Point(left, top), Width = 210, Font = new Font("Segoe UI", 11) };
        this.Controls.Add(txtCode);

        btnVerify = new Button
        {
            Text = "2. Xác nhận",
            Location = new Point(left + 220, top - 2),
            Width = 160,
            Height = 30,
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnVerify.Click += async (s, e) => await HandleVerifyCodeAsync();
        this.Controls.Add(btnVerify);
        top += 45;

        this.Controls.Add(new Label { Text = "Mật khẩu mới muốn đặt:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        top += 22;
        txtNewPassword = new TextBox { UseSystemPasswordChar = true, Location = new Point(left, top), Width = width, Font = new Font("Segoe UI", 10) };
        this.Controls.Add(txtNewPassword);
        top += 40;

        btnReset = new Button
        {
            Text = "3. ĐẶT LẠI MẬT KHẨU MỚI",
            Location = new Point(left, top),
            Width = width,
            Height = 40,
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Enabled = false
        };
        btnReset.Click += async (s, e) => await HandleResetPasswordAsync();
        this.Controls.Add(btnReset);
    }

    private async Task HandleSendCodeAsync()
    {
        string email = txtEmail.Text.Trim();
        if (string.IsNullOrEmpty(email))
        {
            MessageBox.Show("Vui lòng nhập email", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnSendCode.Enabled = false;
        try
        {
            var req = new ForgotPasswordRequestCodeDto { Email = email };
            var res = await ApiClient.PostAsync<RequestCodeResponseData>("api/auth/forgot-password/request-code", req);
            if (res.Success)
            {
                MessageBox.Show("Mã OTP đã được gửi đến email (vui lòng kiểm tra email hoặc console server).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            btnSendCode.Enabled = true;
        }
    }

    private async Task HandleVerifyCodeAsync()
    {
        string email = txtEmail.Text.Trim();
        string code = txtCode.Text.Trim();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            MessageBox.Show("Vui lòng nhập đủ email và mã OTP", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnVerify.Enabled = false;
        try
        {
            var req = new VerifyCodeDto { Email = email, Code = code };
            var res = await ApiClient.PostAsync<VerifyCodeResponseData>("api/auth/forgot-password/verify-code", req);
            if (res.Success && res.Data != null)
            {
                _tempToken = res.Data.TempToken;
                btnReset.Enabled = true;
                MessageBox.Show("Mã OTP hợp lệ! Hãy nhập mật khẩu mới và bấm Bước 3.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(res.Message, "Mã sai hoặc hết hạn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            btnVerify.Enabled = true;
        }
    }

    private async Task HandleResetPasswordAsync()
    {
        string newPass = txtNewPassword.Text.Trim();
        if (string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(_tempToken))
        {
            MessageBox.Show("Vui lòng nhập mật khẩu mới", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnReset.Enabled = false;
        try
        {
            var req = new ResetPasswordDto { TempToken = _tempToken, NewPassword = newPass };
            var res = await ApiClient.PostAsync<object>("api/auth/forgot-password/reset", req);
            if (res.Success)
            {
                MessageBox.Show("Đặt lại mật khẩu thành công! Bạn có thể dùng mật khẩu mới để đăng nhập.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            btnReset.Enabled = true;
        }
    }
}
