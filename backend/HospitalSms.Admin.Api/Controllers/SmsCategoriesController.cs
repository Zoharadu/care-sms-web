using HospitalSms.Admin.Api.Models;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/SmsCategories")]
public class SmsCategoriesController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public SmsCategoriesController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SmsCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SmsCategoryDto>>> GetCategories(
        [FromQuery] SmsCategoryQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ProjectId is <= 0)
        {
            return this.BadRequestWithReason("ProjectId must contain a positive value when supplied.");
        }

        if (query.ProjectId.HasValue
            && !await _context.SmsProjects.AsNoTracking().AnyAsync(
                project => project.ProjectId == query.ProjectId.Value,
                cancellationToken))
        {
            return this.NotFoundWithReason("The requested SMS project does not exist.");
        }

        var categoriesQuery = _context.SmsCategories.AsNoTracking();

        if (query.ProjectId.HasValue)
        {
            categoriesQuery = categoriesQuery.Where(
                category => category.ProjectId == query.ProjectId.Value);
        }

        if (query.IsActive.HasValue)
        {
            categoriesQuery = categoriesQuery.Where(
                category => category.IsActive == query.IsActive.Value);
        }

        var categories = await (
            from category in categoriesQuery
            join project in _context.SmsProjects.AsNoTracking()
                on category.ProjectId equals project.ProjectId into projects
            from project in projects.DefaultIfEmpty()
            orderby category.CategoryId
            select new SmsCategoryDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                ProjectId = category.ProjectId,
                CategoryType = project == null ? null : project.ProjectName,
                IsActive = category.IsActive,
                IsEditable = category.IsEditable,
                CreateDate = category.CreateDate,
                UpdateDate = category.UpdateDate
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SmsCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SmsCategoryDto>> GetCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await (
            from candidate in _context.SmsCategories.AsNoTracking()
            join project in _context.SmsProjects.AsNoTracking()
                on candidate.ProjectId equals project.ProjectId into projects
            from project in projects.DefaultIfEmpty()
            where candidate.CategoryId == id
            select new SmsCategoryDto
            {
                CategoryId = candidate.CategoryId,
                CategoryName = candidate.CategoryName,
                ProjectId = candidate.ProjectId,
                CategoryType = project == null ? null : project.ProjectName,
                IsActive = candidate.IsActive,
                IsEditable = candidate.IsEditable,
                CreateDate = candidate.CreateDate,
                UpdateDate = candidate.UpdateDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (category == null)
        {
            return this.NotFoundWithReason("The requested SMS category does not exist.");
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<SmsCategoryDto>> CreateCategory(
        [FromBody] SaveSmsCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (HasInvalidValues(request))
        {
            return this.BadRequestWithReason(
                "The SMS category request is invalid. A valid name and either a positive ProjectId or a category type are required.");
        }

        var project = await ResolveProject(request, cancellationToken);
        if (project == null)
        {
            return this.NotFoundWithReason(
                "The SMS project referenced by the category request does not exist.");
        }

        var category = new SmsCategory
        {
            CategoryName = request.CategoryName.Trim(),
            ProjectId = project.ProjectId,
            IsActive = request.IsActive,
            IsEditable = request.IsEditable,
            CreateDate = DateTime.Now
        };

        _context.SmsCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.CategoryId },
            ToDto(category, project.ProjectName));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] SaveSmsCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (HasInvalidValues(request))
        {
            return this.BadRequestWithReason(
                "The SMS category request is invalid. A valid name and either a positive ProjectId or a category type are required.");
        }

        if (id != request.CategoryId)
        {
            return this.BadRequestWithReason(
                "The route category ID does not match the request-body category ID.");
        }

        var existing = await _context.SmsCategories
            .SingleOrDefaultAsync(category => category.CategoryId == id, cancellationToken);
        if (existing == null)
        {
            return this.NotFoundWithReason("The SMS category to update does not exist.");
        }

        var project = await ResolveProject(request, cancellationToken);
        if (project == null)
        {
            return this.NotFoundWithReason(
                "The SMS project referenced by the category request does not exist.");
        }

        existing.CategoryName = request.CategoryName.Trim();
        existing.ProjectId = project.ProjectId;
        existing.IsActive = request.IsActive;
        existing.IsEditable = request.IsEditable;
        existing.UpdateDate = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _context.SmsCategories
            .SingleOrDefaultAsync(candidate => candidate.CategoryId == id, cancellationToken);
        if (category == null)
        {
            return this.NotFoundWithReason("The SMS category to delete does not exist.");
        }

        _context.SmsCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<SmsProject?> ResolveProject(
        SaveSmsCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProjectId.HasValue)
        {
            return await _context.SmsProjects
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    project => project.ProjectId == request.ProjectId.Value,
                    cancellationToken);
        }

        var projectName = request.CategoryType!.Trim();
        return await _context.SmsProjects
            .AsNoTracking()
            .SingleOrDefaultAsync(
                project => project.ProjectName == projectName,
                cancellationToken);
    }

    private static bool HasInvalidValues(SaveSmsCategoryRequest? request)
    {
        return request == null
            || string.IsNullOrWhiteSpace(request.CategoryName)
            || request.CategoryName.Length > 50
            || request.ProjectId is <= 0
            || (!request.ProjectId.HasValue && string.IsNullOrWhiteSpace(request.CategoryType));
    }

    private static SmsCategoryDto ToDto(SmsCategory category, string? projectName) => new()
    {
        CategoryId = category.CategoryId,
        CategoryName = category.CategoryName,
        ProjectId = category.ProjectId,
        CategoryType = projectName,
        IsActive = category.IsActive,
        IsEditable = category.IsEditable,
        CreateDate = category.CreateDate,
        UpdateDate = category.UpdateDate
    };
}
