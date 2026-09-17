using HospitalSms.Admin.Api.Models;
using HospitalSms.Application.Abstractions;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/SmsTemplates")]
public class SmsTemplatesController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SmsTemplatesController(
        HospitalSmsDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SmsTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SmsTemplateDto>>> GetTemplates(
        [FromQuery] SmsTemplateQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ProjectId is <= 0 || query.CategoryId is <= 0)
        {
            return this.BadRequestWithReason(
                "ProjectId and CategoryId must contain positive values when supplied.");
        }

        if (query.ProjectId.HasValue
            && !await _context.SmsProjects.AsNoTracking().AnyAsync(
                project => project.ProjectId == query.ProjectId.Value,
                cancellationToken))
        {
            return this.NotFoundWithReason("The requested SMS project does not exist.");
        }

        int? categoryProjectId = null;
        if (query.CategoryId.HasValue)
        {
            categoryProjectId = await _context.SmsCategories
                .AsNoTracking()
                .Where(category => category.CategoryId == query.CategoryId.Value)
                .Select(category => (int?)category.ProjectId)
                .SingleOrDefaultAsync(cancellationToken);

            if (!categoryProjectId.HasValue)
            {
                return this.NotFoundWithReason("The requested SMS category does not exist.");
            }
        }

        if (query.ProjectId.HasValue
            && categoryProjectId.HasValue
            && query.ProjectId.Value != categoryProjectId.Value)
        {
            return this.BadRequestWithReason(
                "The requested SMS category does not belong to the requested SMS project.");
        }

        var templatesQuery = _context.SmsTemplates.AsNoTracking();

        if (query.CategoryId.HasValue)
        {
            templatesQuery = templatesQuery.Where(
                template => template.CategoryId == query.CategoryId.Value);
        }

        if (query.ProjectId.HasValue)
        {
            templatesQuery = templatesQuery.Where(template =>
                _context.SmsCategories.Any(category =>
                    category.CategoryId == template.CategoryId
                    && category.ProjectId == query.ProjectId.Value));
        }

        if (query.IsActive.HasValue)
        {
            templatesQuery = templatesQuery.Where(
                template => template.IsActive == query.IsActive.Value);
        }

        var templates = await templatesQuery
            .OrderBy(template => template.TemplateId)
            .Select(ToDtoExpression)
            .ToListAsync(cancellationToken);

        return Ok(templates);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SmsTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SmsTemplateDto>> GetTemplate(
        int id,
        CancellationToken cancellationToken)
    {
        var template = await _context.SmsTemplates
            .AsNoTracking()
            .Where(candidate => candidate.TemplateId == id)
            .Select(ToDtoExpression)
            .SingleOrDefaultAsync(cancellationToken);

        if (template == null)
        {
            return this.NotFoundWithReason("The requested SMS template does not exist.");
        }

        return Ok(template);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(SmsTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SmsTemplateDto>> CreateTemplate(
        [FromBody] CreateSmsTemplateDto request,
        CancellationToken cancellationToken)
    {
        if (HasInvalidValues(request))
        {
            return this.BadRequestWithReason(
                "The SMS template request is invalid. A valid name and positive CategoryId are required, and text fields must be within their maximum lengths.");
        }

        var currentUser = _currentUserService.GetRequiredUserName();

        var template = new SmsTemplate
        {
            TemplateCode = NormalizeDescription(request.Description),
            TemplateName = request.TemplateName.Trim(),
            CategoryId = request.CategoryId,
            IsActive = request.IsActive,
            IsEditable = request.IsEditable,
            VersionNumber = request.VersionNumber,
            CreateDate = DateTime.Now,
            UpdateUser = currentUser
        };

        _context.SmsTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetTemplate),
            new { id = template.TemplateId },
            ToDto(template));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(
        int id,
        [FromBody] UpdateSmsTemplateDto request,
        CancellationToken cancellationToken)
    {
        if (HasInvalidValues(request))
        {
            return this.BadRequestWithReason(
                "The SMS template request is invalid. A valid name and positive CategoryId are required, and text fields must be within their maximum lengths.");
        }

        if (id != request.TemplateId)
        {
            return this.BadRequestWithReason(
                "The route template ID does not match the request-body template ID.");
        }

        var currentUser = _currentUserService.GetRequiredUserName();

        var existing = await _context.SmsTemplates
            .SingleOrDefaultAsync(template => template.TemplateId == id, cancellationToken);
        if (existing == null)
        {
            return this.NotFoundWithReason("The SMS template to update does not exist.");
        }

        existing.TemplateName = request.TemplateName.Trim();
        existing.TemplateCode = NormalizeDescription(request.Description);
        existing.CategoryId = request.CategoryId;
        existing.IsActive = request.IsActive;
        existing.IsEditable = request.IsEditable;
        existing.VersionNumber = request.VersionNumber;
        existing.UpdateDate = DateTime.Now;
        existing.UpdateUser = currentUser;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTemplate(
        int id,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        var template = await _context.SmsTemplates
            .SingleOrDefaultAsync(candidate => candidate.TemplateId == id, cancellationToken);
        if (template == null)
        {
            return this.NotFoundWithReason("The SMS template to delete does not exist.");
        }

        var templateTriggerIds = await _context.SmsTemplateTriggers
            .Where(templateTrigger => templateTrigger.TemplateId == id)
            .Select(templateTrigger => templateTrigger.TtId)
            .ToListAsync(cancellationToken);
        var rules = await _context.SmsRules
            .Where(rule => templateTriggerIds.Contains(rule.TtId))
            .ToListAsync(cancellationToken);

        _context.SmsRules.RemoveRange(rules);
        await _context.SaveChangesAsync(cancellationToken);

        var templateTriggers = await _context.SmsTemplateTriggers
            .Where(templateTrigger => templateTrigger.TemplateId == id)
            .ToListAsync(cancellationToken);
        var templateLanguages = await _context.SmsTemplateLanguages
            .Where(language => language.TemplateId == id)
            .ToListAsync(cancellationToken);
        var rejectedHospitalUnits = await _context.SmsRejectHospitalUnits
            .Where(reject => reject.TemplateId == id)
            .ToListAsync(cancellationToken);

        _context.SmsTemplateTriggers.RemoveRange(templateTriggers);
        _context.SmsTemplateLanguages.RemoveRange(templateLanguages);
        _context.SmsRejectHospitalUnits.RemoveRange(rejectedHospitalUnits);
        _context.SmsTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    private static SmsTemplateDto ToDto(SmsTemplate template) => new()
    {
        TemplateId = template.TemplateId,
        TemplateCode = template.TemplateCode,
        TemplateName = template.TemplateName,
        Description = template.TemplateCode ?? string.Empty,
        CategoryId = template.CategoryId,
        IsActive = template.IsActive,
        IsEditable = template.IsEditable,
        VersionNumber = template.VersionNumber,
        CreateDate = template.CreateDate,
        UpdateDate = template.UpdateDate,
        UpdateUser = template.UpdateUser
    };

    private static readonly Expression<Func<SmsTemplate, SmsTemplateDto>> ToDtoExpression =
        template => new SmsTemplateDto
        {
            TemplateId = template.TemplateId,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            Description = template.TemplateCode ?? string.Empty,
            CategoryId = template.CategoryId,
            IsActive = template.IsActive,
            IsEditable = template.IsEditable,
            VersionNumber = template.VersionNumber,
            CreateDate = template.CreateDate,
            UpdateDate = template.UpdateDate,
            UpdateUser = template.UpdateUser
        };

    private static bool HasInvalidValues(CreateSmsTemplateDto? request)
    {
        return request == null
            || string.IsNullOrWhiteSpace(request.TemplateName)
            || request.TemplateName.Length > 100
            || request.TemplateCode?.Length > 300
            || request.Description?.Trim().Length > 300
            || request.CategoryId <= 0;
    }

    private static bool HasInvalidValues(UpdateSmsTemplateDto? request)
    {
        return request == null
            || string.IsNullOrWhiteSpace(request.TemplateName)
            || request.TemplateName.Length > 100
            || request.TemplateCode?.Length > 300
            || request.Description?.Trim().Length > 300
            || request.CategoryId <= 0;
    }

    private static string NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
}
