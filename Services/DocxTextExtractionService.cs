using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace WhatsAppDev.Services;

public class DocxTextExtractionService : ITextExtractionService
{
    private readonly ILogger<DocxTextExtractionService> _logger;

    public DocxTextExtractionService(ILogger<DocxTextExtractionService> logger)
    {
        _logger = logger;
    }

    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // OpenXML extraction is synchronous; wrap in Task.Run for worker responsiveness.
        return Task.Run(() =>
        {
            var sb = new StringBuilder();

            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                return string.Empty;
            }

            foreach (var paragraph in body.Elements<Paragraph>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text.Trim());
                    sb.AppendLine();
                }
            }

            var result = sb.ToString().Trim();
            _logger.LogInformation("Extracted {Length} chars from DOCX {FilePath}", result.Length, filePath);
            return result;
        }, cancellationToken);
    }
}

