using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.Models;

namespace WhatsAppDev.Services;

public class KnowledgeIngestionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KnowledgeIngestionWorker> _logger;

    // How often to poll for queued jobs
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    public KnowledgeIngestionWorker(IServiceProvider serviceProvider, ILogger<KnowledgeIngestionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOneJobAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing knowledge ingestion job");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessOneJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var documentService = scope.ServiceProvider.GetRequiredService<DocumentService>();
        var extractor = scope.ServiceProvider.GetRequiredService<ITextExtractionService>();

        var job = await db.KnowledgeIngestionJobs
            .OrderBy(x => x.CreatedAt)
            .Where(x => x.Status == KnowledgeIngestionJobStatus.Queued)
            .FirstOrDefaultAsync(cancellationToken);

        if (job == null)
        {
            return;
        }

        job.Status = KnowledgeIngestionJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        job.ErrorMessage = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            if (string.IsNullOrWhiteSpace(job.FilePath) || !File.Exists(job.FilePath))
            {
                throw new FileNotFoundException("Uploaded file not found on disk.", job.FilePath);
            }

            _logger.LogInformation("Ingestion job {JobId} extracting text", job.Id);
            var extractedText = await extractor.ExtractTextAsync(job.FilePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new InvalidOperationException("Extracted text is empty; cannot ingest knowledge.");
            }

            // Basic normalization to reduce embedding noise.
            extractedText = extractedText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\n\n\n", "\n\n")
                .Trim();

            _logger.LogInformation("Ingestion job {JobId} calling DocumentService.IngestDocumentAsync", job.Id);
            await documentService.IngestDocumentAsync(job.Title, extractedText, cancellationToken);

            job.Status = KnowledgeIngestionJobStatus.Succeeded;
            job.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Ingestion job {JobId} succeeded", job.Id);
        }
        catch (Exception ex)
        {
            job.Status = KnowledgeIngestionJobStatus.Failed;
            job.FinishedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.ToString();
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Ingestion job {JobId} failed", job.Id);
        }
        finally
        {
            // Cleanup temp file
            try
            {
                if (!string.IsNullOrWhiteSpace(job.FilePath) && File.Exists(job.FilePath))
                {
                    File.Delete(job.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temp file for job {JobId}", job.Id);
            }
        }
    }
}

