using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using FontAwesome.Sharp;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;

namespace BusBooking.Client.Forms.Admin.Views;

/// <summary>
/// Màn hình Tổng quan (formTongQuan): 3 thẻ KPI hôm nay, biểu đồ LiveCharts2 và bảng chuyến sắp khởi hành.
/// </summary>
public class formTongQuan : Form
{
    private readonly KpiCard _kpiTickets;
    private readonly KpiCard _kpiTrips;
    private readonly KpiCard _kpiRevenue;

    private readonly PieChart _pieChart;
    private readonly Label _lblChartTitle;
    private readonly DataGridView _dgvUpcoming;
    private readonly ModernButton _btnPrev;
    private readonly ModernButton _btnNext;
    private readonly ModernButton _btnRefresh;
    private readonly Label _lblPage;

    private int _page = 1;
    private int _totalPages = 1;

    public formTongQuan()
    {
        Text = "Tổng quan hệ thống";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        // 1. 3 thẻ thống kê KPI theo đặc tả (Số vé hôm nay, Số chuyến hôm nay, Doanh thu hôm nay)
        var kpiRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 122,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = AppTheme.CanvasBg,
            Margin = new Padding(0, 0, 0, 14)
        };
        for (int i = 0; i < 3; i++) kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        _kpiTickets = new KpiCard("SỐ VÉ BÁN HÔM NAY", "0", "Vé đã thanh toán & xác nhận", IconChar.TicketAlt, AppTheme.Primary)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 8)
        };
        _kpiTrips = new KpiCard("SỐ CHUYẾN HÔM NAY", "0", "Chuyến khai thác trong ngày", IconChar.Bus, AppTheme.PrimaryDark)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 6, 8)
        };
        _kpiRevenue = new KpiCard("DOANH THU HÔM NAY", "0 đ", "Tổng thu Online & Trực tiếp", IconChar.ChartBar, AppTheme.Accent)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 0, 0, 8)
        };

        kpiRow.Controls.Add(_kpiTickets, 0, 0);
        kpiRow.Controls.Add(_kpiTrips, 1, 0);
        kpiRow.Controls.Add(_kpiRevenue, 2, 0);

        // 2. Bảng chuyến sắp khởi hành + Biểu đồ Donut LiveCharts2
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.CanvasBg
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));

        var tripsCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 10, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(18)
        };

        var tripsHeader = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            Width = 600,
            BackColor = AppTheme.CardSurface
        };
        tripsHeader.Controls.Add(new Label
        {
            Text = "Danh sách chuyến xe sắp khởi hành",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(0, 8)
        });

        _btnRefresh = new ModernButton
        {
            Text = "Làm mới",
            Icon = IconChar.SyncAlt,
            IconSize = 13,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Size = new Size(110, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(tripsHeader.Width - 110, 4)
        };
        _btnRefresh.Click += async (_, _) => await LoadAsync();
        tripsHeader.Controls.Add(_btnRefresh);

        var pagerPanel = new DoubleBufferedPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = AppTheme.CardSurface
        };
        _btnPrev = new ModernButton
        {
            Text = "Trước",
            Icon = IconChar.ChevronLeft,
            IconSize = 12,
            FillColor = AppTheme.SubtleBg,
            HoverColor = AppTheme.CardBorder,
            ForeColor = AppTheme.TextSecondary,
            Size = new Size(88, 32),
            Location = new Point(0, 8)
        };
        _btnNext = new ModernButton
        {
            Text = "Sau",
            FillColor = AppTheme.SubtleBg,
            HoverColor = AppTheme.CardBorder,
            ForeColor = AppTheme.TextSecondary,
            Size = new Size(76, 32),
            Location = new Point(96, 8)
        };
        _lblPage = new Label
        {
            Text = "Trang 1/1",
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(186, 15)
        };
        _btnPrev.Click += async (_, _) => { if (_page > 1) { _page--; await LoadUpcomingPageAsync(); } };
        _btnNext.Click += async (_, _) => { if (_page < _totalPages) { _page++; await LoadUpcomingPageAsync(); } };
        pagerPanel.Controls.AddRange(new Control[] { _btnPrev, _btnNext, _lblPage });

        _dgvUpcoming = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgvUpcoming);

        tripsCard.Controls.Add(_dgvUpcoming);
        tripsCard.Controls.Add(pagerPanel);
        tripsCard.Controls.Add(tripsHeader);

        var chartCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 6, 0, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(18)
        };

        _lblChartTitle = new Label
        {
            Text = "Tỷ trọng doanh thu (Online vs Trực tiếp)",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            Dock = DockStyle.Top,
            Height = 36
        };

        _pieChart = new PieChart
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CardSurface,
            LegendPosition = LegendPosition.Bottom,
            LegendTextPaint = new SolidColorPaint(SKColor.Parse("#334155"))
        };

        chartCard.Controls.Add(_pieChart);
        chartCard.Controls.Add(_lblChartTitle);

        split.Controls.Add(tripsCard, 0, 0);
        split.Controls.Add(chartCard, 1, 0);

        Controls.Add(split);
        Controls.Add(kpiRow);
    }

    public async Task LoadAsync()
    {
        await UiHelper.RunButtonActionAsync(_btnRefresh, "Đang tải...", async () =>
        {
            var statsRes = await ApiService.GetAsync<DashboardStatsDto>("api/admin/dashboard/stats");
            if (!statsRes.Success || statsRes.Data == null)
            {
                UiHelper.ShowError(statsRes.Message, FindForm());
                return;
            }

            var stats = statsRes.Data;
            _kpiTickets.UpdateMetric(stats.TicketsToday.ToString("N0"));
            _kpiTrips.UpdateMetric(stats.TripsToday.ToString("N0"));
            _kpiRevenue.UpdateMetric(AppTheme.FormatVnd(stats.RevenueToday));

            await LoadUpcomingPageAsync();
            await LoadChartAsync(stats);
        }, FindForm());
    }

    private async Task LoadUpcomingPageAsync()
    {
        var res = await ApiService.GetAsync<PagedResult<UpcomingTripDto>>(
            $"api/admin/dashboard/upcoming-trips?page={_page}&pageSize=10");
        if (!res.Success || res.Data == null)
            return;

        _totalPages = Math.Max(1, res.Data.TotalPages);
        _lblPage.Text = $"Trang {res.Data.Page}/{_totalPages} ({res.Data.TotalItems} chuyến)";
        _btnPrev.Enabled = _page > 1;
        _btnNext.Enabled = _page < _totalPages;

        _dgvUpcoming.DataSource = res.Data.Items;
        if (_dgvUpcoming.Columns["TripId"] != null) _dgvUpcoming.Columns["TripId"].Visible = false;
        if (_dgvUpcoming.Columns["TripCode"] != null) _dgvUpcoming.Columns["TripCode"].HeaderText = "Mã chuyến";
        if (_dgvUpcoming.Columns["RouteName"] != null) _dgvUpcoming.Columns["RouteName"].HeaderText = "Tuyến xe";
        if (_dgvUpcoming.Columns["DepartureTime"] != null)
        {
            _dgvUpcoming.Columns["DepartureTime"].HeaderText = "Khởi hành";
            _dgvUpcoming.Columns["DepartureTime"].DefaultCellStyle.Format = "HH:mm dd/MM";
        }
        if (_dgvUpcoming.Columns["BookedSeats"] != null) _dgvUpcoming.Columns["BookedSeats"].HeaderText = "Đã đặt";
        if (_dgvUpcoming.Columns["TotalSeats"] != null) _dgvUpcoming.Columns["TotalSeats"].HeaderText = "Tổng ghế";
    }

    private async Task LoadChartAsync(DashboardStatsDto stats)
    {
        if (SessionManager.CurrentUser?.Role == UserRoles.Admin)
        {
            var revRes = await ApiService.GetAsync<RevenueReportDto>(
                $"api/admin/revenue?fromDate={DateTime.Today.AddDays(-29):yyyy-MM-dd}&toDate={DateTime.Today:yyyy-MM-dd}");
            if (revRes.Success && revRes.Data != null)
            {
                decimal online = revRes.Data.OnlineRevenue;
                decimal offline = revRes.Data.OfflineRevenue;
                decimal sum = online + offline;
                double onPct = sum > 0 ? (double)(online * 100m / sum) : 0;
                double offPct = sum > 0 ? (double)(offline * 100m / sum) : 0;

                _lblChartTitle.Text = "Tỷ trọng doanh thu 30 ngày (Online vs Trực tiếp)";
                _pieChart.Series = new ISeries[]
                {
                    new PieSeries<double>
                    {
                        Name = $"Online ({onPct:0.#}%)",
                        Values = new[] { (double)Math.Max(0, online) },
                        InnerRadius = 56,
                        Fill = new SolidColorPaint(SKColor.Parse("#1D4ED8"))
                    },
                    new PieSeries<double>
                    {
                        Name = $"Trực tiếp ({offPct:0.#}%)",
                        Values = new[] { (double)Math.Max(0, offline) },
                        InnerRadius = 56,
                        Fill = new SolidColorPaint(SKColor.Parse("#F97316"))
                    }
                };
                return;
            }
        }

        int booked = stats.UpcomingTrips.Sum(t => t.BookedSeats);
        int empty = Math.Max(0, stats.UpcomingTrips.Sum(t => t.TotalSeats) - booked);
        _lblChartTitle.Text = "Tỷ lệ lấp đầy ghế sắp khởi hành";
        _pieChart.Series = new ISeries[]
        {
            new PieSeries<double>
            {
                Name = $"Đã đặt ({booked})",
                Values = new[] { (double)booked },
                InnerRadius = 56,
                Fill = new SolidColorPaint(SKColor.Parse("#1D4ED8"))
            },
            new PieSeries<double>
            {
                Name = $"Còn trống ({empty})",
                Values = new[] { (double)empty },
                InnerRadius = 56,
                Fill = new SolidColorPaint(SKColor.Parse("#E2E8F0"))
            }
        };
    }
}

public class AdminDashboardView : formTongQuan
{
}
