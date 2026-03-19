using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using WhatsAppDev.Data;
using WhatsAppDev.Models;
using WhatsAppDev.Services;
using Xunit;

namespace WhatsAppDev.Tests;

public class FaqMatchingTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Faqs_" + Guid.NewGuid())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task FindMatchingFaq_Returns_Response_When_Message_Contains_Trigger()
    {
        await using var context = CreateContext();
        context.ChatbotFAQs.Add(new ChatbotFAQ
        {
            Id = Guid.NewGuid(),
            TriggerText = "track container",
            ResponseText = "You can track your container at octology.com/track",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new FaqService(context, cache, NullLogger<FaqService>.Instance);

        var response = await service.FindMatchingFaqAsync("I want to track container from Mumbai");

        Assert.NotNull(response);
        Assert.Equal("You can track your container at octology.com/track", response);
    }

    [Fact]
    public async Task FindMatchingFaq_Returns_Null_When_No_Trigger_Matches()
    {
        await using var context = CreateContext();
        context.ChatbotFAQs.Add(new ChatbotFAQ
        {
            Id = Guid.NewGuid(),
            TriggerText = "freight quote",
            ResponseText = "Request a quote at octology.com/quote",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new FaqService(context, cache, NullLogger<FaqService>.Instance);

        var response = await service.FindMatchingFaqAsync("What is the weather?");

        Assert.Null(response);
    }

    [Fact]
    public async Task FindMatchingFaq_Ignores_Inactive_FAQs()
    {
        await using var context = CreateContext();
        context.ChatbotFAQs.Add(new ChatbotFAQ
        {
            Id = Guid.NewGuid(),
            TriggerText = "old trigger",
            ResponseText = "Old response",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new FaqService(context, cache, NullLogger<FaqService>.Instance);

        var response = await service.FindMatchingFaqAsync("Please use old trigger");

        Assert.Null(response);
    }

    [Fact]
    public async Task FindMatchingFaq_Returns_Null_For_Empty_Message()
    {
        await using var context = CreateContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new FaqService(context, cache, NullLogger<FaqService>.Instance);

        Assert.Null(await service.FindMatchingFaqAsync(""));
        Assert.Null(await service.FindMatchingFaqAsync("   "));
    }
}
