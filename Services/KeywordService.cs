using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.DTOs;
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

    public async Task<List<ChatbotKeywordDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatbotKeywords
            .AsNoTracking()
            .OrderBy(k => k.Keyword)
            .Select(k => new ChatbotKeywordDto
            {
                Id = k.Id,
                Keyword = k.Keyword,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatbotKeywordDto> CreateAsync(CreateChatbotKeywordDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Keyword))
            throw new ArgumentException("Keyword is required.", nameof(dto));

        var entity = new ChatbotKeyword
        {
            Id = Guid.NewGuid(),
            Keyword = dto.Keyword.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ChatbotKeywords.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        InvalidateCache();

        return new ChatbotKeywordDto
        {
            Id = entity.Id,
            Keyword = entity.Keyword,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ChatbotKeywords.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            return false;

        _dbContext.ChatbotKeywords.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        InvalidateCache();

        return true;
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogDebug("Keyword cache invalidated");
    }
}
