using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Customer;

/// <summary>
/// Form Tìm chuyến xe (Card-based UI với Floating Search Bar và TripCardItem).
/// </summary>
public class frmSearchTrips : Form
{
    private readonly ComboBox _cbFrom;
    private readonly ComboBox _cbTo;
    private readonly DateTimePicker _dtpDate;
    private readonly ModernButton _btnSearch;
    private readonly Label _lblResultCount;
    private readonly DoubleBufferedFlowLayoutPanel _flowTrips;
    private readonly RoundedCardPanel _emptyCard;

    private List<TripSearchItemDto> _currentTrips = new();
    private bool _sortByPrice;

    public event Action? BookingCompleted;

    public frmSearchTrips()
    {
        Text = "Tìm chuyến xe";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        // Floating Search Bar
        var searchBar = new RoundedCardPanel
        {
            Dock = DockStyle.Top,
            Height = 104,
            CornerRadius = 14,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(20, 14, 20, 14)
        };

        searchBar.Controls.Add(UiHelper.CreateFieldLabel("ĐIỂM ĐI", 22, 16));
        _cbFrom = UiHelper.CreateStyledComboBox(22, 40, 200, ComboBoxStyle.DropDown);
        _cbFrom.Items.AddRange(new object[] { "TP.HCM", "Đà Lạt", "Vũng Tàu" });
        _cbFrom.Text = "TP.HCM";

        var btnSwap = new ModernButton
        {
            Text = string.Empty,
            Icon = IconChar.ArrowRightArrowLeft,
            IconSize = 15,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            CornerRadius = 18,
            Location = new Point(230, 38),
            Size = new Size(40, 36)
        };
        btnSwap.Click += (_, _) =>
        {
            string tmp = _cbFrom.Text;
            _cbFrom.Text = _cbTo.Text;
            _cbTo.Text = tmp;
        };

        searchBar.Controls.Add(UiHelper.CreateFieldLabel("ĐIỂM ĐẾN", 278, 16));
        _cbTo = UiHelper.CreateStyledComboBox(278, 40, 200, ComboBoxStyle.DropDown);
        _cbTo.Items.AddRange(new object[] { "Đà Lạt", "Vũng Tàu", "TP.HCM" });
        _cbTo.Text = "Đà Lạt";

        searchBar.Controls.Add(UiHelper.CreateFieldLabel("NGÀY KHỞI HÀNH", 494, 16));
        _dtpDate = new DateTimePicker
        {
            Location = new Point(494, 41),
            Size = new Size(180, 34),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = AppTheme.BodyFont,
            Value = DateTime.Today.AddDays(1)
        };

        _btnSearch = new ModernButton
        {
            Text = "Tìm chuyến",
            Icon = IconChar.Search,
            IconSize = 15,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(692, 34),
            Size = new Size(160, 44)
        };
        _btnSearch.Click += async (_, _) => await SearchAsync();

        searchBar.Controls.AddRange(new Control[] { _cbFrom, btnSwap, _cbTo, _dtpDate, _btnSearch });

        // Results Toolbar
        var pnlToolbar = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Width = 900,
            BackColor = AppTheme.CanvasBg,
            Padding = new Padding(4, 12, 4, 6)
        };

        _lblResultCount = new Label
        {
            Text = "Nhấn 'Tìm chuyến' để xem danh sách chuyến xe khả dụng",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CanvasBg,
            AutoSize = true,
            Location = new Point(4, 14)
        };

        var btnSortTime = new ModernButton
        {
            Text = "Giờ sớm nhất",
            Icon = IconChar.Clock,
            IconSize = 13,
            FillColor = Color.White,
            HoverColor = AppTheme.PrimarySoft,
            ForeColor = AppTheme.TextSecondary,
            BorderColor = AppTheme.CardBorder,
            BorderWidth = 1,
            Size = new Size(134, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlToolbar.Width - 280, 10)
        };
        btnSortTime.Click += (_, _) => { _sortByPrice = false; RenderTripCards(); };

        var btnSortPrice = new ModernButton
        {
            Text = "Giá thấp nhất",
            Icon = IconChar.TicketAlt,
            IconSize = 13,
            FillColor = Color.White,
            HoverColor = AppTheme.PrimarySoft,
            ForeColor = AppTheme.TextSecondary,
            BorderColor = AppTheme.CardBorder,
            BorderWidth = 1,
            Size = new Size(134, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlToolbar.Width - 138, 10)
        };
        btnSortPrice.Click += (_, _) => { _sortByPrice = true; RenderTripCards(); };

        pnlToolbar.Controls.AddRange(new Control[] { _lblResultCount, btnSortTime, btnSortPrice });

        _flowTrips = new DoubleBufferedFlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = AppTheme.CanvasBg,
            Padding = new Padding(0, 6, 8, 16)
        };

        _emptyCard = new RoundedCardPanel
        {
            Size = new Size(820, 140),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder
        };
        var emptyIcon = new IconPictureBox
        {
            IconChar = IconChar.Bus,
            IconSize = 36,
            IconColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            Size = new Size(48, 48),
            Location = new Point(36, 44),
            SizeMode = PictureBoxSizeMode.CenterImage
        };
        var emptyTitle = new Label
        {
            Text = "Chưa tìm thấy chuyến xe phù hợp trong ngày đã chọn",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(100, 44)
        };
        var emptySub = new Label
        {
            Text = "Gợi ý: thử chọn tuyến TP.HCM → Đà Lạt hoặc TP.HCM → Vũng Tàu vào ngày mai.",
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(100, 76)
        };
        _emptyCard.Controls.AddRange(new Control[] { emptyIcon, emptyTitle, emptySub });

        _flowTrips.Resize += (_, _) => ResizeCards();

        Controls.Add(_flowTrips);
        Controls.Add(pnlToolbar);
        Controls.Add(searchBar);
    }

    public async Task SearchAsync()
    {
        string from = _cbFrom.Text.Trim();
        string to = _cbTo.Text.Trim();
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            UiHelper.ShowError("Vui lòng chọn Điểm đi và Điểm đến.", FindForm());
            return;
        }

        await UiHelper.RunButtonActionAsync(_btnSearch, "Đang tìm...", async () =>
        {
            string url = $"api/trips?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&date={_dtpDate.Value:yyyy-MM-dd}";
            var res = await ApiService.GetAsync<List<TripSearchItemDto>>(url);
            if (!res.Success)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            _currentTrips = res.Data ?? new List<TripSearchItemDto>();
            RenderTripCards();
        }, FindForm());
    }

    private void RenderTripCards()
    {
        _flowTrips.SuspendLayout();
        _flowTrips.Controls.Clear();

        var ordered = _sortByPrice
            ? _currentTrips.OrderBy(t => t.Price).ThenBy(t => t.DepartureTime).ToList()
            : _currentTrips.OrderBy(t => t.DepartureTime).ToList();

        int cardW = Math.Max(620, _flowTrips.ClientSize.Width - 28);

        if (ordered.Count == 0)
        {
            _lblResultCount.Text = "Không có chuyến xe nào phù hợp";
            _emptyCard.Width = cardW;
            _flowTrips.Controls.Add(_emptyCard);
        }
        else
        {
            _lblResultCount.Text = $"Tìm thấy {ordered.Count} chuyến xe khả dụng";
            foreach (var trip in ordered)
            {
                var card = new TripCardItem(trip) { Width = cardW };
                card.SelectClicked += async (_, selectedTrip) => await OpenSeatSelectionAsync(selectedTrip);
                _flowTrips.Controls.Add(card);
            }
        }

        _flowTrips.ResumeLayout();
    }

    private async Task OpenSeatSelectionAsync(TripSearchItemDto trip)
    {
        using var seatForm = new frmSeatSelection(trip);
        seatForm.ShowDialog(FindForm());
        if (seatForm.BookingSucceeded)
        {
            await SearchAsync();
            BookingCompleted?.Invoke();
        }
    }

    private void ResizeCards()
    {
        int cardW = Math.Max(620, _flowTrips.ClientSize.Width - 28);
        foreach (Control c in _flowTrips.Controls)
        {
            c.Width = cardW;
        }
    }
}
