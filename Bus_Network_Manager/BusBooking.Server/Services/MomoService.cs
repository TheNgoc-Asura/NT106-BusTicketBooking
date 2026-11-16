using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusBooking.Shared.DTOs;

namespace BusBooking.Server.Services;

public interface IMomoService
{
    Task<(bool Success, PaymentInfoDto? PaymentInfo, string ErrorMessage)> CreatePaymentAsync(
        string orderId,
        long amount,
        string orderInfo,
        string redirectUrl,
        string ipnUrl,
        CancellationToken ct = default);

    bool VerifyIpnSignature(MomoIpnRequest ipn);
    string ComputeIpnSignature(MomoIpnRequest ipn);
    string PartnerCode { get; }
}

public class MomoService : IMomoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MomoService> _logger;

    public MomoService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MomoService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public string PartnerCode => _configuration["Momo:PartnerCode"] ?? "MOMO_PARTNER_CODE";
    private string AccessKey => _configuration["Momo:AccessKey"] ?? "MOMO_ACCESS_KEY";
    private string SecretKey => _configuration["Momo:SecretKey"] ?? "MOMO_SECRET_KEY_DEV_2026";
    private string Endpoint => _configuration["Momo:Endpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
    private bool CallLiveApi => _configuration.GetValue("Momo:CallLiveApi", false);

    public async Task<(bool Success, PaymentInfoDto? PaymentInfo, string ErrorMessage)> CreatePaymentAsync(
        string orderId,
        long amount,
        string orderInfo,
        string redirectUrl,
        string ipnUrl,
        CancellationToken ct = default)
    {
        string requestId = $"RQ{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{RandomNumberGenerator.GetInt32(100, 999)}";
        string requestType = "captureWallet";
        string extraData = string.Empty;

        // Chuỗi ký chuẩn MoMo v2 (các khóa sắp xếp theo thứ tự a-z)
        string rawHash =
            $"accessKey={AccessKey}" +
            $"&amount={amount}" +
            $"&extraData={extraData}" +
            $"&ipnUrl={ipnUrl}" +
            $"&orderId={orderId}" +
            $"&orderInfo={orderInfo}" +
            $"&partnerCode={PartnerCode}" +
            $"&redirectUrl={redirectUrl}" +
            $"&requestId={requestId}" +
            $"&requestType={requestType}";

        string signature = ComputeHmacSha256(rawHash, SecretKey);

        if (!CallLiveApi)
        {
            string mockPayUrl = $"https://test-payment.momo.vn/v2/gateway/pay?t={signature[..16]}&orderId={Uri.EscapeDataString(orderId)}";
            string qrPayload = $"2|99|{PartnerCode}|MoMo|{orderId}|0|0|{amount}|{orderInfo}";
            string mockQrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={Uri.EscapeDataString(qrPayload)}";

            _logger.LogInformation("MoMo local sandbox mode: tạo giao dịch OrderId={OrderId}, Amount={Amount}, Signature={Signature}", orderId, amount, signature);

            return (true, new PaymentInfoDto
            {
                Provider = "MoMo",
                OrderId = orderId,
                PayUrl = mockPayUrl,
                QrCodeUrl = mockQrUrl
            }, string.Empty);
        }

        try
        {
            var payload = new
            {
                partnerCode = PartnerCode,
                partnerName = "BusBooking",
                storeId = "BusBookingStore",
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl,
                ipnUrl,
                lang = "vi",
                extraData,
                requestType,
                signature
            };

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            using var response = await client.PostAsJsonAsync(Endpoint, payload, ct);
            string body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MoMo HTTP error {StatusCode}: {Body}", response.StatusCode, body);
                return (false, null, "Không tạo được giao dịch với MoMo");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            int resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
            if (resultCode != 0)
            {
                string msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "Lỗi MoMo" : "Lỗi MoMo";
                _logger.LogWarning("MoMo từ chối tạo giao dịch ({ResultCode}): {Message}", resultCode, msg);
                return (false, null, $"Không tạo được giao dịch với MoMo: {msg}");
            }

            string payUrl = root.TryGetProperty("payUrl", out var pUrl) ? pUrl.GetString() ?? string.Empty : string.Empty;
            string qrCodeUrl = root.TryGetProperty("qrCodeUrl", out var qUrl) ? qUrl.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(qrCodeUrl) && !string.IsNullOrWhiteSpace(payUrl))
            {
                qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={Uri.EscapeDataString(payUrl)}";
            }

            return (true, new PaymentInfoDto
            {
                Provider = "MoMo",
                OrderId = orderId,
                PayUrl = payUrl,
                QrCodeUrl = qrCodeUrl
            }, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi kết nối cổng thanh toán MoMo cho OrderId={OrderId}", orderId);
            return (false, null, "Không tạo được giao dịch với MoMo");
        }
    }

    public string ComputeIpnSignature(MomoIpnRequest ipn)
    {
        string rawHash =
            $"accessKey={AccessKey}" +
            $"&amount={ipn.Amount}" +
            $"&extraData={ipn.ExtraData}" +
            $"&message={ipn.Message}" +
            $"&orderId={ipn.OrderId}" +
            $"&orderInfo={ipn.OrderInfo}" +
            $"&orderType={ipn.OrderType}" +
            $"&partnerCode={ipn.PartnerCode}" +
            $"&payType={ipn.PayType}" +
            $"&requestId={ipn.RequestId}" +
            $"&responseTime={ipn.ResponseTime}" +
            $"&resultCode={ipn.ResultCode}" +
            $"&transId={ipn.TransId}";

        return ComputeHmacSha256(rawHash, SecretKey);
    }

    public bool VerifyIpnSignature(MomoIpnRequest ipn)
    {
        if (string.IsNullOrWhiteSpace(ipn.Signature))
            return false;

        if (!string.Equals(ipn.PartnerCode, PartnerCode, StringComparison.Ordinal))
            return false;

        string expected = ComputeIpnSignature(ipn);
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected.ToLowerInvariant());
        byte[] actualBytes = Encoding.UTF8.GetBytes(ipn.Signature.Trim().ToLowerInvariant());

        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static string ComputeHmacSha256(string data, string secretKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
