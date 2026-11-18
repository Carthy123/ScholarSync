using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;

namespace ScholarSync.Pages.Communications;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class TwilioStatusModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly IConfiguration _configuration;

    public TwilioStatusModel(SchoolDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var form = await Request.ReadFormAsync();
        var authToken = _configuration["Twilio:AuthToken"];
        var signature = Request.Headers["X-Twilio-Signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authToken) || !IsValidSignature(form, signature, authToken))
            return Unauthorized();

        var messageId = form["MessageSid"].FirstOrDefault() ?? form["SmsSid"].FirstOrDefault();
        var rawStatus = form["MessageStatus"].FirstOrDefault() ?? form["SmsStatus"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(rawStatus))
            return BadRequest();

        var communication = await _context.GuardianCommunications.FirstOrDefaultAsync(item => item.ProviderMessageId == messageId);
        if (communication == null)
            return new OkResult();

        communication.Status = rawStatus.ToLowerInvariant() switch
        {
            "delivered" => "Delivered",
            "read" => "Read",
            "failed" or "undelivered" => "Failed",
            "sent" => "Sent",
            "queued" or "accepted" or "scheduled" or "sending" => "Pending",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(rawStatus.ToLowerInvariant())
        };
        var errorCode = form["ErrorCode"].FirstOrDefault();
        var errorMessage = form["ChannelStatusMessage"].FirstOrDefault();
        communication.StatusDetails = string.IsNullOrWhiteSpace(errorCode) && string.IsNullOrWhiteSpace(errorMessage)
            ? $"Twilio status callback: {rawStatus}."
            : $"Twilio status callback: {rawStatus}. Error {errorCode}: {errorMessage}";
        if (communication.Status is "Delivered" or "Read")
            communication.SentAt ??= DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return new OkResult();
    }

    private bool IsValidSignature(IFormCollection form, string? signature, string authToken)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        var callbackUrl = _configuration["Twilio:StatusCallbackUrl"];
        var url = string.IsNullOrWhiteSpace(callbackUrl)
            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}"
            : callbackUrl;
        var data = new StringBuilder(url);
        foreach (var key in form.Keys.OrderBy(key => key, StringComparer.Ordinal))
            data.Append(key).Append(form[key].ToString());
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(authToken));
        var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data.ToString())));
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }
}
