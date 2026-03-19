using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.Models;

namespace WhatsAppDev.Services;

public class KeywordService
{
    private const string CacheKey = "ChatbotKeywords_Active";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KeywordService> _logger;

    public KeywordService(
        AppDbContext dbContext,
        IMemoryCache cache,
        ILogger<KeywordService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<string>> GetActiveKeywordsAsync(CancellationToken cancellationToken = default)
    {
        return (await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var keywords = await _dbContext.ChatbotKeywords
                .Where(k => k.IsActive)
                .OrderBy(k => k.Keyword)
                .Select(k => k.Keyword.Trim().ToLowerInvariant())
                .Distinct()
                .ToListAsync(cancellationToken);
            _logger.LogDebug("Loaded {Count} active keywords from database", keywords.Count);
            return keywords;
        })) ?? new List<string>();
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogDebug("Keyword cache invalidated");
    }
}
