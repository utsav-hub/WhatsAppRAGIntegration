using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.DTOs;
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

    public async Task<List<ChatbotFaqDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatbotFAQs
            .AsNoTracking()
            .OrderBy(f => f.TriggerText)
            .Select(f => new ChatbotFaqDto
            {
                Id = f.Id,
                TriggerText = f.TriggerText,
                ResponseText = f.ResponseText,
                IsActive = f.IsActive,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatbotFaqDto> CreateAsync(CreateChatbotFaqDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null)
            throw new ArgumentException("FAQ payload is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.TriggerText))
            throw new ArgumentException("TriggerText is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.ResponseText))
            throw new ArgumentException("ResponseText is required.", nameof(dto));

        var entity = new ChatbotFAQ
        {
            Id = Guid.NewGuid(),
            TriggerText = dto.TriggerText.Trim(),
            ResponseText = dto.ResponseText.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ChatbotFAQs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        InvalidateCache();

        return new ChatbotFaqDto
        {
            Id = entity.Id,
            TriggerText = entity.TriggerText,
            ResponseText = entity.ResponseText,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ChatbotFAQs.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            return false;

        _dbContext.ChatbotFAQs.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        InvalidateCache();

        return true;
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogDebug("FAQ cache invalidated");
    }
}
