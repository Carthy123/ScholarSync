using System.Net;
using System.Net.Mail;

namespace ScholarSync.Security;

public class EmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetAsync(string recipient, string resetLink)
    {
        var section = _configuration.GetSection("Smtp");
        var host = section["Host"];
        var fromAddress = section["FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogError("SMTP is not configured. Password reset email was not sent.");
            return false;
        }

        using var message = new MailMessage(fromAddress, recipient)
        {
            Subject = "ScholarSync password reset",
            Body = $"A password reset was requested for your ScholarSync account.\n\nUse this link within 30 minutes:\n{resetLink}\n\nIf you did not request this, you can ignore this email.",
            IsBodyHtml = false
        };

        using var client = new SmtpClient(host, int.TryParse(section["Port"], out var port) ? port : 587)
        {
            EnableSsl = !string.Equals(section["EnableSsl"], "false", StringComparison.OrdinalIgnoreCase)
        };
        var username = section["Username"];
        var password = section["Password"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        await client.SendMailAsync(message);
        return true;
    }
}
