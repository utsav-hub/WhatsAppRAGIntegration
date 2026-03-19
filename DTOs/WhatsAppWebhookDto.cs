namespace WhatsAppDev.DTOs;

public class WhatsAppWebhookValueDto
{
    public List<WhatsAppMessageDto>? Messages { get; set; }
}

public class WhatsAppWebhookChangeDto
{
    public WhatsAppWebhookValueDto? Value { get; set; }
}

public class WhatsAppWebhookEntryDto
{
    public List<WhatsAppWebhookChangeDto>? Changes { get; set; }
}

public class WhatsAppWebhookDto
{
    public List<WhatsAppWebhookEntryDto>? Entry { get; set; }
}

