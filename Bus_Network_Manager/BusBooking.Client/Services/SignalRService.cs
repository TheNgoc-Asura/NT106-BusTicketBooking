using BusBooking.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace BusBooking.Client.Services;

/// <summary>
/// Quản lý kết nối SignalR (/hubs/support) lắng nghe ReceiveMessage, PaymentConfirmed và ReceiveNotification.
/// </summary>
public sealed class SignalRService
{
    public static SignalRService Instance { get; } = new();

    private HubConnection? _connection;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public event Action<bool>? ConnectionStateChanged;
    public event Action<SupportMessageDto>? MessageReceived;
    public event Action<PaymentConfirmedEventDto>? PaymentConfirmed;
    public event Action<NotificationDto>? NotificationReceived;

    private SignalRService()
    {
    }

    public async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionManager.AccessToken))
            return;

        await StopAsync();

        string hubUrl = $"{ApiService.BaseUrl.TrimEnd('/')}/hubs/support?access_token={Uri.EscapeDataString(SessionManager.AccessToken)}";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<SupportMessageDto>("ReceiveMessage", msg => MessageReceived?.Invoke(msg));
        _connection.On<PaymentConfirmedEventDto>("PaymentConfirmed", evt => PaymentConfirmed?.Invoke(evt));
        _connection.On<NotificationDto>("ReceiveNotification", notif => NotificationReceived?.Invoke(notif));

        _connection.Reconnecting += _ =>
        {
            ConnectionStateChanged?.Invoke(false);
            return Task.CompletedTask;
        };
        _connection.Reconnected += _ =>
        {
            ConnectionStateChanged?.Invoke(true);
            return Task.CompletedTask;
        };
        _connection.Closed += _ =>
        {
            ConnectionStateChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync();
            ConnectionStateChanged?.Invoke(true);
        }
        catch
        {
            ConnectionStateChanged?.Invoke(false);
        }
    }

    public async Task StopAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
            }
            catch
            {
                // Bỏ qua lỗi khi đóng kết nối cũ
            }
            finally
            {
                _connection = null;
                ConnectionStateChanged?.Invoke(false);
            }
        }
    }

    public async Task<bool> SendMessageAsync(int ticketId, string content)
    {
        if (_connection == null || _connection.State != HubConnectionState.Connected)
            return false;

        await _connection.InvokeAsync("SendMessage", ticketId, content);
        return true;
    }

    /// <summary>
    /// Thực thi an toàn sự kiện SignalR trên UI thread của WinForms control.
    /// </summary>
    public static void SafeInvoke(Control? control, Action action)
    {
        if (control == null || control.IsDisposed || !control.IsHandleCreated)
            return;

        try
        {
            if (control.InvokeRequired)
            {
                control.BeginInvoke(() =>
                {
                    if (!control.IsDisposed)
                        action();
                });
            }
            else
            {
                action();
            }
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
