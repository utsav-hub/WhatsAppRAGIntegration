using Microsoft.Extensions.Logging;

namespace WhatsAppDev.Services;

public class MessageProcessorService
{
    private readonly ConversationService _conversationService;
    private readonly LeadService _leadService;
    private readonly OllamaService _ollamaService;
    private readonly WhatsAppService _whatsAppService;
    private readonly KeywordService _keywordService;
    private readonly SettingsService _settingsService;
    private readonly FaqService _faqService;
    private readonly CacheService _cacheService;
    private readonly RagService _ragService;
    private readonly ILogger<MessageProcessorService> _logger;

    public MessageProcessorService(
        ConversationService conversationService,
        LeadService leadService,
        OllamaService ollamaService,
        WhatsAppService whatsAppService,
        KeywordService keywordService,
        SettingsService settingsService,
        FaqService faqService,
        CacheService cacheService,
        RagService ragService,
        ILogger<MessageProcessorService> logger)
    {
        _conversationService = conversationService;
        _leadService = leadService;
        _ollamaService = ollamaService;
        _whatsAppService = whatsAppService;
        _keywordService = keywordService;
        _settingsService = settingsService;
        _faqService = faqService;
        _cacheService = cacheService;
        _ragService = ragService;
        _logger = logger;
    }

    public async Task ProcessIncomingMessageAsync(string phoneNumber, string messageText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogWarning("Received empty message from {PhoneNumber}", phoneNumber);
            return;
        }

        var trimmedMessage = messageText.Trim();
        var normalizedMessage = CacheService.NormalizeMessage(trimmedMessage);

        _logger.LogInformation("Incoming message from {PhoneNumber}: {Message}", phoneNumber, trimmedMessage);

        // 1. Check cache (per user + normalized message)
        var cacheKey = CacheService.BuildMessageKey(phoneNumber, normalizedMessage);
        var cachedResponse = _cacheService.Get(cacheKey);
        if (!string.IsNullOrEmpty(cachedResponse))
        {
            _logger.LogInformation("Returning cached response for {PhoneNumber}", phoneNumber);

            var cachedUser = await _conversationService.GetOrCreateUserAsync(phoneNumber, cancellationToken);
            await _conversationService.AddConversationAsync(cachedUser.Id, trimmedMessage, cachedResponse, cancellationToken);
            await _whatsAppService.SendTextMessageAsync(phoneNumber, cachedResponse, cancellationToken);

            return;
        }

        var lower = normalizedMessage;

        var user = await _conversationService.GetOrCreateUserAsync(phoneNumber, cancellationToken);

        string responseText;

        // 2. Check greeting
        if (lower.Contains("hi") || lower.Contains("hello"))
        {
            var greetingMessage = await _settingsService.GetSettingAsync(SettingsService.Keys.GreetingMessage, cancellationToken);
            responseText = string.IsNullOrWhiteSpace(greetingMessage)
                ? "Welcome to Octology Logistics Assistant 🚢\n\n1 Track container\n2 Freight quote\n3 Import documentation\n4 Talk to agent"
                : greetingMessage;
            _logger.LogInformation("Greeting detected; returning configured greeting");
        }
        else
        {
            // 3. Check FAQ match
            var faqResponse = await _faqService.FindMatchingFaqAsync(trimmedMessage, cancellationToken);
            if (faqResponse != null)
            {
                responseText = faqResponse;
                _logger.LogInformation("FAQ match; returning FAQ response");
            }
            else
            {
                // 4. Check domain keywords
                var keywords = await _keywordService.GetActiveKeywordsAsync(cancellationToken);
                var inScope = keywords.Count == 0 || keywords.Any(k => lower.Contains(k));

                if (!inScope)
                {
                    var outOfScopeReply = await _settingsService.GetSettingAsync(SettingsService.Keys.OutOfScopeReply, cancellationToken);
                    responseText = string.IsNullOrWhiteSpace(outOfScopeReply)
                        ? "I can only help with logistics, shipment tracking, freight quotes, and import documentation. Please ask something related to these topics."
                        : outOfScopeReply;
                    _logger.LogInformation("Message outside domain (keyword detection); returning out-of-scope reply");
                }
                else
                {
                    // 5. Call RAG pipeline (vector search + LLM)
                    _logger.LogInformation("Calling RAG pipeline for in-scope message");
                    responseText = await _ragService.GenerateResponseAsync(trimmedMessage, cancellationToken);
                    _logger.LogInformation("RAG response generated");

                    // 7. Store response in cache to avoid repeated LLM calls for the same question
                    _cacheService.Set(cacheKey, responseText);
                }
            }
        }

        // Lead creation for freight/quote intent
        if (lower.Contains("freight") || lower.Contains("quote"))
        {
            await _leadService.CreateLeadAsync(phoneNumber, trimmedMessage, cancellationToken);
        }

        await _conversationService.AddConversationAsync(user.Id, trimmedMessage, responseText, cancellationToken);
        await _whatsAppService.SendTextMessageAsync(phoneNumber, responseText, cancellationToken);

        _logger.LogInformation("Final response sent to {PhoneNumber}", phoneNumber);
    }
}
