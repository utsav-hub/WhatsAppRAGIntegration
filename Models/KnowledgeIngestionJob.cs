namespace WhatsAppDev.Models;

public enum KnowledgeIngestionJobStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

public class KnowledgeIngestionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Friendly title that will become KnowledgeDocument.Title
    public string Title { get; set; } = string.Empty;

    // Stored temporarily on disk until the worker extracts and ingests it
    public string FilePath { get; set; } = string.Empty;

    // Original client filename (for logging / debugging)
    public string OriginalFileName { get; set; } = string.Empty;

    public KnowledgeIngestionJobStatus Status { get; set; } = KnowledgeIngestionJobStatus.Queued;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public string? ErrorMessage { get; set; }
}

