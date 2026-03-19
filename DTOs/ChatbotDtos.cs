namespace WhatsAppDev.DTOs;

public class ChatbotKeywordDto
{
    public Guid Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateChatbotKeywordDto
{
    public string Keyword { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ChatbotSettingDto
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
}

public class UpdateChatbotSettingsDto
{
    public List<ChatbotSettingDto> Settings { get; set; } = new();
}

public class ChatbotFaqDto
{
    public Guid Id { get; set; }
    public string TriggerText { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateChatbotFaqDto
{
    public string TriggerText { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
