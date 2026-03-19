using Microsoft.AspNetCore.Mvc;
using WhatsAppDev.DTOs;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("api/chatbot/faqs")]
public class ChatbotFaqController : ControllerBase
{
    private readonly FaqService _faqService;

    public ChatbotFaqController(FaqService faqService)
    {
        _faqService = faqService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatbotFaqDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _faqService.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ChatbotFaqDto>> Create([FromBody] CreateChatbotFaqDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.TriggerText))
            return BadRequest("TriggerText is required.");
        if (string.IsNullOrWhiteSpace(dto.ResponseText))
            return BadRequest("ResponseText is required.");

        var created = await _faqService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetAll), created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _faqService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
