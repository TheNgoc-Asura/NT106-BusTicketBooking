using System.Drawing.Drawing2D;
using System.Reflection;
using BusBooking.Client.Theme;

namespace BusBooking.Client.Controls;

public static class ModernGridHelper
{
    private static readonly PropertyInfo? DoubleBufferedProp =
        typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// Cấu hình DataGridView hiện đại: bỏ viền dọc, hàng cao 44px, bật DoubleBuffered chống giật lag, hỗ trợ badge trạng thái.
    /// </summary>
    public static void ApplyModernStyle(DataGridView grid, params string[] badgeColumnNames)
    {
        DoubleBufferedProp?.SetValue(grid, true);

        grid.BackgroundColor = AppTheme.CardSurface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = AppTheme.CardBorder;
        grid.EnableHeadersVisualStyles = false;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 44;

        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 42;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.SubtleBg,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.CaptionBoldFont,
            Padding = new Padding(8, 0, 8, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.CardSurface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.BodyFont,
            SelectionBackColor = AppTheme.PrimarySoft,
            SelectionForeColor = AppTheme.TextPrimary,
            Padding = new Padding(8, 0, 8, 0)
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 251, 253),
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.BodyFont,
            SelectionBackColor = AppTheme.PrimarySoft,
            SelectionForeColor = AppTheme.TextPrimary,
            Padding = new Padding(8, 0, 8, 0)
        };

        if (badgeColumnNames.Length > 0)
        {
            var badgeCols = new HashSet<string>(badgeColumnNames, StringComparer.OrdinalIgnoreCase);
            grid.CellPainting += (s, e) => PaintBadgeCell(grid, badgeCols, e);
        }
    }

    private static void PaintBadgeCell(DataGridView grid, HashSet<string> badgeCols, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        string colName = grid.Columns[e.ColumnIndex].Name;
        string headerText = grid.Columns[e.ColumnIndex].DataPropertyName;
        if (!badgeCols.Contains(colName) && !badgeCols.Contains(headerText))
            return;

        string? text = e.FormattedValue?.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return;

        e.PaintBackground(e.ClipBounds, true);
        e.Graphics!.SmoothingMode = SmoothingMode.AntiAlias;

        var (backColor, foreColor) = AppTheme.GetStatusColors(text);
        var measured = TextRenderer.MeasureText(e.Graphics, text, AppTheme.CaptionBoldFont);

        int pillW = Math.Min(e.CellBounds.Width - 14, measured.Width + 20);
        int pillH = 26;
        int pillX = e.CellBounds.X + 8;
        int pillY = e.CellBounds.Y + (e.CellBounds.Height - pillH) / 2;

        var pillRect = new Rectangle(pillX, pillY, Math.Max(40, pillW), pillH);
        using var path = AppTheme.CreateRoundedRect(pillRect, pillH / 2);
        using var brush = new SolidBrush(backColor);
        e.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            e.Graphics,
            text,
            AppTheme.CaptionBoldFont,
            pillRect,
            foreColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        e.Handled = true;
    }
}
