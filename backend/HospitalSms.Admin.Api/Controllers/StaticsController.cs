using HospitalSms.Application.Dtos;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/statics")]
public class StaticsController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public StaticsController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StaticValueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StaticValueDto>>> GetStatics(
        CancellationToken cancellationToken)
    {
        var statics = await (
            from staticValue in _context.SmsStatics.AsNoTracking()
            join placeholder in _context.SmsPlaceholderCatalogs.AsNoTracking()
                on staticValue.PlaceholderId equals placeholder.PlaceholderId
            where staticValue.IsActive
            orderby placeholder.DisplayName
            select new StaticValueDto
            {
                FieldName = staticValue.ValueName ?? placeholder.DisplayName,
                StaticValue = staticValue.StaticValue
            })
            .ToListAsync(cancellationToken);

        return Ok(statics);
    }
}
