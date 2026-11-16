using System;
using System.Drawing;
using System.Windows.Forms;
using BusTicket.Client.Services;
using BusTicket.Core.DTOs;

namespace BusTicket.Client.Forms;

public class frmDangKy : Form
{
    private TextBox txtFullName = null!;
    private TextBox txtEmail = null!;
    private TextBox txtPassword = null!;
    private TextBox txtCode = null!;
    private Button btnSendCode = null!;
    private Button btnVerify = null!;
    private Button btnComplete = null!;
    private Label lblInfo = null!;
    private string? _tempToken;

    public frmDangKy()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "Đăng ký tài khoản hành khách - NT106.R12";
        this.Size = new Size(500, 560);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(41, 128, 185) };
        var lblTitle = new Label
        {
            Text = "ĐĂNG KÝ TÀI KHOẢN",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlHeader.Controls.Add(lblTitle);
        this.Controls.Add(pnlHeader);

        int top = 90;
        int left = 40;
        int width = 400;

        // Step 1 info
        var lblStep1 = new Label { Text = "Bước 1: Điền thông tin cá nhân", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) };
        this.Controls.Add(lblStep1);
        top += 25;

        this.Controls.Add(new Label { Text = "Họ và tên:", Location = new Point(left, top), AutoSize = true });
        top += 20;
        txtFullName = new TextBox { Location = new Point(left, top), Width = width, Font = new Font("Segoe UI", 10) };
        this.Controls.Add(txtFullName);
        top += 30;

        this.Controls.Add(new Label { Text = "Email:", Location = new Point(left, top), AutoSize = true });
        top += 20;
        txtEmail = new TextBox { Location = new Point(left, top), Width = width, Font = new Font("Segoe UI", 10) };
        this.Controls.Add(txtEmail);
        top += 30;

        this.Controls.Add(new Label { Text = "Mật khẩu:", Location = new Point(left, top), AutoSize = true });
        top += 20;
        txtPassword = new TextBox { UseSystemPasswordChar = true, Location = new Point(left, top), Width = width, Font = new Font("Segoe UI", 10) };
        this.Controls.Add(txtPassword);
        top += 35;

        btnSendCode = new Button
        {
            Text = "1. Gửi mã xác thực qua Email (OTP)",
            Location = new Point(left, top),
            Width = width,
            Height = 35,
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        btnSendCode.Click += async (s, e) => await HandleSendCodeAsync();
        this.Controls.Add(btnSendCode);
        top += 50;

        // Step 2 info
        var lblStep2 = new Label { Text = "Bước 2: Xác nhận mã OTP 6 số & Hoàn tất", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) };
        this.Controls.Add(lblStep2);
        top += 25;

        this.Controls.Add(new Label { Text = "Mã xác thực (6 số nhận qua Email / Console):", Location = new Point(left, top), AutoSize = true });
        top += 20;
        txtCode = new TextBox { Location = new Point(left, top), Width = 230, Font = new Font("Segoe UI", 11) };
        this.Controls.Add(txtCode);

        btnVerify = new Button
        {
            Text = "2. Xác nhận mã",
            Location = new Point(left + 240, top - 2),
            Width = 160,
            Height = 30,
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnVerify.Click += async (s, e) => await HandleVerifyCodeAsync();
        this.Controls.Add(btnVerify);
        top += 45;

        btnComplete = new Button
        {
            Text = "3. HOÀN TẤT ĐĂNG KÝ",
            Location = new Point(left, top),
            Width = width,
            Height = 40,
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Enabled = false
        };
        btnComplete.Click += async (s, e) => await HandleCompleteRegisterAsync();
        this.Controls.Add(btnComplete);
        top += 45;

        lblInfo = new Label
        {
            Location = new Point(left, top),
            Width = width,
            Height = 35,
            ForeColor = Color.DarkSlateGray,
            TextAlign = ContentAlignment.MiddleCenter
        };
        this.Controls.Add(lblInfo);
    }

    private async Task HandleSendCodeAsync()
    {
        string name = txtFullName.Text.Trim();
        string email = txtEmail.Text.Trim();
        string pass = txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            MessageBox.Show("Vui lòng điền đủ họ tên, email và mật khẩu", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnSendCode.Enabled = false;
        btnSendCode.Text = "Đang gửi OTP...";

        try
        {
            var req = new RegisterRequestCodeDto { FullName = name, Email = email, Password = pass };
            var res = await ApiClient.PostAsync<RequestCodeResponseData>("api/auth/register/request-code", req);

            if (res.Success)
            {
                lblInfo.Text = "Mã OTP đã được gửi đến email (và xuất ra console server)!";
                MessageBox.Show("Mã xác thực 6 số đã được gửi tới email của bạn (vui lòng kiểm tra email hoặc console server).", "Đã gửi mã", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            btnSendCode.Enabled = true;
            btnSendCode.Text = "1. Gửi mã xác thực qua Email (OTP)";
        }
    }

    private async Task HandleVerifyCodeAsync()
    {
        string email = txtEmail.Text.Trim();
        string code = txtCode.Text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            MessageBox.Show("Vui lòng nhập email và mã OTP", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnVerify.Enabled = false;

        try
        {
            var req = new VerifyCodeDto { Email = email, Code = code };
            var res = await ApiClient.PostAsync<VerifyCodeResponseData>("api/auth/register/verify-code", req);

            if (res.Success && res.Data != null)
            {
                _tempToken = res.Data.TempToken;
                btnComplete.Enabled = true;
                lblInfo.Text = "Xác nhận mã thành công! Bấm Bước 3 để hoàn tất.";
                MessageBox.Show("Mã OTP chính xác. Vui lòng bấm '3. Hoàn tất đăng ký'.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(res.Message, "Mã không đúng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            btnVerify.Enabled = true;
        }
    }

    private async Task HandleCompleteRegisterAsync()
    {
        if (string.IsNullOrEmpty(_tempToken))
        {
            MessageBox.Show("Vui lòng xác nhận mã OTP trước", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnComplete.Enabled = false;
        try
        {
            var req = new CompleteRegisterDto
            {
                TempToken = _tempToken,
                FullName = txtFullName.Text.Trim(),
                Password = txtPassword.Text.Trim()
            };
            var res = await ApiClient.PostAsync<CompleteRegisterResponseData>("api/auth/register/complete", req);

            if (res.Success)
            {
                MessageBox.Show("Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            btnComplete.Enabled = true;
        }
    }
}
