using HospitalSms.Application.Dtos;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public RoutesController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SmsCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SmsCategoryDto>>> GetRoutes(
        CancellationToken cancellationToken)
    {
        var categories = await CategoryQuery()
            .OrderBy(category => category.CategoryId)
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SmsCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SmsCategoryDto>> GetRouteById(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await CategoryQuery()
            .SingleOrDefaultAsync(candidate => candidate.CategoryId == id, cancellationToken);

        if (category == null)
        {
            return this.NotFoundWithReason("The requested route category does not exist.");
        }

        return Ok(category);
    }

    private IQueryable<SmsCategoryDto> CategoryQuery()
    {
        return
            from category in _context.SmsCategories.AsNoTracking()
            join project in _context.SmsProjects.AsNoTracking()
                on category.ProjectId equals project.ProjectId into projects
            from project in projects.DefaultIfEmpty()
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
            };
    }
}
