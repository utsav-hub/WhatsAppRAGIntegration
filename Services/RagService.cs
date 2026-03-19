using System.Text;
using Microsoft.Extensions.Logging;

namespace WhatsAppDev.Services;

public class RagService
{
    private readonly VectorSearchService _vectorSearchService;
    private readonly OllamaService _ollamaService;
    private readonly ILogger<RagService> _logger;

    public RagService(
        VectorSearchService vectorSearchService,
        OllamaService ollamaService,
        ILogger<RagService> logger)
    {
        _vectorSearchService = vectorSearchService;
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RAG: generating response for message: {Message}", userMessage);

        var chunks = await _vectorSearchService.SearchAsync(userMessage, cancellationToken);

        string? promptOverride;

        if (chunks.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a logistics assistant.");
            sb.AppendLine("Use the knowledge below to answer.");
            sb.AppendLine();
            sb.AppendLine("Knowledge:");

            foreach (var chunk in chunks)
            {
                sb.AppendLine(chunk);
                sb.AppendLine();
            }

            sb.AppendLine("Question:");
            sb.AppendLine(userMessage);
            sb.AppendLine();
            sb.AppendLine("Answer clearly.");

            promptOverride = sb.ToString();

            _logger.LogInformation("RAG: using {ChunkCount} retrieved chunks", chunks.Count);
            _logger.LogDebug("RAG: final prompt:\n{Prompt}", promptOverride);
        }
        else
        {
            _logger.LogInformation("RAG: no chunks retrieved; falling back to base system prompt");
            promptOverride = null;
        }

        var response = await _ollamaService.GenerateResponseAsync(userMessage, promptOverride, cancellationToken);

        _logger.LogInformation("RAG: LLM response generated");
        _logger.LogDebug("RAG: LLM response: {Response}", response);

        return response;
    }
}

