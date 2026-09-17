using HospitalSms.Domain.Entities;
using HospitalSms.Application.Dtos;
using HospitalSms.Application.Abstractions;
using HospitalSms.Admin.Api.Helpers;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsTriggersController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SmsTriggersController(
        HospitalSmsDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet("by-template/{templateId:int}")]
    [ProducesResponseType(typeof(IEnumerable<TemplateClinicalTriggerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<TemplateClinicalTriggerDto>>> GetTriggerByTemplate(
        int templateId,
        CancellationToken cancellationToken)
    {
        if (templateId <= 0)
        {
            return this.BadRequestWithReason("TemplateId must contain a positive value.");
        }

        var triggers = await (
            from templateTrigger in _context.SmsTemplateTriggers.AsNoTracking()
            join triggerCatalog in _context.SmsTriggerCatalogs.AsNoTracking()
                on templateTrigger.TriggerId equals triggerCatalog.TriggerId
            where templateTrigger.TemplateId == templateId
            orderby templateTrigger.TtId
            select new TemplateClinicalTriggerDto
            {
                TtId = templateTrigger.TtId,
                TemplateId = templateTrigger.TemplateId,
                TriggerId = triggerCatalog.TriggerId,
                TriggerName = triggerCatalog.TriggerName,
                TriggerCode = triggerCatalog.TriggerCode,
                Description = triggerCatalog.Description,
                IsActive = triggerCatalog.IsActive,
                UpdateUser = templateTrigger.UpdateUser
            })
            .ToListAsync(cancellationToken);

        return Ok(triggers);
    }

    [HttpPost("template-trigger")]
    [Authorize]
    [ProducesResponseType(typeof(SmsTemplateTriggerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SmsTemplateTriggerResponse>> CreateTemplateTrigger(
        [FromBody] CreateSmsTemplateTriggerRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.TemplateId <= 0 || request.TriggerId <= 0)
        {
            return this.BadRequestWithReason(
                "The template-trigger request must contain positive TemplateId and TriggerId values.");
        }

        var currentUser = _currentUserService.GetRequiredUserName();

        var templateExists = await _context.SmsTemplates
            .AsNoTracking()
            .AnyAsync(template => template.TemplateId == request.TemplateId, cancellationToken);
        var triggerExists = await _context.SmsTriggerCatalogs
            .AsNoTracking()
            .AnyAsync(trigger => trigger.TriggerId == request.TriggerId, cancellationToken);

        if (!templateExists || !triggerExists)
        {
            var reason = !templateExists && !triggerExists
                ? "The requested SMS template and trigger do not exist."
                : !templateExists
                    ? "The requested SMS template does not exist."
                    : "The requested SMS trigger does not exist.";

            return this.NotFoundWithReason(reason);
        }

        var associationExists = await _context.SmsTemplateTriggers
            .AsNoTracking()
            .AnyAsync(
                templateTrigger => templateTrigger.TemplateId == request.TemplateId,
                cancellationToken);

        if (associationExists)
        {
            return TemplateTriggerAlreadyExistsConflict(request.TemplateId);
        }

        var templateTrigger = new SmsTemplateTrigger
        {
            TemplateId = request.TemplateId,
            TriggerId = request.TriggerId,
            CreateDate = DateTime.Now,
            UpdateDate = null,
            UpdateUser = currentUser
        };

        _context.SmsTemplateTriggers.Add(templateTrigger);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _context.ChangeTracker.Clear();
            var concurrentlyCreatedAssociationExists = await _context.SmsTemplateTriggers
                .AsNoTracking()
                .AnyAsync(
                    candidate => candidate.TemplateId == request.TemplateId,
                    cancellationToken);

            if (concurrentlyCreatedAssociationExists
                || DatabaseExceptionHelper.IsUniqueConstraintViolation(exception))
            {
                return TemplateTriggerAlreadyExistsConflict(request.TemplateId);
            }

            throw;
        }

        var response = new SmsTemplateTriggerResponse
        {
            TtId = templateTrigger.TtId,
            TemplateId = templateTrigger.TemplateId,
            TriggerId = templateTrigger.TriggerId,
            CreateDate = templateTrigger.CreateDate,
            UpdateDate = templateTrigger.UpdateDate,
            UpdateUser = templateTrigger.UpdateUser
        };

        return CreatedAtAction(
            nameof(GetTriggerByTemplate),
            new { templateId = templateTrigger.TemplateId },
            response);
    }

    [HttpPut("template-trigger/{templateId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTemplateTrigger(
        int templateId,
        [FromBody] UpdateSmsTemplateTriggerRequest request,
        CancellationToken cancellationToken)
    {
        if (templateId <= 0)
        {
            return this.BadRequestWithReason("TemplateId must contain a positive value.");
        }

        if (request == null || request.TriggerId <= 0)
        {
            return this.BadRequestWithReason("TriggerId must contain a positive value.");
        }

        var currentUser = _currentUserService.GetRequiredUserName();

        var templateExists = await _context.SmsTemplates
            .AsNoTracking()
            .AnyAsync(template => template.TemplateId == templateId, cancellationToken);

        if (!templateExists)
        {
            return this.NotFoundWithReason("The requested SMS template does not exist.");
        }

        var trigger = await _context.SmsTriggerCatalogs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TriggerId == request.TriggerId,
                cancellationToken);

        if (trigger == null)
        {
            return this.NotFoundWithReason("The requested SMS trigger does not exist.");
        }

        if (!trigger.IsActive)
        {
            return this.NotFoundWithReason("The requested SMS trigger is inactive.");
        }

        var associations = await _context.SmsTemplateTriggers
            .Where(templateTrigger => templateTrigger.TemplateId == templateId)
            .OrderBy(templateTrigger => templateTrigger.TtId)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (associations.Count == 0)
        {
            return this.NotFoundWithReason(
                "No template-trigger association exists for the requested SMS template.");
        }

        if (associations.Count > 1)
        {
            return AmbiguousTemplateTriggerConflict(templateId);
        }

        var association = associations[0];
        if (association.TriggerId == request.TriggerId)
        {
            return NoContent();
        }

        association.TriggerId = request.TriggerId;
        association.UpdateDate = DateTime.Now;
        association.UpdateUser = currentUser;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("catalog")]
    public async Task<ActionResult<IEnumerable<SmsTriggerCatalog>>> GetTriggerCatalog()
    {
        return await _context.SmsTriggerCatalogs.Where(c => c.IsActive).ToListAsync();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SmsTriggerSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SmsTriggerSettingsDto>>> GetTriggers(
        CancellationToken cancellationToken)
    {
        var triggerSettingsQuery =
            from templateTrigger in _context.SmsTemplateTriggers.AsNoTracking()
            join triggerCatalog in _context.SmsTriggerCatalogs.AsNoTracking()
                on templateTrigger.TriggerId equals triggerCatalog.TriggerId
            join template in _context.SmsTemplates.AsNoTracking()
                on templateTrigger.TemplateId equals template.TemplateId
            join rule in _context.SmsRules.AsNoTracking()
                on templateTrigger.TtId equals rule.TtId into rules
            from rule in rules.DefaultIfEmpty()
            select new SmsTriggerSettingsDto
            {
                RuleId = rule == null ? null : rule.RuleId,
                TtId = templateTrigger.TtId,
                TemplateId = template.TemplateId,
                TemplateUpdateUser = template.UpdateUser,
                TriggerId = triggerCatalog.TriggerId,
                TriggerCode = triggerCatalog.TriggerCode,
                TriggerName = triggerCatalog.TriggerName,
                TriggerDescription = triggerCatalog.Description,
                TriggerIsActive = triggerCatalog.IsActive,
                TemplateTriggerUpdateUser = templateTrigger.UpdateUser,
                DependOnTtId = rule == null ? null : rule.DependOnTtId,
                DependencyMaxMinutes = rule == null ? null : rule.DependencyMaxMinutes,
                MaxTimeTokefInMinutes = rule == null ? null : rule.MaxTimeTokefInMinutes,
                IsConstant = rule == null ? null : rule.IsConstant,
                StartTimeRange = rule == null ? null : rule.StartTimeRange,
                EndTimeRange = rule == null ? null : rule.EndTimeRange,
                IsRecurring = rule == null ? null : rule.IsRecurring,
                RecurringIntervalDays = rule == null ? null : rule.RecurringIntervalDays,
                RecurringTimeOfDay = rule == null ? null : rule.RecurringTimeOfDay,
                RecurringStopCondition = rule == null ? null : rule.RecurringStopCondition,
                CreateDate = rule == null ? null : rule.CreateDate,
                UpdateDate = rule == null ? null : rule.UpdateDate,
                UpdateUser = rule == null ? null : rule.UpdateUser
            };

        var triggerSettings = await triggerSettingsQuery
            .OrderBy(setting => setting.TemplateId)
            .ThenBy(setting => setting.TtId)
            .ThenBy(setting => setting.RuleId)
            .ToListAsync(cancellationToken);

        return Ok(triggerSettings);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult<SmsTrigger> GetTrigger(int id) => LegacyTriggerEndpointRemoved();

    [HttpPost]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult<SmsTrigger> CreateTrigger([FromBody] SmsTrigger trigger) =>
        LegacyTriggerEndpointRemoved();

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult UpdateTrigger(int id, [FromBody] SmsTrigger trigger) =>
        LegacyTriggerEndpointRemoved();

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult DeleteTrigger(int id) => LegacyTriggerEndpointRemoved();

    private ObjectResult LegacyTriggerEndpointRemoved()
    {
        return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
        {
            Status = StatusCodes.Status410Gone,
            Title = "The legacy SMS trigger resource no longer exists.",
            Detail = "Use /api/SmsTriggers/template-trigger and /api/SmsRules with dbo.sms_template_trigger, dbo.sms_trigger_catalog and dbo.sms_rules."
        });
    }

    private ConflictObjectResult TemplateTriggerAlreadyExistsConflict(int templateId)
    {
        return Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "A template-trigger association already exists for this template.",
            Detail = $"Template {templateId} can have only one trigger association."
        });
    }

    private ConflictObjectResult AmbiguousTemplateTriggerConflict(int templateId)
    {
        return Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Multiple template-trigger associations exist for this template.",
            Detail = $"Template {templateId} has more than one trigger association, so the association to update is ambiguous."
        });
    }
}
