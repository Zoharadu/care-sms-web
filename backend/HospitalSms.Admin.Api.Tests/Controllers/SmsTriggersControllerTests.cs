using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class SmsTriggersControllerTests
{
    [Fact]
    public async Task CreateTemplateTrigger_ValidRequest_CreatesAssociation()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(123));
        context.SmsTriggerCatalogs.Add(CreateCatalog(
            4,
            "PATIENT_DISCHARGED",
            "Patient discharged"));
        await context.SaveChangesAsync();

        var result = await CreateController(context).CreateTemplateTrigger(
            new CreateSmsTemplateTriggerRequest
            {
                TemplateId = 123,
                TriggerId = 4
            },
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<SmsTemplateTriggerResponse>(createdResult.Value);
        Assert.True(response.TtId > 0);
        Assert.Equal(123, response.TemplateId);
        Assert.Equal(4, response.TriggerId);
        Assert.Single(await context.SmsTemplateTriggers.ToListAsync());
    }

    [Fact]
    public async Task CreateTemplateTrigger_SecondAssociationForTemplate_ReturnsConflict()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(123));
        context.SmsTriggerCatalogs.AddRange(
            CreateCatalog(4, "PATIENT_DISCHARGED", "Patient discharged"),
            CreateCatalog(5, "PATIENT_ADMITTED", "Patient admitted"));
        context.SmsTemplateTriggers.Add(CreateAssociation(10, 123, 4));
        await context.SaveChangesAsync();

        var result = await CreateController(context).CreateTemplateTrigger(
            new CreateSmsTemplateTriggerRequest
            {
                TemplateId = 123,
                TriggerId = 5
            },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Single(await context.SmsTemplateTriggers.ToListAsync());
    }

    [Fact]
    public async Task UpdateTemplateTrigger_ValidRequest_UpdatesOnlyAssociationAndServerAudit()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(123));
        context.SmsTriggerCatalogs.AddRange(
            CreateCatalog(4, "OLD_TRIGGER", "Old trigger", "Old description"),
            CreateCatalog(7, "NEW_TRIGGER", "New trigger", "New description"));
        context.SmsTemplateTriggers.Add(CreateAssociation(10, 123, 4));
        await context.SaveChangesAsync();

        var result = await CreateController(context, @"EXAMPLE\template.editor")
            .UpdateTemplateTrigger(
                123,
                new UpdateSmsTemplateTriggerRequest(7),
                CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        var association = Assert.Single(await context.SmsTemplateTriggers.ToListAsync());
        Assert.Equal(10, association.TtId);
        Assert.Equal(123, association.TemplateId);
        Assert.Equal(7, association.TriggerId);
        Assert.NotNull(association.UpdateDate);
        Assert.Equal(@"EXAMPLE\template.editor", association.UpdateUser);

        var updatedTrigger = await context.SmsTriggerCatalogs.SingleAsync(item => item.TriggerId == 7);
        Assert.Equal("NEW_TRIGGER", updatedTrigger.TriggerCode);
        Assert.Equal("New trigger", updatedTrigger.TriggerName);
        Assert.Equal("New description", updatedTrigger.Description);
        Assert.True(updatedTrigger.IsActive);
    }

    [Fact]
    public async Task UpdateTemplateTrigger_NonPositiveTemplateId_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).UpdateTemplateTrigger(
            0,
            new UpdateSmsTemplateTriggerRequest(7),
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task UpdateTemplateTrigger_NonPositiveTriggerId_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).UpdateTemplateTrigger(
            123,
            new UpdateSmsTemplateTriggerRequest(0),
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task UpdateTemplateTrigger_MissingTemplate_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.SmsTriggerCatalogs.Add(CreateCatalog(7, "NEW_TRIGGER", "New trigger"));
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateTemplateTrigger(
            123,
            new UpdateSmsTemplateTriggerRequest(7),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateTemplateTrigger_MissingTrigger_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(123));
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateTemplateTrigger(
            123,
            new UpdateSmsTemplateTriggerRequest(7),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateTemplateTrigger_InactiveTrigger_ReturnsNotFoundWithoutChangingAssociation()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(123));
        context.SmsTriggerCatalogs.AddRange(
            CreateCatalog(4, "OLD_TRIGGER", "Old trigger"),
            CreateCatalog(7, "INACTIVE_TRIGGER", "Inactive trigger", isActive: false));
        context.SmsTemplateTriggers.Add(CreateAssociation(10, 123, 4));
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateTemplateTrigger(
            123,
            new UpdateSmsTemplateTriggerRequest(7),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        var association = Assert.Single(await context.SmsTemplateTriggers.ToListAsync());
        Assert.Equal(4, association.TriggerId);
    }

    [Fact]
    public async Task UpdateTemplateTrigger_MissingAssociation_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(123));
        context.SmsTriggerCatalogs.Add(CreateCatalog(7, "NEW_TRIGGER", "New trigger"));
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateTemplateTrigger(
            123,
            new UpdateSmsTemplateTriggerRequest(7),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateTemplateTrigger_SameTrigger_IsIdempotent()
    {
        await using var context = CreateContext();
        var previousUpdateDate = new DateTime(2026, 8, 20, 10, 30, 0, DateTimeKind.Local);
        var association = CreateAssociation(10, 123, 7);
        association.UpdateDate = previousUpdateDate;
        association.UpdateUser = @"EXAMPLE\previous.user";

        context.SmsTemplates.Add(CreateTemplate(123));
        context.SmsTriggerCatalogs.Add(CreateCatalog(7, "CURRENT_TRIGGER", "Current trigger"));
        context.SmsTemplateTriggers.Add(association);
        await context.SaveChangesAsync();

        var result = await CreateController(context, @"EXAMPLE\template.editor")
            .UpdateTemplateTrigger(
                123,
                new UpdateSmsTemplateTriggerRequest(7),
                CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var storedAssociation = Assert.Single(await context.SmsTemplateTriggers.ToListAsync());
        Assert.Equal(7, storedAssociation.TriggerId);
        Assert.Equal(previousUpdateDate, storedAssociation.UpdateDate);
        Assert.Equal(@"EXAMPLE\previous.user", storedAssociation.UpdateUser);
    }

    [Fact]
    public async Task UpdateTemplateTrigger_MultipleAssociations_ReturnsConflictWithoutUpdatingEither()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(123));
        context.SmsTriggerCatalogs.AddRange(
            CreateCatalog(4, "FIRST_TRIGGER", "First trigger"),
            CreateCatalog(5, "SECOND_TRIGGER", "Second trigger"),
            CreateCatalog(7, "NEW_TRIGGER", "New trigger"));
        context.SmsTemplateTriggers.AddRange(
            CreateAssociation(10, 123, 4),
            CreateAssociation(11, 123, 5));
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateTemplateTrigger(
            123,
            new UpdateSmsTemplateTriggerRequest(7),
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal(new[] { 4, 5 }, await context.SmsTemplateTriggers
            .OrderBy(item => item.TtId)
            .Select(item => item.TriggerId)
            .ToArrayAsync());
    }

    [Fact]
    public void UpdateTemplateTrigger_ContractRequiresAuthorizationAndDoesNotAcceptAuditFields()
    {
        var action = typeof(SmsTriggersController).GetMethod(
            nameof(SmsTriggersController.UpdateTemplateTrigger));

        Assert.NotNull(action);
        Assert.NotEmpty(action.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        Assert.Equal(
            new[] { nameof(UpdateSmsTemplateTriggerRequest.TriggerId) },
            typeof(UpdateSmsTemplateTriggerRequest)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray());
    }

    [Fact]
    public async Task GetTriggers_AssociationWithRule_ReturnsJoinedTriggerAndRuleSettings()
    {
        await using var context = CreateContext();
        var createDate = new DateTime(2026, 8, 19, 21, 10, 14, DateTimeKind.Utc);

        context.SmsTemplates.Add(CreateTemplate(123));
        context.SmsTemplateTriggers.Add(CreateAssociation(10, 123, 4));
        context.SmsTriggerCatalogs.Add(CreateCatalog(
            4,
            "PATIENT_DISCHARGED",
            "Patient discharged",
            "Raised when a patient is discharged"));
        context.SmsRules.Add(new SmsRule
        {
            RuleId = 7,
            TtId = 10,
            DependOnTtId = 5,
            DependencyMaxMinutes = 30,
            MaxTimeTokefInMinutes = 120,
            IsConstant = false,
            StartTimeRange = "08:00",
            EndTimeRange = "17:00",
            IsRecurring = true,
            RecurringIntervalDays = 2,
            RecurringTimeOfDay = "08:30",
            RecurringStopCondition = "discharged",
            CreateDate = createDate
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTriggers(CancellationToken.None);

        var setting = Assert.Single(GetOkTriggerSettings(result));
        Assert.Equal(7, setting.RuleId);
        Assert.Equal(10, setting.TtId);
        Assert.Equal(123, setting.TemplateId);
        Assert.Equal(4, setting.TriggerId);
        Assert.Equal("PATIENT_DISCHARGED", setting.TriggerCode);
        Assert.Equal("Patient discharged", setting.TriggerName);
        Assert.Equal("Raised when a patient is discharged", setting.TriggerDescription);
        Assert.True(setting.TriggerIsActive);
        Assert.Equal(5, setting.DependOnTtId);
        Assert.Equal(30, setting.DependencyMaxMinutes);
        Assert.Equal(120, setting.MaxTimeTokefInMinutes);
        Assert.False(setting.IsConstant);
        Assert.Equal("08:00", setting.StartTimeRange);
        Assert.Equal("17:00", setting.EndTimeRange);
        Assert.True(setting.IsRecurring);
        Assert.Equal(2, setting.RecurringIntervalDays);
        Assert.Equal("08:30", setting.RecurringTimeOfDay);
        Assert.Equal("discharged", setting.RecurringStopCondition);
        Assert.Equal(createDate, setting.CreateDate);
        Assert.Null(setting.UpdateDate);

        var json = JsonSerializer.Serialize(
            GetOkTriggerSettings(result),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"ruleId\":7", json);
        Assert.Contains("\"ttId\":10", json);
        Assert.DoesNotContain("sendType", json);
        Assert.DoesNotContain("sendTime", json);
    }

    [Fact]
    public async Task GetTriggers_AssociationWithoutRule_ReturnsNullRuleSettings()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(123));
        context.SmsTemplateTriggers.Add(CreateAssociation(10, 123, 4));
        context.SmsTriggerCatalogs.Add(CreateCatalog(
            4,
            "PATIENT_DISCHARGED",
            "Patient discharged"));
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTriggers(CancellationToken.None);

        var setting = Assert.Single(GetOkTriggerSettings(result));
        Assert.Null(setting.RuleId);
        Assert.Null(setting.DependOnTtId);
        Assert.Null(setting.DependencyMaxMinutes);
        Assert.Null(setting.MaxTimeTokefInMinutes);
        Assert.Null(setting.IsConstant);
        Assert.Null(setting.StartTimeRange);
        Assert.Null(setting.EndTimeRange);
        Assert.Null(setting.IsRecurring);
        Assert.Null(setting.RecurringIntervalDays);
        Assert.Null(setting.RecurringTimeOfDay);
        Assert.Null(setting.RecurringStopCondition);
        Assert.Null(setting.CreateDate);
        Assert.Null(setting.UpdateDate);
    }

    [Fact]
    public async Task GetTriggers_MultipleRules_ReturnsOneOrderedRowPerRule()
    {
        await using var context = CreateContext();
        context.SmsTemplates.AddRange(CreateTemplate(200), CreateTemplate(100));
        context.SmsTemplateTriggers.AddRange(
            CreateAssociation(20, 200, 5),
            CreateAssociation(10, 100, 4));
        context.SmsTriggerCatalogs.AddRange(
            CreateCatalog(5, "SECOND", "Second trigger"),
            CreateCatalog(4, "FIRST", "First trigger"));
        context.SmsRules.AddRange(
            CreateRule(9, 10, 90),
            CreateRule(3, 10, 30));
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTriggers(CancellationToken.None);

        var settings = GetOkTriggerSettings(result);
        Assert.Collection(
            settings,
            setting =>
            {
                Assert.Equal(100, setting.TemplateId);
                Assert.Equal(10, setting.TtId);
                Assert.Equal(3, setting.RuleId);
                Assert.Equal(30, setting.MaxTimeTokefInMinutes);
            },
            setting =>
            {
                Assert.Equal(100, setting.TemplateId);
                Assert.Equal(10, setting.TtId);
                Assert.Equal(9, setting.RuleId);
                Assert.Equal(90, setting.MaxTimeTokefInMinutes);
            },
            setting =>
            {
                Assert.Equal(200, setting.TemplateId);
                Assert.Equal(20, setting.TtId);
                Assert.Null(setting.RuleId);
            });
    }

    [Fact]
    public async Task GetTriggers_NoActualSchemaData_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).GetTriggers(CancellationToken.None);

        Assert.Empty(GetOkTriggerSettings(result));
    }

    [Fact]
    public async Task GetTrigger_LegacyRoute_ReturnsGoneWithoutQueryingRemovedTable()
    {
        await using var context = CreateContext();

        var result = CreateController(context).GetTrigger(99);

        var gone = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status410Gone, gone.StatusCode);
    }

    [Fact]
    public async Task GetTriggerByTemplate_ExistingAssociation_ReturnsCatalogTrigger()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateAssociation(1, 123, 4));
        context.SmsTriggerCatalogs.Add(CreateCatalog(
            4,
            "PATIENT_DISCHARGED",
            "שחרור מטופל",
            "אירוע שמופעל בעת שחרור המטופל"));
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .GetTriggerByTemplate(123, CancellationToken.None);

        var trigger = Assert.Single(GetOkTriggers(result));
        Assert.Equal(1, trigger.TtId);
        Assert.Equal(123, trigger.TemplateId);
        Assert.Equal(4, trigger.TriggerId);
        Assert.Equal("שחרור מטופל", trigger.TriggerName);
        Assert.Equal("PATIENT_DISCHARGED", trigger.TriggerCode);
        Assert.Equal("אירוע שמופעל בעת שחרור המטופל", trigger.Description);
        Assert.True(trigger.IsActive);
    }

    [Fact]
    public async Task GetTriggerByTemplate_DoesNotReturnTriggerFromAnotherTemplate()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.AddRange(
            CreateAssociation(1, 123, 4),
            CreateAssociation(2, 456, 5));
        context.SmsTriggerCatalogs.AddRange(
            CreateCatalog(4, "PATIENT_DISCHARGED", "שחרור מטופל"),
            CreateCatalog(5, "PATIENT_ADMITTED", "קבלת מטופל"));
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .GetTriggerByTemplate(123, CancellationToken.None);

        var trigger = Assert.Single(GetOkTriggers(result));
        Assert.Equal(4, trigger.TriggerId);
        Assert.NotEqual(5, trigger.TriggerId);
    }

    [Fact]
    public async Task GetTriggerByTemplate_NonPositiveTemplateId_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var result = await CreateController(context)
            .GetTriggerByTemplate(0, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task GetTriggerByTemplate_TemplateWithoutAssociation_ReturnsEmptyList()
    {
        await using var context = CreateContext();
        context.SmsTriggerCatalogs.Add(
            CreateCatalog(4, "PATIENT_DISCHARGED", "שחרור מטופל"));
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .GetTriggerByTemplate(123, CancellationToken.None);

        Assert.Empty(GetOkTriggers(result));
    }

    [Fact]
    public async Task GetTriggerByTemplate_AssociationWithoutCatalogEntry_ReturnsEmptyList()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateAssociation(1, 123, 999));
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .GetTriggerByTemplate(123, CancellationToken.None);

        Assert.Empty(GetOkTriggers(result));
    }

    [Fact]
    public async Task GetTriggerByTemplate_InactiveCatalogEntry_ReturnsInactiveTrigger()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateAssociation(1, 123, 4));
        context.SmsTriggerCatalogs.Add(CreateCatalog(
            4,
            "PATIENT_DISCHARGED",
            "שחרור מטופל",
            isActive: false));
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .GetTriggerByTemplate(123, CancellationToken.None);

        var trigger = Assert.Single(GetOkTriggers(result));
        Assert.False(trigger.IsActive);
    }

    [Fact]
    public async Task GetTriggerByTemplate_MultipleAssociations_ReturnsAllTriggers()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.AddRange(
            CreateAssociation(1, 123, 4),
            CreateAssociation(2, 123, 5));
        context.SmsTriggerCatalogs.AddRange(
            CreateCatalog(4, "PATIENT_DISCHARGED", "שחרור מטופל"),
            CreateCatalog(5, "PATIENT_ADMITTED", "קבלת מטופל"));
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .GetTriggerByTemplate(123, CancellationToken.None);

        var triggers = GetOkTriggers(result);
        Assert.Equal(2, triggers.Count);
        Assert.Contains(triggers, trigger => trigger.TriggerId == 4);
        Assert.Contains(triggers, trigger => trigger.TriggerId == 5);
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static SmsTriggersController CreateController(
        HospitalSmsDbContext context,
        string? userName = TestCurrentUserService.DefaultUserName) =>
        new(context, new TestCurrentUserService(userName));

    private static List<TemplateClinicalTriggerDto> GetOkTriggers(
        ActionResult<IEnumerable<TemplateClinicalTriggerDto>> result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IEnumerable<TemplateClinicalTriggerDto>>(okResult.Value).ToList();
    }

    private static List<SmsTriggerSettingsDto> GetOkTriggerSettings(
        ActionResult<IEnumerable<SmsTriggerSettingsDto>> result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IEnumerable<SmsTriggerSettingsDto>>(okResult.Value).ToList();
    }

    private static SmsTemplate CreateTemplate(int templateId) => new()
    {
        TemplateId = templateId,
        TemplateCode = $"TEMPLATE_{templateId}",
        TemplateName = $"Template {templateId}",
        CategoryId = 1
    };

    private static SmsRule CreateRule(int ruleId, int ttId, double maxTimeTokefInMinutes) => new()
    {
        RuleId = ruleId,
        TtId = ttId,
        MaxTimeTokefInMinutes = maxTimeTokefInMinutes,
        CreateDate = new DateTime(2026, 8, 19, 21, 10, 14, DateTimeKind.Utc)
    };

    private static SmsTemplateTrigger CreateAssociation(
        int ttId,
        int templateId,
        int triggerId) => new()
        {
            TtId = ttId,
            TemplateId = templateId,
            TriggerId = triggerId
        };

    private static SmsTriggerCatalog CreateCatalog(
        int triggerId,
        string triggerCode,
        string triggerName,
        string? description = null,
        bool isActive = true) => new()
        {
            TriggerId = triggerId,
            TriggerCode = triggerCode,
            TriggerName = triggerName,
            Description = description,
            IsActive = isActive
        };
}
