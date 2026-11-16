using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusTicket.Core.DTOs;

namespace BusTicket.Client.Services;

public class RealtimeClient
{
    private static RealtimeClient? _instance;
    public static RealtimeClient Instance => _instance ??= new RealtimeClient();

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private bool _isConnected = false;

    public event Action<SupportMessageDto>? OnMessageReceived;
    public event Action<int, string, string>? OnPaymentConfirmed; // ticketId, ticketCode, status
    public event Action<NotificationDto>? OnNotificationReceived;
    public event Action<bool>? OnConnectionChanged;

    public bool IsConnected => _isConnected && _webSocket?.State == WebSocketState.Open;

    public async Task StartAsync()
    {
        if (IsConnected) return;

        try
        {
            _cts = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            string wsBase = SessionManager.BaseUrl.Replace("http://", "ws://").Replace("https://", "wss://").TrimEnd('/');
            string url = $"{wsBase}/hubs/support?access_token={Uri.EscapeDataString(SessionManager.Token ?? "")}";

            await _webSocket.ConnectAsync(new Uri(url), _cts.Token);
            _isConnected = true;
            OnConnectionChanged?.Invoke(true);

            // SignalR Handshake
            string handshake = "{\"protocol\":\"json\",\"version\":1}\u001e";
            byte[] handshakeBytes = Encoding.UTF8.GetBytes(handshake);
            await _webSocket.SendAsync(new ArraySegment<byte>(handshakeBytes), WebSocketMessageType.Text, true, _cts.Token);

            _ = ReceiveLoopAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _isConnected = false;
            OnConnectionChanged?.Invoke(false);
            Console.WriteLine("WebSocket connection failed: " + ex.Message);
        }
    }

    public async Task StopAsync()
    {
        try
        {
            _cts?.Cancel();
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
        catch { }
        finally
        {
            _isConnected = false;
            OnConnectionChanged?.Invoke(false);
        }
    }

    public async Task SendMessageAsync(int ticketId, string content)
    {
        if (!IsConnected || _webSocket == null) return;

        try
        {
            var invocation = new
            {
                type = 1,
                target = "SendMessage",
                arguments = new object[] { ticketId, content }
            };

            string json = JsonSerializer.Serialize(invocation) + "\u001e";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi gửi tin nhắn qua WebSocket: " + ex.Message);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        byte[] buffer = new byte[8192];
        var ms = new MemoryStream();

        try
        {
            while (!ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                ms.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                {
                    string raw = Encoding.UTF8.GetString(ms.ToArray());
                    ms.SetLength(0);

                    // SignalR protocol separates messages with \u001e
                    var messages = raw.Split('\u001e', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var m in messages)
                    {
                        ProcessSignalRMessage(m.Trim());
                    }
                }
            }
        }
        catch (Exception)
        {
            // Disconnected
        }
        finally
        {
            _isConnected = false;
            OnConnectionChanged?.Invoke(false);
        }
    }

    private void ProcessSignalRMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeProp) && typeProp.GetInt32() == 1) // Invocation
            {
                string target = root.GetProperty("target").GetString() ?? "";
                var args = root.GetProperty("arguments");

                if (target == "ReceiveMessage" && args.GetArrayLength() > 0)
                {
                    var msgJson = args[0].GetRawText();
                    var msg = JsonSerializer.Deserialize<SupportMessageDto>(msgJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (msg != null) OnMessageReceived?.Invoke(msg);
                }
                else if (target == "PaymentConfirmed" && args.GetArrayLength() > 0)
                {
                    var arg = args[0];
                    int tId = arg.GetProperty("ticketId").GetInt32();
                    string code = arg.GetProperty("ticketCode").GetString() ?? "";
                    string st = arg.GetProperty("status").GetString() ?? "";
                    OnPaymentConfirmed?.Invoke(tId, code, st);
                }
                else if (target == "ReceiveNotification" && args.GetArrayLength() > 0)
                {
                    var notifJson = args[0].GetRawText();
                    var notif = JsonSerializer.Deserialize<NotificationDto>(notifJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (notif != null) OnNotificationReceived?.Invoke(notif);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Parse SignalR message error: " + ex.Message);
        }
    }
}
