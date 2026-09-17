using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalSms.Infrastructure.Data;
using HospitalSms.Domain.Entities;
using HospitalSms.Application.Dtos;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnitsController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public UnitsController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UnitResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<UnitResponseDto>>> GetUnits(
        [FromQuery] int? hospitalId,
        CancellationToken cancellationToken)
    {
        if (hospitalId is null or <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid hospitalId.",
                Detail = "The hospitalId query parameter is required and must be a positive integer."
            });
        }

        var units = await _context.SmsUnits
            .AsNoTracking()
            .Where(unit => unit.HospitalId == hospitalId.Value)
            .OrderBy(unit => unit.UnitName)
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

    [HttpGet("hospital/{hospitalId}/unit/{unitId}")]
    [ProducesResponseType(typeof(UnitResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitResponseDto>> GetUnitById(
        int hospitalId,
        int unitId,
        CancellationToken cancellationToken)
    {
        var unit = await _context.SmsUnits
            .AsNoTracking()
            .Where(candidate => candidate.HospitalId == hospitalId && candidate.UnitId == unitId)
            .Select(candidate => new UnitResponseDto
            {
                SmsUnitId = candidate.SmsUnitId,
                HospitalId = candidate.HospitalId,
                UnitId = candidate.UnitId,
                UnitName = candidate.UnitName,
                IsActive = candidate.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (unit == null)
        {
            return this.NotFoundWithReason(
                "The requested SMS unit does not exist for the specified hospital.");
        }

        return Ok(unit);
    }

    [HttpPost]
    public async Task<ActionResult<SmsUnit>> PostUnit(SmsUnit unit)
    {
        unit.CreateDate = DateTime.Now;
        _context.SmsUnits.Add(unit);
        await _context.SaveChangesAsync();
        return Ok(unit);
    }
}
