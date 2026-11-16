using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Dialogs;

/// <summary>
/// Form Thêm / Sửa tuyến xe kèm danh sách điểm đón, điểm trả động (formThemTuyen / formSuaTuyen).
/// </summary>
public class frmRouteEditor : Form
{
    private readonly int? _routeId;
    private readonly ModernTextBox _txtOrigin;
    private readonly ModernTextBox _txtDestination;
    private readonly ModernTextBox _txtDistance;
    private readonly ModernTextBox _txtDuration;
    private readonly ComboBox _cbStatus;
    private readonly DoubleBufferedFlowLayoutPanel _flowPickups;
    private readonly DoubleBufferedFlowLayoutPanel _flowDropoffs;
    private readonly ModernButton _btnSave;

    public frmRouteEditor(int? routeId = null)
    {
        _routeId = routeId;
        Text = routeId.HasValue ? "Sửa thông tin tuyến xe" : "Thêm tuyến xe mới";
        Size = new Size(700, 720);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(20);
        DoubleBuffered = true;

        var card = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder
        };

        card.Controls.Add(new Label
        {
            Text = Text,
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(22, 16)
        });

        card.Controls.Add(UiHelper.CreateFieldLabel("ĐIỂM ĐI", 22, 56));
        _txtOrigin = new ModernTextBox { Location = new Point(22, 78), Size = new Size(288, 40), PlaceholderText = "vd: TP.HCM" };

        card.Controls.Add(UiHelper.CreateFieldLabel("ĐIỂM ĐẾN", 326, 56));
        _txtDestination = new ModernTextBox { Location = new Point(326, 78), Size = new Size(288, 40), PlaceholderText = "vd: Đà Lạt" };

        card.Controls.Add(UiHelper.CreateFieldLabel("QUÃNG ĐƯỜNG (KM)", 22, 130));
        _txtDistance = new ModernTextBox { Location = new Point(22, 152), Size = new Size(185, 40), Text = "300" };

        card.Controls.Add(UiHelper.CreateFieldLabel("THỜI GIAN (PHÚT)", 222, 130));
        _txtDuration = new ModernTextBox { Location = new Point(222, 152), Size = new Size(185, 40), Text = "420" };

        _cbStatus = UiHelper.CreateStyledComboBox(424, 154, 190);
        _cbStatus.Items.AddRange(new object[] { RouteStatuses.Active, RouteStatuses.Inactive });
        _cbStatus.SelectedIndex = 0;

        if (routeId.HasValue)
        {
            card.Controls.Add(UiHelper.CreateFieldLabel("TRẠNG THÁI", 424, 130));
            card.Controls.Add(_cbStatus);
        }

        // Danh sách điểm đón động
        card.Controls.Add(UiHelper.CreateFieldLabel("DANH SÁCH ĐIỂM ĐÓN", 22, 206));
        var btnAddPickup = new ModernButton
        {
            Text = "+ Thêm điểm đón",
            Icon = IconChar.Plus,
            IconSize = 11,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Font = AppTheme.CaptionBoldFont,
            Location = new Point(474, 200),
            Size = new Size(140, 28)
        };
        _flowPickups = CreateDynamicPointList(22, 232, 592, 140);
        btnAddPickup.Click += (_, _) => AddPointRow(_flowPickups, null, string.Empty, string.Empty);

        // Danh sách điểm trả động
        card.Controls.Add(UiHelper.CreateFieldLabel("DANH SÁCH ĐIỂM TRẢ", 22, 384));
        var btnAddDropoff = new ModernButton
        {
            Text = "+ Thêm điểm trả",
            Icon = IconChar.Plus,
            IconSize = 11,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Font = AppTheme.CaptionBoldFont,
            Location = new Point(474, 378),
            Size = new Size(140, 28)
        };
        _flowDropoffs = CreateDynamicPointList(22, 410, 592, 140);
        btnAddDropoff.Click += (_, _) => AddPointRow(_flowDropoffs, null, string.Empty, string.Empty);

        _btnSave = new ModernButton
        {
            Text = routeId.HasValue ? "Lưu cập nhật tuyến" : "Thêm tuyến xe",
            Icon = IconChar.Check,
            FillColor = AppTheme.Accent,
            HoverColor = AppTheme.AccentHover,
            Location = new Point(22, 562),
            Size = new Size(592, 42)
        };
        _btnSave.Click += async (_, _) => await SaveAsync();

        card.Controls.AddRange(new Control[]
        {
            _txtOrigin, _txtDestination, _txtDistance, _txtDuration,
            btnAddPickup, _flowPickups,
            btnAddDropoff, _flowDropoffs,
            _btnSave
        });
        Controls.Add(card);

        if (routeId.HasValue)
        {
            Load += async (_, _) => await LoadDetailAsync();
        }
        else
        {
            AddPointRow(_flowPickups, null, "Bến xe trung tâm", string.Empty);
            AddPointRow(_flowDropoffs, null, "Bến xe điểm đến", string.Empty);
        }
    }

    private static DoubleBufferedFlowLayoutPanel CreateDynamicPointList(int x, int y, int width, int height)
    {
        return new DoubleBufferedFlowLayoutPanel
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = AppTheme.SubtleBg,
            Padding = new Padding(6)
        };
    }

    private void AddPointRow(FlowLayoutPanel host, int? pointId, string name, string address)
    {
        var row = new Panel
        {
            Size = new Size(560, 42),
            BackColor = AppTheme.SubtleBg,
            Margin = new Padding(0, 0, 0, 4),
            Tag = pointId
        };

        var txtName = new ModernTextBox
        {
            Location = new Point(0, 2),
            Size = new Size(215, 36),
            PlaceholderText = "Tên điểm đón/trả...",
            Text = name
        };
        var txtAddress = new ModernTextBox
        {
            Location = new Point(223, 2),
            Size = new Size(290, 36),
            PlaceholderText = "Địa chỉ cụ thể...",
            Text = address
        };
        var btnRemove = new ModernButton
        {
            Text = string.Empty,
            Icon = IconChar.Times,
            IconSize = 12,
            FillColor = AppTheme.DangerBg,
            HoverColor = Color.FromArgb(254, 202, 202),
            ForeColor = AppTheme.DangerFg,
            Location = new Point(521, 3),
            Size = new Size(34, 34)
        };
        btnRemove.Click += (_, _) =>
        {
            if (host.Controls.Count <= 1)
            {
                UiHelper.ShowError("Mỗi tuyến cần giữ lại ít nhất 1 điểm.", this);
                return;
            }
            host.Controls.Remove(row);
            row.Dispose();
        };

        row.Controls.AddRange(new Control[] { txtName, txtAddress, btnRemove });
        host.Controls.Add(row);
    }

    private static List<RoutePointInputDto> CollectPoints(FlowLayoutPanel host)
    {
        var list = new List<RoutePointInputDto>();
        foreach (Control c in host.Controls)
        {
            if (c is not Panel row) continue;
            var boxes = row.Controls.OfType<ModernTextBox>().ToList();
            if (boxes.Count < 2) continue;
            string name = boxes[0].Text.Trim();
            string addr = boxes[1].Text.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                list.Add(new RoutePointInputDto
                {
                    Id = row.Tag as int?,
                    Name = name,
                    Address = string.IsNullOrWhiteSpace(addr) ? name : addr
                });
            }
        }
        return list;
    }

    private async Task LoadDetailAsync()
    {
        var res = await ApiService.GetAsync<RouteDetailDto>($"api/admin/routes/{_routeId!.Value}");
        if (!res.Success || res.Data == null)
        {
            UiHelper.ShowError(res.Message, this);
            return;
        }

        var d = res.Data;
        _txtOrigin.Text = d.Origin;
        _txtDestination.Text = d.Destination;
        _txtDistance.Text = d.DistanceKm?.ToString("0.##") ?? string.Empty;
        _txtDuration.Text = d.DurationMinutes?.ToString() ?? string.Empty;
        _cbStatus.SelectedItem = d.Status;

        _flowPickups.Controls.Clear();
        foreach (var p in d.PickupPoints) AddPointRow(_flowPickups, p.Id, p.Name, p.Address);
        if (_flowPickups.Controls.Count == 0) AddPointRow(_flowPickups, null, string.Empty, string.Empty);

        _flowDropoffs.Controls.Clear();
        foreach (var p in d.DropoffPoints) AddPointRow(_flowDropoffs, p.Id, p.Name, p.Address);
        if (_flowDropoffs.Controls.Count == 0) AddPointRow(_flowDropoffs, null, string.Empty, string.Empty);
    }

    private async Task SaveAsync()
    {
        var pickups = CollectPoints(_flowPickups);
        var dropoffs = CollectPoints(_flowDropoffs);

        if (string.IsNullOrWhiteSpace(_txtOrigin.Text) || string.IsNullOrWhiteSpace(_txtDestination.Text) ||
            pickups.Count == 0 || dropoffs.Count == 0)
        {
            UiHelper.ShowError("Vui lòng nhập Điểm đi, Điểm đến và ít nhất 1 điểm đón, 1 điểm trả.", this);
            return;
        }

        decimal? dist = decimal.TryParse(_txtDistance.Text.Trim(), out decimal dVal) ? dVal : null;
        int? dur = int.TryParse(_txtDuration.Text.Trim(), out int mVal) ? mVal : null;

        await UiHelper.RunButtonActionAsync(_btnSave, "Đang lưu...", async () =>
        {
            if (_routeId.HasValue)
            {
                var res = await ApiService.PutAsync<object>($"api/admin/routes/{_routeId.Value}", new UpdateRouteRequest
                {
                    Origin = _txtOrigin.Text.Trim(),
                    Destination = _txtDestination.Text.Trim(),
                    DistanceKm = dist,
                    DurationMinutes = dur,
                    Status = _cbStatus.SelectedItem?.ToString() ?? RouteStatuses.Active,
                    PickupPoints = pickups,
                    DropoffPoints = dropoffs
                });
                if (!res.Success) { UiHelper.ShowError(res.Message, this); return; }
            }
            else
            {
                var res = await ApiService.PostAsync<CreateRouteResponseData>("api/admin/routes", new CreateRouteRequest
                {
                    Origin = _txtOrigin.Text.Trim(),
                    Destination = _txtDestination.Text.Trim(),
                    DistanceKm = dist,
                    DurationMinutes = dur,
                    PickupPoints = pickups,
                    DropoffPoints = dropoffs
                });
                if (!res.Success) { UiHelper.ShowError(res.Message, this); return; }
            }

            DialogResult = DialogResult.OK;
            Close();
        }, this);
    }
}

public class formThemTuyen : frmRouteEditor
{
    public formThemTuyen() : base(null) { }
}

public class formSuaTuyen : frmRouteEditor
{
    public formSuaTuyen(int routeId) : base(routeId) { }
}
