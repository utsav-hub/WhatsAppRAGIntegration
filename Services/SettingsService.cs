using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;

namespace WhatsAppDev.Services;

public class SettingsService
{
    private const string CacheKeyPrefix = "ChatbotSetting_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SettingsService> _logger;

    public static class Keys
    {
        public const string OutOfScopeReply = "OUT_OF_SCOPE_REPLY";
        public const string GreetingMessage = "GREETING_MESSAGE";
        public const string SystemPrompt = "SYSTEM_PROMPT";
    }

    public SettingsService(
        AppDbContext dbContext,
        IMemoryCache cache,
        ILogger<SettingsService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var cacheKey = CacheKeyPrefix + key;
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var setting = await _dbContext.ChatbotSettings
                .AsNoTracking()
                .Where(s => s.SettingKey == key)
                .Select(s => s.SettingValue)
                .FirstOrDefaultAsync(cancellationToken);
            _logger.LogDebug("Loaded setting {Key} from database", key);
            return setting ?? null;
        });
    }

    public void InvalidateCache(string? key = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            // Remove all settings cache entries by using a custom cache key pattern - we can't enumerate, so we invalidate common keys
            foreach (var k in new[] { Keys.OutOfScopeReply, Keys.GreetingMessage, Keys.SystemPrompt })
                _cache.Remove(CacheKeyPrefix + k);
        }
        else
        {
            _cache.Remove(CacheKeyPrefix + key);
        }
        _logger.LogDebug("Settings cache invalidated for key {Key}", key ?? "all");
    }
}
