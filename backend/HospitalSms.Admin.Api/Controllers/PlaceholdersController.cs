using HospitalSms.Admin.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalSms.Infrastructure.Data;
using HospitalSms.Domain.Entities;
using HospitalSms.Application.Dtos;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/placeholders")]
public class PlaceholdersController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public PlaceholdersController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlaceholderCatalogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<PlaceholderCatalogDto>>> GetPlaceholders(
        [FromQuery] PlaceholderQuery query,
        CancellationToken cancellationToken)
    {
        var hasProjectId = query.ProjectId.HasValue;
        var hasCategoryId = query.CategoryId.HasValue;

        if (hasProjectId != hasCategoryId
            || query.ProjectId is <= 0
            || query.CategoryId is <= 0)
        {
            return this.BadRequestWithReason(
                "ProjectId and CategoryId must either both be omitted or both contain positive values.");
        }

        var placeholdersQuery = _context.SmsPlaceholderCatalogs.AsNoTracking();

        if (hasProjectId)
        {
            var projectId = query.ProjectId!.Value;
            var categoryId = query.CategoryId!.Value;

            if (!await _context.SmsProjects.AsNoTracking().AnyAsync(
                project => project.ProjectId == projectId,
                cancellationToken))
            {
                return this.NotFoundWithReason("The requested SMS project does not exist.");
            }

            var categoryProjectId = await _context.SmsCategories
                .AsNoTracking()
                .Where(category => category.CategoryId == categoryId)
                .Select(category => (int?)category.ProjectId)
                .SingleOrDefaultAsync(cancellationToken);

            if (!categoryProjectId.HasValue)
            {
                return this.NotFoundWithReason("The requested SMS category does not exist.");
            }

            if (categoryProjectId.Value != projectId)
            {
                return this.BadRequestWithReason(
                    "The requested SMS category does not belong to the requested SMS project.");
            }

            placeholdersQuery = placeholdersQuery.Where(placeholder =>
                _context.SmsPlaceholderScopes.Any(scope =>
                    scope.PlaceholderId == placeholder.PlaceholderId
                    && scope.ProjectId == projectId
                    && scope.IsActive
                    && (scope.CategoryId == null || scope.CategoryId == categoryId)));
        }

        var placeholders = await placeholdersQuery
            .OrderBy(placeholder => placeholder.PlaceholderId)
            .Select(placeholder => new PlaceholderCatalogDto
            {
                PlaceholderId = placeholder.PlaceholderId,
                PlaceholderName = placeholder.PlaceholderName,
                DisplayName = placeholder.DisplayName
            })
            .ToListAsync(cancellationToken);

        return Ok(placeholders);
    }

    [HttpPost]
    public async Task<ActionResult<SmsPlaceholderCatalog>> CreatePlaceholder(SmsPlaceholderCatalog placeholder)
    {
        placeholder.CreateDate = DateTime.Now;
        _context.SmsPlaceholderCatalogs.Add(placeholder);
        await _context.SaveChangesAsync();
        return Ok(placeholder);
    }
}
