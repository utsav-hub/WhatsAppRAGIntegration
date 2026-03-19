using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppDev.Data;
using WhatsAppDev.DTOs;
using WhatsAppDev.Models;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("api/chatbot/settings")]
public class ChatbotSettingsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly SettingsService _settingsService;

    public ChatbotSettingsController(AppDbContext dbContext, SettingsService settingsService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatbotSettingDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _dbContext.ChatbotSettings
            .AsNoTracking()
            .OrderBy(s => s.SettingKey)
            .Select(s => new ChatbotSettingDto
            {
                SettingKey = s.SettingKey,
                SettingValue = s.SettingValue
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPut]
    public async Task<ActionResult> Put([FromBody] UpdateChatbotSettingsDto dto, CancellationToken cancellationToken)
    {
        if (dto.Settings == null || dto.Settings.Count == 0)
            return BadRequest("At least one setting is required.");

        foreach (var item in dto.Settings)
        {
            if (string.IsNullOrWhiteSpace(item.SettingKey))
                continue;

            var key = item.SettingKey.Trim();
            var value = item.SettingValue ?? string.Empty;

            var existing = await _dbContext.ChatbotSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key, cancellationToken);

            if (existing != null)
            {
                existing.SettingValue = value;
            }
            else
            {
                _dbContext.ChatbotSettings.Add(new ChatbotSetting
                {
                    Id = Guid.NewGuid(),
                    SettingKey = key,
                    SettingValue = value
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _settingsService.InvalidateCache();
        return NoContent();
    }
}
