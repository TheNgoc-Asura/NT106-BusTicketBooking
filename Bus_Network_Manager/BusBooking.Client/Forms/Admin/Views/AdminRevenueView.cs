using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;

namespace BusBooking.Client.Forms.Admin.Views;

/// <summary>
/// Báo cáo doanh thu (formDoanhThu) - Chỉ dành cho Admin.
/// </summary>
public class formDoanhThu : Form
{
    private readonly DateTimePicker _dtpFrom;
    private readonly DateTimePicker _dtpTo;
    private readonly ModernButton _btnFilter;

    private readonly KpiCard _kpiTotal;
    private readonly KpiCard _kpiOnline;
    private readonly KpiCard _kpiOffline;

    private readonly CartesianChart _barChart;
    private readonly PieChart _pieChart;
    private readonly DataGridView _dgvDetails;

    public formDoanhThu()
    {
        Text = "Báo cáo doanh thu";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        var filterCard = new RoundedCardPanel
        {
            Dock = DockStyle.Top,
            Height = 70,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(18, 14, 18, 14)
        };

        var lblFrom = UiHelper.CreateFieldLabel("TỪ NGÀY", 18, 25);
        _dtpFrom = new DateTimePicker
        {
            Location = new Point(86, 20),
            Size = new Size(155, 32),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = AppTheme.BodyFont,
            Value = DateTime.Today.AddDays(-14)
        };

        var lblTo = UiHelper.CreateFieldLabel("ĐẾN NGÀY", 262, 25);
        _dtpTo = new DateTimePicker
        {
            Location = new Point(338, 20),
            Size = new Size(155, 32),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = AppTheme.BodyFont,
            Value = DateTime.Today
        };

        _btnFilter = new ModernButton
        {
            Text = "Xem thống kê",
            Icon = IconChar.ChartBar,
            IconSize = 14,
            FillColor = AppTheme.Primary,
            HoverColor = AppTheme.PrimaryHover,
            Location = new Point(514, 16),
            Size = new Size(156, 38)
        };
        _btnFilter.Click += async (_, _) => await LoadAsync();
        filterCard.Controls.AddRange(new Control[] { lblFrom, _dtpFrom, lblTo, _dtpTo, _btnFilter });

        var kpiRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 124,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = AppTheme.CanvasBg,
            Padding = new Padding(0, 10, 0, 8)
        };
        for (int i = 0; i < 3; i++) kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        _kpiTotal = new KpiCard("TỔNG DOANH THU", "0 đ", "Vé Đã thanh toán & Đã xác nhận", IconChar.ChartPie, AppTheme.PrimaryDark)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0)
        };
        _kpiOnline = new KpiCard("DOANH THU ONLINE (MOMO)", "0 đ", "Đặt trực tuyến trên ứng dụng", IconChar.Qrcode, AppTheme.Primary)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 6, 0)
        };
        _kpiOffline = new KpiCard("DOANH THU TRỰC TIẾP (QUẦY)", "0 đ", "Bán trực tiếp tại quầy vé", IconChar.TicketAlt, AppTheme.Accent)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 0, 0, 0)
        };
        kpiRow.Controls.Add(_kpiTotal, 0, 0);
        kpiRow.Controls.Add(_kpiOnline, 1, 0);
        kpiRow.Controls.Add(_kpiOffline, 2, 0);

        var chartRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 255,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.CanvasBg,
            Padding = new Padding(0, 4, 0, 10)
        };
        chartRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
        chartRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));

        var barCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(14)
        };
        barCard.Controls.Add(new Label
        {
            Text = "Biểu đồ doanh thu theo ngày (Online vs Trực tiếp)",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            Dock = DockStyle.Top,
            Height = 30
        });
        _barChart = new CartesianChart
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CardSurface,
            LegendPosition = LegendPosition.Top,
            LegendTextPaint = new SolidColorPaint(SKColor.Parse("#334155"))
        };
        barCard.Controls.Add(_barChart);
        _barChart.BringToFront();

        var pieCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 0, 0, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(14)
        };
        pieCard.Controls.Add(new Label
        {
            Text = "Tỷ trọng kênh bán vé",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            Dock = DockStyle.Top,
            Height = 30
        });
        _pieChart = new PieChart
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CardSurface,
            LegendPosition = LegendPosition.Bottom,
            LegendTextPaint = new SolidColorPaint(SKColor.Parse("#334155"))
        };
        pieCard.Controls.Add(_pieChart);
        _pieChart.BringToFront();

        chartRow.Controls.Add(barCard, 0, 0);
        chartRow.Controls.Add(pieCard, 1, 0);

        var tableCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(16)
        };
        _dgvDetails = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgvDetails);
        tableCard.Controls.Add(_dgvDetails);

        Controls.Add(tableCard);
        Controls.Add(chartRow);
        Controls.Add(kpiRow);
        Controls.Add(filterCard);
    }

    public async Task LoadAsync()
    {
        if (_dtpFrom.Value.Date > _dtpTo.Value.Date)
        {
            UiHelper.ShowError("Từ ngày không được lớn hơn Đến ngày.", FindForm());
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnFilter, "Đang tải...", async () =>
        {
            string url = $"api/admin/revenue?fromDate={_dtpFrom.Value:yyyy-MM-dd}&toDate={_dtpTo.Value:yyyy-MM-dd}";
            var res = await ApiService.GetAsync<RevenueReportDto>(url);
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            var r = res.Data;
            _kpiTotal.UpdateMetric(AppTheme.FormatVnd(r.TotalRevenue));
            _kpiOnline.UpdateMetric(AppTheme.FormatVnd(r.OnlineRevenue));
            _kpiOffline.UpdateMetric(AppTheme.FormatVnd(r.OfflineRevenue));

            var ascDetails = r.Details.OrderBy(d => d.Date).ToList();
            string[] labels = ascDetails.Select(d => d.Date).ToArray();
            double[] onlineVals = ascDetails.Select(d => (double)d.Online).ToArray();
            double[] offlineVals = ascDetails.Select(d => (double)d.Offline).ToArray();

            _barChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Online",
                    Values = onlineVals,
                    Fill = new SolidColorPaint(SKColor.Parse("#1D4ED8"))
                },
                new ColumnSeries<double>
                {
                    Name = "Trực tiếp",
                    Values = offlineVals,
                    Fill = new SolidColorPaint(SKColor.Parse("#F97316"))
                }
            };
            _barChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B"))
                }
            };
            _barChart.YAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                    Labeler = v => $"{v:N0}"
                }
            };

            decimal sum = r.OnlineRevenue + r.OfflineRevenue;
            double onPct = sum > 0 ? (double)(r.OnlineRevenue * 100m / sum) : 0;
            double offPct = sum > 0 ? (double)(r.OfflineRevenue * 100m / sum) : 0;

            _pieChart.Series = new ISeries[]
            {
                new PieSeries<double>
                {
                    Name = $"Online ({onPct:0.#}%)",
                    Values = new[] { (double)Math.Max(0, r.OnlineRevenue) },
                    InnerRadius = 52,
                    Fill = new SolidColorPaint(SKColor.Parse("#1D4ED8"))
                },
                new PieSeries<double>
                {
                    Name = $"Trực tiếp ({offPct:0.#}%)",
                    Values = new[] { (double)Math.Max(0, r.OfflineRevenue) },
                    InnerRadius = 52,
                    Fill = new SolidColorPaint(SKColor.Parse("#F97316"))
                }
            };

            _dgvDetails.DataSource = r.Details;
            if (_dgvDetails.Columns["Date"] != null) _dgvDetails.Columns["Date"].HeaderText = "Ngày";
            if (_dgvDetails.Columns["Online"] != null)
            {
                _dgvDetails.Columns["Online"].HeaderText = "Doanh thu Online (VNĐ)";
                _dgvDetails.Columns["Online"].DefaultCellStyle.Format = "N0";
            }
            if (_dgvDetails.Columns["Offline"] != null)
            {
                _dgvDetails.Columns["Offline"].HeaderText = "Doanh thu Trực tiếp (VNĐ)";
                _dgvDetails.Columns["Offline"].DefaultCellStyle.Format = "N0";
            }
            if (_dgvDetails.Columns["Total"] != null)
            {
                _dgvDetails.Columns["Total"].HeaderText = "Tổng cộng (VNĐ)";
                _dgvDetails.Columns["Total"].DefaultCellStyle.Format = "N0";
            }
        }, FindForm());
    }
}

public class AdminRevenueView : formDoanhThu
{
}
