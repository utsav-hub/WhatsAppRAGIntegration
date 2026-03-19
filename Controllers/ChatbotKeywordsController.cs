using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppDev.Data;
using WhatsAppDev.DTOs;
using WhatsAppDev.Models;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("api/chatbot/keywords")]
public class ChatbotKeywordsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly KeywordService _keywordService;

    public ChatbotKeywordsController(AppDbContext dbContext, KeywordService keywordService)
    {
        _dbContext = dbContext;
        _keywordService = keywordService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatbotKeywordDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _dbContext.ChatbotKeywords
            .OrderBy(k => k.Keyword)
            .Select(k => new ChatbotKeywordDto
            {
                Id = k.Id,
                Keyword = k.Keyword,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ChatbotKeywordDto>> Create([FromBody] CreateChatbotKeywordDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Keyword))
            return BadRequest("Keyword is required.");

        var entity = new ChatbotKeyword
        {
            Id = Guid.NewGuid(),
            Keyword = dto.Keyword.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ChatbotKeywords.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _keywordService.InvalidateCache();

        return CreatedAtAction(nameof(GetAll), new ChatbotKeywordDto
        {
            Id = entity.Id,
            Keyword = entity.Keyword,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ChatbotKeywords.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            return NotFound();

        _dbContext.ChatbotKeywords.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _keywordService.InvalidateCache();
        return NoContent();
    }
}
