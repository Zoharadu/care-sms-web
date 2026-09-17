using HospitalSms.Application.Dtos;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/SmsProjects")]
public class SmsProjectsController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public SmsProjectsController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SmsProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SmsProjectDto>>> GetProjects(
        CancellationToken cancellationToken)
    {
        var projects = await _context.SmsProjects
            .AsNoTracking()
            .OrderBy(project => project.ProjectId)
            .Select(project => new SmsProjectDto
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                IsActive = project.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(projects);
    }
}
