using Microsoft.AspNetCore.Mvc;
using WhatsAppDev.DTOs;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AdminAnalyticsService _analyticsService;

    public AdminController(AdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// High-level daily summary for dashboard cards.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<AdminSummaryDto>> GetSummary([FromQuery] DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        var dto = await _analyticsService.GetSummaryAsync(date, cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    /// Paged conversation list for monitoring what users and the bot said.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<List<AdminConversationDto>>> GetConversations(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? phoneNumber = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var items = await _analyticsService.GetConversationsAsync(from, to, phoneNumber, skip, take, cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// Lead list for sales / operations follow-up.
    /// </summary>
    [HttpGet("leads")]
    public async Task<ActionResult<List<AdminLeadDto>>> GetLeads(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? phoneNumber = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _analyticsService.GetLeadsAsync(from, to, phoneNumber, cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// Conversation mix insights (greetings vs freight quotes vs other) for a given day.
    /// </summary>
    [HttpGet("insights/conversations")]
    public async Task<ActionResult<AdminConversationInsightsDto>> GetConversationInsights(
        [FromQuery] DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        var dto = await _analyticsService.GetConversationInsightsAsync(date, cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    /// Time-series of user and conversation counts for trend charts.
    /// </summary>
    [HttpGet("insights/traffic")]
    public async Task<ActionResult<List<AdminTrafficPointDto>>> GetTraffic(
        [FromQuery] string range = "7d", // 7d, 30d, 90d
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetTrafficAsync(range, cancellationToken);
        return Ok(result);
    }
}

