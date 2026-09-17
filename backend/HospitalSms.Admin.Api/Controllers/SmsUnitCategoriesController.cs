using System.Data;
using HospitalSms.Application.Abstractions;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/SmsUnitCategories")]
public class SmsUnitCategoriesController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SmsUnitCategoriesController(
        HospitalSmsDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SmsUnitCategoryResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SmsUnitCategoryResponseDto>>> GetUnitCategories(
        CancellationToken cancellationToken)
    {
        var unitCategories = await _context.SmsUnitCategories
            .AsNoTracking()
            .OrderBy(item => item.UcId)
            .Select(item => new SmsUnitCategoryResponseDto
            {
                UcId = item.UcId,
                SmsUnitId = item.SmsUnitId,
                CategoryId = item.CategoryId,
                IsActive = item.IsActive,
                CreateDate = item.CreateDate
            })
            .ToListAsync(cancellationToken);

        return Ok(unitCategories);
    }

    [HttpGet("details")]
    [ProducesResponseType(typeof(IEnumerable<SmsUnitCategoryDetailsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SmsUnitCategoryDetailsDto>>> GetUnitCategoryDetails(
        CancellationToken cancellationToken)
    {
        var details = await _context.SmsUnitCategoryViews
            .AsNoTracking()
            .OrderBy(item => item.UcId)
            .Select(item => new SmsUnitCategoryDetailsDto
            {
                UcId = item.UcId,
                ProjectId = item.ProjectId,
                ProjectName = item.ProjectName,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                HospitalId = item.HospitalId,
                HospitalName = item.HospitalName,
                SmsUnitId = item.SmsUnitId,
                UnitId = item.UnitId,
                UnitName = item.UnitName,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(details);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(SmsUnitCategoryWriteResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SmsUnitCategoryWriteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SmsUnitCategoryWriteResponseDto>> CreateUnitCategory(
        [FromBody] CreateSmsUnitCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null
            || request.ProjectId <= 0
            || request.CategoryId <= 0
            || request.HospitalId <= 0
            || request.SmsUnitId <= 0)
        {
            return BadRequestProblem(
                "Invalid unit-category request.",
                "ProjectId, CategoryId, HospitalId and SmsUnitId must all contain positive values.");
        }

        var currentUser = _currentUserService.GetRequiredUserName();

        var categoryProjectId = await _context.SmsCategories
            .AsNoTracking()
            .Where(category => category.CategoryId == request.CategoryId)
            .Select(category => (int?)category.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!categoryProjectId.HasValue)
        {
            return NotFoundProblem(
                "SMS category not found.",
                "The requested SMS category does not exist.");
        }

        if (categoryProjectId.Value != request.ProjectId)
        {
            return BadRequestProblem(
                "Category and project do not match.",
                "The requested SMS category does not belong to the requested project.");
        }

        var unitHospitalId = await _context.SmsUnits
            .AsNoTracking()
            .Where(unit => unit.SmsUnitId == request.SmsUnitId)
            .Select(unit => (int?)unit.HospitalId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!unitHospitalId.HasValue)
        {
            return NotFoundProblem(
                "SMS unit not found.",
                "The requested SMS unit does not exist.");
        }

        if (unitHospitalId.Value != request.HospitalId)
        {
            return BadRequestProblem(
                "Unit and hospital do not match.",
                "The requested SMS unit does not belong to the requested hospital.");
        }

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;

        var existing = await _context.SmsUnitCategories
            .SingleOrDefaultAsync(
                item => item.SmsUnitId == request.SmsUnitId
                    && item.CategoryId == request.CategoryId,
                cancellationToken);

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UpdateUser = currentUser;
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return Ok(ToWriteResponse(existing));
        }

        var unitCategory = new SmsUnitCategory
        {
            SmsUnitId = request.SmsUnitId,
            CategoryId = request.CategoryId,
            IsActive = true,
            CreateDate = DateTime.Now,
            UpdateUser = currentUser
        };

        _context.SmsUnitCategories.Add(unitCategory);
        await _context.SaveChangesAsync(cancellationToken);

        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return CreatedAtAction(
            nameof(GetUnitCategories),
            ToWriteResponse(unitCategory));
    }

    [Authorize]
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateSmsUnitCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid unit-category identifier.",
                Detail = "The id route parameter must be a positive integer."
            });
        }

        if (!request.IsActive.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing status value.",
                Detail = "The isActive property is required."
            });
        }

        var currentUser = _currentUserService.GetRequiredUserName();

        var unitCategory = await _context.SmsUnitCategories
            .SingleOrDefaultAsync(item => item.UcId == id, cancellationToken);

        if (unitCategory == null)
        {
            return NotFound();
        }

        unitCategory.IsActive = request.IsActive.Value;
        unitCategory.UpdateUser = currentUser;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static SmsUnitCategoryWriteResponseDto ToWriteResponse(SmsUnitCategory item) => new()
    {
        UcId = item.UcId,
        SmsUnitId = item.SmsUnitId,
        CategoryId = item.CategoryId,
        IsActive = item.IsActive
    };

    private BadRequestObjectResult BadRequestProblem(string title, string detail)
    {
        this.SetFailureReason(detail);
        return BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = title,
            Detail = detail
        });
    }

    private NotFoundObjectResult NotFoundProblem(string title, string detail)
    {
        this.SetFailureReason(detail);
        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = title,
            Detail = detail
        });
    }
}
