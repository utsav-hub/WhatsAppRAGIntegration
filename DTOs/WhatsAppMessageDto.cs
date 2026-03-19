namespace WhatsAppDev.DTOs;

public class WhatsAppMessageTextDto
{
    public string? Body { get; set; }
}

public class WhatsAppMessageDto
{
    public string From { get; set; } = string.Empty;
    public WhatsAppMessageTextDto? Text { get; set; }
}

