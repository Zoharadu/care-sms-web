using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsRejectHospitalUnitsController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public SmsRejectHospitalUnitsController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SmsRejectHospitalUnit>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SmsRejectHospitalUnit>>> GetRejectHospitalUnits(
        [FromQuery] int? templateId,
        CancellationToken cancellationToken)
    {
        var query = _context.SmsRejectHospitalUnits.AsNoTracking();

        if (templateId.HasValue)
        {
            query = query.Where(reject => reject.TemplateId == templateId.Value);
        }

        var rejects = await query
            .OrderBy(reject => reject.TemplateId)
            .ThenBy(reject => reject.HospitalId)
            .ThenBy(reject => reject.UnitId)
            .ToListAsync(cancellationToken);

        return Ok(rejects);
    }

    [HttpPut("replace")]
    [HttpPost("replace")]
    [ProducesResponseType(typeof(ReplaceRejectHospitalUnitsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReplaceRejectHospitalUnitsResponse>> Replace(
        [FromBody] ReplaceRejectHospitalUnitsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.TemplateId <= 0 || request.Items == null)
        {
            return this.BadRequestWithReason(
                "The replacement request must include a positive TemplateId and an items collection.");
        }

        if (request.Items.Any(item => item.HospitalId <= 0 || item.UnitId <= 0))
        {
            return this.BadRequestWithReason(
                "Every rejected hospital-unit item must contain positive HospitalId and UnitId values.");
        }

        var items = request.Items
            .DistinctBy(item => (item.HospitalId, item.UnitId))
            .ToList();

        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        var templateExists = await _context.SmsTemplates
            .AsNoTracking()
            .AnyAsync(template => template.TemplateId == request.TemplateId, cancellationToken);

        if (!templateExists)
        {
            return this.NotFoundWithReason("The SMS template referenced by the replacement request does not exist.");
        }

        if (items.Count > 0)
        {
            var hospitalIds = items
                .Select(item => item.HospitalId)
                .Distinct()
                .ToList();

            var existingHospitalIds = await _context.SmsHospitals
                .AsNoTracking()
                .Where(hospital => hospitalIds.Contains(hospital.HospitalId))
                .Select(hospital => hospital.HospitalId)
                .ToListAsync(cancellationToken);

            if (existingHospitalIds.Count != hospitalIds.Count)
            {
                return this.NotFoundWithReason(
                    "One or more hospitals referenced by the replacement request do not exist.");
            }

            var unitIds = items
                .Select(item => item.UnitId)
                .Distinct()
                .ToList();

            var existingUnits = await _context.SmsUnits
                .AsNoTracking()
                .Where(unit => hospitalIds.Contains(unit.HospitalId) && unitIds.Contains(unit.UnitId))
                .Select(unit => new { unit.HospitalId, unit.UnitId })
                .ToListAsync(cancellationToken);

            var existingUnitKeys = existingUnits
                .Select(unit => (unit.HospitalId, unit.UnitId))
                .ToHashSet();

            if (items.Any(item => !existingUnitKeys.Contains((item.HospitalId, item.UnitId))))
            {
                return this.NotFoundWithReason(
                    "One or more hospital-unit pairs referenced by the replacement request do not exist.");
            }
        }

        var existingRejects = await _context.SmsRejectHospitalUnits
            .Where(reject => reject.TemplateId == request.TemplateId)
            .ToListAsync(cancellationToken);

        var requestedKeys = items
            .Select(item => (item.HospitalId, item.UnitId))
            .ToHashSet();
        var existingKeys = existingRejects
            .Select(reject => (reject.HospitalId, reject.UnitId))
            .ToHashSet();

        var rejectsToDelete = existingRejects
            .Where(reject => !requestedKeys.Contains((reject.HospitalId, reject.UnitId)))
            .ToList();

        _context.SmsRejectHospitalUnits.RemoveRange(rejectsToDelete);

        var now = DateTime.Now;
        var newRejects = items
            .Where(item => !existingKeys.Contains((item.HospitalId, item.UnitId)))
            .Select(item => new SmsRejectHospitalUnit
            {
                TemplateId = request.TemplateId,
                HospitalId = item.HospitalId,
                UnitId = item.UnitId,
                CreateDate = now,
                UpdateDate = null
            });

        _context.SmsRejectHospitalUnits.AddRange(newRejects);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new ReplaceRejectHospitalUnitsResponse
        {
            TemplateId = request.TemplateId,
            SavedCount = items.Count,
            Items = items.Select(item => new RejectHospitalUnitItemResponse
            {
                HospitalId = item.HospitalId,
                UnitId = item.UnitId
            }).ToList()
        });
    }
}
