using System.Text;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace WhatsAppDev.Services;

public class PdfTextExtractionService : ITextExtractionService
{
    private readonly ILogger<PdfTextExtractionService> _logger;

    public PdfTextExtractionService(ILogger<PdfTextExtractionService> logger)
    {
        _logger = logger;
    }

    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // PdfPig extraction is synchronous; wrap in Task.Run to keep the worker responsive.
        return Task.Run(() =>
        {
            var sb = new StringBuilder();
            using var document = PdfDocument.Open(filePath);

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text.Trim());
                    sb.AppendLine();
                }
            }

            var result = sb.ToString().Trim();
            _logger.LogInformation("Extracted {Length} chars from PDF {FilePath}", result.Length, filePath);
            return result;
        }, cancellationToken);
    }
}

