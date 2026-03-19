using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.Models;

namespace WhatsAppDev.Services;

public class KnowledgeIngestionJobService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<KnowledgeIngestionJobService> _logger;

    public KnowledgeIngestionJobService(AppDbContext dbContext, ILogger<KnowledgeIngestionJobService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> CreateJobAsync(KnowledgeIngestionJob job, CancellationToken cancellationToken = default)
    {
        _dbContext.KnowledgeIngestionJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created knowledge ingestion job {JobId} with title '{Title}'", job.Id, job.Title);
        return job.Id;
    }

    public async Task<KnowledgeIngestionJob?> GetJobAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.KnowledgeIngestionJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}

