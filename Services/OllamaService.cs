using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Config;

namespace WhatsAppDev.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaService> _logger;

    private const string DefaultSystemPrompt =
        "You are a logistics assistant for a platform called Octology. " +
        "Help users with shipment tracking, freight quotes, and import documentation. " +
        "Be concise and professional.";

    public OllamaService(
        IHttpClientFactory httpClientFactory,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Ollama");
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string userMessage, string? systemPromptOverride = null, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{_settings.Endpoint.TrimEnd('/')}";
        var systemPrompt = string.IsNullOrWhiteSpace(systemPromptOverride) ? DefaultSystemPrompt : systemPromptOverride;

        var requestBody = new
        {
            model = _settings.Model,
            prompt = $"{systemPrompt}\n\nUser: {userMessage}\nAssistant:",
            stream = false
        };

        _logger.LogInformation("Sending prompt to Ollama model {Model}", _settings.Model);

        using var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken);

        if (content == null || string.IsNullOrWhiteSpace(content.Response))
        {
            _logger.LogWarning("Empty response from Ollama");
            return "I'm sorry, I could not generate a response at this time.";
        }

        _logger.LogInformation("Received response from Ollama");
        return content.Response.Trim();
    }

    private sealed class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}

