using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/SmsAuditLogs")]
public class SmsAuditLogsController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public SmsAuditLogsController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SmsAuditLog>>> GetAuditLogs()
    {
        var logs = await _context.SmsAuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(500)
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SmsAuditLog>> GetAuditLog(long id)
    {
        var log = await _context.SmsAuditLogs.FindAsync(id);

        if (log == null)
        {
            return this.NotFoundWithReason("The requested SMS audit-log record does not exist.");
        }

        return Ok(log);
    }

    [HttpPost]
    public async Task<ActionResult<SmsAuditLog>> CreateAuditLog(SmsAuditLog log)
    {
        log.CreatedAt = DateTime.UtcNow;
        _context.SmsAuditLogs.Add(log);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAuditLog), new { id = log.AuditLogId }, log);
    }
}
