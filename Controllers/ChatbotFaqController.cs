using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppDev.Data;
using WhatsAppDev.DTOs;
using WhatsAppDev.Models;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("api/chatbot/faqs")]
public class ChatbotFaqController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly FaqService _faqService;

    public ChatbotFaqController(AppDbContext dbContext, FaqService faqService)
    {
        _dbContext = dbContext;
        _faqService = faqService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatbotFaqDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _dbContext.ChatbotFAQs
            .OrderBy(f => f.TriggerText)
            .Select(f => new ChatbotFaqDto
            {
                Id = f.Id,
                TriggerText = f.TriggerText,
                ResponseText = f.ResponseText,
                IsActive = f.IsActive,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ChatbotFaqDto>> Create([FromBody] CreateChatbotFaqDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.TriggerText))
            return BadRequest("TriggerText is required.");
        if (string.IsNullOrWhiteSpace(dto.ResponseText))
            return BadRequest("ResponseText is required.");

        var entity = new ChatbotFAQ
        {
            Id = Guid.NewGuid(),
            TriggerText = dto.TriggerText.Trim(),
            ResponseText = dto.ResponseText.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ChatbotFAQs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _faqService.InvalidateCache();

        return CreatedAtAction(nameof(GetAll), new ChatbotFaqDto
        {
            Id = entity.Id,
            TriggerText = entity.TriggerText,
            ResponseText = entity.ResponseText,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ChatbotFAQs.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            return NotFound();

        _dbContext.ChatbotFAQs.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _faqService.InvalidateCache();
        return NoContent();
    }
}
