using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusTicket.Client.Services;
using BusTicket.Core.DTOs;
using BusTicket.Core.Enums;

namespace BusTicket.Client.Forms;

public class frmAdminMain : Form
{
    private Panel pnlSidebar = null!;
    private Panel pnlContent = null!;
    private Label lblRoleInfo = null!;
    private Label lblWsStatus = null!;

    public frmAdminMain()
    {
        InitializeComponents();
        SetupRealtimeEvents();
        ShowDashboardView();
    }

    private void InitializeComponents()
    {
        this.Text = "Hệ thống Đặt vé xe khách trực tuyến - Phân hệ Quản trị & Điều hành";
        this.Size = new Size(1280, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1150, 720);
        this.Font = new Font("Segoe UI", 9.5f);

        // Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(24, 43, 73) };
        var lblBrand = new Label
        {
            Text = "🚌 BAN QUẢN TRỊ & ĐIỀU HÀNH NHÀ XE",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(15, 17),
            AutoSize = true
        };
        lblRoleInfo = new Label
        {
            Text = $"Tài khoản: {SessionManager.CurrentUser?.FullName} | Vai trò: [{SessionManager.CurrentUser?.Role}]",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(200, 220, 255),
            Location = new Point(450, 19),
            AutoSize = true
        };
        lblWsStatus = new Label
        {
            Text = "🟢 WebSocket: Đã kết nối",
            ForeColor = Color.PaleGreen,
            Location = new Point(920, 19),
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        var btnLogout = new Button
        {
            Text = "Đăng xuất",
            Location = new Point(1150, 14),
            Width = 90,
            Height = 32,
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLogout.Click += (s, e) =>
        {
            _ = RealtimeClient.Instance.StopAsync();
            SessionManager.ClearSession();
            this.Close();
        };

        pnlHeader.Controls.AddRange(new Control[] { lblBrand, lblRoleInfo, lblWsStatus, btnLogout });
        this.Controls.Add(pnlHeader);

        // Sidebar
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(0, 10, 0, 0)
        };

        string role = SessionManager.CurrentUser?.Role ?? Roles.Staff;

        AddSidebarButton("📊 Tổng Quan", () => ShowDashboardView());

        if (role == Roles.Admin || role == Roles.Staff)
        {
            AddSidebarButton("🚌 Quản lý chuyến xe", () => ShowTripsView());
            AddSidebarButton("🛣️ Quản lý tuyến xe", () => ShowRoutesView());
            AddSidebarButton("🚐 Quản lý xe & Bảo trì", () => ShowVehiclesView());
            AddSidebarButton("💺 Sơ đồ ghế chuyến xe", () => ShowSeatsView());
            AddSidebarButton("🎟️ Quản lý vé & Bán vé", () => ShowTicketsView());
        }

        if (role == Roles.Admin)
        {
            AddSidebarButton("👥 Phân quyền nhân viên", () => ShowPermissionsView());
        }

        if (role == Roles.Admin || role == Roles.Staff || role == Roles.Support)
        {
            AddSidebarButton("🔎 Tra cứu khách hàng", () => ShowCustomersView());
        }

        if (role == Roles.Admin || role == Roles.Support)
        {
            AddSidebarButton("💬 Hỗ trợ khách hàng", () => ShowSupportTicketsView());
        }

        if (role == Roles.Admin)
        {
            AddSidebarButton("📈 Báo cáo doanh thu", () => ShowRevenueView());
        }

        this.Controls.Add(pnlSidebar);

        // Content Area
        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(10)
        };
        this.Controls.Add(pnlContent);
    }

    private void AddSidebarButton(string title, Action action)
    {
        var btn = new Button
        {
            Text = "  " + title,
            Dock = DockStyle.Top,
            Height = 44,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 41, 59),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(51, 65, 85);
        btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(30, 41, 59);
        btn.Click += (s, e) => action();
        pnlSidebar.Controls.Add(btn);
        btn.BringToFront();
    }

    private void SetCurrentView(Control view)
    {
        pnlContent.Controls.Clear();
        view.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(view);
    }

    private void SetupRealtimeEvents()
    {
        RealtimeClient.Instance.OnConnectionChanged += (connected) =>
        {
            if (this.IsDisposed) return;
            this.Invoke(() =>
            {
                lblWsStatus.Text = connected ? "🟢 WebSocket: Đã kết nối" : "🔴 WebSocket: Ngắt kết nối";
                lblWsStatus.ForeColor = connected ? Color.PaleGreen : Color.Tomato;
            });
        };
    }

    private static DataGridView CreateStyledGrid()
    {
        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            EnableHeadersVisualStyles = false
        };
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 60, 114);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgv.ColumnHeadersHeight = 35;
        dgv.RowTemplate.Height = 28;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);
        return dgv;
    }

    // =========================================================================
    // 1. TỔNG QUAN (DASHBOARD)
    // =========================================================================
    private void ShowDashboardView()
    {
        var pnl = new Panel { AutoScroll = true, Dock = DockStyle.Fill };

        var lblTitle = new Label { Text = "TỔNG QUAN HOẠT ĐỘNG TRONG NGÀY (DASHBOARD)", Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) };
        pnl.Controls.Add(lblTitle);

        var pnlCards = new Panel { Location = new Point(10, 45), Size = new Size(980, 95) };
        var card1 = CreateMetricCard("VÉ BÁN HÔM NAY", "0 vé", Color.FromArgb(41, 128, 185)); card1.Location = new Point(0, 0);
        var card2 = CreateMetricCard("CHUYẾN CHẠY HÔM NAY", "0 chuyến", Color.FromArgb(39, 174, 96)); card2.Location = new Point(325, 0);
        var card3 = CreateMetricCard("DOANH THU TRONG NGÀY", "0 VNĐ", Color.FromArgb(230, 126, 34)); card3.Location = new Point(650, 0);
        pnlCards.Controls.AddRange(new Control[] { card1, card2, card3 });
        pnl.Controls.Add(pnlCards);

        var lblUpcoming = new Label { Text = "DANH SÁCH CHUYẾN XE SẮP KHỞI HÀNH:", Location = new Point(10, 155), AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
        pnl.Controls.Add(lblUpcoming);

        var dgvUpcoming = CreateStyledGrid();
        var pnlGridContainer = new Panel { Location = new Point(10, 185), Size = new Size(980, 480) };
        pnlGridContainer.Controls.Add(dgvUpcoming);
        pnl.Controls.Add(pnlGridContainer);

        SetCurrentView(pnl);

        _ = Task.Run(async () =>
        {
            var res = await ApiClient.GetAsync<DashboardStatsDto>("api/admin/dashboard/stats");
            if (res.Success && res.Data != null && !this.IsDisposed)
            {
                this.Invoke(() =>
                {
                    ((Label)card1.Controls[1]).Text = $"{res.Data.TicketsToday} vé";
                    ((Label)card2.Controls[1]).Text = $"{res.Data.TripsToday} chuyến";
                    ((Label)card3.Controls[1]).Text = $"{res.Data.RevenueToday:N0} đ";
                    dgvUpcoming.DataSource = res.Data.UpcomingTrips.Select(t => new
                    {
                        MaChuyen = t.TripCode,
                        TuyenDuong = t.RouteName,
                        GioKhoiHanh = t.DepartureTime.ToString("dd/MM/yyyy HH:mm"),
                        GheDaDat = t.BookedSeats,
                        TongSoGhe = t.TotalSeats
                    }).ToList();
                    FormatGridColumns(dgvUpcoming, new Dictionary<string, string>
                    {
                        { "MaChuyen", "Mã Chuyến" },
                        { "TuyenDuong", "Tuyến Đường" },
                        { "GioKhoiHanh", "Giờ Khởi Hành" },
                        { "GheDaDat", "Số Ghế Đã Đặt" },
                        { "TongSoGhe", "Tổng Số Ghế" }
                    });
                });
            }
        });
    }

    private Panel CreateMetricCard(string title, string value, Color accentColor)
    {
        var card = new Panel { Size = new Size(305, 90), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var pnlBar = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = accentColor };
        var lblT = new Label { Text = title, Location = new Point(15, 12), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        var lblV = new Label { Text = value, Location = new Point(15, 38), AutoSize = true, ForeColor = accentColor, Font = new Font("Segoe UI", 16, FontStyle.Bold) };
        card.Controls.AddRange(new Control[] { pnlBar, lblV, lblT });
        return card;
    }

    private static void FormatGridColumns(DataGridView dgv, Dictionary<string, string> mappings)
    {
        foreach (var kvp in mappings)
        {
            if (dgv.Columns.Contains(kvp.Key))
            {
                dgv.Columns[kvp.Key].HeaderText = kvp.Value;
            }
        }
    }

    // =========================================================================
    // 2. QUẢN LÝ CHUYẾN XE (Master - Detail: SỬA TRỰC TIẾP & THÊM MỚI)
    // =========================================================================
    private void ShowTripsView()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 640 };

        // Left Panel: Trips Grid
        var pnlLeft = new Panel { Dock = DockStyle.Fill };
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(240, 244, 248) };
        var btnNewTrip = new Button { Text = "➕ Thêm Chuyến Mới", Location = new Point(10, 8), Width = 150, Height = 32, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        var btnRefresh = new Button { Text = "🔄 Làm Mới", Location = new Point(170, 8), Width = 100, Height = 32, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
        pnlTop.Controls.AddRange(new Control[] { btnNewTrip, btnRefresh });

        var dgv = CreateStyledGrid();
        pnlLeft.Controls.AddRange(new Control[] { dgv, pnlTop });
        split.Panel1.Controls.Add(pnlLeft);

        // Right Panel: Form Chỉnh Sửa / Thêm Mới Chuyến Xe
        var pnlRight = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(15) };
        var lblFormTitle = new Label { Text = "CHI TIẾT & CHỈNH SỬA CHUYẾN XE", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) };
        pnlRight.Controls.Add(lblFormTitle);

        int top = 45, left = 15, width = 340;

        pnlRight.Controls.Add(new Label { Text = "Mã chuyến xe (Tự động / Xem):", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtTripCode = new TextBox { Location = new Point(left, top), Width = width, ReadOnly = true, BackColor = Color.FromArgb(240, 240, 240) };
        pnlRight.Controls.Add(txtTripCode); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Tuyến xe:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var cbRoute = new ComboBox { Location = new Point(left, top), Width = width, DropDownStyle = ComboBoxStyle.DropDownList };
        pnlRight.Controls.Add(cbRoute); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Xe vận chuyển:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var cbVehicle = new ComboBox { Location = new Point(left, top), Width = width, DropDownStyle = ComboBoxStyle.DropDownList };
        pnlRight.Controls.Add(cbVehicle); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Tài xế phụ trách:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var cbDriver = new ComboBox { Location = new Point(left, top), Width = width, DropDownStyle = ComboBoxStyle.DropDownList };
        pnlRight.Controls.Add(cbDriver); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Giờ khởi hành:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var dtpDep = new DateTimePicker { Location = new Point(left, top), Width = width, CustomFormat = "yyyy-MM-dd HH:mm", Format = DateTimePickerFormat.Custom };
        pnlRight.Controls.Add(dtpDep); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Giá vé niêm yết (VNĐ):", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtPrice = new TextBox { Location = new Point(left, top), Width = width, Text = "280000" };
        pnlRight.Controls.Add(txtPrice); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Trạng thái chuyến:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var cbStatus = new ComboBox { Location = new Point(left, top), Width = width, DropDownStyle = ComboBoxStyle.DropDownList };
        cbStatus.Items.AddRange(new object[] { TripStatuses.Upcoming, TripStatuses.Running, TripStatuses.Completed, TripStatuses.Cancelled });
        cbStatus.SelectedIndex = 0;
        pnlRight.Controls.Add(cbStatus); top += 38;

        // Action Buttons
        var btnSaveEdit = new Button
        {
            Text = "💾 CẬP NHẬT CHUYẾN",
            Location = new Point(left, top),
            Width = 165,
            Height = 38,
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        var btnSaveNew = new Button
        {
            Text = "➕ LƯU MỚI",
            Location = new Point(left + 175, top),
            Width = 165,
            Height = 38,
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        top += 44;

        var btnCancelTrip = new Button
        {
            Text = "❌ HỦY CHUYẾN NÀY",
            Location = new Point(left, top),
            Width = width,
            Height = 36,
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        pnlRight.Controls.AddRange(new Control[] { btnSaveEdit, btnSaveNew, btnCancelTrip });
        split.Panel2.Controls.Add(pnlRight);

        SetCurrentView(split);

        int currentTripId = 0;
        TripFormOptionsDto? options = null;

        async Task LoadOptionsAsync()
        {
            var res = await ApiClient.GetAsync<TripFormOptionsDto>("api/admin/trips/form-options");
            if (res.Success && res.Data != null)
            {
                options = res.Data;
                cbRoute.DataSource = options.Routes; cbRoute.DisplayMember = "Name"; cbRoute.ValueMember = "Id";
                cbVehicle.DataSource = options.Vehicles; cbVehicle.DisplayMember = "LicensePlate"; cbVehicle.ValueMember = "Id";
                cbDriver.DataSource = options.Drivers; cbDriver.DisplayMember = "Name"; cbDriver.ValueMember = "Id";
            }
        }

        async Task LoadTripsListAsync()
        {
            var res = await ApiClient.GetAsync<PagedResult<AdminTripDto>>("api/admin/trips?page=1&pageSize=50");
            if (res.Success && res.Data != null)
            {
                dgv.DataSource = res.Data.Items.Select(t => new
                {
                    MaChuyen = t.TripCode,
                    TuyenDuong = t.RouteName,
                    GioKhoiHanh = t.DepartureTime.ToString("dd/MM/yyyy HH:mm"),
                    BienSoXe = t.VehiclePlate,
                    TaiXe = t.DriverName,
                    GiaVe = $"{t.Price:N0} đ",
                    GheDaDat = $"{t.BookedSeats}/{t.TotalSeats}",
                    TrangThai = t.Status,
                    RawObj = t
                }).ToList();

                FormatGridColumns(dgv, new Dictionary<string, string>
                {
                    { "MaChuyen", "Mã Chuyến" },
                    { "TuyenDuong", "Tuyến Đường" },
                    { "GioKhoiHanh", "Giờ Khởi Hành" },
                    { "BienSoXe", "Biển Số Xe" },
                    { "TaiXe", "Tài Xế" },
                    { "GiaVe", "Giá Vé" },
                    { "GheDaDat", "Số Ghế" },
                    { "TrangThai", "Trạng Thái" }
                });
                if (dgv.Columns.Contains("RawObj")) dgv.Columns["RawObj"].Visible = false;
            }
        }

        dgv.SelectionChanged += (s, e) =>
        {
            if (dgv.SelectedRows.Count > 0 && dgv.SelectedRows[0].Cells["RawObj"].Value is AdminTripDto t)
            {
                currentTripId = t.TripId;
                txtTripCode.Text = t.TripCode;
                dtpDep.Value = t.DepartureTime;
                txtPrice.Text = t.Price.ToString("0");
                cbStatus.SelectedItem = t.Status;

                // Match comboboxes
                if (options != null)
                {
                    var r = options.Routes.FirstOrDefault(x => x.Name == t.RouteName);
                    if (r != null) cbRoute.SelectedValue = r.Id;
                    var v = options.Vehicles.FirstOrDefault(x => x.LicensePlate == t.VehiclePlate);
                    if (v != null) cbVehicle.SelectedValue = v.Id;
                    var d = options.Drivers.FirstOrDefault(x => x.Name == t.DriverName);
                    if (d != null) cbDriver.SelectedValue = d.Id;
                }
            }
        };

        btnNewTrip.Click += (s, e) =>
        {
            currentTripId = 0;
            txtTripCode.Text = "(Hệ thống tự sinh mã khi lưu)";
            dtpDep.Value = DateTime.Today.AddDays(1).AddHours(8);
            txtPrice.Text = "280000";
            cbStatus.SelectedIndex = 0;
            MessageBox.Show("Form đã sẵn sàng! Vui lòng chọn thông tin và bấm '➕ LƯU MỚI'.", "Thêm chuyến mới", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        btnSaveEdit.Click += async (s, e) =>
        {
            if (currentTripId == 0)
            {
                MessageBox.Show("Vui lòng click chọn 1 chuyến xe trên danh sách để sửa, hoặc bấm 'LƯU MỚI' nếu muốn thêm mới.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, out var price))
            {
                MessageBox.Show("Giá vé không hợp lệ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var req = new UpdateTripRequest
            {
                RouteId = (int)cbRoute.SelectedValue!,
                VehicleId = (int)cbVehicle.SelectedValue!,
                DriverId = (int)cbDriver.SelectedValue!,
                DepartureTime = dtpDep.Value,
                Price = price,
                Surcharge = 0,
                Status = cbStatus.SelectedItem?.ToString() ?? TripStatuses.Upcoming
            };

            var res = await ApiClient.PutAsync<object>($"api/admin/trips/{currentTripId}", req);
            if (res.Success)
            {
                MessageBox.Show("Cập nhật thông tin chuyến xe thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadTripsListAsync();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnSaveNew.Click += async (s, e) =>
        {
            if (!decimal.TryParse(txtPrice.Text, out var price)) return;
            var req = new CreateTripRequest
            {
                RouteId = (int)cbRoute.SelectedValue!,
                VehicleId = (int)cbVehicle.SelectedValue!,
                DriverId = (int)cbDriver.SelectedValue!,
                DepartureTime = dtpDep.Value,
                Price = price,
                Surcharge = 0
            };
            var res = await ApiClient.PostAsync<object>("api/admin/trips", req);
            if (res.Success)
            {
                MessageBox.Show("Thêm chuyến xe mới thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadTripsListAsync();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi thêm chuyến", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnCancelTrip.Click += async (s, e) =>
        {
            if (currentTripId == 0) return;
            if (MessageBox.Show($"Bạn có chắc chắn muốn hủy chuyến xe {txtTripCode.Text}?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var req = new UpdateTripRequest
                {
                    RouteId = (int)cbRoute.SelectedValue!,
                    VehicleId = (int)cbVehicle.SelectedValue!,
                    DriverId = (int)cbDriver.SelectedValue!,
                    DepartureTime = dtpDep.Value,
                    Price = decimal.TryParse(txtPrice.Text, out var pr) ? pr : 280000,
                    Status = TripStatuses.Cancelled
                };
                await ApiClient.PutAsync<object>($"api/admin/trips/{currentTripId}", req);
                await LoadTripsListAsync();
            }
        };

        btnRefresh.Click += async (s, e) => await LoadTripsListAsync();

        _ = Task.Run(async () =>
        {
            await LoadOptionsAsync();
            await LoadTripsListAsync();
        });
    }

    // =========================================================================
    // 3. QUẢN LÝ TUYẾN XE (Master - Detail: SỬA TRỰC TIẾP & THÊM MỚI)
    // =========================================================================
    private void ShowRoutesView()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 640 };

        var pnlLeft = new Panel { Dock = DockStyle.Fill };
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(240, 244, 248) };
        var btnNew = new Button { Text = "➕ Thêm Tuyến Mới", Location = new Point(10, 8), Width = 150, Height = 32, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        var btnRefresh = new Button { Text = "🔄 Làm Mới", Location = new Point(170, 8), Width = 100, Height = 32, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
        pnlTop.Controls.AddRange(new Control[] { btnNew, btnRefresh });

        var dgv = CreateStyledGrid();
        pnlLeft.Controls.AddRange(new Control[] { dgv, pnlTop });
        split.Panel1.Controls.Add(pnlLeft);

        // Right Panel: Sửa / Thêm Tuyến
        var pnlRight = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(15) };
        pnlRight.Controls.Add(new Label { Text = "CHI TIẾT & CHỈNH SỬA TUYẾN ĐƯỜNG", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) });

        int top = 45, left = 15, width = 340;
        pnlRight.Controls.Add(new Label { Text = "Mã tuyến:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtCode = new TextBox { Location = new Point(left, top), Width = width, ReadOnly = true, BackColor = Color.FromArgb(240, 240, 240) };
        pnlRight.Controls.Add(txtCode); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Điểm khởi hành (Origin):", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtOrigin = new TextBox { Location = new Point(left, top), Width = width, Text = "TP.HCM" };
        pnlRight.Controls.Add(txtOrigin); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Điểm đến (Destination):", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtDest = new TextBox { Location = new Point(left, top), Width = width, Text = "Đà Lạt" };
        pnlRight.Controls.Add(txtDest); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Cự ly (km):", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtDist = new TextBox { Location = new Point(left, top), Width = width, Text = "308" };
        pnlRight.Controls.Add(txtDist); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Thời gian ước tính (phút):", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtDur = new TextBox { Location = new Point(left, top), Width = width, Text = "450" };
        pnlRight.Controls.Add(txtDur); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Trạng thái tuyến:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var cbStatus = new ComboBox { Location = new Point(left, top), Width = width, DropDownStyle = ComboBoxStyle.DropDownList };
        cbStatus.Items.AddRange(new object[] { RouteStatuses.Active, RouteStatuses.Inactive });
        cbStatus.SelectedIndex = 0;
        pnlRight.Controls.Add(cbStatus); top += 40;

        var btnUpdate = new Button { Text = "💾 CẬP NHẬT TUYẾN", Location = new Point(left, top), Width = 165, Height = 38, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
        var btnCreate = new Button { Text = "➕ LƯU TUYẾN MỚI", Location = new Point(left + 175, top), Width = 165, Height = 38, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
        pnlRight.Controls.AddRange(new Control[] { btnUpdate, btnCreate });

        split.Panel2.Controls.Add(pnlRight);
        SetCurrentView(split);

        int currentRouteId = 0;

        async Task LoadRoutesListAsync()
        {
            var res = await ApiClient.GetAsync<PagedResult<AdminRouteDto>>("api/admin/routes?page=1&pageSize=50");
            if (res.Success && res.Data != null)
            {
                dgv.DataSource = res.Data.Items.Select(r => new
                {
                    MaTuyen = r.RouteCode,
                    DiemDi = r.Origin,
                    DiemDen = r.Destination,
                    TrangThai = r.Status,
                    ChuyenSapToi = r.UpcomingTrips,
                    Raw = r
                }).ToList();

                FormatGridColumns(dgv, new Dictionary<string, string>
                {
                    { "MaTuyen", "Mã Tuyến" },
                    { "DiemDi", "Điểm Khởi Hành" },
                    { "DiemDen", "Điểm Đến" },
                    { "TrangThai", "Trạng Thái" },
                    { "ChuyenSapToi", "Chuyến Sắp Chạy" }
                });
                if (dgv.Columns.Contains("Raw")) dgv.Columns["Raw"].Visible = false;
            }
        }

        dgv.SelectionChanged += (s, e) =>
        {
            if (dgv.SelectedRows.Count > 0 && dgv.SelectedRows[0].Cells["Raw"].Value is AdminRouteDto r)
            {
                currentRouteId = r.RouteId;
                txtCode.Text = r.RouteCode;
                txtOrigin.Text = r.Origin;
                txtDest.Text = r.Destination;
                cbStatus.SelectedItem = r.Status;
            }
        };

        btnNew.Click += (s, e) =>
        {
            currentRouteId = 0;
            txtCode.Text = "(Tự động sinh mã)";
            txtOrigin.Text = "";
            txtDest.Text = "";
            txtDist.Text = "100";
            txtDur.Text = "120";
            cbStatus.SelectedIndex = 0;
            MessageBox.Show("Form đã sẵn sàng! Nhập thông tin và bấm '➕ LƯU TUYẾN MỚI'.", "Thêm tuyến mới", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        btnUpdate.Click += async (s, e) =>
        {
            if (currentRouteId == 0)
            {
                MessageBox.Show("Vui lòng click chọn 1 tuyến xe trên danh sách để sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var req = new UpdateRouteRequest
            {
                Origin = txtOrigin.Text.Trim(),
                Destination = txtDest.Text.Trim(),
                DistanceKm = decimal.TryParse(txtDist.Text, out var dist) ? dist : 100,
                DurationMinutes = int.TryParse(txtDur.Text, out var dur) ? dur : 120,
                Status = cbStatus.SelectedItem?.ToString() ?? RouteStatuses.Active
            };

            var res = await ApiClient.PutAsync<object>($"api/admin/routes/{currentRouteId}", req);
            if (res.Success)
            {
                MessageBox.Show("Cập nhật tuyến xe thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadRoutesListAsync();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnCreate.Click += async (s, e) =>
        {
            if (string.IsNullOrEmpty(txtOrigin.Text) || string.IsNullOrEmpty(txtDest.Text))
            {
                MessageBox.Show("Vui lòng điền điểm đi và điểm đến", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var req = new CreateRouteRequest
            {
                Origin = txtOrigin.Text.Trim(),
                Destination = txtDest.Text.Trim(),
                DistanceKm = decimal.TryParse(txtDist.Text, out var d) ? d : 100,
                DurationMinutes = int.TryParse(txtDur.Text, out var dur) ? dur : 120,
                PickupPoints = new List<StopPointInputDto> { new StopPointInputDto { Name = $"Bến xe {txtOrigin.Text.Trim()}", Address = txtOrigin.Text.Trim() } },
                DropoffPoints = new List<StopPointInputDto> { new StopPointInputDto { Name = $"Bến xe {txtDest.Text.Trim()}", Address = txtDest.Text.Trim() } }
            };

            var res = await ApiClient.PostAsync<object>("api/admin/routes", req);
            if (res.Success)
            {
                MessageBox.Show("Thêm tuyến xe mới thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadRoutesListAsync();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnRefresh.Click += async (s, e) => await LoadRoutesListAsync();
        _ = LoadRoutesListAsync();
    }

    // =========================================================================
    // 4. QUẢN LÝ XE & BẢO TRÌ (SỬA TRỰC TIẾP & BẢO DƯỠNG)
    // =========================================================================
    private void ShowVehiclesView()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 380 };

        // Top: Quản lý xe
        var pnlVehicles = new Panel { Dock = DockStyle.Fill };
        var pnlTopV = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(240, 244, 248) };
        var lblVTitle = new Label { Text = "DANH SÁCH ĐỘI XE", Location = new Point(10, 12), AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) };
        pnlTopV.Controls.Add(lblVTitle);

        var splitTop = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 650 };
        var dgvVehicles = CreateStyledGrid();
        splitTop.Panel1.Controls.Add(dgvVehicles);

        var pnlEditV = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15) };
        pnlEditV.Controls.Add(new Label { Text = "SỬA / THÊM MỚI PHƯƠNG TIỆN", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) });

        int top = 40, left = 15, width = 300;
        pnlEditV.Controls.Add(new Label { Text = "Biển số xe:", Location = new Point(left, top), AutoSize = true }); top += 20;
        var txtPlate = new TextBox { Location = new Point(left, top), Width = width, Text = "51B-123.45" };
        pnlEditV.Controls.Add(txtPlate); top += 30;

        pnlEditV.Controls.Add(new Label { Text = "Loại xe:", Location = new Point(left, top), AutoSize = true }); top += 20;
        var cbType = new ComboBox { Location = new Point(left, top), Width = width, DropDownStyle = ComboBoxStyle.DropDownList };
        pnlEditV.Controls.Add(cbType); top += 38;

        var btnUpdateV = new Button { Text = "💾 Cập Nhật Xe", Location = new Point(left, top), Width = 145, Height = 34, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        var btnCreateV = new Button { Text = "➕ Thêm Xe Mới", Location = new Point(left + 155, top), Width = 145, Height = 34, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        pnlEditV.Controls.AddRange(new Control[] { btnUpdateV, btnCreateV });

        splitTop.Panel2.Controls.Add(pnlEditV);
        pnlVehicles.Controls.AddRange(new Control[] { splitTop, pnlTopV });
        split.Panel1.Controls.Add(pnlVehicles);

        // Bottom: Lịch bảo trì
        var pnlM = new Panel { Dock = DockStyle.Fill };
        var pnlTopM = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(240, 244, 248) };
        var lblMTitle = new Label { Text = "LỊCH TRÌNH BẢO DƯỠNG & SỬA CHỮA ĐỊNH KỲ", Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(230, 126, 34) };
        pnlTopM.Controls.Add(lblMTitle);

        var splitBottom = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 650 };
        var dgvM = CreateStyledGrid();
        splitBottom.Panel1.Controls.Add(dgvM);

        var pnlAddM = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15) };
        pnlAddM.Controls.Add(new Label { Text = "LẬP PHIẾU BẢO TRÌ CHO XE", Location = new Point(15, 8), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) });

        int mTop = 32;
        pnlAddM.Controls.Add(new Label { Text = "Từ ngày:", Location = new Point(left, mTop), AutoSize = true });
        pnlAddM.Controls.Add(new Label { Text = "Đến ngày:", Location = new Point(left + 160, mTop), AutoSize = true }); mTop += 20;
        var dtpMStart = new DateTimePicker { Location = new Point(left, mTop), Width = 145, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(5) };
        var dtpMEnd = new DateTimePicker { Location = new Point(left + 160, mTop), Width = 145, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(7) };
        pnlAddM.Controls.AddRange(new Control[] { dtpMStart, dtpMEnd }); mTop += 28;

        pnlAddM.Controls.Add(new Label { Text = "Hạng mục bảo dưỡng:", Location = new Point(left, mTop), AutoSize = true }); mTop += 20;
        var txtNote = new TextBox { Location = new Point(left, mTop), Width = width, Text = "Thay lốp và kiểm tra dầu máy" };
        pnlAddM.Controls.Add(txtNote); mTop += 32;

        var btnSaveM = new Button { Text = "➕ Lưu Phiếu Bảo Trì", Location = new Point(left, mTop), Width = width, Height = 34, BackColor = Color.FromArgb(230, 126, 34), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        pnlAddM.Controls.Add(btnSaveM);

        splitBottom.Panel2.Controls.Add(pnlAddM);
        pnlM.Controls.AddRange(new Control[] { splitBottom, pnlTopM });
        split.Panel2.Controls.Add(pnlM);

        SetCurrentView(split);

        int currentVehicleId = 0;
        List<VehicleTypeDto> vTypes = new();

        async Task LoadDataAsync()
        {
            var resTypes = await ApiClient.GetAsync<List<VehicleTypeDto>>("api/admin/vehicle-types");
            if (resTypes.Success && resTypes.Data != null)
            {
                vTypes = resTypes.Data;
                cbType.DataSource = vTypes; cbType.DisplayMember = "Name"; cbType.ValueMember = "Id";
            }

            var res = await ApiClient.GetAsync<MaintenanceResponseData>("api/admin/maintenance");
            if (res.Success && res.Data != null)
            {
                dgvVehicles.DataSource = res.Data.Vehicles.Select(v => new
                {
                    MaXe = v.VehicleId,
                    BienSoXe = v.LicensePlate,
                    LoaiXe = v.VehicleType,
                    TrangThai = v.Status,
                    Raw = v
                }).ToList();

                FormatGridColumns(dgvVehicles, new Dictionary<string, string>
                {
                    { "MaXe", "Mã Xe" },
                    { "BienSoXe", "Biển Số Xe" },
                    { "LoaiXe", "Loại Phương Tiện" },
                    { "TrangThai", "Trạng Thái Hoạt Động" }
                });
                if (dgvVehicles.Columns.Contains("Raw")) dgvVehicles.Columns["Raw"].Visible = false;

                dgvM.DataSource = res.Data.Schedules.Select(m => new
                {
                    MaPhieu = m.Id,
                    BienSoXe = m.LicensePlate,
                    TuNgay = m.StartDate,
                    DenNgay = m.EndDate,
                    GhiChu = m.Note
                }).ToList();

                FormatGridColumns(dgvM, new Dictionary<string, string>
                {
                    { "MaPhieu", "Mã Phiếu" },
                    { "BienSoXe", "Biển Số Xe" },
                    { "TuNgay", "Ngày Bắt Đầu" },
                    { "DenNgay", "Ngày Kết Thúc" },
                    { "GhiChu", "Hạng Mục Bảo Dưỡng" }
                });
            }
        }

        dgvVehicles.SelectionChanged += (s, e) =>
        {
            if (dgvVehicles.SelectedRows.Count > 0 && dgvVehicles.SelectedRows[0].Cells["Raw"].Value is AdminVehicleDto v)
            {
                currentVehicleId = v.VehicleId;
                txtPlate.Text = v.LicensePlate;
                cbType.SelectedValue = v.VehicleTypeId;
            }
        };

        btnUpdateV.Click += async (s, e) =>
        {
            if (currentVehicleId == 0) return;
            var req = new CreateVehicleRequest { LicensePlate = txtPlate.Text.Trim(), VehicleTypeId = (int)cbType.SelectedValue! };
            var res = await ApiClient.PutAsync<object>($"api/admin/vehicles/{currentVehicleId}", req);
            if (res.Success)
            {
                MessageBox.Show("Cập nhật thông tin xe thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnCreateV.Click += async (s, e) =>
        {
            var req = new CreateVehicleRequest { LicensePlate = txtPlate.Text.Trim(), VehicleTypeId = (int)cbType.SelectedValue! };
            var res = await ApiClient.PostAsync<object>("api/admin/vehicles", req);
            if (res.Success)
            {
                MessageBox.Show("Thêm xe mới thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnSaveM.Click += async (s, e) =>
        {
            if (currentVehicleId == 0)
            {
                MessageBox.Show("Vui lòng click chọn 1 xe trên danh sách ở trên để lập lịch bảo trì.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var req = new CreateMaintenanceRequest
            {
                VehicleId = currentVehicleId,
                StartDate = dtpMStart.Value.ToString("yyyy-MM-dd"),
                EndDate = dtpMEnd.Value.ToString("yyyy-MM-dd"),
                Note = txtNote.Text.Trim()
            };
            var res = await ApiClient.PostAsync<object>("api/admin/maintenance", req);
            if (res.Success)
            {
                MessageBox.Show("Thêm lịch bảo trì xe thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataAsync();
            }
        };

        _ = LoadDataAsync();
    }

    // =========================================================================
    // 5. SƠ ĐỒ GHẾ (formGhe)
    // =========================================================================
    private void ShowSeatsView()
    {
        var pnl = new Panel { Dock = DockStyle.Fill };
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(240, 244, 248) };
        pnlTop.Controls.Add(new Label { Text = "Mã Chuyến Xe:", Location = new Point(15, 18), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
        var txtTripId = new TextBox { Location = new Point(130, 15), Width = 100, Text = "101" };
        pnlTop.Controls.Add(txtTripId);

        var btnLoadSeats = new Button { Text = "Xem Sơ Đồ Ghế", Location = new Point(245, 13), Width = 140, Height = 30, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        pnlTop.Controls.Add(btnLoadSeats);

        var lblLegend = new Label { Text = "🟩 Trống | 🟨 Chờ thanh toán | 🟥 Đã đặt", Location = new Point(410, 18), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
        pnlTop.Controls.Add(lblLegend);

        var pnlMatrix = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20), BackColor = Color.White };
        pnl.Controls.AddRange(new Control[] { pnlMatrix, pnlTop });
        SetCurrentView(pnl);

        async Task LoadSeatsAsync()
        {
            if (!int.TryParse(txtTripId.Text, out var tid)) return;
            pnlMatrix.Controls.Clear();

            var res = await ApiClient.GetAsync<TripSeatsResponseData>($"api/admin/trips/{tid}/seats");
            if (res.Success && res.Data != null)
            {
                foreach (var s in res.Data.Seats)
                {
                    var btn = new Button
                    {
                        Text = $"{s.SeatCode}\n{s.Status}\n{s.Price / 1000:N0}k",
                        Width = 80,
                        Height = 65,
                        Margin = new Padding(6),
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
                    };
                    btn.BackColor = s.Status == SeatStatuses.Available ? Color.LightGreen : (s.Status == SeatStatuses.Pending ? Color.Gold : Color.LightCoral);
                    pnlMatrix.Controls.Add(btn);
                }
            }
            else
            {
                MessageBox.Show("Không tìm thấy chuyến xe", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        btnLoadSeats.Click += async (s, e) => await LoadSeatsAsync();
        _ = LoadSeatsAsync();
    }

    // =========================================================================
    // 6. QUẢN LÝ VÉ & BÁN VÉ TẠI QUẦY (formQuanLyVe, formDatVeMoi)
    // =========================================================================
    private void ShowTicketsView()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 650 };

        var pnlLeft = new Panel { Dock = DockStyle.Fill };
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(240, 244, 248) };
        var btnConfirm = new Button { Text = "✅ Xác Nhận Vé Đã Chọn", Location = new Point(10, 8), Width = 180, Height = 32, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        var btnRefresh = new Button { Text = "🔄 Làm Mới", Location = new Point(200, 8), Width = 100, Height = 32, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
        pnlTop.Controls.AddRange(new Control[] { btnConfirm, btnRefresh });

        var dgv = CreateStyledGrid();
        pnlLeft.Controls.AddRange(new Control[] { dgv, pnlTop });
        split.Panel1.Controls.Add(pnlLeft);

        // Right Panel: Form Bán vé trực tiếp tại quầy cho khách vãng lai
        var pnlRight = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(15) };
        pnlRight.Controls.Add(new Label { Text = "ĐẶT & BÁN VÉ TRỰC TIẾP TẠI QUẦY", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) });

        int top = 45, left = 15, width = 340;
        pnlRight.Controls.Add(new Label { Text = "Mã chuyến xe (Trip ID):", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtTripId = new TextBox { Location = new Point(left, top), Width = width, Text = "101" };
        pnlRight.Controls.Add(txtTripId); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Mã ghế (Seat ID):", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtSeatId = new TextBox { Location = new Point(left, top), Width = width, Text = "5003" };
        pnlRight.Controls.Add(txtSeatId); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Họ tên hành khách:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtCustName = new TextBox { Location = new Point(left, top), Width = width, Text = "Khách Hàng Tại Quầy" };
        pnlRight.Controls.Add(txtCustName); top += 30;

        pnlRight.Controls.Add(new Label { Text = "Số điện thoại:", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) }); top += 20;
        var txtCustPhone = new TextBox { Location = new Point(left, top), Width = width, Text = "0988777666" };
        pnlRight.Controls.Add(txtCustPhone); top += 40;

        var btnCounterBook = new Button
        {
            Text = "🎟️ IN & XÁC NHẬN VÉ TẠI QUẦY",
            Location = new Point(left, top),
            Width = width,
            Height = 44,
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        pnlRight.Controls.Add(btnCounterBook);

        split.Panel2.Controls.Add(pnlRight);
        SetCurrentView(split);

        async Task LoadTicketsListAsync()
        {
            var res = await ApiClient.GetAsync<PagedResult<AdminTicketDto>>("api/admin/tickets?page=1&pageSize=50");
            if (res.Success && res.Data != null)
            {
                dgv.DataSource = res.Data.Items.Select(t => new
                {
                    MaVe = t.TicketCode,
                    Chuyen = t.TripCode,
                    HanhKhach = t.PassengerName,
                    ViTriGhe = t.SeatCode,
                    GiaTien = $"{t.TotalAmount:N0} đ",
                    KenhBan = t.Channel,
                    TrangThai = t.Status,
                    Raw = t
                }).ToList();

                FormatGridColumns(dgv, new Dictionary<string, string>
                {
                    { "MaVe", "Mã Vé" },
                    { "Chuyen", "Mã Chuyến" },
                    { "HanhKhach", "Hành Khách" },
                    { "ViTriGhe", "Ghế" },
                    { "GiaTien", "Tổng Tiền" },
                    { "KenhBan", "Kênh Đặt" },
                    { "TrangThai", "Trạng Thái" }
                });
                if (dgv.Columns.Contains("Raw")) dgv.Columns["Raw"].Visible = false;
            }
        }

        btnConfirm.Click += async (s, e) =>
        {
            if (dgv.SelectedRows.Count == 0) return;
            if (dgv.SelectedRows[0].Cells["Raw"].Value is AdminTicketDto ticket)
            {
                var res = await ApiClient.PutAsync<object>($"api/admin/tickets/{ticket.TicketId}/confirm");
                if (res.Success)
                {
                    MessageBox.Show("Xác nhận vé thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadTicketsListAsync();
                }
                else
                {
                    MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        };

        btnCounterBook.Click += async (s, e) =>
        {
            if (!int.TryParse(txtTripId.Text, out var tid) || !int.TryParse(txtSeatId.Text, out var sid)) return;
            var req = new CounterBookingRequest
            {
                TripId = tid,
                SeatId = sid,
                PassengerName = txtCustName.Text.Trim(),
                PassengerPhone = txtCustPhone.Text.Trim(),
                PickupPointId = 1,
                DropoffPointId = 7
            };
            var res = await ApiClient.PostAsync<object>("api/admin/tickets", req);
            if (res.Success)
            {
                MessageBox.Show("Bán vé tại quầy thành công! Tiền ghi nhận vào kênh Trực tiếp.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadTicketsListAsync();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi đặt vé", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnRefresh.Click += async (s, e) => await LoadTicketsListAsync();
        _ = LoadTicketsListAsync();
    }

    // =========================================================================
    // 7. PHÂN QUYỀN TRUY CẬP & NHÂN VIÊN (KHÓA / MỞ KHÓA)
    // =========================================================================
    private void ShowPermissionsView()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 650 };

        var pnlLeft = new Panel { Dock = DockStyle.Fill };
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(240, 244, 248) };
        var btnToggleLock = new Button { Text = "🔒 KHÓA / MỞ KHÓA TÀI KHOẢN ĐANG CHỌN", Location = new Point(10, 8), Width = 310, Height = 32, BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        pnlTop.Controls.Add(btnToggleLock);

        var dgv = CreateStyledGrid();
        pnlLeft.Controls.AddRange(new Control[] { dgv, pnlTop });
        split.Panel1.Controls.Add(pnlLeft);

        var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15) };
        pnlRight.Controls.Add(new Label { Text = "BẢNG PHÂN QUYỀN VAI TRÒ HỆ THỐNG", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) });
        var dgvPerms = CreateStyledGrid();
        var pnlPermContainer = new Panel { Location = new Point(15, 45), Size = new Size(360, 400) };
        pnlPermContainer.Controls.Add(dgvPerms);
        pnlRight.Controls.Add(pnlPermContainer);
        split.Panel2.Controls.Add(pnlRight);

        SetCurrentView(split);

        async Task LoadDataAsync()
        {
            var res = await ApiClient.GetAsync<StaffAccountsResponseData>("api/admin/staff-accounts");
            if (res.Success && res.Data != null)
            {
                dgv.DataSource = res.Data.Accounts.Select(a => new
                {
                    MaNV = a.AccountId,
                    HoTen = a.FullName,
                    Email = a.Email,
                    VaiTro = a.Role,
                    TrangThai = a.Status,
                    Raw = a
                }).ToList();

                FormatGridColumns(dgv, new Dictionary<string, string>
                {
                    { "MaNV", "Mã NV" },
                    { "HoTen", "Họ và Tên" },
                    { "Email", "Email Đăng Nhập" },
                    { "VaiTro", "Vai Trò" },
                    { "TrangThai", "Trạng Thái" }
                });
                if (dgv.Columns.Contains("Raw")) dgv.Columns["Raw"].Visible = false;

                dgvPerms.DataSource = res.Data.Permissions.Select(p => new
                {
                    VaiTro = p.Role,
                    QuyenHan = string.Join(",\n", p.Functions)
                }).ToList();

                FormatGridColumns(dgvPerms, new Dictionary<string, string>
                {
                    { "VaiTro", "Vai Trò" },
                    { "QuyenHan", "Chức Năng Được Phép" }
                });
            }
        }

        btnToggleLock.Click += async (s, e) =>
        {
            if (dgv.SelectedRows.Count == 0) return;
            if (dgv.SelectedRows[0].Cells["Raw"].Value is StaffAccountDto acc)
            {
                var res = await ApiClient.PutAsync<object>($"api/admin/staff-accounts/{acc.AccountId}/lock");
                if (res.Success)
                {
                    MessageBox.Show(res.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadDataAsync();
                }
            }
        };

        _ = LoadDataAsync();
    }

    // =========================================================================
    // 8. TRA CỨU KHÁCH HÀNG (formTraCuuKhachHang)
    // =========================================================================
    private void ShowCustomersView()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 650 };

        var pnlLeft = new Panel { Dock = DockStyle.Fill };
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(240, 244, 248) };
        pnlTop.Controls.Add(new Label { Text = "Tìm kiếm:", Location = new Point(10, 14), AutoSize = true });
        var txtKeyword = new TextBox { Location = new Point(80, 11), Width = 180 };
        pnlTop.Controls.Add(txtKeyword);

        var btnSearch = new Button { Text = "🔍 Tìm", Location = new Point(270, 9), Width = 80, Height = 30, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        pnlTop.Controls.Add(btnSearch);

        var dgv = CreateStyledGrid();
        pnlLeft.Controls.AddRange(new Control[] { dgv, pnlTop });
        split.Panel1.Controls.Add(pnlLeft);

        var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(15) };
        var lblDetailTitle = new Label { Text = "LỊCH SỬ ĐẶT VÉ CỦA KHÁCH HÀNG", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) };
        var rtbHistory = new RichTextBox { Location = new Point(15, 45), Size = new Size(360, 500), ReadOnly = true, Font = new Font("Segoe UI", 9.5f), BackColor = Color.FromArgb(248, 250, 252) };
        pnlRight.Controls.AddRange(new Control[] { lblDetailTitle, rtbHistory });
        split.Panel2.Controls.Add(pnlRight);

        SetCurrentView(split);

        async Task LoadDataAsync()
        {
            string kw = txtKeyword.Text.Trim();
            var res = await ApiClient.GetAsync<PagedResult<CustomerItemDto>>($"api/admin/customers?fullName={Uri.EscapeDataString(kw)}&page=1&pageSize=50");
            if (res.Success && res.Data != null)
            {
                dgv.DataSource = res.Data.Items.Select(c => new
                {
                    MaKH = c.CustomerId,
                    HoTen = c.FullName,
                    Email = c.Email,
                    SoDienThoai = c.Phone,
                    SoVeDaMua = c.TicketCount,
                    Raw = c
                }).ToList();

                FormatGridColumns(dgv, new Dictionary<string, string>
                {
                    { "MaKH", "Mã KH" },
                    { "HoTen", "Họ và Tên" },
                    { "Email", "Email" },
                    { "SoDienThoai", "Số Điện Thoại" },
                    { "SoVeDaMua", "Số Vé Đã Đặt" }
                });
                if (dgv.Columns.Contains("Raw")) dgv.Columns["Raw"].Visible = false;
            }
        }

        dgv.SelectionChanged += async (s, e) =>
        {
            if (dgv.SelectedRows.Count > 0 && dgv.SelectedRows[0].Cells["Raw"].Value is CustomerItemDto c)
            {
                var res = await ApiClient.GetAsync<CustomerDetailData>($"api/admin/customers/{c.CustomerId}");
                if (res.Success && res.Data != null)
                {
                    rtbHistory.Clear();
                    rtbHistory.SelectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
                    rtbHistory.AppendText($"Khách hàng: {res.Data.FullName}\nEmail: {res.Data.Email} | SĐT: {res.Data.Phone}\n\n");
                    rtbHistory.SelectionFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                    rtbHistory.AppendText("DANH SÁCH CÁC VÉ ĐÃ ĐẶT:\n-------------------------------------\n");
                    foreach (var t in res.Data.Tickets)
                    {
                        rtbHistory.AppendText($"• Mã vé: {t.TicketCode}\n  Tuyến: {t.RouteName}\n  Giờ đi: {t.DepartureTime:dd/MM/yyyy HH:mm}\n  Ghế: {t.SeatCode} | Tiền: {t.TotalAmount:N0} đ\n  Trạng thái: [{t.Status}]\n\n");
                    }
                }
            }
        };

        btnSearch.Click += async (s, e) => await LoadDataAsync();
        _ = LoadDataAsync();
    }

    // =========================================================================
    // 9. HỖ TRỢ KHÁCH HÀNG (TIẾP NHẬN & CHAT REALTIME SIGNALR)
    // =========================================================================
    private void ShowSupportTicketsView()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 550 };

        var pnlLeft = new Panel { Dock = DockStyle.Fill };
        var pnlTopL = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(240, 244, 248) };
        var btnClaim = new Button { Text = "🙋 Nhận Xử Lý", Location = new Point(10, 8), Width = 130, Height = 32, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var btnResolve = new Button { Text = "✅ Hoàn Thành Ticket", Location = new Point(150, 8), Width = 160, Height = 32, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        pnlTopL.Controls.AddRange(new Control[] { btnClaim, btnResolve });
        var dgv = CreateStyledGrid();
        pnlLeft.Controls.AddRange(new Control[] { dgv, pnlTopL });
        split.Panel1.Controls.Add(pnlLeft);

        var pnlRight = new Panel { Dock = DockStyle.Fill };
        var rtbMessages = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, Font = new Font("Segoe UI", 10), Padding = new Padding(12) };
        var pnlChatSend = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Color.FromArgb(240, 244, 248), Padding = new Padding(8) };
        var txtReply = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5f) };
        var btnSend = new Button { Text = "Gửi Trả Lời", Dock = DockStyle.Right, Width = 130, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        pnlChatSend.Controls.AddRange(new Control[] { txtReply, btnSend });
        pnlRight.Controls.AddRange(new Control[] { rtbMessages, pnlChatSend });
        split.Panel2.Controls.Add(pnlRight);

        SetCurrentView(split);

        int selectedTicketId = 77;

        async Task LoadTicketsAsync()
        {
            var res = await ApiClient.GetAsync<AdminSupportTicketListResponseData>("api/admin/support/tickets?page=1&pageSize=50");
            if (res.Success && res.Data != null)
            {
                dgv.DataSource = res.Data.Tickets.Items.Select(t => new
                {
                    MaTicket = t.TicketId,
                    TieuDe = t.Title,
                    KhachHang = t.CustomerName,
                    TrangThai = t.Status,
                    NguoiXuLy = t.HandlerName ?? "(Chưa có)",
                    Raw = t
                }).ToList();

                FormatGridColumns(dgv, new Dictionary<string, string>
                {
                    { "MaTicket", "Mã Ticket" },
                    { "TieuDe", "Nội Dung Yêu Cầu" },
                    { "KhachHang", "Khách Hàng" },
                    { "TrangThai", "Trạng Thái" },
                    { "NguoiXuLy", "Người Xử Lý" }
                });
                if (dgv.Columns.Contains("Raw")) dgv.Columns["Raw"].Visible = false;
            }
        }

        async Task LoadMessagesAsync(int ticketId)
        {
            selectedTicketId = ticketId;
            rtbMessages.Clear();
            var res = await ApiClient.GetAsync<List<SupportMessageDto>>($"api/admin/support/tickets/{ticketId}/messages");
            if (res.Success && res.Data != null)
            {
                foreach (var m in res.Data)
                {
                    bool isCust = m.SenderRole == Roles.Customer;
                    rtbMessages.SelectionColor = isCust ? Color.FromArgb(30, 60, 114) : Color.FromArgb(165, 0, 100);
                    rtbMessages.SelectionFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    rtbMessages.AppendText(isCust ? "[Khách hàng]: " : "[CSKH Nhà xe]: ");
                    rtbMessages.SelectionColor = Color.Black;
                    rtbMessages.SelectionFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                    rtbMessages.AppendText(m.Content + "\n\n");
                }
                rtbMessages.ScrollToCaret();
            }
        }

        dgv.SelectionChanged += async (s, e) =>
        {
            if (dgv.SelectedRows.Count > 0 && dgv.SelectedRows[0].Cells["Raw"].Value is SupportTicketSummaryDto t)
            {
                await LoadMessagesAsync(t.TicketId);
            }
        };

        btnSend.Click += async (s, e) =>
        {
            string reply = txtReply.Text.Trim();
            if (string.IsNullOrEmpty(reply)) return;
            txtReply.Clear();

            if (RealtimeClient.Instance.IsConnected)
            {
                await RealtimeClient.Instance.SendMessageAsync(selectedTicketId, reply);
                rtbMessages.SelectionColor = Color.FromArgb(165, 0, 100);
                rtbMessages.SelectionFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                rtbMessages.AppendText("[CSKH Nhà xe]: ");
                rtbMessages.SelectionColor = Color.Black;
                rtbMessages.SelectionFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                rtbMessages.AppendText(reply + "\n\n");
            }
        };

        btnClaim.Click += async (s, e) =>
        {
            await ApiClient.PutAsync<object>($"api/admin/support/tickets/{selectedTicketId}/status", new UpdateTicketStatusRequest { Status = SupportTicketStatuses.Processing });
            await LoadTicketsAsync();
        };

        btnResolve.Click += async (s, e) =>
        {
            await ApiClient.PutAsync<object>($"api/admin/support/tickets/{selectedTicketId}/status", new UpdateTicketStatusRequest { Status = SupportTicketStatuses.Resolved });
            await LoadTicketsAsync();
        };

        _ = LoadTicketsAsync();
        _ = LoadMessagesAsync(77);
    }

    // =========================================================================
    // 10. BÁO CÁO DOANH THU (formDoanhThu)
    // =========================================================================
    private void ShowRevenueView()
    {
        var pnl = new Panel { Dock = DockStyle.Fill };
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(240, 244, 248) };

        pnlTop.Controls.Add(new Label { Text = "Từ ngày:", Location = new Point(15, 18), AutoSize = true });
        var dtpFrom = new DateTimePicker { Location = new Point(80, 15), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-14) };
        pnlTop.Controls.Add(dtpFrom);

        pnlTop.Controls.Add(new Label { Text = "Đến ngày:", Location = new Point(230, 18), AutoSize = true });
        var dtpTo = new DateTimePicker { Location = new Point(300, 15), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        pnlTop.Controls.Add(dtpTo);

        var btnFilter = new Button { Text = "Xem Doanh Thu", Location = new Point(450, 13), Width = 140, Height = 30, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        pnlTop.Controls.Add(btnFilter);

        var pnlCards = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = Color.White };
        var cTotal = CreateMetricCard("TỔNG DOANH THU", "0 VNĐ", Color.FromArgb(41, 128, 185)); cTotal.Location = new Point(15, 2);
        var cOnline = CreateMetricCard("DOANH THU ONLINE (MOMO)", "0 VNĐ", Color.FromArgb(165, 0, 100)); cOnline.Location = new Point(335, 2);
        var cOffline = CreateMetricCard("DOANH THU TẠI QUẦY", "0 VNĐ", Color.FromArgb(39, 174, 96)); cOffline.Location = new Point(655, 2);
        pnlCards.Controls.AddRange(new Control[] { cTotal, cOnline, cOffline });

        var dgv = CreateStyledGrid();

        pnl.Controls.AddRange(new Control[] { dgv, pnlCards, pnlTop });
        SetCurrentView(pnl);

        async Task LoadDataAsync()
        {
            string f = dtpFrom.Value.ToString("yyyy-MM-dd");
            string t = dtpTo.Value.ToString("yyyy-MM-dd");
            var res = await ApiClient.GetAsync<RevenueReportDto>($"api/admin/revenue?fromDate={f}&toDate={t}");
            if (res.Success && res.Data != null)
            {
                ((Label)cTotal.Controls[1]).Text = $"{res.Data.TotalRevenue:N0} đ";
                ((Label)cOnline.Controls[1]).Text = $"{res.Data.OnlineRevenue:N0} đ";
                ((Label)cOffline.Controls[1]).Text = $"{res.Data.OfflineRevenue:N0} đ";
                dgv.DataSource = res.Data.Details.Select(d => new
                {
                    Ngay = d.Date,
                    DoanhThuOnline = $"{d.Online:N0} đ",
                    DoanhThuTaiQuay = $"{d.Offline:N0} đ",
                    TongDoanhThu = $"{d.Total:N0} đ"
                }).ToList();

                FormatGridColumns(dgv, new Dictionary<string, string>
                {
                    { "Ngay", "Ngày Giao Dịch" },
                    { "DoanhThuOnline", "Doanh Thu Online (MoMo)" },
                    { "DoanhThuTaiQuay", "Doanh Thu Tại Quầy" },
                    { "TongDoanhThu", "Tổng Cộng Ngày" }
                });
            }
        }

        btnFilter.Click += async (s, e) => await LoadDataAsync();
        _ = LoadDataAsync();
    }
}
