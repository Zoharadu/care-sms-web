using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DdlLogsController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public DdlLogsController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DdlLog>>> GetDdlLogs([FromQuery] int limit = 100)
    {
        var logs = await _context.DdlLogs
            .Take(limit)
            .ToListAsync();

        return Ok(logs);
    }
}
