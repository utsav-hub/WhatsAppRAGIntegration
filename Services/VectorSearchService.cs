using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using WhatsAppDev.Data;

namespace WhatsAppDev.Services;

public class VectorSearchService
{
    private readonly AppDbContext _dbContext;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<VectorSearchService> _logger;

    public VectorSearchService(
        AppDbContext dbContext,
        EmbeddingService embeddingService,
        ILogger<VectorSearchService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<List<string>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<string>();
        }

        const int topK = 3;

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

        var chunks = await _dbContext.KnowledgeChunks
            .Where(c => c.Embedding != null)
            .OrderBy(c => c.Embedding!.CosineDistance(queryEmbedding))
            .Take(topK)
            .Select(c => c.ChunkText)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Vector search found {Count} chunks for query '{Query}'", chunks.Count, query);
        return chunks;
    }
}

