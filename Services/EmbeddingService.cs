using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using WhatsAppDev.Config;

namespace WhatsAppDev.Services;

public class EmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly OllamaSettings _settings;
    private const string DefaultEmbeddingModel = "nomic-embed-text";

    public EmbeddingService(
        IHttpClientFactory httpClientFactory,
        IOptions<OllamaSettings> settings,
        ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Ollama");
        _logger = logger;
        _settings = settings.Value;
    }

    private sealed class OllamaEmbeddingResponse
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    public async Task<Vector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Vector(Array.Empty<float>());
        }

        var endpoint = $"{_settings.Endpoint.TrimEnd('/')}/embeddings";

        var body = new
        {
            model = DefaultEmbeddingModel,
            prompt = text
        };

        _logger.LogInformation("Requesting embedding from Ollama model {Model}", DefaultEmbeddingModel);

        using var response = await _httpClient.PostAsJsonAsync(endpoint, body, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken);
        if (payload == null || payload.Embedding.Length == 0)
        {
            _logger.LogWarning("Received empty embedding from Ollama for text fragment of length {Length}", text.Length);
            return new Vector(Array.Empty<float>());
        }

        return new Vector(payload.Embedding);
    }
}

