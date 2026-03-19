namespace WhatsAppDev.Services;

public interface ITextExtractionService
{
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
}

