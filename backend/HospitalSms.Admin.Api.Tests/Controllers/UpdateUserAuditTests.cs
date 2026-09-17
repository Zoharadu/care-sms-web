using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Admin.Api.Models;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class UpdateUserAuditTests
{
    private const string ConnectedUser = @"EXAMPLE\audit.user";

    [Theory]
    [InlineData(typeof(SmsTemplate), "sms_template")]
    [InlineData(typeof(SmsRule), "sms_rules")]
    [InlineData(typeof(SmsTemplateTrigger), "sms_template_trigger")]
    [InlineData(typeof(SmsTemplateLanguage), "sms_template_language")]
    public void Model_MapsNullableUpdateUserAsNvarchar50(
        Type entityType,
        string tableName)
    {
        using var context = CreateContext();
        var mappedEntity = context.Model.FindEntityType(entityType);
        var property = mappedEntity?.FindProperty(nameof(SmsTemplate.UpdateUser));
        var table = StoreObjectIdentifier.Table(tableName, "dbo");

        Assert.NotNull(property);
        Assert.Equal("update_user", property!.GetColumnName(table));
        Assert.Equal(
            "nvarchar(50)",
            property.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value);
        Assert.Equal(50, property.GetMaxLength());
        Assert.True(property.IsNullable);
        Assert.Equal(true, property.IsUnicode());
    }

    [Fact]
    public async Task ReadEndpoints_ReturnUpdateUsersFromAllFourTables()
    {
        await using var context = CreateContext();
        context.SmsProjects.Add(new SmsProject
        {
            ProjectId = 1,
            ProjectName = "Project"
        });
        context.SmsCategories.Add(new SmsCategory
        {
            CategoryId = 2,
            CategoryName = "Category",
            ProjectId = 1
        });
        context.SmsTemplates.Add(new SmsTemplate
        {
            TemplateId = 10,
            TemplateName = "Template",
            TemplateCode = "Description",
            CategoryId = 2,
            UpdateUser = "template.user"
        });
        context.SmsTemplateTriggers.Add(new SmsTemplateTrigger
        {
            TtId = 20,
            TemplateId = 10,
            TriggerId = 30,
            UpdateUser = "template-trigger.user"
        });
        context.SmsTriggerCatalogs.Add(new SmsTriggerCatalog
        {
            TriggerId = 30,
            TriggerCode = "TRIGGER",
            TriggerName = "Trigger"
        });
        context.SmsRules.Add(new SmsRule
        {
            RuleId = 40,
            TtId = 20,
            CreateDate = DateTime.Now,
            UpdateUser = "rule.user"
        });
        context.SmsTemplateLanguages.Add(new SmsTemplateLanguage
        {
            TemplateLanguageId = 50,
            TemplateId = 10,
            LanguageCode = "he",
            TemplateText = "Body",
            UpdateUser = "language.user"
        });
        await context.SaveChangesAsync();

        var templateResult = await new SmsTemplatesController(
                context,
                new TestCurrentUserService())
            .GetTemplate(10, CancellationToken.None);
        var template = Assert.IsType<SmsTemplateDto>(
            Assert.IsType<OkObjectResult>(templateResult.Result).Value);
        Assert.Equal("template.user", template.UpdateUser);

        var triggerResult = await new SmsTriggersController(
                context,
                new TestCurrentUserService())
            .GetTriggerByTemplate(10, CancellationToken.None);
        var triggers = Assert.IsAssignableFrom<IEnumerable<TemplateClinicalTriggerDto>>(
            Assert.IsType<OkObjectResult>(triggerResult.Result).Value);
        Assert.Equal("template-trigger.user", Assert.Single(triggers).UpdateUser);

        var settingsResult = await new SmsTriggersController(
                context,
                new TestCurrentUserService())
            .GetTriggers(CancellationToken.None);
        var settings = Assert.IsAssignableFrom<IEnumerable<SmsTriggerSettingsDto>>(
            Assert.IsType<OkObjectResult>(settingsResult.Result).Value);
        var setting = Assert.Single(settings);
        Assert.Equal("template.user", setting.TemplateUpdateUser);
        Assert.Equal("template-trigger.user", setting.TemplateTriggerUpdateUser);
        Assert.Equal("rule.user", setting.UpdateUser);

        var hierarchyResult = await new SmsRulesController(
            context,
            new TestCurrentUserService()).GetRules(
            new SmsRulesQuery { ProjectId = 1, CategoryId = 2 },
            CancellationToken.None);
        var hierarchy = Assert.IsAssignableFrom<IEnumerable<SmsRuleHierarchyDto>>(
            Assert.IsType<OkObjectResult>(hierarchyResult.Result).Value);
        var hierarchyItem = Assert.Single(hierarchy);
        Assert.Equal("template.user", hierarchyItem.Template.UpdateUser);
        Assert.Equal("template-trigger.user", hierarchyItem.TemplateTrigger.UpdateUser);
        Assert.Equal("rule.user", hierarchyItem.Rule?.UpdateUser);

        var languageResult = await new SmsTemplateLanguagesController(
                context,
                new TestCurrentUserService())
            .GetTemplateLanguage(50);
        var language = Assert.IsType<SmsTemplateLanguage>(
            Assert.IsType<OkObjectResult>(languageResult.Result).Value);
        Assert.Equal("language.user", language.UpdateUser);
    }

    [Fact]
    public async Task UpdateEndpoints_SaveConnectedUserAndUpdateDate()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(new SmsTemplate
        {
            TemplateId = 10,
            TemplateName = "Template",
            TemplateCode = "Description",
            CategoryId = 2
        });
        context.SmsTemplateTriggers.Add(new SmsTemplateTrigger
        {
            TtId = 20,
            TemplateId = 10,
            TriggerId = 30
        });
        context.SmsRules.Add(new SmsRule
        {
            RuleId = 40,
            TtId = 20,
            CreateDate = DateTime.Now
        });
        context.SmsTemplateLanguages.Add(new SmsTemplateLanguage
        {
            TemplateLanguageId = 50,
            TemplateId = 10,
            LanguageCode = "he",
            TemplateText = "Old body"
        });
        await context.SaveChangesAsync();

        var templateController = new SmsTemplatesController(
            context,
            new TestCurrentUserService(ConnectedUser));
        await templateController.UpdateTemplate(
            10,
            new UpdateSmsTemplateDto
            {
                TemplateId = 10,
                TemplateName = "Updated template",
                Description = "Updated description",
                CategoryId = 2,
                IsActive = true,
                IsEditable = true,
                VersionNumber = 2
            },
            CancellationToken.None);

        var ruleController = new SmsRulesController(
            context,
            new TestCurrentUserService(ConnectedUser));
        await ruleController.UpdateRule(
            40,
            new UpdateSmsRuleRequest
            {
                TtId = 20,
                MaxTimeTokefInMinutes = 90
            },
            CancellationToken.None);

        var languageRequest = JsonSerializer.Deserialize<UpdateSmsTemplateLanguageRequest>(
            """
            {
              "templateLanguageId": 50,
              "templateId": 10,
              "languageCode": "he",
              "templateText": "Updated body",
              "isDefault": true,
              "isActive": true,
              "updateUser": "forged.frontend.user"
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var languageController = new SmsTemplateLanguagesController(
            context,
            new TestCurrentUserService(ConnectedUser));
        await languageController.UpdateTemplateLanguage(50, languageRequest!);

        var template = await context.SmsTemplates.SingleAsync();
        var rule = await context.SmsRules.SingleAsync();
        var language = await context.SmsTemplateLanguages.SingleAsync();

        Assert.Equal(ConnectedUser, template.UpdateUser);
        Assert.Equal(ConnectedUser, rule.UpdateUser);
        Assert.Equal(ConnectedUser, language.UpdateUser);
        Assert.NotNull(template.UpdateDate);
        Assert.NotNull(rule.UpdateDate);
        Assert.NotNull(language.UpdateDate);
        Assert.Null(typeof(UpdateSmsTemplateDto).GetProperty("UpdateUser"));
        Assert.Null(typeof(UpdateSmsRuleRequest).GetProperty("UpdateUser"));
        Assert.Null(typeof(UpdateSmsTemplateLanguageRequest).GetProperty("UpdateUser"));
        Assert.Null(typeof(CreateSmsTemplateDto).GetProperty("UpdateUser"));
        Assert.Null(typeof(CreateSmsRuleRequest).GetProperty("UpdateUser"));
        Assert.Null(typeof(CreateSmsTemplateTriggerRequest).GetProperty("UpdateUser"));
        Assert.Null(typeof(CreateSmsTemplateLanguageRequest).GetProperty("UpdateUser"));
    }

    [Fact]
    public async Task CreateEndpoints_SaveConnectedUserForAllFourTables()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(new SmsTemplate
        {
            TemplateId = 10,
            TemplateName = "Existing template",
            TemplateCode = "Existing description",
            CategoryId = 2
        });
        context.SmsTemplateTriggers.Add(new SmsTemplateTrigger
        {
            TtId = 20,
            TemplateId = 10,
            TriggerId = 30
        });
        context.SmsTriggerCatalogs.AddRange(
            new SmsTriggerCatalog
            {
                TriggerId = 30,
                TriggerCode = "EXISTING_TRIGGER",
                TriggerName = "Existing trigger"
            },
            new SmsTriggerCatalog
            {
                TriggerId = 31,
                TriggerCode = "NEW_TRIGGER",
                TriggerName = "New trigger"
            });
        await context.SaveChangesAsync();

        var templateController = new SmsTemplatesController(
            context,
            new TestCurrentUserService(ConnectedUser));
        var templateResult = await templateController.CreateTemplate(
            new CreateSmsTemplateDto
            {
                TemplateName = "New template",
                Description = "New description",
                CategoryId = 2,
                IsActive = true,
                IsEditable = true
            },
            CancellationToken.None);
        var createdTemplate = Assert.IsType<SmsTemplateDto>(
            Assert.IsType<CreatedAtActionResult>(templateResult.Result).Value);

        var ruleController = new SmsRulesController(
            context,
            new TestCurrentUserService(ConnectedUser));
        var ruleResult = await ruleController.CreateRule(
            new CreateSmsRuleRequest
            {
                TtId = 20,
                MaxTimeTokefInMinutes = 90
            },
            CancellationToken.None);
        var createdRule = Assert.IsType<SmsRuleResponse>(
            Assert.IsType<OkObjectResult>(ruleResult.Result).Value);

        var languageController = new SmsTemplateLanguagesController(
            context,
            new TestCurrentUserService(ConnectedUser));
        var languageResult = await languageController.CreateTemplateLanguage(
            new CreateSmsTemplateLanguageRequest
            {
                TemplateId = 10,
                LanguageCode = "he",
                TemplateText = "Body",
                IsActive = true
            });
        var createdLanguage = Assert.IsType<SmsTemplateLanguage>(
            Assert.IsType<CreatedAtActionResult>(languageResult.Result).Value);

        var triggerController = new SmsTriggersController(
            context,
            new TestCurrentUserService(ConnectedUser));
        var triggerResult = await triggerController.CreateTemplateTrigger(
            new CreateSmsTemplateTriggerRequest
            {
                TemplateId = createdTemplate.TemplateId,
                TriggerId = 31
            },
            CancellationToken.None);
        var createdTemplateTrigger = Assert.IsType<SmsTemplateTriggerResponse>(
            Assert.IsType<CreatedAtActionResult>(triggerResult.Result).Value);

        Assert.Equal(ConnectedUser, createdTemplate.UpdateUser);
        Assert.Equal(ConnectedUser, createdRule.UpdateUser);
        Assert.Equal(ConnectedUser, createdLanguage.UpdateUser);
        Assert.Equal(ConnectedUser, createdTemplateTrigger.UpdateUser);

        Assert.Equal(
            ConnectedUser,
            (await context.SmsTemplates.FindAsync(createdTemplate.TemplateId))?.UpdateUser);
        Assert.Equal(
            ConnectedUser,
            (await context.SmsRules.FindAsync(createdRule.RuleId))?.UpdateUser);
        Assert.Equal(
            ConnectedUser,
            (await context.SmsTemplateLanguages.FindAsync(
                createdLanguage.TemplateLanguageId))?.UpdateUser);
        Assert.Equal(
            ConnectedUser,
            (await context.SmsTemplateTriggers.FindAsync(
                createdTemplateTrigger.TtId))?.UpdateUser);
    }

    [Theory]
    [InlineData(typeof(SmsTemplatesController), nameof(SmsTemplatesController.CreateTemplate))]
    [InlineData(typeof(SmsTemplatesController), nameof(SmsTemplatesController.UpdateTemplate))]
    [InlineData(typeof(SmsRulesController), nameof(SmsRulesController.CreateRule))]
    [InlineData(typeof(SmsRulesController), nameof(SmsRulesController.UpdateRule))]
    [InlineData(typeof(SmsTemplateLanguagesController), nameof(SmsTemplateLanguagesController.CreateTemplateLanguage))]
    [InlineData(typeof(SmsTemplateLanguagesController), nameof(SmsTemplateLanguagesController.UpdateTemplateLanguage))]
    [InlineData(typeof(SmsTriggersController), nameof(SmsTriggersController.CreateTemplateTrigger))]
    public void AuditedWriteEndpoint_RequiresAuthenticatedUser(
        Type controllerType,
        string methodName)
    {
        var method = controllerType.GetMethod(methodName);

        Assert.NotNull(method);
        Assert.NotEmpty(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
    }

    [Fact]
    public async Task UpdateTemplate_MissingIdentity_RejectsBeforeChangingEntity()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(new SmsTemplate
        {
            TemplateId = 10,
            TemplateName = "Template",
            TemplateCode = "Description",
            CategoryId = 2,
            UpdateUser = "previous.user"
        });
        await context.SaveChangesAsync();

        var controller = new SmsTemplatesController(
            context,
            new TestCurrentUserService(userName: null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            controller.UpdateTemplate(
                10,
                new UpdateSmsTemplateDto
                {
                    TemplateId = 10,
                    TemplateName = "Updated template",
                    Description = "Updated description",
                    CategoryId = 2,
                    IsActive = true,
                    IsEditable = true
                },
                CancellationToken.None));

        var template = await context.SmsTemplates.SingleAsync();
        Assert.Equal("previous.user", template.UpdateUser);
        Assert.Null(template.UpdateDate);
    }

    [Fact]
    public void ResponseContracts_SerializeUpdateUserUsingCamelCase()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        Assert.Contains(
            "\"updateUser\":null",
            JsonSerializer.Serialize(new SmsTemplateDto(), options));
        Assert.Contains(
            "\"updateUser\":null",
            JsonSerializer.Serialize(new SmsRuleResponse(), options));
        Assert.Contains(
            "\"updateUser\":null",
            JsonSerializer.Serialize(new SmsTemplateTriggerResponse(), options));
        Assert.Contains(
            "\"updateUser\":null",
            JsonSerializer.Serialize(new SmsTemplateLanguage(), options));
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }

}
