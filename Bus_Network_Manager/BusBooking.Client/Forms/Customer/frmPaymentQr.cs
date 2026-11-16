using System.Diagnostics;
using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using FontAwesome.Sharp;
using QRCoder;

namespace BusBooking.Client.Forms.Customer;

/// <summary>
/// Màn hình Thanh toán MoMo QR: hiển thị thông tin vé, tải/sinh mã QR, nút kiểm tra fallback và tự động xác nhận qua SignalR.
/// </summary>
public class frmPaymentQR : Form
{
    private readonly BookingResponseData _booking;
    private readonly PictureBox _picQr;
    private readonly BadgeLabel _statusBadge;
    private readonly Label _lblHint;
    private readonly ModernButton _btnSimulatePaid;
    private readonly ModernButton _btnCheckStatus;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private bool _isPaid;

    public bool PaymentCompleted => _isPaid;

    public frmPaymentQR(
        BookingResponseData booking,
        TripSearchItemDto? trip = null,
        string? seatCode = null,
        string? passengerName = null)
    {
        _booking = booking;

        Text = $"Thanh toán MoMo - Vé {booking.TicketCode}";
        var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 800);
        Size = new Size(540, Math.Min(700, Math.Max(560, workArea.Height - 24)));
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
            AutoScroll = true,
            CornerRadius = 14,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(24)
        };

        var momoHeader = new RoundedCardPanel
        {
            Location = new Point(24, 20),
            Size = new Size(432, 64),
            CornerRadius = 12,
            SurfaceColor = AppTheme.MomoBrand,
            BorderThickness = 0,
            Padding = new Padding(16, 10, 16, 10)
        };

        var iconQr = new IconPictureBox
        {
            IconChar = IconChar.Qrcode,
            IconSize = 28,
            IconColor = Color.White,
            BackColor = AppTheme.MomoBrand,
            Location = new Point(16, 16),
            Size = new Size(32, 32),
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        var lblMomoTitle = new Label
        {
            Text = "CỔNG THANH TOÁN VÍ MOMO",
            Font = AppTheme.CardTitleFont,
            ForeColor = Color.White,
            BackColor = AppTheme.MomoBrand,
            AutoSize = true,
            Location = new Point(58, 12)
        };

        var lblOrder = new Label
        {
            Text = $"Mã vé: {booking.TicketCode}  •  Đơn: {booking.Payment.OrderId}",
            Font = AppTheme.CaptionFont,
            ForeColor = Color.FromArgb(252, 231, 243),
            BackColor = AppTheme.MomoBrand,
            AutoSize = true,
            Location = new Point(58, 36)
        };
        momoHeader.Controls.AddRange(new Control[] { iconQr, lblMomoTitle, lblOrder });

        // Tóm tắt thông tin vé
        var ticketSummary = new RoundedCardPanel
        {
            Location = new Point(24, 94),
            Size = new Size(432, 56),
            CornerRadius = 10,
            SurfaceColor = AppTheme.SubtleBg,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(12, 8, 12, 8)
        };
        string summaryLine1 = trip != null
            ? $"{trip.Origin} → {trip.Destination} ({trip.DepartureTime:HH:mm dd/MM})"
            : $"Vé đặt chỗ #{booking.TicketCode}";
        string summaryLine2 = $"Ghế: {seatCode ?? "--"}   •   Hành khách: {passengerName ?? SessionManager.CurrentUser?.FullName ?? "--"}";

        ticketSummary.Controls.Add(new Label
        {
            Text = summaryLine1,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.PrimaryDark,
            BackColor = AppTheme.SubtleBg,
            AutoSize = true,
            Location = new Point(12, 8)
        });
        ticketSummary.Controls.Add(new Label
        {
            Text = summaryLine2,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.SubtleBg,
            AutoSize = true,
            Location = new Point(12, 29)
        });

        var lblAmount = new Label
        {
            Text = AppTheme.FormatVnd(booking.TotalAmount),
            Font = new Font("Segoe UI", 21f, FontStyle.Bold),
            ForeColor = AppTheme.Accent,
            BackColor = AppTheme.CardSurface,
            Location = new Point(24, 156),
            Size = new Size(432, 38),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _statusBadge = new BadgeLabel
        {
            Location = new Point(135, 198),
            Size = new Size(210, 28)
        };
        _statusBadge.SetSemanticStatus(booking.Status);

        var qrFrame = new RoundedCardPanel
        {
            Size = new Size(210, 210),
            Location = new Point(135, 236),
            CornerRadius = 12,
            SurfaceColor = Color.White,
            BorderColor = AppTheme.MomoBrand,
            BorderThickness = 2,
            Padding = new Padding(10)
        };

        _picQr = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White,
            Image = GenerateQrBitmap(GetQrPayload())
        };
        qrFrame.Controls.Add(_picQr);

        _lblHint = new Label
        {
            Text = "Quét mã QR bằng ứng dụng MoMo hoặc nhấn nút bên dưới.\r\nHệ thống tự đóng & xác nhận ngay khi nhận sự kiện SignalR.",
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            Location = new Point(24, 456),
            Size = new Size(432, 36),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var btnOpenBrowser = new ModernButton
        {
            Text = "Mở liên kết MoMo",
            Icon = IconChar.Qrcode,
            FillColor = AppTheme.MomoBrand,
            HoverColor = Color.FromArgb(131, 24, 67),
            Location = new Point(24, 502),
            Size = new Size(206, 40)
        };
        btnOpenBrowser.Click += (_, _) => OpenPayUrl();

        _btnCheckStatus = new ModernButton
        {
            Text = "Kiểm tra thanh toán",
            Icon = IconChar.SyncAlt,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            BorderColor = AppTheme.Primary,
            BorderWidth = 1,
            Location = new Point(242, 502),
            Size = new Size(214, 40)
        };
        _btnCheckStatus.Click += async (_, _) => await CheckPaymentStatusAsync(silent: false);

        _btnSimulatePaid = new ModernButton
        {
            Text = "Giả lập MoMo thanh toán thành công (Local Demo)",
            Icon = IconChar.CheckCircle,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(24, 554),
            Size = new Size(432, 42)
        };
        _btnSimulatePaid.Click += async (_, _) => await SimulatePaymentAsync();

        card.Controls.AddRange(new Control[]
        {
            momoHeader, ticketSummary, lblAmount, _statusBadge,
            qrFrame, _lblHint,
            btnOpenBrowser, _btnCheckStatus, _btnSimulatePaid
        });

        Controls.Add(card);

        _pollTimer = new System.Windows.Forms.Timer { Interval = 3500 };
        _pollTimer.Tick += async (_, _) =>
        {
            if (!_isPaid)
                await CheckPaymentStatusAsync(silent: true);
        };
        _pollTimer.Start();

        SignalRService.Instance.PaymentConfirmed += OnRealtimePaymentConfirmed;
        Load += async (_, _) => await TryLoadRemoteQrImageAsync();
        FormClosed += (_, _) =>
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            SignalRService.Instance.PaymentConfirmed -= OnRealtimePaymentConfirmed;
        };
    }

    private string GetQrPayload()
        => string.IsNullOrWhiteSpace(_booking.Payment.QrCodeUrl) ? _booking.Payment.PayUrl : _booking.Payment.QrCodeUrl;

    private async Task TryLoadRemoteQrImageAsync()
    {
        if (string.IsNullOrWhiteSpace(_booking.Payment.QrCodeUrl))
            return;

        var downloaded = await ApiService.TryDownloadImageAsync(_booking.Payment.QrCodeUrl);
        if (downloaded != null && !IsDisposed)
        {
            var old = _picQr.Image;
            _picQr.Image = downloaded;
            old?.Dispose();
        }
    }

    private void OnRealtimePaymentConfirmed(PaymentConfirmedEventDto evt)
    {
        if (evt.TicketId != _booking.TicketId)
            return;

        SignalRService.SafeInvoke(this, () => MarkPaymentCompleted(evt.Status));
    }

    private async Task CheckPaymentStatusAsync(bool silent)
    {
        var res = await ApiService.GetAsync<PaymentStatusResponseData>($"api/bookings/{_booking.TicketId}/payment-status");
        if (!res.Success || res.Data == null)
        {
            if (!silent) UiHelper.ShowError(res.Message, this);
            return;
        }

        if (res.Data.Status == TicketStatuses.Paid || res.Data.Status == TicketStatuses.Confirmed)
        {
            MarkPaymentCompleted(res.Data.Status);
        }
        else if (!silent)
        {
            _statusBadge.SetSemanticStatus(res.Data.Status);
            CenterBadge();
            UiHelper.ShowInfo($"Trạng thái hiện tại: {res.Data.Status}. Vui lòng hoàn tất thanh toán.", this);
        }
    }

    private async Task SimulatePaymentAsync()
    {
        await UiHelper.RunButtonActionAsync(_btnSimulatePaid, "Đang xác nhận MoMo...", async () =>
        {
            var res = await ApiService.PostAsync<object>($"api/payments/momo/simulate/{_booking.TicketId}");
            if (!res.Success)
            {
                UiHelper.ShowError(res.Message, this);
                return;
            }

            MarkPaymentCompleted(TicketStatuses.Paid);
        }, this);
    }

    private void MarkPaymentCompleted(string status)
    {
        if (_isPaid) return;
        _isPaid = true;
        _pollTimer.Stop();

        _statusBadge.SetSemanticStatus(status);
        CenterBadge();
        _lblHint.Text = "Thanh toán hoàn tất! Hóa đơn điện tử đã được ghi nhận và gửi về email.";
        _lblHint.ForeColor = AppTheme.SuccessFg;
        _btnSimulatePaid.Enabled = false;
        _btnSimulatePaid.Text = "Đã thanh toán thành công";

        UiHelper.ShowInfo($"Vé {_booking.TicketCode} đã được thanh toán thành công!", this);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void CenterBadge()
    {
        if (_statusBadge.Parent != null)
            _statusBadge.Location = new Point((_statusBadge.Parent.ClientSize.Width - _statusBadge.Width) / 2, 198);
    }

    private void OpenPayUrl()
    {
        if (string.IsNullOrWhiteSpace(_booking.Payment.PayUrl))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _booking.Payment.PayUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            UiHelper.ShowError($"Không thể mở trình duyệt: {ex.Message}", this);
        }
    }

    public static Image GenerateQrBitmap(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(
            string.IsNullOrWhiteSpace(payload) ? "BusBooking-MoMo" : payload,
            QRCodeGenerator.ECCLevel.Q);
        using var pngQr = new PngByteQRCode(data);
        byte[] bytes = pngQr.GetGraphic(10);
        using var ms = new MemoryStream(bytes);
        using var img = Image.FromStream(ms);
        return new Bitmap(img);
    }
}
