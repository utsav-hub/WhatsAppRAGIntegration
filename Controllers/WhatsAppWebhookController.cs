using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Config;
using WhatsAppDev.DTOs;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("webhook")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppSettings _whatsAppSettings;
    private readonly MessageProcessorService _messageProcessor;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IOptions<WhatsAppSettings> whatsAppSettings,
        MessageProcessorService messageProcessor,
        ILogger<WhatsAppWebhookController> logger)
    {
        _whatsAppSettings = whatsAppSettings.Value;
        _messageProcessor = messageProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Webhook verification endpoint for Meta WhatsApp Cloud API.
    /// </summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode == "subscribe" && verifyToken == _whatsAppSettings.VerifyToken)
        {
            _logger.LogInformation("Webhook verification successful.");
            return Content(challenge ?? string.Empty);
        }

        _logger.LogWarning("Webhook verification failed. Mode: {Mode}, Token: {Token}", mode, verifyToken);
        return Unauthorized();
    }

    /// <summary>
    /// Receives incoming WhatsApp messages from Meta WhatsApp Cloud API.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveWhatsAppMessage([FromBody] WhatsAppWebhookDto payload, CancellationToken cancellationToken)
    {
        try
        {
            var message = payload.Entry?
                .FirstOrDefault()?
                .Changes?
                .FirstOrDefault()?
                .Value?
                .Messages?
                .FirstOrDefault();

            if (message == null)
            {
                _logger.LogWarning("Received webhook without message payload.");
                return Ok();
            }

            var from = message.From;
            var body = message.Text?.Body ?? string.Empty;

            _logger.LogInformation("Incoming WhatsApp message from {From}: {Body}", from, body);

            await _messageProcessor.ProcessIncomingMessageAsync(from, body, cancellationToken);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WhatsApp webhook.");
            // Return 200 to avoid webhook retries storm; log for investigation
            return Ok();
        }
    }
}

