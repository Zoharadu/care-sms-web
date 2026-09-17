using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BtholSms2SendListDeltasController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public BtholSms2SendListDeltasController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BtholSms2SendListDelta>>> GetSendListDeltas([FromQuery] int limit = 100)
    {
        var list = await _context.BtholSms2SendListDeltas
            .Take(limit)
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("deleted")]
    public async Task<ActionResult<IEnumerable<BtholSms2SendListDeltaDeleted>>> GetDeletedSendListDeltas([FromQuery] int limit = 100)
    {
        var list = await _context.BtholSms2SendListDeltaDeleted
            .Take(limit)
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<BtholSms2SendListDelta>> CreateDelta(BtholSms2SendListDelta delta)
    {
        delta.DateCreated = DateTime.Now;
        _context.BtholSms2SendListDeltas.Add(delta);
        await _context.SaveChangesAsync();
        return Ok(delta);
    }
}
