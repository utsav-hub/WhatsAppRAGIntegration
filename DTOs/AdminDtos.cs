using WhatsAppDev.Models;

namespace WhatsAppDev.DTOs;

public class AdminSummaryDto
{
    public DateOnly Date { get; set; }
    public int NewUsers { get; set; }
    public int TotalUsers { get; set; }
    public int ConversationsToday { get; set; }
    public int LeadsToday { get; set; }
    public int TotalLeads { get; set; }
}

public class AdminConversationDto
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string? BotResponse { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AdminLeadDto
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class AdminConversationInsightsDto
{
    public DateOnly Date { get; set; }
    public int TotalConversations { get; set; }
    public int GreetingCount { get; set; }
    public int FreightQuoteCount { get; set; }
    public int OtherCount { get; set; }
}

public class AdminTrafficPointDto
{
    public DateOnly Date { get; set; }
    public int NewUsers { get; set; }
    public int Conversations { get; set; }
}


