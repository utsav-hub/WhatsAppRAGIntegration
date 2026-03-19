using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace WhatsAppDev.Services;

/// <summary>
/// Thin wrapper around IMemoryCache for storing chatbot responses.
/// Keys and values are simple strings and entries are short-lived.
/// </summary>
public class CacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Normalizes a user message for use as a cache key component.
    /// Lowercases and trims whitespace.
    /// </summary>
    public static string NormalizeMessage(string message)
    {
        return message.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Builds a cache key for a user's message so that responses are scoped per user.
    /// </summary>
    public static string BuildMessageKey(string phoneNumber, string normalizedMessage)
    {
        return $"msg:{phoneNumber}:{normalizedMessage}";
    }

    public string? Get(string key)
    {
        if (_cache.TryGetValue<string>(key, out var value))
        {
            _logger.LogInformation("Cache hit for key {Key}", key);
            return value;
        }

        _logger.LogInformation("Cache miss for key {Key}", key);
        return null;
    }

    public void Set(string key, string value)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultTtl
        };

        _cache.Set(key, value, options);
        _logger.LogInformation("Cached response for key {Key} with TTL {TtlMinutes} minutes", key, DefaultTtl.TotalMinutes);
    }
}

