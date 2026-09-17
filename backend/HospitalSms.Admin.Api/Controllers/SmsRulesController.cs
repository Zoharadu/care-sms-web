using HospitalSms.Admin.Api.Helpers;
using HospitalSms.Admin.Api.Models;
using HospitalSms.Application.Abstractions;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsRulesController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SmsRulesController(
        HospitalSmsDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SmsRuleHierarchyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SmsRuleHierarchyDto>>> GetRules(
        [FromQuery] SmsRulesQuery query,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateScope(
            query.ProjectId,
            query.CategoryId,
            cancellationToken);
        if (validationError != null)
        {
            return validationError;
        }

        var rules = await LoadRules(
            query.ProjectId!.Value,
            query.CategoryId!.Value,
            templateId: null,
            cancellationToken);

        return Ok(rules);
    }

    [HttpGet("by-template/{templateId:int}")]
    [ProducesResponseType(typeof(IEnumerable<SmsRuleHierarchyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SmsRuleHierarchyDto>>> GetRulesByTemplate(
        int templateId,
        [FromQuery] SmsRulesQuery query,
        CancellationToken cancellationToken)
    {
        if (templateId <= 0)
        {
            return this.BadRequestWithReason("TemplateId must contain a positive value.");
        }

        var validationError = await ValidateScope(
            query.ProjectId,
            query.CategoryId,
            cancellationToken);
        if (validationError != null)
        {
            return validationError;
        }

        var templateCategoryId = await _context.SmsTemplates
            .AsNoTracking()
            .Where(template => template.TemplateId == templateId)
            .Select(template => (int?)template.CategoryId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!templateCategoryId.HasValue)
        {
            return this.NotFoundWithReason("The requested SMS template does not exist.");
        }

        if (templateCategoryId.Value != query.CategoryId!.Value)
        {
            return this.BadRequestWithReason(
                "The requested SMS template does not belong to the specified category.");
        }

        var rules = await LoadRules(
            query.ProjectId!.Value,
            query.CategoryId.Value,
            templateId,
            cancellationToken);

        return Ok(rules);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(SmsRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SmsRuleResponse>> CreateRule(
        [FromBody] CreateSmsRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || HasInvalidValues(
                request.TtId,
                request.DependOnTtId,
                request.DependencyMaxMinutes,
                request.MaxTimeTokefInMinutes,
                request.StartTimeRange,
                request.EndTimeRange,
                request.RecurringIntervalDays,
                request.RecurringTimeOfDay,
                request.RecurringStopCondition)
            || HasMissingRequiredRecurringStopCondition(
                request.IsRecurring,
                request.RecurringStopCondition))
        {
            return this.BadRequestWithReason(
                "The SMS rule request contains invalid identifiers, durations, time-range settings, or recurrence settings.");
        }

        var currentUser = _currentUserService.GetRequiredUserName();

        var templateTrigger = await _context.SmsTemplateTriggers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TtId == request.TtId,
                cancellationToken);

        if (templateTrigger == null)
        {
            return this.NotFoundWithReason(
                "The requested template-trigger association does not exist.");
        }

        var existingRuleId = await FindRuleIdForTemplate(
            templateTrigger.TemplateId,
            excludedRuleId: null,
            cancellationToken);

        if (existingRuleId.HasValue)
        {
            return RuleAlreadyExistsConflict(templateTrigger.TemplateId, existingRuleId.Value);
        }

        if (!await DependencyExists(request.DependOnTtId, cancellationToken))
        {
            return this.NotFoundWithReason(
                "The requested dependency template-trigger association does not exist.");
        }

        var rule = new SmsRule
        {
            TtId = request.TtId,
            CreateDate = DateTime.Now,
            UpdateDate = null,
            UpdateUser = currentUser
        };
        ApplyRequest(rule, request);

        _context.SmsRules.Add(rule);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _context.ChangeTracker.Clear();
            var concurrentlyCreatedRuleId = await FindRuleIdForTemplate(
                templateTrigger.TemplateId,
                excludedRuleId: null,
                cancellationToken);

            if (concurrentlyCreatedRuleId.HasValue
                || DatabaseExceptionHelper.IsUniqueConstraintViolation(exception))
            {
                return RuleAlreadyExistsConflict(
                    templateTrigger.TemplateId,
                    concurrentlyCreatedRuleId);
            }

            throw;
        }

        return Ok(ToResponse(rule));
    }

    [HttpPut("{ruleId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(SmsRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SmsRuleResponse>> UpdateRule(
        int ruleId,
        [FromBody] UpdateSmsRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (ruleId <= 0 || request == null || HasInvalidValues(
                request.TtId,
                request.DependOnTtId,
                request.DependencyMaxMinutes,
                request.MaxTimeTokefInMinutes,
                request.StartTimeRange,
                request.EndTimeRange,
                request.RecurringIntervalDays,
                request.RecurringTimeOfDay,
                request.RecurringStopCondition)
            || HasInvalidTimeRangeUpdate(request))
        {
            return this.BadRequestWithReason(
                "The SMS rule update contains an invalid rule identifier, template-trigger identifier, duration, time-range setting, or recurrence setting.");
        }

        var currentUser = _currentUserService.GetRequiredUserName();

        var rule = await _context.SmsRules
            .SingleOrDefaultAsync(candidate => candidate.RuleId == ruleId, cancellationToken);

        if (rule == null)
        {
            return this.NotFoundWithReason("The requested SMS rule does not exist.");
        }

        if (HasInvalidRecurringUpdate(rule, request))
        {
            return this.BadRequestWithReason(
                "Enabled recurrence requires a positive interval, a valid HH:mm time, and the supported stop condition.");
        }

        var templateTrigger = await _context.SmsTemplateTriggers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TtId == request.TtId,
                cancellationToken);

        if (templateTrigger == null)
        {
            return this.NotFoundWithReason(
                "The requested template-trigger association does not exist.");
        }

        if (request.DependOnTtIdSpecified
            && !await DependencyExists(request.DependOnTtId, cancellationToken))
        {
            return this.NotFoundWithReason(
                "The requested dependency template-trigger association does not exist.");
        }

        var existingRuleId = await FindRuleIdForTemplate(
            templateTrigger.TemplateId,
            ruleId,
            cancellationToken);

        if (existingRuleId.HasValue)
        {
            return RuleAlreadyExistsConflict(templateTrigger.TemplateId, existingRuleId.Value);
        }

        ApplyRequest(rule, request);
        rule.UpdateDate = DateTime.Now;
        rule.UpdateUser = currentUser;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _context.ChangeTracker.Clear();
            var conflictingRuleId = await FindRuleIdForTemplate(
                templateTrigger.TemplateId,
                ruleId,
                cancellationToken);

            if (conflictingRuleId.HasValue
                || DatabaseExceptionHelper.IsUniqueConstraintViolation(exception))
            {
                return RuleAlreadyExistsConflict(templateTrigger.TemplateId, conflictingRuleId);
            }

            throw;
        }

        return Ok(ToResponse(rule));
    }

    private async Task<int?> FindRuleIdForTemplate(
        int templateId,
        int? excludedRuleId,
        CancellationToken cancellationToken)
    {
        return await (
            from templateTrigger in _context.SmsTemplateTriggers.AsNoTracking()
            join rule in _context.SmsRules.AsNoTracking()
                on templateTrigger.TtId equals rule.TtId
            where templateTrigger.TemplateId == templateId
                  && (!excludedRuleId.HasValue || rule.RuleId != excludedRuleId.Value)
            select (int?)rule.RuleId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<ActionResult?> ValidateScope(
        int? projectId,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        if (!projectId.HasValue
            || !categoryId.HasValue
            || projectId.Value <= 0
            || categoryId.Value <= 0)
        {
            return this.BadRequestWithReason(
                "ProjectId and CategoryId must both contain positive values.");
        }

        if (!await _context.SmsProjects
            .AsNoTracking()
            .AnyAsync(project => project.ProjectId == projectId.Value, cancellationToken))
        {
            return this.NotFoundWithReason("The requested SMS project does not exist.");
        }

        var categoryProjectId = await _context.SmsCategories
            .AsNoTracking()
            .Where(category => category.CategoryId == categoryId.Value)
            .Select(category => (int?)category.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!categoryProjectId.HasValue)
        {
            return this.NotFoundWithReason("The requested SMS category does not exist.");
        }

        if (categoryProjectId.Value != projectId.Value)
        {
            return this.BadRequestWithReason(
                "The requested SMS category does not belong to the specified project.");
        }

        return null;
    }

    private async Task<List<SmsRuleHierarchyDto>> LoadRules(
        int projectId,
        int categoryId,
        int? templateId,
        CancellationToken cancellationToken)
    {
        var rulesQuery =
            from project in _context.SmsProjects.AsNoTracking()
            join category in _context.SmsCategories.AsNoTracking()
                on project.ProjectId equals category.ProjectId
            join template in _context.SmsTemplates.AsNoTracking()
                on category.CategoryId equals template.CategoryId
            join templateTrigger in _context.SmsTemplateTriggers.AsNoTracking()
                on template.TemplateId equals templateTrigger.TemplateId
            join trigger in _context.SmsTriggerCatalogs.AsNoTracking()
                on templateTrigger.TriggerId equals trigger.TriggerId
            join rule in _context.SmsRules.AsNoTracking()
                on templateTrigger.TtId equals rule.TtId into matchingRules
            from rule in matchingRules.DefaultIfEmpty()
            where project.ProjectId == projectId
                  && category.CategoryId == categoryId
                  && (!templateId.HasValue || template.TemplateId == templateId.Value)
            orderby template.TemplateId,
                templateTrigger.TtId,
                rule == null ? (int?)null : rule.RuleId
            select new SmsRuleHierarchyDto
            {
                ProjectId = project.ProjectId,
                Category = new SmsRuleCategoryDetailsDto
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    IsActive = category.IsActive,
                    IsEditable = category.IsEditable
                },
                Template = new SmsRuleTemplateDetailsDto
                {
                    TemplateId = template.TemplateId,
                    TemplateCode = template.TemplateCode,
                    TemplateName = template.TemplateName,
                    CategoryId = template.CategoryId,
                    IsActive = template.IsActive,
                    IsEditable = template.IsEditable,
                    VersionNumber = template.VersionNumber,
                    UpdateUser = template.UpdateUser
                },
                TemplateTrigger = new SmsRuleTemplateTriggerDetailsDto
                {
                    TtId = templateTrigger.TtId,
                    TriggerId = trigger.TriggerId,
                    TriggerCode = trigger.TriggerCode,
                    TriggerName = trigger.TriggerName,
                    TriggerDescription = trigger.Description,
                    TriggerIsActive = trigger.IsActive,
                    CreateDate = templateTrigger.CreateDate,
                    UpdateDate = templateTrigger.UpdateDate,
                    UpdateUser = templateTrigger.UpdateUser
                },
                Rule = rule == null
                    ? null
                    : new SmsRuleResponse
                    {
                        RuleId = rule.RuleId,
                        TtId = rule.TtId,
                        DependOnTtId = rule.DependOnTtId,
                        DependencyMaxMinutes = rule.DependencyMaxMinutes,
                        MaxTimeTokefInMinutes = rule.MaxTimeTokefInMinutes,
                        IsConstant = rule.IsConstant,
                        StartTimeRange = rule.StartTimeRange,
                        EndTimeRange = rule.EndTimeRange,
                        IsRecurring = rule.IsRecurring,
                        RecurringIntervalDays = rule.RecurringIntervalDays,
                        RecurringTimeOfDay = rule.RecurringTimeOfDay,
                        RecurringStopCondition = rule.RecurringStopCondition,
                        CreateDate = rule.CreateDate,
                        UpdateDate = rule.UpdateDate,
                        UpdateUser = rule.UpdateUser
                    }
            };

        var rules = await rulesQuery.ToListAsync(cancellationToken);
        if (rules.Count == 0)
        {
            return rules;
        }

        var exclusions = await (
            from project in _context.SmsProjects.AsNoTracking()
            join category in _context.SmsCategories.AsNoTracking()
                on project.ProjectId equals category.ProjectId
            join template in _context.SmsTemplates.AsNoTracking()
                on category.CategoryId equals template.CategoryId
            join reject in _context.SmsRejectHospitalUnits.AsNoTracking()
                on template.TemplateId equals reject.TemplateId
            join hospitalCandidate in _context.SmsHospitals.AsNoTracking()
                on reject.HospitalId equals hospitalCandidate.HospitalId into hospitals
            from hospital in hospitals.DefaultIfEmpty()
            join unitCandidate in _context.SmsUnits.AsNoTracking()
                on new { reject.HospitalId, reject.UnitId }
                equals new { unitCandidate.HospitalId, unitCandidate.UnitId } into units
            from unit in units.DefaultIfEmpty()
            where project.ProjectId == projectId
                  && category.CategoryId == categoryId
                  && (!templateId.HasValue || template.TemplateId == templateId.Value)
                  && _context.SmsTemplateTriggers.Any(
                      templateTrigger => templateTrigger.TemplateId == template.TemplateId)
            orderby template.TemplateId, reject.HospitalId, reject.UnitId
            select new
            {
                template.TemplateId,
                Item = new ExcludedHospitalUnitDto
                {
                    HospitalId = reject.HospitalId,
                    HospitalName = hospital == null ? null : hospital.HospitalName,
                    UnitId = reject.UnitId,
                    UnitName = unit == null ? null : unit.UnitName
                }
            })
            .ToListAsync(cancellationToken);

        var exclusionsByTemplate = exclusions
            .GroupBy(exclusion => exclusion.TemplateId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(exclusion => exclusion.Item).ToList());

        foreach (var rule in rules)
        {
            if (exclusionsByTemplate.TryGetValue(rule.Template.TemplateId, out var items))
            {
                rule.ExcludedHospitalUnits = items;
            }
        }

        return rules;
    }

    private async Task<bool> DependencyExists(
        int? dependOnTtId,
        CancellationToken cancellationToken)
    {
        return !dependOnTtId.HasValue
            || await _context.SmsTemplateTriggers
                .AsNoTracking()
                .AnyAsync(
                    templateTrigger => templateTrigger.TtId == dependOnTtId.Value,
                    cancellationToken);
    }

    private ConflictObjectResult RuleAlreadyExistsConflict(int templateId, int? ruleId)
    {
        var detail = ruleId.HasValue
            ? $"Template {templateId} already has rule {ruleId.Value}. Update it with PUT /api/SmsRules/{ruleId.Value}."
            : $"Template {templateId} already has a rule. Update the existing rule with PUT /api/SmsRules/{{ruleId}}.";

        return Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "A rule already exists for this template.",
            Detail = detail
        });
    }

    private static void ApplyRequest(SmsRule rule, CreateSmsRuleRequest request)
    {
        rule.DependOnTtId = request.DependOnTtId;
        rule.DependencyMaxMinutes = request.DependencyMaxMinutes;
        rule.MaxTimeTokefInMinutes = request.MaxTimeTokefInMinutes;
        rule.IsConstant = request.IsConstant;
        rule.StartTimeRange = request.StartTimeRange;
        rule.EndTimeRange = request.EndTimeRange;
        rule.IsRecurring = request.IsRecurring;
        rule.RecurringIntervalDays = request.RecurringIntervalDays;
        rule.RecurringTimeOfDay = request.RecurringTimeOfDay;
        rule.RecurringStopCondition = request.RecurringStopCondition;
    }

    private static void ApplyRequest(SmsRule rule, UpdateSmsRuleRequest request)
    {
        rule.TtId = request.TtId;

        if (request.DependOnTtIdSpecified)
        {
            rule.DependOnTtId = request.DependOnTtId;

            if (!request.DependOnTtId.HasValue && !request.DependencyMaxMinutesSpecified)
            {
                rule.DependencyMaxMinutes = 0;
            }
        }

        if (request.DependencyMaxMinutesSpecified)
        {
            rule.DependencyMaxMinutes = request.DependencyMaxMinutes;
        }

        if (request.MaxTimeTokefInMinutesSpecified)
        {
            rule.MaxTimeTokefInMinutes = request.MaxTimeTokefInMinutes;
        }

        if (request.IsConstantSpecified)
        {
            rule.IsConstant = request.IsConstant;
        }

        if (request.StartTimeRangeSpecified && request.EndTimeRangeSpecified)
        {
            rule.StartTimeRange = request.StartTimeRange;
            rule.EndTimeRange = request.EndTimeRange;
        }

        if (request.IsRecurringSpecified)
        {
            rule.IsRecurring = request.IsRecurring;
        }

        if (request.RecurringIntervalDaysSpecified)
        {
            rule.RecurringIntervalDays = request.RecurringIntervalDays;
        }

        if (request.RecurringTimeOfDaySpecified)
        {
            rule.RecurringTimeOfDay = request.RecurringTimeOfDay;
        }

        if (request.RecurringStopConditionSpecified)
        {
            rule.RecurringStopCondition = request.RecurringStopCondition;
        }

    }

    private static SmsRuleResponse ToResponse(SmsRule rule) => new()
    {
        RuleId = rule.RuleId,
        TtId = rule.TtId,
        DependOnTtId = rule.DependOnTtId,
        DependencyMaxMinutes = rule.DependencyMaxMinutes,
        MaxTimeTokefInMinutes = rule.MaxTimeTokefInMinutes,
        IsConstant = rule.IsConstant,
        StartTimeRange = rule.StartTimeRange,
        EndTimeRange = rule.EndTimeRange,
        IsRecurring = rule.IsRecurring,
        RecurringIntervalDays = rule.RecurringIntervalDays,
        RecurringTimeOfDay = rule.RecurringTimeOfDay,
        RecurringStopCondition = rule.RecurringStopCondition,
        CreateDate = rule.CreateDate,
        UpdateDate = rule.UpdateDate,
        UpdateUser = rule.UpdateUser
    };

    private static bool HasInvalidValues(
        int ttId,
        int? dependOnTtId,
        double? dependencyMaxMinutes,
        double? maxTimeTokefInMinutes,
        string? startTimeRange,
        string? endTimeRange,
        int? recurringIntervalDays,
        string? recurringTimeOfDay,
        string? recurringStopCondition)
    {
        return ttId <= 0
            || dependOnTtId is <= 0
            || IsInvalidMinutes(dependencyMaxMinutes)
            || IsInvalidMinutes(maxTimeTokefInMinutes)
            || HasInvalidTimeRange(startTimeRange, endTimeRange)
            || recurringIntervalDays is <= 0
            || IsInvalidTime(recurringTimeOfDay)
            || IsInvalidRecurringStopCondition(recurringStopCondition);
    }

    private static bool HasInvalidTimeRangeUpdate(UpdateSmsRuleRequest request)
    {
        return request.StartTimeRangeSpecified != request.EndTimeRangeSpecified
            || (request.StartTimeRangeSpecified
                && HasInvalidTimeRange(request.StartTimeRange, request.EndTimeRange));
    }

    private static bool HasInvalidTimeRange(string? startTimeRange, string? endTimeRange)
    {
        return (startTimeRange == null) != (endTimeRange == null)
            || IsInvalidTime(startTimeRange)
            || IsInvalidTime(endTimeRange);
    }

    private static bool HasInvalidRecurringUpdate(
        SmsRule rule,
        UpdateSmsRuleRequest request)
    {
        if (!request.HasRecurringChanges)
        {
            return false;
        }

        var isRecurring = request.IsRecurringSpecified
            ? request.IsRecurring
            : rule.IsRecurring;

        if (isRecurring != true)
        {
            return false;
        }

        var intervalDays = request.RecurringIntervalDaysSpecified
            ? request.RecurringIntervalDays
            : rule.RecurringIntervalDays;
        var timeOfDay = request.RecurringTimeOfDaySpecified
            ? request.RecurringTimeOfDay
            : rule.RecurringTimeOfDay;
        var stopCondition = request.RecurringStopConditionSpecified
            ? request.RecurringStopCondition
            : rule.RecurringStopCondition;

        return intervalDays is null or <= 0
            || string.IsNullOrEmpty(timeOfDay)
            || IsInvalidTime(timeOfDay)
            || string.IsNullOrEmpty(stopCondition)
            || IsInvalidRecurringStopCondition(stopCondition);
    }

    private static bool IsInvalidTime(string? value)
    {
        return value != null
            && !TimeOnly.TryParseExact(
                value,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _);
    }

    private static bool IsInvalidRecurringStopCondition(string? value)
    {
        return value != null
            && !string.Equals(
                value,
                SmsRuleStopConditions.Discharged,
                StringComparison.Ordinal)
            && !string.Equals(
                value,
                SmsRuleStopConditions.Never,
                StringComparison.Ordinal);
    }

    private static bool HasMissingRequiredRecurringStopCondition(
        bool? isRecurring,
        string? recurringStopCondition)
    {
        return isRecurring == true && string.IsNullOrEmpty(recurringStopCondition);
    }

    private static bool IsInvalidMinutes(double? value)
    {
        return value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0);
    }
}
