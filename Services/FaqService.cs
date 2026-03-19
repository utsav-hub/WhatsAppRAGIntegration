using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.Models;

namespace WhatsAppDev.Services;

public class FaqService
{
    private const string CacheKey = "ChatbotFAQs_Active";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FaqService> _logger;

    public FaqService(
        AppDbContext dbContext,
        IMemoryCache cache,
        ILogger<FaqService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> FindMatchingFaqAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var faqs = await GetActiveFaqsAsync(cancellationToken);
        var lower = message.Trim().ToLowerInvariant();

        foreach (var faq in faqs)
        {
            var trigger = faq.TriggerText.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(trigger))
                continue;
            if (lower.Contains(trigger))
            {
                _logger.LogInformation("FAQ match: trigger '{Trigger}' matched message", faq.TriggerText);
                return faq.ResponseText;
            }
        }

        return null;
    }

    internal async Task<List<ChatbotFAQ>> GetActiveFaqsAsync(CancellationToken cancellationToken = default)
    {
        return (await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var list = await _dbContext.ChatbotFAQs
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.TriggerText)
                .ToListAsync(cancellationToken);
            _logger.LogDebug("Loaded {Count} active FAQs from database", list.Count);
            return list;
        })) ?? new List<ChatbotFAQ>();
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogDebug("FAQ cache invalidated");
    }
}
