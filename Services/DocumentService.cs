using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.Models;

namespace WhatsAppDev.Services;

public class DocumentService
{
    private readonly AppDbContext _dbContext;
    private readonly TextChunkingService _chunkingService;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        AppDbContext dbContext,
        TextChunkingService chunkingService,
        EmbeddingService embeddingService,
        ILogger<DocumentService> logger)
    {
        _dbContext = dbContext;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task IngestDocumentAsync(string title, string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.KnowledgeDocuments.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var chunks = _chunkingService.ChunkText(content);
        _logger.LogInformation("Ingesting document '{Title}' into {ChunkCount} chunks", document.Title, chunks.Count);

        foreach (var chunkText in chunks)
        {
            var chunk = new KnowledgeChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ChunkText = chunkText,
                Embedding = await _embeddingService.GenerateEmbeddingAsync(chunkText, cancellationToken),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.KnowledgeChunks.Add(chunk);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Finished ingesting document '{Title}'", document.Title);
    }

    public async Task<List<KnowledgeDocument>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.KnowledgeDocuments
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doc = await _dbContext.KnowledgeDocuments.FindAsync(new object[] { id }, cancellationToken);
        if (doc == null)
        {
            return;
        }

        var chunks = _dbContext.KnowledgeChunks.Where(c => c.DocumentId == id);
        _dbContext.KnowledgeChunks.RemoveRange(chunks);
        _dbContext.KnowledgeDocuments.Remove(doc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted document '{Title}' and associated chunks", doc.Title);
    }
}

