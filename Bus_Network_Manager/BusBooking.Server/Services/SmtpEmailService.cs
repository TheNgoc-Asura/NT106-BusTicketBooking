using System.Net;
using System.Net.Mail;
using BusBooking.Shared.Enums;

namespace BusBooking.Server.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendOtpEmailAsync(string toEmail, string code, string purpose, CancellationToken cancellationToken = default)
    {
        string subject = purpose == OtpPurposes.Register
            ? "[BusBooking] Mã xác thực đăng ký tài khoản"
            : "[BusBooking] Mã xác thực khôi phục mật khẩu";

        string actionText = purpose == OtpPurposes.Register
            ? "hoàn tất đăng ký tài khoản"
            : "đặt lại mật khẩu tài khoản";

        string body = $"""
            Xin chào Quý khách,

            Mã xác thực (OTP) để {actionText} của bạn là: {code}

            Mã có hiệu lực trong vòng 5 phút (300 giây). Vui lòng không chia sẻ mã này cho bất kỳ ai.

            Trân trọng,
            Hệ thống Đặt vé xe trực tuyến - BusBooking (NT106.R12 - Nhóm 07)
            """;

        _logger.LogInformation("Chuẩn bị gửi OTP mục đích {Purpose} tới {Email}", purpose, toEmail);
        return await SendEmailInternalAsync(toEmail, subject, body, isHtml: false, devOtpCode: code, cancellationToken);
    }

    public Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
        => SendEmailInternalAsync(toEmail, subject, body, isHtml, devOtpCode: null, cancellationToken);

    private async Task<bool> SendEmailInternalAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml,
        string? devOtpCode,
        CancellationToken cancellationToken)
    {
        string host = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
        int port = _configuration.GetValue("Smtp:Port", 587);
        string? user = _configuration["Smtp:User"];
        string? pass = _configuration["Smtp:Pass"];
        string fromName = _configuration["Smtp:FromName"] ?? "BusBooking System";
        bool enableFallbackLog = _configuration.GetValue("Smtp:EnableFallbackLog", true);

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            if (enableFallbackLog)
            {
                _logger.LogWarning(
                    "Chưa cấu hình Smtp:User/Smtp:Pass trong appsettings.json. Bật chế độ fallback log Email tới {Email}. {OtpInfo}",
                    toEmail,
                    devOtpCode != null ? $"OTP={devOtpCode}" : string.Empty);

                Console.WriteLine("=======================================================");
                Console.WriteLine($"[DEV EMAIL FALLBACK] To: {toEmail}");
                Console.WriteLine($"Subject: {subject}");
                if (devOtpCode != null)
                    Console.WriteLine($"OTP CODE: >>> {devOtpCode} <<<");
                Console.WriteLine("=======================================================");
                return true;
            }

            _logger.LogError("Chưa cấu hình Smtp:User hoặc Smtp:Pass và EnableFallbackLog=false.");
            return false;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user.Trim(), pass),
                EnableSsl = true,
                Timeout = 15000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(user.Trim(), fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            message.To.Add(toEmail.Trim());

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Đã gửi email thành công tới {Email} qua SMTP {Host}:{Port}", toEmail, host, port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gửi email qua máy chủ SMTP {Host}:{Port} tới {Email}", host, port, toEmail);
            return false;
        }
    }
}
