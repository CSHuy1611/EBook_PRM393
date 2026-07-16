using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MathIBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MathIBook.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpHost = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var smtpUser = _configuration["Smtp:Username"];
        var smtpPass = _configuration["Smtp:Password"];
        var isDevelopment = _configuration["ASPNETCORE_ENVIRONMENT"] == "Development"
            || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass) ||
            smtpUser.Contains("your_email") || smtpPass.Contains("your_app_password") ||
            smtpUser == "dia_chi_gmail_cua_ban@gmail.com" || smtpPass == "ma_16_ky_tu_google_vua_cap")
        {
            _logger.LogWarning("SMTP credentials are not configured or are placeholders. Mocking email send in development.");
            _logger.LogWarning($"[MOCK EMAIL SENT] To: {toEmail}, Subject: {subject}, Body: {body}");
            return;
        }

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
                Timeout = 5000 // 5-second timeout to prevent API request hanging
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser, "Math-IBook Support"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            if (isDevelopment)
            {
                _logger.LogWarning("Fallback: SMTP failed but continuing because we are in Development mode. [MOCK EMAIL SENT] To: {Email}, Subject: {Subject}", toEmail, subject);
                return;
            }
            throw;
        }
    }
}
