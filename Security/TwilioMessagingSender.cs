using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ScholarSync.Security;

public class TwilioMessagingSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioMessagingSender> _logger;

    public TwilioMessagingSender(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TwilioMessagingSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DeliveryResult> SendAsync(string channel, string recipientPhone, string message, string? statusCallbackUrl = null)
    {
        var section = _configuration.GetSection("Twilio");
        var accountSid = section["AccountSid"];
        var authToken = section["AuthToken"];
        var from = string.Equals(channel, "WhatsApp", StringComparison.OrdinalIgnoreCase) ? section["WhatsAppFrom"] : section["SmsFrom"];
        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(from))
            return new DeliveryResult(false, null, "Twilio is not configured yet.");

        var destination = NormalizePhone(recipientPhone);
        if (destination == null)
            return new DeliveryResult(false, null, "The guardian phone number must use international format, for example +233241234567.");

        var prefix = string.Equals(channel, "WhatsApp", StringComparison.OrdinalIgnoreCase) ? "whatsapp:" : string.Empty;
        var form = new Dictionary<string, string>
        {
            ["To"] = prefix + destination,
            ["From"] = prefix + from,
            ["Body"] = message
        };
        if (!string.IsNullOrWhiteSpace(statusCallbackUrl))
            form["StatusCallback"] = statusCallbackUrl;
        var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(accountSid)}/Messages.json";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        try
        {
            var response = await _httpClientFactory.CreateClient().SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Twilio rejected a {Channel} message: {Response}", channel, responseBody);
                return new DeliveryResult(false, null, $"Twilio rejected the message ({(int)response.StatusCode}).");
            }

            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            var messageId = root.TryGetProperty("sid", out var sid) ? sid.GetString() : null;
            var status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : "accepted";
            return new DeliveryResult(true, messageId, $"Twilio accepted the message with status '{status}'.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Twilio {Channel} delivery failed.", channel);
            return new DeliveryResult(false, null, "The Twilio service could not be reached.");
        }
    }

    private static string? NormalizePhone(string value)
    {
        var phone = new string(value.Where(character => char.IsDigit(character) || character == '+').ToArray());
        return phone.StartsWith('+') && phone.Length >= 10 ? phone : null;
    }

    public sealed record DeliveryResult(bool Sent, string? ProviderMessageId, string Details);
}
