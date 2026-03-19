using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WhatsAppDev.Data;
using WhatsAppDev.Models;
using WhatsAppDev.Services;
using Xunit;

namespace WhatsAppDev.Tests;

public class KeywordDetectionTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Keywords_" + Guid.NewGuid())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetActiveKeywords_Returns_Only_Active_Keywords()
    {
        await using var context = CreateContext();
        context.ChatbotKeywords.AddRange(
            new ChatbotKeyword { Id = Guid.NewGuid(), Keyword = "shipment", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ChatbotKeyword { Id = Guid.NewGuid(), Keyword = "freight", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ChatbotKeyword { Id = Guid.NewGuid(), Keyword = "inactive", IsActive = false, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<KeywordService>.Instance;
        var service = new KeywordService(context, cache, logger);

        var keywords = await service.GetActiveKeywordsAsync();

        Assert.Equal(2, keywords.Count);
        Assert.Contains("shipment", keywords);
        Assert.Contains("freight", keywords);
        Assert.DoesNotContain("inactive", keywords);
    }

    [Fact]
    public async Task Message_InScope_When_It_Contains_Any_Keyword()
    {
        await using var context = CreateContext();
        context.ChatbotKeywords.Add(new ChatbotKeyword { Id = Guid.NewGuid(), Keyword = "track", IsActive = true, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KeywordService(context, cache, NullLogger<KeywordService>.Instance);
        var keywords = await service.GetActiveKeywordsAsync();

        var message = "I want to track my container";
        var lower = message.ToLowerInvariant();
        var inScope = keywords.Any(k => lower.Contains(k));

        Assert.True(inScope);
    }

    [Fact]
    public async Task Message_OutOfScope_When_No_Keyword_Matches()
    {
        await using var context = CreateContext();
        context.ChatbotKeywords.Add(new ChatbotKeyword { Id = Guid.NewGuid(), Keyword = "shipment", IsActive = true, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KeywordService(context, cache, NullLogger<KeywordService>.Instance);
        var keywords = await service.GetActiveKeywordsAsync();

        var message = "what is the weather today";
        var lower = message.ToLowerInvariant();
        var inScope = keywords.Any(k => lower.Contains(k));

        Assert.False(inScope);
    }

    [Fact]
    public async Task Empty_Keywords_List_Treats_All_Messages_As_InScope()
    {
        await using var context = CreateContext();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KeywordService(context, cache, NullLogger<KeywordService>.Instance);
        var keywords = await service.GetActiveKeywordsAsync();

        Assert.Empty(keywords);
        var inScope = keywords.Count == 0 || keywords.Any(k => "anything".Contains(k));
        Assert.True(inScope);
    }
}
