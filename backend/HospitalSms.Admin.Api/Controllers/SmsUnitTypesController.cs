using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsUnitTypesController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public SmsUnitTypesController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UnitResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UnitResponseDto>>> GetUnitTypes(
        CancellationToken cancellationToken)
    {
        var units = await _context.SmsUnits
            .AsNoTracking()
            .OrderBy(unit => unit.HospitalId)
            .ThenBy(unit => unit.UnitId)
            .Select(unit => new UnitResponseDto
            {
                SmsUnitId = unit.SmsUnitId,
                HospitalId = unit.HospitalId,
                UnitId = unit.UnitId,
                UnitName = unit.UnitName,
                IsActive = unit.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(units);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult<SmsUnitType> GetUnitType(int id) => UnitTypeCatalogRemoved();

    [HttpPost]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult<SmsUnitType> CreateUnitType([FromBody] SmsUnitType unitType) =>
        UnitTypeCatalogRemoved();

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult UpdateUnitType(int id, [FromBody] SmsUnitType unitType) =>
        UnitTypeCatalogRemoved();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult DeleteUnitType(int id) => UnitTypeCatalogRemoved();

    private ObjectResult UnitTypeCatalogRemoved()
    {
        return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
        {
            Status = StatusCodes.Status410Gone,
            Title = "The SMS unit-type catalog no longer exists.",
            Detail = "Use GET /api/SmsUnitTypes or GET /api/Units?hospitalId={hospitalId} to read dbo.sms_unit."
        });
    }
}
