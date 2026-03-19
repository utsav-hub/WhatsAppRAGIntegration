using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Models;
using WhatsAppDev.Services;

namespace WhatsAppDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly LeadService _leadService;
    private readonly ILogger<LeadsController> _logger;

    public LeadsController(LeadService leadService, ILogger<LeadsController> logger)
    {
        _leadService = leadService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Lead>>> GetAll(CancellationToken cancellationToken)
    {
        var leads = await _leadService.GetAllAsync(cancellationToken);
        return Ok(leads);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Lead>> GetById(int id, CancellationToken cancellationToken)
    {
        var lead = await _leadService.GetByIdAsync(id, cancellationToken);
        if (lead == null)
        {
            return NotFound();
        }

        return Ok(lead);
    }
}

