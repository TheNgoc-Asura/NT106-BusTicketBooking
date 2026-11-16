namespace BusBooking.Server.Services;

public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(string toEmail, string code, string purpose, CancellationToken cancellationToken = default);
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
}
