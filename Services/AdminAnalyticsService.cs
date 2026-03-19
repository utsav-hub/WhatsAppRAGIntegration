using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.DTOs;

namespace WhatsAppDev.Services;

public class AdminAnalyticsService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminAnalyticsService> _logger;

    public AdminAnalyticsService(AppDbContext dbContext, ILogger<AdminAnalyticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AdminSummaryDto> GetSummaryAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dayStart = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = targetDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var newUsersToday = await _dbContext.Users
            .CountAsync(u => u.CreatedDate >= dayStart && u.CreatedDate <= dayEnd, cancellationToken);

        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);

        var conversationsToday = await _dbContext.Conversations
            .CountAsync(c => c.Timestamp >= dayStart && c.Timestamp <= dayEnd, cancellationToken);

        var leadsToday = await _dbContext.Leads
            .CountAsync(l => l.CreatedDate >= dayStart && l.CreatedDate <= dayEnd, cancellationToken);

        var totalLeads = await _dbContext.Leads.CountAsync(cancellationToken);

        _logger.LogDebug("Admin summary calculated for {Date}", targetDate);

        return new AdminSummaryDto
        {
            Date = targetDate,
            NewUsers = newUsersToday,
            TotalUsers = totalUsers,
            ConversationsToday = conversationsToday,
            LeadsToday = leadsToday,
            TotalLeads = totalLeads
        };
    }

    public async Task<List<AdminConversationDto>> GetConversationsAsync(
        DateTime? from,
        DateTime? to,
        string? phoneNumber,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Conversations
            .Include(c => c.User)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(c => c.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(c => c.Timestamp <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var normalized = phoneNumber.Trim();
            query = query.Where(c => c.User != null && c.User.PhoneNumber == normalized);
        }

        var items = await query
            .OrderByDescending(c => c.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(c => new AdminConversationDto
            {
                Id = c.Id,
                PhoneNumber = c.User != null ? c.User.PhoneNumber : string.Empty,
                UserMessage = c.UserMessage,
                BotResponse = c.BotResponse,
                Timestamp = c.Timestamp
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<List<AdminLeadDto>> GetLeadsAsync(
        DateTime? from,
        DateTime? to,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Leads.AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(l => l.CreatedDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.CreatedDate <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var normalized = phoneNumber.Trim();
            query = query.Where(l => l.PhoneNumber == normalized);
        }

        return await query
            .OrderByDescending(l => l.CreatedDate)
            .Select(l => new AdminLeadDto
            {
                Id = l.Id,
                PhoneNumber = l.PhoneNumber,
                Requirement = l.Requirement,
                CreatedDate = l.CreatedDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminConversationInsightsDto> GetConversationInsightsAsync(
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dayStart = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = targetDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var messages = await _dbContext.Conversations
            .Where(c => c.Timestamp >= dayStart && c.Timestamp <= dayEnd)
            .Select(c => c.UserMessage)
            .ToListAsync(cancellationToken);

        var total = messages.Count;
        var greetings = 0;
        var freight = 0;
        var other = 0;

        foreach (var msg in messages)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                continue;
            }

            var lower = msg.ToLowerInvariant();
            var isGreeting = lower.Contains("hi") || lower.Contains("hello");
            var isFreightQuote = lower.Contains("freight") || lower.Contains("quote") ||
                                 lower.Contains("shipment") || lower.Contains("container") ||
                                 lower.Contains("track") || lower.Contains("tracking");

            if (isGreeting)
            {
                greetings++;
            }
            else if (isFreightQuote)
            {
                freight++;
            }
            else
            {
                other++;
            }
        }

        return new AdminConversationInsightsDto
        {
            Date = targetDate,
            TotalConversations = total,
            GreetingCount = greetings,
            FreightQuoteCount = freight,
            OtherCount = other
        };
    }

    public async Task<List<AdminTrafficPointDto>> GetTrafficAsync(
        string range,
        CancellationToken cancellationToken = default)
    {
        int days = range switch
        {
            "30d" => 30,
            "90d" => 90,
            _ => 7
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-days + 1);

        // Pull once and group in memory to keep SQL simple and portable.
        var userData = await _dbContext.Users
            .Where(u => u.CreatedDate >= startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .Select(u => u.CreatedDate)
            .ToListAsync(cancellationToken);

        var convoData = await _dbContext.Conversations
            .Where(c => c.Timestamp >= startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .Select(c => c.Timestamp)
            .ToListAsync(cancellationToken);

        var result = new List<AdminTrafficPointDto>(days);

        for (var d = 0; d < days; d++)
        {
            var date = startDate.AddDays(d);
            var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dayEnd = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            var newUsers = userData.Count(t => t >= dayStart && t <= dayEnd);
            var conversations = convoData.Count(t => t >= dayStart && t <= dayEnd);

            result.Add(new AdminTrafficPointDto
            {
                Date = date,
                NewUsers = newUsers,
                Conversations = conversations
            });
        }

        return result;
    }
}

