using System;
using System.Drawing;
using System.Windows.Forms;
using BusTicket.Client.Services;
using BusTicket.Core.DTOs;
using BusTicket.Core.Enums;

namespace BusTicket.Client.Forms;

public class frmDangNhap : Form
{
    private TextBox txtServerUrl = null!;
    private TextBox txtEmail = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!;
    private Button btnRegister = null!;
    private Button btnForgotPassword = null!;
    private Label lblStatus = null!;

    public frmDangNhap()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "Hệ thống Đặt vé xe khách trực tuyến - NT106.R12 (Nhóm 07)";
        this.Size = new Size(540, 580);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(245, 247, 250);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = Color.FromArgb(30, 60, 114)
        };

        var lblTitle = new Label
        {
            Text = "ỨNG DỤNG ĐẶT VÉ XE KHÁCH",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 55
        };

        var lblSub = new Label
        {
            Text = "Môn: Lập trình mạng căn bản (NT106.R12) - Nhóm 07 - UIT",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(200, 220, 255),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };

        pnlHeader.Controls.Add(lblSub);
        pnlHeader.Controls.Add(lblTitle);
        this.Controls.Add(pnlHeader);

        int top = 120;
        int left = 50;
        int width = 420;

        // Server URL
        var lblServer = new Label { Text = "Địa chỉ Máy chủ (Web API Base URL):", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        this.Controls.Add(lblServer);
        top += 25;

        txtServerUrl = new TextBox { Text = SessionManager.BaseUrl, Location = new Point(left, top), Width = width, Font = new Font("Segoe UI", 10) };
        this.Controls.Add(txtServerUrl);
        top += 35;

        // Email
        var lblEmail = new Label { Text = "Email đăng nhập:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        this.Controls.Add(lblEmail);
        top += 25;

        txtEmail = new TextBox { Text = "nguyenvana@gmail.com", Location = new Point(left, top), Width = width, Font = new Font("Segoe UI", 10) };
        this.Controls.Add(txtEmail);
        top += 35;

        // Password
        var lblPass = new Label { Text = "Mật khẩu:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        this.Controls.Add(lblPass);
        top += 25;

        txtPassword = new TextBox { Text = "Matkhau@123", UseSystemPasswordChar = true, Location = new Point(left, top), Width = width, Font = new Font("Segoe UI", 10) };
        this.Controls.Add(txtPassword);
        top += 40;

        // Login Button
        btnLogin = new Button
        {
            Text = "ĐĂNG NHẬP",
            Location = new Point(left, top),
            Width = width,
            Height = 42,
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLogin.Click += async (s, e) => await HandleLoginAsync();
        this.Controls.Add(btnLogin);
        top += 50;

        // Register & Forgot buttons
        btnRegister = new Button
        {
            Text = "Đăng ký tài khoản mới",
            Location = new Point(left, top),
            Width = 200,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnRegister.Click += (s, e) =>
        {
            var reg = new frmDangKy();
            reg.ShowDialog();
        };
        this.Controls.Add(btnRegister);

        btnForgotPassword = new Button
        {
            Text = "Quên mật khẩu?",
            Location = new Point(left + 220, top),
            Width = 200,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnForgotPassword.Click += (s, e) =>
        {
            var forgot = new frmQuenMatKhau();
            forgot.ShowDialog();
        };
        this.Controls.Add(btnForgotPassword);
        top += 45;

        // Quick login for demo
        var grpDemo = new GroupBox
        {
            Text = "Đăng nhập nhanh (Dành cho Giảng viên / Hội đồng đánh giá):",
            Location = new Point(left, top),
            Width = width,
            Height = 110,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
        };

        var btnQuickCust = new Button { Text = "Khách Hàng", Location = new Point(15, 25), Width = 90, Height = 32, BackColor = Color.White };
        btnQuickCust.Click += (s, e) => { txtEmail.Text = "nguyenvana@gmail.com"; txtPassword.Text = "Matkhau@123"; };

        var btnQuickAdmin = new Button { Text = "Admin", Location = new Point(115, 25), Width = 90, Height = 32, BackColor = Color.White };
        btnQuickAdmin.Click += (s, e) => { txtEmail.Text = "admin@nhaxe.vn"; txtPassword.Text = "Matkhau@123"; };

        var btnQuickStaff = new Button { Text = "Nhà Xe", Location = new Point(215, 25), Width = 90, Height = 32, BackColor = Color.White };
        btnQuickStaff.Click += (s, e) => { txtEmail.Text = "tranvanb@nhaxe.vn"; txtPassword.Text = "Matkhau@123"; };

        var btnQuickSupport = new Button { Text = "Hỗ Trợ", Location = new Point(315, 25), Width = 90, Height = 32, BackColor = Color.White };
        btnQuickSupport.Click += (s, e) => { txtEmail.Text = "support@nhaxe.vn"; txtPassword.Text = "Matkhau@123"; };

        var lblDemoNote = new Label
        {
            Text = "Mật khẩu mẫu chung: Matkhau@123",
            Location = new Point(15, 70),
            AutoSize = true,
            ForeColor = Color.DarkGray
        };

        grpDemo.Controls.Add(btnQuickCust);
        grpDemo.Controls.Add(btnQuickAdmin);
        grpDemo.Controls.Add(btnQuickStaff);
        grpDemo.Controls.Add(btnQuickSupport);
        grpDemo.Controls.Add(lblDemoNote);
        this.Controls.Add(grpDemo);

        top += 120;
        lblStatus = new Label
        {
            Location = new Point(left, top),
            Width = width,
            ForeColor = Color.Red,
            TextAlign = ContentAlignment.MiddleCenter
        };
        this.Controls.Add(lblStatus);
    }

    private async Task HandleLoginAsync()
    {
        string email = txtEmail.Text.Trim();
        string pass = txtPassword.Text.Trim();
        SessionManager.BaseUrl = txtServerUrl.Text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            MessageBox.Show("Vui lòng nhập email và mật khẩu", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnLogin.Enabled = false;
        btnLogin.Text = "Đang đăng nhập...";
        lblStatus.Text = "";

        try
        {
            var req = new LoginRequest { Email = email, Password = pass };
            var res = await ApiClient.PostAsync<LoginResponseData>("api/auth/login", req);

            if (res.Success && res.Data != null)
            {
                SessionManager.SetSession(res.Data.Token, res.Data.User);

                // Start realtime WebSocket connection
                _ = RealtimeClient.Instance.StartAsync();

                this.Hide();

                if (res.Data.User.Role == Roles.Customer)
                {
                    var custForm = new frmCustomerMain();
                    custForm.FormClosed += (s, e) => this.Close();
                    custForm.Show();
                }
                else
                {
                    var adminForm = new frmAdminMain();
                    adminForm.FormClosed += (s, e) => this.Close();
                    adminForm.Show();
                }
            }
            else
            {
                lblStatus.Text = res.Message;
                MessageBox.Show(res.Message, "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            btnLogin.Enabled = true;
            btnLogin.Text = "ĐĂNG NHẬP";
        }
    }
}
