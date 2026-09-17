using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BtholFactSentMessagesController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public BtholFactSentMessagesController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BtholFactSentMessage>>> GetSentMessages([FromQuery] int limit = 100)
    {
        var messages = await _context.BtholFactSentMessages
            .OrderByDescending(m => m.RowId)
            .Take(limit)
            .ToListAsync();

        return Ok(messages);
    }

    [HttpGet("{rowId}")]
    public async Task<ActionResult<BtholFactSentMessage>> GetSentMessage(long rowId)
    {
        var message = await _context.BtholFactSentMessages.FindAsync(rowId);
        if (message == null)
        {
            return this.NotFoundWithReason("The requested sent-message record does not exist.");
        }
        return Ok(message);
    }

    [HttpPost]
    public async Task<ActionResult<BtholFactSentMessage>> CreateSentMessage(BtholFactSentMessage message)
    {
        _context.BtholFactSentMessages.Add(message);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSentMessage), new { rowId = message.RowId }, message);
    }
}
