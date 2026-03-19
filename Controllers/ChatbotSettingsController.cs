using Microsoft.AspNetCore.Mvc;
using WhatsAppDev.DTOs;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("api/chatbot/settings")]
public class ChatbotSettingsController : ControllerBase
{
    private readonly SettingsService _settingsService;

    public ChatbotSettingsController(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatbotSettingDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _settingsService.GetAllAsync(cancellationToken));
    }

    [HttpPut]
    public async Task<ActionResult> Put([FromBody] UpdateChatbotSettingsDto dto, CancellationToken cancellationToken)
    {
        if (dto.Settings == null || dto.Settings.Count == 0)
            return BadRequest("At least one setting is required.");
        await _settingsService.UpsertAsync(dto.Settings, cancellationToken);
        return NoContent();
    }
}
