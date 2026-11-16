using BusBooking.Client.Controls;
using BusBooking.Client.Theme;

namespace BusBooking.Client.Helpers;

public static class UiHelper
{
    public static Label CreateFieldLabel(string text, int x, int y, Color? backColor = null)
    {
        return new Label
        {
            Text = text,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.TextSecondary,
            BackColor = backColor ?? AppTheme.CardSurface,
            AutoSize = true,
            Location = new Point(x, y)
        };
    }

    public static ComboBox CreateStyledComboBox(int x, int y, int width, ComboBoxStyle style = ComboBoxStyle.DropDownList)
    {
        return new ComboBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 34),
            DropDownStyle = style,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.BodyFont,
            BackColor = Color.White,
            ForeColor = AppTheme.TextPrimary
        };
    }

    /// <summary>
    /// Bọc lời gọi API bất đồng bộ kèm hiệu ứng Loading trên nút và bắt lỗi an toàn.
    /// </summary>
    public static async Task RunButtonActionAsync(
        ModernButton? button,
        string loadingText,
        Func<Task> action,
        Form? owner = null)
    {
        try
        {
            button?.SetLoading(true, loadingText);
            if (owner != null) owner.Cursor = Cursors.WaitCursor;
            await action();
        }
        catch (Exception ex)
        {
            ShowError($"Có lỗi xảy ra: {ex.Message}", owner);
        }
        finally
        {
            button?.SetLoading(false);
            if (owner != null && !owner.IsDisposed) owner.Cursor = Cursors.Default;
        }
    }

    public static void ShowError(string message, IWin32Window? owner = null)
        => MessageBox.Show(owner, message, "Thông báo lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void ShowInfo(string message, IWin32Window? owner = null)
        => MessageBox.Show(owner, message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
