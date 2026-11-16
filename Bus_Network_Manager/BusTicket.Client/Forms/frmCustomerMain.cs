using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusTicket.Client.Services;
using BusTicket.Core.DTOs;
using BusTicket.Core.Enums;

namespace BusTicket.Client.Forms;

public class frmCustomerMain : Form
{
    private TabControl tabControl = null!;
    private Label lblConnectionStatus = null!;
    private Label lblUserInfo = null!;

    // Tab 1: Đặt vé
    private ComboBox cbFrom = null!;
    private ComboBox cbTo = null!;
    private DateTimePicker dtpDeparture = null!;
    private Button btnSearchTrips = null!;
    private DataGridView dgvTrips = null!;
    private FlowLayoutPanel pnlSeats = null!;
    private Label lblSelectedSeat = null!;
    private TextBox txtPassengerName = null!;
    private TextBox txtPassengerPhone = null!;
    private TextBox txtPassengerEmail = null!;
    private ComboBox cbPickup = null!;
    private ComboBox cbDropoff = null!;
    private Button btnBookAndPay = null!;
    private TripDto? _selectedTrip;
    private SeatDto? _selectedSeat;
    private List<StopPointDto> _currentPickups = new();
    private List<StopPointDto> _currentDropoffs = new();

    // Tab 2: Vé của tôi
    private ComboBox cbTicketFilter = null!;
    private DataGridView dgvMyTickets = null!;
    private Button btnRefreshTickets = null!;
    private Button btnTicketDetail = null!;

    // Tab 3: Thông báo
    private DataGridView dgvNotifications = null!;
    private Button btnMarkRead = null!;

    // Tab 4: Chat hỗ trợ
    private RichTextBox rtbChatHistory = null!;
    private TextBox txtChatMessage = null!;
    private Button btnSendChat = null!;
    private int _currentSupportTicketId = 77;

    // Tab 5: Tài khoản
    private TextBox txtProfileName = null!;
    private TextBox txtProfilePhone = null!;
    private TextBox txtProfileEmail = null!;
    private TextBox txtOldPass = null!;
    private TextBox txtNewPass = null!;

    public frmCustomerMain()
    {
        InitializeComponents();
        SetupRealtimeEvents();
        this.Load += async (s, e) =>
        {
            await LoadTripsAsync();
            await LoadMyTicketsAsync();
            await LoadNotificationsAsync();
            await LoadChatHistoryAsync();
            await LoadProfileAsync();
        };
    }

    private void InitializeComponents()
    {
        this.Text = "Hệ thống Đặt vé xe khách trực tuyến - Cổng Khách Hàng";
        this.Size = new Size(1100, 720);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1000, 650);
        this.Font = new Font("Segoe UI", 9.5f);

        // Header
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(30, 60, 114) };
        var lblBrand = new Label
        {
            Text = "🚌 ĐẶT VÉ XE KHÁCH TRỰC TUYẾN",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(15, 16),
            AutoSize = true
        };
        lblUserInfo = new Label
        {
            Text = $"Khách hàng: {SessionManager.CurrentUser?.FullName} ({SessionManager.CurrentUser?.Email})",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(220, 235, 255),
            Location = new Point(400, 18),
            AutoSize = true
        };
        lblConnectionStatus = new Label
        {
            Text = "🟢 WebSocket: Đã kết nối",
            ForeColor = Color.PaleGreen,
            Location = new Point(780, 18),
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        var btnLogout = new Button
        {
            Text = "Đăng xuất",
            Location = new Point(970, 14),
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

        pnlHeader.Controls.AddRange(new Control[] { lblBrand, lblUserInfo, lblConnectionStatus, btnLogout });
        this.Controls.Add(pnlHeader);

        // TabControl
        tabControl = new TabControl { Dock = DockStyle.Fill };
        tabControl.TabPages.Add(CreateBookingTab());
        tabControl.TabPages.Add(CreateMyTicketsTab());
        tabControl.TabPages.Add(CreateNotificationsTab());
        tabControl.TabPages.Add(CreateSupportChatTab());
        tabControl.TabPages.Add(CreateAccountTab());

        this.Controls.Add(tabControl);
    }

    private void SetupRealtimeEvents()
    {
        RealtimeClient.Instance.OnConnectionChanged += (connected) =>
        {
            if (this.IsDisposed) return;
            this.Invoke(() =>
            {
                lblConnectionStatus.Text = connected ? "🟢 WebSocket: Đã kết nối" : "🔴 WebSocket: Ngắt kết nối";
                lblConnectionStatus.ForeColor = connected ? Color.PaleGreen : Color.Tomato;
            });
        };

        RealtimeClient.Instance.OnMessageReceived += (msg) =>
        {
            if (this.IsDisposed) return;
            this.Invoke(() =>
            {
                AppendChatMessage(msg.SenderRole, msg.Content, msg.SentAt);
            });
        };

        RealtimeClient.Instance.OnPaymentConfirmed += (ticketId, ticketCode, status) =>
        {
            if (this.IsDisposed) return;
            this.Invoke(async () =>
            {
                MessageBox.Show($"Vé {ticketCode} đã thanh toán thành công qua MoMo!\nTrạng thái: {status}", "MoMo IPN Webhook Xác nhận", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadMyTicketsAsync();
                await LoadTripSeatsAsync();
            });
        };

        RealtimeClient.Instance.OnNotificationReceived += (notif) =>
        {
            if (this.IsDisposed) return;
            this.Invoke(async () =>
            {
                await LoadNotificationsAsync();
            });
        };
    }

    // ---------------- TAB 1: TÌM CHUYẾN & ĐẶT VÉ ----------------
    private TabPage CreateBookingTab()
    {
        var tab = new TabPage("🔍 Tìm Chuyến & Đặt Vé");

        var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(240, 244, 248) };
        pnlFilter.Controls.Add(new Label { Text = "Điểm đi:", Location = new Point(15, 20), AutoSize = true });
        cbFrom = new ComboBox { Location = new Point(75, 17), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
        cbFrom.Items.AddRange(new object[] { "Tất cả", "TP.HCM", "Đà Lạt", "Vũng Tàu", "Nha Trang" });
        cbFrom.SelectedIndex = 0;
        pnlFilter.Controls.Add(cbFrom);

        pnlFilter.Controls.Add(new Label { Text = "Điểm đến:", Location = new Point(220, 20), AutoSize = true });
        cbTo = new ComboBox { Location = new Point(290, 17), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
        cbTo.Items.AddRange(new object[] { "Tất cả", "TP.HCM", "Đà Lạt", "Vũng Tàu", "Nha Trang" });
        cbTo.SelectedIndex = 0;
        pnlFilter.Controls.Add(cbTo);

        pnlFilter.Controls.Add(new Label { Text = "Ngày đi:", Location = new Point(435, 20), AutoSize = true });
        dtpDeparture = new DateTimePicker { Location = new Point(495, 17), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1) };
        pnlFilter.Controls.Add(dtpDeparture);

        btnSearchTrips = new Button
        {
            Text = "Tìm Chuyến",
            Location = new Point(640, 15),
            Width = 110,
            Height = 32,
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSearchTrips.Click += async (s, e) => await LoadTripsAsync();
        pnlFilter.Controls.Add(btnSearchTrips);

        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 210
        };

        // Grid Trips
        dgvTrips = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White
        };
        dgvTrips.SelectionChanged += async (s, e) =>
        {
            if (dgvTrips.SelectedRows.Count > 0)
            {
                _selectedTrip = dgvTrips.SelectedRows[0].DataBoundItem as TripDto;
                await LoadTripSeatsAsync();
            }
        };
        splitContainer.Panel1.Controls.Add(dgvTrips);

        // Bottom Booking panel: Seats + Passenger Form
        var splitBottom = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 620
        };

        // Left: Seat Map
        var grpSeats = new GroupBox
        {
            Text = "SƠ ĐỒ GHẾ NGỒI (Chọn 1 ghế trống màu xanh lá)",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        pnlSeats = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        var pnlSeatLegend = new Panel { Dock = DockStyle.Bottom, Height = 35 };
        pnlSeatLegend.Controls.Add(new Label { Text = "🟩 Trống | 🟨 Đang chọn | 🟥 Đã đặt", Location = new Point(15, 8), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Regular) });
        grpSeats.Controls.Add(pnlSeats);
        grpSeats.Controls.Add(pnlSeatLegend);
        splitBottom.Panel1.Controls.Add(grpSeats);

        // Right: Passenger Info & Book Button
        var grpPassenger = new GroupBox
        {
            Text = "THÔNG TIN ĐẶT VÉ & THANH TOÁN",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };

        int pTop = 25;
        int pLeft = 15;
        int pWidth = 380;

        lblSelectedSeat = new Label { Text = "Ghế đang chọn: (Chưa chọn)", Location = new Point(pLeft, pTop), AutoSize = true, ForeColor = Color.DarkBlue };
        grpPassenger.Controls.Add(lblSelectedSeat);
        pTop += 28;

        grpPassenger.Controls.Add(new Label { Text = "Họ tên hành khách:", Location = new Point(pLeft, pTop), AutoSize = true, Font = new Font("Segoe UI", 9) });
        pTop += 20;
        txtPassengerName = new TextBox { Text = SessionManager.CurrentUser?.FullName ?? "", Location = new Point(pLeft, pTop), Width = pWidth, Font = new Font("Segoe UI", 9.5f) };
        grpPassenger.Controls.Add(txtPassengerName);
        pTop += 30;

        grpPassenger.Controls.Add(new Label { Text = "Số điện thoại:", Location = new Point(pLeft, pTop), AutoSize = true, Font = new Font("Segoe UI", 9) });
        pTop += 20;
        txtPassengerPhone = new TextBox { Text = SessionManager.CurrentUser?.Phone ?? "0901234567", Location = new Point(pLeft, pTop), Width = pWidth, Font = new Font("Segoe UI", 9.5f) };
        grpPassenger.Controls.Add(txtPassengerPhone);
        pTop += 30;

        grpPassenger.Controls.Add(new Label { Text = "Email nhận vé:", Location = new Point(pLeft, pTop), AutoSize = true, Font = new Font("Segoe UI", 9) });
        pTop += 20;
        txtPassengerEmail = new TextBox { Text = SessionManager.CurrentUser?.Email ?? "", Location = new Point(pLeft, pTop), Width = pWidth, Font = new Font("Segoe UI", 9.5f) };
        grpPassenger.Controls.Add(txtPassengerEmail);
        pTop += 30;

        grpPassenger.Controls.Add(new Label { Text = "Điểm đón:", Location = new Point(pLeft, pTop), AutoSize = true, Font = new Font("Segoe UI", 9) });
        pTop += 20;
        cbPickup = new ComboBox { Location = new Point(pLeft, pTop), Width = pWidth, DropDownStyle = ComboBoxStyle.DropDownList };
        grpPassenger.Controls.Add(cbPickup);
        pTop += 30;

        grpPassenger.Controls.Add(new Label { Text = "Điểm trả:", Location = new Point(pLeft, pTop), AutoSize = true, Font = new Font("Segoe UI", 9) });
        pTop += 20;
        cbDropoff = new ComboBox { Location = new Point(pLeft, pTop), Width = pWidth, DropDownStyle = ComboBoxStyle.DropDownList };
        grpPassenger.Controls.Add(cbDropoff);
        pTop += 40;

        btnBookAndPay = new Button
        {
            Text = "ĐẶT VÉ & THANH TOÁN MOMO",
            Location = new Point(pLeft, pTop),
            Width = pWidth,
            Height = 44,
            BackColor = Color.FromArgb(165, 0, 100), // MoMo pink/magenta
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnBookAndPay.Click += async (s, e) => await HandleBookAndPayAsync();
        grpPassenger.Controls.Add(btnBookAndPay);

        splitBottom.Panel2.Controls.Add(grpPassenger);
        splitContainer.Panel2.Controls.Add(splitBottom);

        tab.Controls.Add(splitContainer);
        tab.Controls.Add(pnlFilter);
        return tab;
    }

    private async Task LoadTripsAsync()
    {
        string from = cbFrom.SelectedIndex > 0 ? cbFrom.SelectedItem?.ToString() ?? "" : "";
        string to = cbTo.SelectedIndex > 0 ? cbTo.SelectedItem?.ToString() ?? "" : "";
        string date = dtpDeparture.Value.ToString("yyyy-MM-dd");

        string endpoint = $"api/trips?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&date={date}";
        var res = await ApiClient.GetAsync<List<TripDto>>(endpoint);

        if (res.Success && res.Data != null)
        {
            dgvTrips.DataSource = res.Data;
            if (res.Data.Any())
            {
                _selectedTrip = res.Data.First();
                await LoadTripSeatsAsync();
            }
        }
    }

    private async Task LoadTripSeatsAsync()
    {
        if (_selectedTrip == null) return;

        pnlSeats.Controls.Clear();
        _selectedSeat = null;
        lblSelectedSeat.Text = "Ghế đang chọn: (Chưa chọn)";

        var res = await ApiClient.GetAsync<TripSeatsResponseData>($"api/trips/{_selectedTrip.TripId}/seats");
        if (res.Success && res.Data != null)
        {
            _currentPickups = res.Data.PickupPoints;
            _currentDropoffs = res.Data.DropoffPoints;

            cbPickup.DataSource = null;
            cbPickup.DataSource = _currentPickups;
            cbPickup.DisplayMember = "Name";
            cbPickup.ValueMember = "Id";

            cbDropoff.DataSource = null;
            cbDropoff.DataSource = _currentDropoffs;
            cbDropoff.DisplayMember = "Name";
            cbDropoff.ValueMember = "Id";

            foreach (var seat in res.Data.Seats)
            {
                var btn = new Button
                {
                    Text = $"{seat.SeatCode}\n{seat.Price / 1000:N0}k",
                    Width = 65,
                    Height = 55,
                    Margin = new Padding(4),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Tag = seat
                };

                if (seat.Status == SeatStatuses.Available)
                {
                    btn.BackColor = Color.LightGreen;
                    btn.Cursor = Cursors.Hand;
                    btn.Click += (s, e) =>
                    {
                        foreach (Control c in pnlSeats.Controls)
                        {
                            if (c is Button b && b.Tag is SeatDto sDto && sDto.Status == SeatStatuses.Available)
                            {
                                b.BackColor = Color.LightGreen;
                            }
                        }
                        btn.BackColor = Color.Gold;
                        _selectedSeat = seat;
                        lblSelectedSeat.Text = $"Ghế đang chọn: [{seat.SeatCode}] - Giá: {seat.Price:N0} VNĐ";
                    };
                }
                else
                {
                    btn.BackColor = Color.LightCoral;
                    btn.ForeColor = Color.White;
                    btn.Enabled = false;
                }

                pnlSeats.Controls.Add(btn);
            }
        }
    }

    private async Task HandleBookAndPayAsync()
    {
        if (_selectedTrip == null)
        {
            MessageBox.Show("Vui lòng chọn chuyến xe trước", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_selectedSeat == null)
        {
            MessageBox.Show("Vui lòng click chọn 1 ghế trống trên sơ đồ", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (cbPickup.SelectedValue == null || cbDropoff.SelectedValue == null)
        {
            MessageBox.Show("Vui lòng chọn điểm đón và điểm trả", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int pickupId = (int)cbPickup.SelectedValue;
        int dropoffId = (int)cbDropoff.SelectedValue;

        var req = new CreateBookingRequest
        {
            TripId = _selectedTrip.TripId,
            SeatId = _selectedSeat.SeatId,
            PassengerName = txtPassengerName.Text.Trim(),
            PassengerPhone = txtPassengerPhone.Text.Trim(),
            PassengerEmail = txtPassengerEmail.Text.Trim(),
            PickupPointId = pickupId,
            DropoffPointId = dropoffId
        };

        btnBookAndPay.Enabled = false;
        try
        {
            var res = await ApiClient.PostAsync<BookingResponseData>("api/bookings", req);
            if (res.Success && res.Data != null)
            {
                ShowMomoPaymentDialog(res.Data);
            }
            else
            {
                MessageBox.Show(res.Message, "Không thể đặt vé", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await LoadTripSeatsAsync();
            }
        }
        finally
        {
            btnBookAndPay.Enabled = true;
        }
    }

    private void ShowMomoPaymentDialog(BookingResponseData booking)
    {
        var diag = new Form
        {
            Text = "CỔNG THANH TOÁN MOMO - QUÉT MÃ QR ĐỂ THANH TOÁN",
            Size = new Size(480, 520),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            BackColor = Color.White
        };

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(165, 0, 100) };
        var lblTitle = new Label { Text = "VÍ ĐIỆN TỬ MOMO", ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        pnlHeader.Controls.Add(lblTitle);
        diag.Controls.Add(pnlHeader);

        int top = 75;
        diag.Controls.Add(new Label { Text = $"Mã vé: {booking.TicketCode}", Location = new Point(40, top), AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) });
        top += 25;
        diag.Controls.Add(new Label { Text = $"Số tiền thanh toán: {booking.TotalAmount:N0} VNĐ", Location = new Point(40, top), AutoSize = true, ForeColor = Color.FromArgb(165, 0, 100), Font = new Font("Segoe UI", 11, FontStyle.Bold) });
        top += 30;

        // QR Code PictureBox
        var picQr = new PictureBox
        {
            Location = new Point(115, top),
            Size = new Size(230, 230),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        try
        {
            picQr.LoadAsync(booking.Payment.QrCodeUrl);
        }
        catch { }
        diag.Controls.Add(picQr);
        top += 245;

        var lblWaiting = new Label
        {
            Text = "Đang chờ thanh toán qua WebSocket thời gian thực...",
            Location = new Point(40, top),
            Width = 380,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.DarkSlateGray
        };
        diag.Controls.Add(lblWaiting);
        top += 35;

        // Simulated Mock Button to Trigger MoMo Webhook IPN immediately for testing!
        var btnSimulateSuccess = new Button
        {
            Text = "⚡ Giả lập Quét mã & Thanh toán thành công (Test MoMo IPN)",
            Location = new Point(40, top),
            Width = 380,
            Height = 38,
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSimulateSuccess.Click += async (s, e) =>
        {
            btnSimulateSuccess.Enabled = false;
            btnSimulateSuccess.Text = "Đang gửi IPN Webhook...";

            var ipn = new MomoIpnRequest
            {
                PartnerCode = "MOMO_PARTNER_CODE",
                OrderId = booking.TicketCode,
                RequestId = "RQ" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Amount = (long)booking.TotalAmount,
                OrderInfo = $"Thanh toan ve {booking.TicketCode}",
                OrderType = "momo_wallet",
                TransId = 4088878653,
                ResultCode = 0,
                Message = "Successful.",
                PayType = "qr",
                ResponseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ExtraData = "",
                Signature = "test_signature"
            };

            await ApiClient.PostAsync<object>("api/payments/momo/ipn", ipn);
            diag.Close();
        };
        diag.Controls.Add(btnSimulateSuccess);

        diag.ShowDialog();
    }

    // ---------------- TAB 2: VÉ CỦA TÔI ----------------
    private TabPage CreateMyTicketsTab()
    {
        var tab = new TabPage("🎟️ Vé Của Tôi & Lịch Sử");

        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(240, 244, 248) };
        pnlTop.Controls.Add(new Label { Text = "Trạng thái:", Location = new Point(15, 18), AutoSize = true });
        cbTicketFilter = new ComboBox { Location = new Point(90, 15), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        cbTicketFilter.Items.AddRange(new object[] { "Tất cả", TicketStatuses.Paid, TicketStatuses.Confirmed, TicketStatuses.PendingPayment, TicketStatuses.Cancelled });
        cbTicketFilter.SelectedIndex = 0;
        cbTicketFilter.SelectedIndexChanged += async (s, e) => await LoadMyTicketsAsync();
        pnlTop.Controls.Add(cbTicketFilter);

        btnRefreshTickets = new Button { Text = "Làm Mới", Location = new Point(260, 13), Width = 90, Height = 30, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnRefreshTickets.Click += async (s, e) => await LoadMyTicketsAsync();
        pnlTop.Controls.Add(btnRefreshTickets);

        btnTicketDetail = new Button { Text = "Xem Chi Tiết Vé", Location = new Point(365, 13), Width = 140, Height = 30, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnTicketDetail.Click += async (s, e) => await ShowSelectedTicketDetailAsync();
        pnlTop.Controls.Add(btnTicketDetail);

        dgvMyTickets = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White
        };

        tab.Controls.Add(dgvMyTickets);
        tab.Controls.Add(pnlTop);
        return tab;
    }

    private async Task LoadMyTicketsAsync()
    {
        string status = cbTicketFilter?.SelectedIndex > 0 ? cbTicketFilter.SelectedItem?.ToString() ?? "" : "";
        string endpoint = $"api/tickets/my?status={Uri.EscapeDataString(status)}&page=1&pageSize=50";

        var res = await ApiClient.GetAsync<PagedResult<MyTicketDto>>(endpoint);
        if (res.Success && res.Data != null)
        {
            dgvMyTickets.DataSource = res.Data.Items;
        }
    }

    private async Task ShowSelectedTicketDetailAsync()
    {
        if (dgvMyTickets.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn 1 vé để xem", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var item = dgvMyTickets.SelectedRows[0].DataBoundItem as MyTicketDto;
        if (item == null) return;

        var res = await ApiClient.GetAsync<TicketDetailDto>($"api/tickets/{item.TicketId}");
        if (res.Success && res.Data != null)
        {
            var t = res.Data;
            string msg = $@"
========================================
CHI TIẾT VÉ XE ĐIỆN TỬ: {t.TicketCode}
========================================
- Tuyến xe: {t.Trip.RouteName}
- Khởi hành: {t.Trip.DepartureTime:dd/MM/yyyy HH:mm}
- Biển số xe: {t.Trip.VehiclePlate}
- Vị trí ghế: {t.SeatCode}
- Hành khách: {t.Passenger.Name} (SĐT: {t.Passenger.Phone})
- Điểm đón: {t.PickupPoint}
- Điểm trả: {t.DropoffPoint}
- Tổng tiền: {t.TotalAmount:N0} VNĐ
- Phương thức: {t.PaymentMethod}
- Trạng thái: {t.Status}
- Thời gian thanh toán: {t.PaidAt:dd/MM/yyyy HH:mm:ss}
========================================";
            MessageBox.Show(msg, "Thông tin chi tiết vé", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // ---------------- TAB 3: THÔNG BÁO ----------------
    private TabPage CreateNotificationsTab()
    {
        var tab = new TabPage("🔔 Thông Báo");

        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(240, 244, 248) };
        btnMarkRead = new Button { Text = "Đánh dấu đã đọc", Location = new Point(15, 10), Width = 150, Height = 30, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnMarkRead.Click += async (s, e) =>
        {
            if (dgvNotifications.SelectedRows.Count > 0)
            {
                var notif = dgvNotifications.SelectedRows[0].DataBoundItem as NotificationDto;
                if (notif != null)
                {
                    await ApiClient.PutAsync<object>($"api/notifications/{notif.Id}/read");
                    await LoadNotificationsAsync();
                }
            }
        };
        pnlTop.Controls.Add(btnMarkRead);

        dgvNotifications = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White
        };

        tab.Controls.Add(dgvNotifications);
        tab.Controls.Add(pnlTop);
        return tab;
    }

    private async Task LoadNotificationsAsync()
    {
        var res = await ApiClient.GetAsync<PagedResult<NotificationDto>>("api/notifications?page=1&pageSize=50");
        if (res.Success && res.Data != null)
        {
            dgvNotifications.DataSource = res.Data.Items;
        }
    }

    // ---------------- TAB 4: CHAT HỖ TRỢ TRỰC TUYẾN ----------------
    private TabPage CreateSupportChatTab()
    {
        var tab = new TabPage("💬 Hỗ Trợ Trực Tuyến (Chat)");

        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Color.FromArgb(240, 244, 248), Padding = new Padding(10) };
        txtChatMessage = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5f) };
        txtChatMessage.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendSupportMessageAsync();
            }
        };

        btnSendChat = new Button
        {
            Text = "Gửi Tin Nhắn",
            Dock = DockStyle.Right,
            Width = 120,
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        btnSendChat.Click += async (s, e) => await SendSupportMessageAsync();

        pnlBottom.Controls.Add(txtChatMessage);
        pnlBottom.Controls.Add(btnSendChat);

        rtbChatHistory = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 10),
            Padding = new Padding(15)
        };

        tab.Controls.Add(rtbChatHistory);
        tab.Controls.Add(pnlBottom);
        return tab;
    }

    private async Task LoadChatHistoryAsync()
    {
        var res = await ApiClient.GetAsync<PagedResult<SupportMessageDto>>("api/support/messages?page=1&pageSize=100");
        if (res.Success && res.Data != null)
        {
            rtbChatHistory.Clear();
            foreach (var msg in res.Data.Items)
            {
                AppendChatMessage(msg.SenderRole, msg.Content, msg.SentAt);
                _currentSupportTicketId = msg.TicketId;
            }
        }
    }

    private async Task SendSupportMessageAsync()
    {
        string text = txtChatMessage.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        txtChatMessage.Clear();

        // 1. Send via WebSocket if connected
        if (RealtimeClient.Instance.IsConnected)
        {
            await RealtimeClient.Instance.SendMessageAsync(_currentSupportTicketId, text);
            AppendChatMessage("Customer", text, DateTime.Now);
        }
        else // 2. Fallback to REST API
        {
            var res = await ApiClient.PostAsync<object>("api/support/messages", new CreateSupportMessageDto { Content = text });
            if (res.Success)
            {
                AppendChatMessage("Customer", text, DateTime.Now);
            }
        }
    }

    private void AppendChatMessage(string senderRole, string content, DateTime time)
    {
        bool isMe = senderRole == Roles.Customer;
        string prefix = isMe ? $"[Tôi - {time:HH:mm}]: " : $"[Nhân viên CSKH - {time:HH:mm}]: ";

        rtbChatHistory.SelectionStart = rtbChatHistory.TextLength;
        rtbChatHistory.SelectionColor = isMe ? Color.FromArgb(30, 60, 114) : Color.FromArgb(165, 0, 100);
        rtbChatHistory.SelectionFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        rtbChatHistory.AppendText(prefix);

        rtbChatHistory.SelectionColor = Color.Black;
        rtbChatHistory.SelectionFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        rtbChatHistory.AppendText(content + "\n\n");

        rtbChatHistory.ScrollToCaret();
    }

    // ---------------- TAB 5: TÀI KHOẢN CÁ NHÂN ----------------
    private TabPage CreateAccountTab()
    {
        var tab = new TabPage("👤 Tài Khoản");

        var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(30) };

        int top = 20;
        int left = 40;
        int width = 380;

        pnl.Controls.Add(new Label { Text = "THÔNG TIN CÁ NHÂN", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) });
        top += 35;

        pnl.Controls.Add(new Label { Text = "Email (Định danh không đổi):", Location = new Point(left, top), AutoSize = true });
        top += 22;
        txtProfileEmail = new TextBox { Location = new Point(left, top), Width = width, ReadOnly = true, BackColor = Color.FromArgb(238, 238, 238) };
        pnl.Controls.Add(txtProfileEmail);
        top += 35;

        pnl.Controls.Add(new Label { Text = "Họ và tên:", Location = new Point(left, top), AutoSize = true });
        top += 22;
        txtProfileName = new TextBox { Location = new Point(left, top), Width = width };
        pnl.Controls.Add(txtProfileName);
        top += 35;

        pnl.Controls.Add(new Label { Text = "Số điện thoại:", Location = new Point(left, top), AutoSize = true });
        top += 22;
        txtProfilePhone = new TextBox { Location = new Point(left, top), Width = width };
        pnl.Controls.Add(txtProfilePhone);
        top += 40;

        var btnUpdateProfile = new Button { Text = "Lưu Thông Tin Cá Nhân", Location = new Point(left, top), Width = width, Height = 36, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnUpdateProfile.Click += async (s, e) =>
        {
            var req = new UpdateProfileDto { FullName = txtProfileName.Text.Trim(), Phone = txtProfilePhone.Text.Trim() };
            var res = await ApiClient.PutAsync<object>("api/account/profile", req);
            if (res.Success)
            {
                MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (SessionManager.CurrentUser != null)
                {
                    SessionManager.CurrentUser.FullName = req.FullName;
                    SessionManager.CurrentUser.Phone = req.Phone ?? "";
                    lblUserInfo.Text = $"Khách hàng: {SessionManager.CurrentUser.FullName} ({SessionManager.CurrentUser.Email})";
                }
            }
        };
        pnl.Controls.Add(btnUpdateProfile);
        top += 60;

        // Change password section
        pnl.Controls.Add(new Label { Text = "ĐỔI MẬT KHẨU", Location = new Point(left, top), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 114) });
        top += 35;

        pnl.Controls.Add(new Label { Text = "Mật khẩu hiện tại:", Location = new Point(left, top), AutoSize = true });
        top += 22;
        txtOldPass = new TextBox { UseSystemPasswordChar = true, Location = new Point(left, top), Width = width };
        pnl.Controls.Add(txtOldPass);
        top += 35;

        pnl.Controls.Add(new Label { Text = "Mật khẩu mới:", Location = new Point(left, top), AutoSize = true });
        top += 22;
        txtNewPass = new TextBox { UseSystemPasswordChar = true, Location = new Point(left, top), Width = width };
        pnl.Controls.Add(txtNewPass);
        top += 40;

        var btnChangePass = new Button { Text = "Đổi Mật Khẩu", Location = new Point(left, top), Width = width, Height = 36, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnChangePass.Click += async (s, e) =>
        {
            if (string.IsNullOrEmpty(txtOldPass.Text) || string.IsNullOrEmpty(txtNewPass.Text))
            {
                MessageBox.Show("Vui lòng điền đủ mật khẩu cũ và mới", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var req = new ChangePasswordDto { OldPassword = txtOldPass.Text.Trim(), NewPassword = txtNewPass.Text.Trim() };
            var res = await ApiClient.PostAsync<object>("api/account/change-password", req);
            if (res.Success)
            {
                MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtOldPass.Clear();
                txtNewPass.Clear();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        pnl.Controls.Add(btnChangePass);

        tab.Controls.Add(pnl);
        return tab;
    }

    private async Task LoadProfileAsync()
    {
        var res = await ApiClient.GetAsync<ProfileDto>("api/account/profile");
        if (res.Success && res.Data != null)
        {
            txtProfileEmail.Text = res.Data.Email;
            txtProfileName.Text = res.Data.FullName;
            txtProfilePhone.Text = res.Data.Phone;
        }
    }
}
