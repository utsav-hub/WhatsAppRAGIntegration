using System.Text;

namespace WhatsAppDev.Services;

public class TextChunkingService
{
    private const int MaxChunkSize = 500;

    public List<string> ChunkText(string text)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var span = text.AsSpan();
        var index = 0;

        while (index < span.Length)
        {
            var remaining = span.Length - index;
            var length = Math.Min(MaxChunkSize, remaining);
            var chunkSpan = span.Slice(index, length);

            if (length == MaxChunkSize && index + length < span.Length)
            {
                var lastSpace = chunkSpan.LastIndexOf(' ');
                if (lastSpace > 0 && lastSpace > MaxChunkSize * 0.6)
                {
                    chunkSpan = chunkSpan.Slice(0, lastSpace);
                    length = chunkSpan.Length;
                }
            }

            var chunk = chunkSpan.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                result.Add(chunk);
            }

            index += length;
        }

        return result;
    }
}

