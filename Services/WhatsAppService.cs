using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Config;

namespace WhatsAppDev.Services;

public class WhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("WhatsAppGraphApi");
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendTextMessageAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccessToken) || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
        {
            _logger.LogWarning("WhatsApp configuration is missing. Skipping sending message.");
            return;
        }

        var requestUri = $"https://graph.facebook.com/v19.0/{_settings.PhoneNumberId}/messages";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhoneNumber,
            type = "text",
            text = new
            {
                body
            }
        };

        request.Content = JsonContent.Create(payload);

        _logger.LogInformation("Sending WhatsApp message to {PhoneNumber}", toPhoneNumber);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("WhatsApp API response: {StatusCode} - {Body}", response.StatusCode, responseBody);

        response.EnsureSuccessStatusCode();
    }
}

