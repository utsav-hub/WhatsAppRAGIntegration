using Microsoft.AspNetCore.Mvc;
using WhatsAppDev.DTOs;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("api/chatbot/keywords")]
public class ChatbotKeywordsController : ControllerBase
{
    private readonly KeywordService _keywordService;

    public ChatbotKeywordsController(KeywordService keywordService)
    {
        _keywordService = keywordService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatbotKeywordDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _keywordService.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ChatbotKeywordDto>> Create([FromBody] CreateChatbotKeywordDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Keyword))
            return BadRequest("Keyword is required.");

        var created = await _keywordService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetAll), created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _keywordService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
