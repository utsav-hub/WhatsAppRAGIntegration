using Microsoft.Extensions.Logging;

namespace WhatsAppDev.Services;

public class TextExtractionService : ITextExtractionService
{
    private readonly PdfTextExtractionService _pdf;
    private readonly DocxTextExtractionService _docx;
    private readonly ILogger<TextExtractionService> _logger;

    public TextExtractionService(
        PdfTextExtractionService pdf,
        DocxTextExtractionService docx,
        ILogger<TextExtractionService> logger)
    {
        _pdf = pdf;
        _docx = docx;
        _logger = logger;
    }

    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        _logger.LogInformation("Extracting text from {Ext} file {FilePath}", ext, filePath);

        return ext switch
        {
            ".pdf" => _pdf.ExtractTextAsync(filePath, cancellationToken),
            ".docx" => _docx.ExtractTextAsync(filePath, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported file extension '{ext}'.")
        };
    }
}

