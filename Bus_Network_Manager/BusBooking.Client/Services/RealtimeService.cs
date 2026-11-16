namespace BusBooking.Client.Services;

/// <summary>
/// Lớp tương thích chuyển tiếp sang SignalRService.
/// </summary>
public static class RealtimeService
{
    public static SignalRService Instance => SignalRService.Instance;

    public static void SafeInvoke(Control? control, Action action)
        => SignalRService.SafeInvoke(control, action);
}
