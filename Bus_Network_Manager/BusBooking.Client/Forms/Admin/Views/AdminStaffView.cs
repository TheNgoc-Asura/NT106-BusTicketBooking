using BusBooking.Client.Controls;
using BusBooking.Client.Helpers;
using BusBooking.Client.Services;
using BusBooking.Client.Theme;
using BusBooking.Shared.DTOs;
using FontAwesome.Sharp;

namespace BusBooking.Client.Forms.Admin.Views;

/// <summary>
/// Quản lý quyền truy cập nhân viên (formQuanLyTruyCap) - Chỉ dành cho Admin.
/// </summary>
public class formQuanLyTruyCap : Form
{
    private readonly ModernButton _btnRefresh;
    private readonly ModernButton _btnLock;
    private readonly DataGridView _dgvAccounts;
    private readonly DataGridView _dgvPermissions;

    public formQuanLyTruyCap()
    {
        Text = "Quản lý quyền truy cập";
        BackColor = AppTheme.CanvasBg;
        Font = AppTheme.BodyFont;
        Padding = new Padding(22, 18, 22, 18);
        DoubleBuffered = true;

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.CanvasBg
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));

        var accCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(16)
        };

        var accHeader = new DoubleBufferedPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Width = 600,
            BackColor = AppTheme.CardSurface
        };
        accHeader.Controls.Add(new Label
        {
            Text = "Danh sách tài khoản nhân viên",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(0, 10)
        });

        _btnLock = new ModernButton
        {
            Text = "Khóa tài khoản",
            Icon = IconChar.Ban,
            IconSize = 13,
            FillColor = AppTheme.DangerBg,
            HoverColor = Color.FromArgb(254, 202, 202),
            ForeColor = AppTheme.DangerFg,
            Size = new Size(146, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(accHeader.Width - 146, 4)
        };
        _btnLock.Click += async (_, _) => await LockSelectedAsync();

        _btnRefresh = new ModernButton
        {
            Text = "Tải lại",
            Icon = IconChar.SyncAlt,
            IconSize = 13,
            FillColor = AppTheme.PrimarySoft,
            HoverColor = Color.FromArgb(219, 234, 254),
            ForeColor = AppTheme.Primary,
            Size = new Size(96, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(accHeader.Width - 252, 4)
        };
        _btnRefresh.Click += async (_, _) => await LoadAsync();

        accHeader.Controls.AddRange(new Control[] { _btnRefresh, _btnLock });

        _dgvAccounts = new DataGridView { Dock = DockStyle.Fill };
        ModernGridHelper.ApplyModernStyle(_dgvAccounts, "Status");

        accCard.Controls.Add(_dgvAccounts);
        accCard.Controls.Add(accHeader);

        var permCard = new RoundedCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 0, 0, 0),
            CornerRadius = 12,
            SurfaceColor = AppTheme.CardSurface,
            BorderColor = AppTheme.CardBorder,
            Padding = new Padding(16)
        };
        permCard.Controls.Add(new Label
        {
            Text = "Bảng phân quyền theo vai trò",
            Font = AppTheme.CardTitleFont,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.CardSurface,
            Dock = DockStyle.Top,
            Height = 40
        });

        _dgvPermissions = new DataGridView
        {
            Dock = DockStyle.Fill,
            DefaultCellStyle = { WrapMode = DataGridViewTriState.True }
        };
        ModernGridHelper.ApplyModernStyle(_dgvPermissions);
        _dgvPermissions.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

        permCard.Controls.Add(_dgvPermissions);
        _dgvPermissions.BringToFront();

        split.Controls.Add(accCard, 0, 0);
        split.Controls.Add(permCard, 1, 0);
        Controls.Add(split);
    }

    public async Task LoadAsync()
    {
        await UiHelper.RunButtonActionAsync(_btnRefresh, "Đang tải...", async () =>
        {
            var res = await ApiService.GetAsync<StaffAccountsResponseData>("api/admin/staff-accounts");
            if (!res.Success || res.Data == null)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            _dgvAccounts.DataSource = res.Data.Accounts;
            if (_dgvAccounts.Columns["AccountId"] != null) _dgvAccounts.Columns["AccountId"].Visible = false;
            if (_dgvAccounts.Columns["FullName"] != null) _dgvAccounts.Columns["FullName"].HeaderText = "Họ và tên";
            if (_dgvAccounts.Columns["Email"] != null) _dgvAccounts.Columns["Email"].HeaderText = "Email";
            if (_dgvAccounts.Columns["Role"] != null) _dgvAccounts.Columns["Role"].HeaderText = "Vai trò";
            if (_dgvAccounts.Columns["Status"] != null) _dgvAccounts.Columns["Status"].HeaderText = "Trạng thái";

            _dgvPermissions.DataSource = res.Data.Permissions.Select(p => new
            {
                VaiTro = p.Role,
                ChucNangDuocPhep = string.Join(", ", p.Functions)
            }).ToList();
            if (_dgvPermissions.Columns["VaiTro"] != null)
            {
                _dgvPermissions.Columns["VaiTro"].HeaderText = "Vai trò";
                _dgvPermissions.Columns["VaiTro"].FillWeight = 28;
            }
            if (_dgvPermissions.Columns["ChucNangDuocPhep"] != null)
            {
                _dgvPermissions.Columns["ChucNangDuocPhep"].HeaderText = "Quyền chức năng";
                _dgvPermissions.Columns["ChucNangDuocPhep"].FillWeight = 72;
            }
        }, FindForm());
    }

    private async Task LockSelectedAsync()
    {
        if (_dgvAccounts.CurrentRow?.DataBoundItem is not StaffAccountDto acc)
        {
            UiHelper.ShowError("Vui lòng chọn một tài khoản nhân viên.", FindForm());
            return;
        }

        if (MessageBox.Show(FindForm(),
                $"Xác nhận khóa tài khoản {acc.FullName} ({acc.Email})?",
                "Xác nhận khóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        await UiHelper.RunButtonActionAsync(_btnLock, "Đang khóa...", async () =>
        {
            var res = await ApiService.PutAsync<LockStaffResponseData>($"api/admin/staff-accounts/{acc.AccountId}/lock");
            if (!res.Success)
            {
                UiHelper.ShowError(res.Message, FindForm());
                return;
            }

            UiHelper.ShowInfo($"Đã khóa tài khoản {acc.Email}.", FindForm());
            await LoadAsync();
        }, FindForm());
    }
}

public class AdminStaffView : formQuanLyTruyCap
{
}
