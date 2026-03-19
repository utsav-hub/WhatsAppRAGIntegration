using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsAppDev.Data;
using WhatsAppDev.Models;

namespace WhatsAppDev.Services;

public class LeadService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LeadService> _logger;

    public LeadService(AppDbContext dbContext, ILogger<LeadService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Lead> CreateLeadAsync(string phoneNumber, string requirement, CancellationToken cancellationToken = default)
    {
        var lead = new Lead
        {
            PhoneNumber = phoneNumber.Trim(),
            Requirement = requirement,
            CreatedDate = DateTime.UtcNow
        };

        _dbContext.Leads.Add(lead);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created lead for phone {PhoneNumber}", lead.PhoneNumber);

        return lead;
    }

    public async Task<List<Lead>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Leads
            .OrderByDescending(l => l.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lead?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Leads.FindAsync(new object[] { id }, cancellationToken);
    }
}

