using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.Models;

namespace WhatsAppDev.Services;

public class ConversationService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(AppDbContext dbContext, ILogger<ConversationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalized = phoneNumber.Trim();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalized, cancellationToken);

        if (user != null)
        {
            return user;
        }

        user = new User
        {
            PhoneNumber = normalized,
            CreatedDate = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new user for phone {PhoneNumber}", normalized);

        return user;
    }

    public async Task<Conversation> AddConversationAsync(
        int userId,
        string userMessage,
        string botResponse,
        CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation
        {
            UserId = userId,
            UserMessage = userMessage,
            BotResponse = botResponse,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved conversation for user {UserId}", userId);

        return conversation;
    }
}

