using Microsoft.AspNetCore.Mvc;
using WhatsAppDev.Models;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KnowledgeController : ControllerBase
{
    private readonly DocumentService _documentService;
    private readonly KnowledgeIngestionJobService _jobService;

    public KnowledgeController(DocumentService documentService, KnowledgeIngestionJobService jobService)
    {
        _documentService = documentService;
        _jobService = jobService;
    }

    public sealed class KnowledgeRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public sealed class KnowledgeUploadJobResponse
    {
        public Guid JobId { get; set; }
    }

    public sealed class KnowledgeIngestionJobDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public KnowledgeIngestionJobStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KnowledgeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Title and content are required.");
        }

        await _documentService.IngestDocumentAsync(request.Title, request.Content, cancellationToken);
        return Accepted();
    }

    /// <summary>
    /// Upload a PDF/DOCX file and enqueue a background ingestion job.
    /// For now extraction is local text-based; OCR can be added later.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<KnowledgeUploadJobResponse>> Upload(
        [FromForm] string title,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
            return BadRequest("Title is required.");

        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".docx")
            return BadRequest("Only .pdf and .docx are supported for now.");

        var uploadsRoot = Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var safeFileName = Path.GetFileName(file.FileName);
        var tempFileName = $"{Guid.NewGuid()}_{safeFileName}";
        var tempPath = Path.Combine(uploadsRoot, tempFileName);

        await using (var stream = System.IO.File.Create(tempPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var job = new KnowledgeIngestionJob
        {
            Title = title.Trim(),
            FilePath = tempPath,
            OriginalFileName = safeFileName,
            Status = KnowledgeIngestionJobStatus.Queued
        };

        await _jobService.CreateJobAsync(job, cancellationToken);

        return Accepted(new KnowledgeUploadJobResponse { JobId = job.Id });
    }

    [HttpGet("ingestion-jobs/{id:guid}")]
    public async Task<ActionResult<KnowledgeIngestionJobDto>> GetIngestionJob(Guid id, CancellationToken cancellationToken)
    {
        var job = await _jobService.GetJobAsync(id, cancellationToken);

        if (job == null)
            return NotFound();

        return Ok(new KnowledgeIngestionJobDto
        {
            Id = job.Id,
            Title = job.Title,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            FinishedAt = job.FinishedAt,
            ErrorMessage = job.ErrorMessage
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<KnowledgeDocument>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _documentService.GetAllAsync(cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _documentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

