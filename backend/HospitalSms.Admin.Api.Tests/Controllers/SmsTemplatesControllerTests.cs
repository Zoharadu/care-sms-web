using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Admin.Api.Models;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class SmsTemplatesControllerTests
{
    [Fact]
    public async Task GetTemplates_ReturnsCurrentTemplateFieldsInStableOrder()
    {
        await using var context = CreateContext();
        context.SmsTemplates.AddRange(
            CreateTemplate(2, "Second description"),
            CreateTemplate(1, "First description"));
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .GetTemplates(new SmsTemplateQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var templates = Assert.IsAssignableFrom<IEnumerable<SmsTemplateDto>>(ok.Value).ToList();
        Assert.Collection(
            templates,
            template =>
            {
                Assert.Equal("First description", template.TemplateCode);
                Assert.Equal("First description", template.Description);
            },
            template =>
            {
                Assert.Equal("Second description", template.TemplateCode);
                Assert.Equal("Second description", template.Description);
            });
    }

    [Fact]
    public async Task GetTemplates_CategoryFilter_ReturnsOnlyTemplatesFromCategory()
    {
        await using var context = CreateContext();
        SeedProjectsCategoriesAndTemplates(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTemplates(
            new SmsTemplateQuery { CategoryId = 1 },
            CancellationToken.None);

        var templates = GetOkTemplates(result);
        Assert.Equal(new[] { 1, 2 }, templates.Select(template => template.TemplateId));
        Assert.Equal(
            new[] { "Description 1", "Description 2" },
            templates.Select(template => template.Description));
    }

    [Fact]
    public async Task GetTemplates_ProjectFilter_ReturnsTemplatesFromAllProjectCategories()
    {
        await using var context = CreateContext();
        SeedProjectsCategoriesAndTemplates(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTemplates(
            new SmsTemplateQuery { ProjectId = 1 },
            CancellationToken.None);

        var templates = GetOkTemplates(result);
        Assert.Equal(new[] { 1, 2, 3 }, templates.Select(template => template.TemplateId));
        Assert.Equal(
            new[] { "Description 1", "Description 2", "Description 3" },
            templates.Select(template => template.Description));
    }

    [Fact]
    public async Task GetTemplates_MatchingProjectAndCategory_ReturnsFilteredTemplates()
    {
        await using var context = CreateContext();
        SeedProjectsCategoriesAndTemplates(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTemplates(
            new SmsTemplateQuery { ProjectId = 1, CategoryId = 2 },
            CancellationToken.None);

        var template = Assert.Single(GetOkTemplates(result));
        Assert.Equal(3, template.TemplateId);
        Assert.Equal("Description 3", template.Description);
    }

    [Fact]
    public async Task GetTemplates_MismatchedProjectAndCategory_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        SeedProjectsCategoriesAndTemplates(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTemplates(
            new SmsTemplateQuery { ProjectId = 1, CategoryId = 3 },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task GetTemplates_ExistingCategoryWithoutTemplates_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();
        context.SmsProjects.Add(new SmsProject { ProjectId = 1, ProjectName = "Emergency" });
        context.SmsCategories.Add(new SmsCategory
        {
            CategoryId = 10,
            CategoryName = "Empty category",
            ProjectId = 1
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTemplates(
            new SmsTemplateQuery { CategoryId = 10 },
            CancellationToken.None);

        Assert.Empty(GetOkTemplates(result));
    }

    [Fact]
    public async Task GetTemplates_IsActiveFilter_IsApplied()
    {
        await using var context = CreateContext();
        context.SmsTemplates.AddRange(
            CreateTemplateForCategory(1, 1),
            new SmsTemplate
            {
                TemplateId = 2,
                TemplateCode = "Inactive",
                TemplateName = "Inactive template",
                CategoryId = 1,
                IsActive = false
            });
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTemplates(
            new SmsTemplateQuery { IsActive = false },
            CancellationToken.None);

        Assert.Equal(2, Assert.Single(GetOkTemplates(result)).TemplateId);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(null, 0)]
    public async Task GetTemplates_NonPositiveIdentifier_ReturnsBadRequest(
        int? projectId,
        int? categoryId)
    {
        await using var context = CreateContext();

        var result = await CreateController(context).GetTemplates(
            new SmsTemplateQuery { ProjectId = projectId, CategoryId = categoryId },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Theory]
    [InlineData(999, null)]
    [InlineData(null, 999)]
    public async Task GetTemplates_UnknownIdentifier_ReturnsNotFound(
        int? projectId,
        int? categoryId)
    {
        await using var context = CreateContext();

        var result = await CreateController(context).GetTemplates(
            new SmsTemplateQuery { ProjectId = projectId, CategoryId = categoryId },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetTemplate_ReturnsDescriptionFromExistingTemplateDescriptionColumn()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(
            CreateTemplate(123, "Appointment description"));
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .GetTemplate(123, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var template = Assert.IsType<SmsTemplateDto>(ok.Value);
        Assert.Equal("Appointment description", template.TemplateCode);
        Assert.Equal("Appointment description", template.Description);
    }

    [Fact]
    public void Model_DoesNotMapASeparateDescriptionColumn()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(SmsTemplate));

        Assert.NotNull(entityType);
        Assert.DoesNotContain(entityType!.GetProperties(), property => property.Name == "Description");
        Assert.Equal(
            "template_description",
            entityType.FindProperty(nameof(SmsTemplate.TemplateCode))!.GetColumnName());
    }

    [Fact]
    public async Task CreateTemplate_WithDescription_TrimsAndReturnsDescription()
    {
        await using var context = CreateContext();
        var request = CreateRequest(
            "APPOINTMENT_REMINDER",
            "  נשלחת למטופל יום לפני התור  ");

        var result = await CreateController(context)
            .CreateTemplate(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<SmsTemplateDto>(created.Value);
        var stored = Assert.Single(await context.SmsTemplates.ToListAsync());
        Assert.Equal("נשלחת למטופל יום לפני התור", stored.TemplateCode);
        Assert.Equal("נשלחת למטופל יום לפני התור", response.Description);
        Assert.Equal(response.Description, response.TemplateCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTemplate_WithoutMeaningfulDescription_StoresAndReturnsEmptyString(
        string? description)
    {
        await using var context = CreateContext();
        var request = CreateRequest("APPOINTMENT_REMINDER", description);

        var result = await CreateController(context)
            .CreateTemplate(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<SmsTemplateDto>(created.Value);
        Assert.Equal(string.Empty, Assert.Single(await context.SmsTemplates.ToListAsync()).TemplateCode);
        Assert.Equal(string.Empty, response.Description);
    }

    [Fact]
    public async Task UpdateTemplate_UpdatesTrimmedDescriptionInExistingColumn()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(
            CreateTemplate(123, "Old description"));
        await context.SaveChangesAsync();
        var request = UpdateRequest(123, "APPOINTMENT_REMINDER", "  Updated description  ");

        var result = await CreateController(context)
            .UpdateTemplate(123, request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var stored = Assert.Single(await context.SmsTemplates.ToListAsync());
        Assert.Equal("Updated description", stored.TemplateCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateTemplate_EmptyDescription_ClearsExistingColumn(
        string description)
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(
            CreateTemplate(123, "Existing description"));
        await context.SaveChangesAsync();
        var request = UpdateRequest(123, "APPOINTMENT_REMINDER", description);

        var result = await CreateController(context)
            .UpdateTemplate(123, request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var stored = Assert.Single(await context.SmsTemplates.ToListAsync());
        Assert.Equal(string.Empty, stored.TemplateCode);
    }

    [Fact]
    public async Task DeleteTemplate_RemovesTemplateAndAllItsOwnedRows()
    {
        await using var context = CreateContext();
        SeedTemplateGraph(context, templateId: 1, ttId: 101);
        SeedTemplateGraph(context, templateId: 2, ttId: 202);
        context.SmsRules.Add(new SmsRule
        {
            RuleId = 3,
            TtId = 202,
            DependOnTtId = 1,
            CreateDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .DeleteTemplate(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await context.SmsTemplates.AnyAsync(template => template.TemplateId == 1));
        Assert.False(await context.SmsTemplateLanguages.AnyAsync(language => language.TemplateId == 1));
        Assert.False(await context.SmsTemplateTriggers.AnyAsync(trigger => trigger.TemplateId == 1));
        Assert.False(await context.SmsRejectHospitalUnits.AnyAsync(reject => reject.TemplateId == 1));
        Assert.False(await context.SmsRules.AnyAsync(rule => rule.TtId == 101));

        Assert.True(await context.SmsTemplates.AnyAsync(template => template.TemplateId == 2));
        Assert.True(await context.SmsTemplateLanguages.AnyAsync(language => language.TemplateId == 2));
        Assert.True(await context.SmsTemplateTriggers.AnyAsync(trigger => trigger.TemplateId == 2));
        Assert.True(await context.SmsRejectHospitalUnits.AnyAsync(reject => reject.TemplateId == 2));
        Assert.Equal(2, await context.SmsRules.CountAsync(rule => rule.TtId == 202));
    }

    [Fact]
    public async Task DeleteTemplate_UnknownTemplate_ReturnsNotFoundAndChangesNothing()
    {
        await using var context = CreateContext();
        SeedTemplateGraph(context, templateId: 2, ttId: 202);
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .DeleteTemplate(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Single(await context.SmsTemplates.ToListAsync());
        Assert.Single(await context.SmsTemplateLanguages.ToListAsync());
        Assert.Single(await context.SmsTemplateTriggers.ToListAsync());
        Assert.Single(await context.SmsRejectHospitalUnits.ToListAsync());
        Assert.Single(await context.SmsRules.ToListAsync());
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static SmsTemplatesController CreateController(HospitalSmsDbContext context) =>
        new(context, new TestCurrentUserService());

    private static void SeedTemplateGraph(HospitalSmsDbContext context, int templateId, int ttId)
    {
        context.SmsTemplates.Add(new SmsTemplate
        {
            TemplateId = templateId,
            TemplateCode = $"TEMPLATE_{templateId}",
            TemplateName = $"Template {templateId}",
            CategoryId = 1
        });
        context.SmsTemplateLanguages.Add(new SmsTemplateLanguage
        {
            TemplateLanguageId = templateId,
            TemplateId = templateId,
            LanguageCode = "he",
            TemplateText = $"Template text {templateId}"
        });
        context.SmsTemplateTriggers.Add(new SmsTemplateTrigger
        {
            TtId = ttId,
            TemplateId = templateId,
            TriggerId = templateId
        });
        context.SmsRules.Add(new SmsRule
        {
            RuleId = templateId,
            TtId = ttId,
            CreateDate = DateTime.Now
        });
        context.SmsRejectHospitalUnits.Add(new SmsRejectHospitalUnit
        {
            TemplateId = templateId,
            HospitalId = 20,
            UnitId = templateId,
            CreateDate = DateTime.Now
        });
    }

    private static SmsTemplate CreateTemplate(int templateId, string description) => new()
    {
        TemplateId = templateId,
        TemplateCode = description,
        TemplateName = $"Template {templateId}",
        CategoryId = 1
    };

    private static CreateSmsTemplateDto CreateRequest(
        string templateCode,
        string? description) => new()
        {
            TemplateCode = templateCode,
            TemplateName = "Appointment reminder",
            Description = description,
            CategoryId = 1,
            IsActive = true,
            IsEditable = true,
            VersionNumber = 1
        };

    private static UpdateSmsTemplateDto UpdateRequest(
        int templateId,
        string templateCode,
        string? description) => new()
        {
            TemplateId = templateId,
            TemplateCode = templateCode,
            TemplateName = "Appointment reminder",
            Description = description,
            CategoryId = 1,
            IsActive = true,
            IsEditable = true,
            VersionNumber = 1
        };

    private static List<SmsTemplateDto> GetOkTemplates(
        ActionResult<IEnumerable<SmsTemplateDto>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IEnumerable<SmsTemplateDto>>(ok.Value).ToList();
    }

    private static void SeedProjectsCategoriesAndTemplates(HospitalSmsDbContext context)
    {
        context.SmsProjects.AddRange(
            new SmsProject { ProjectId = 1, ProjectName = "Emergency" },
            new SmsProject { ProjectId = 2, ProjectName = "Hospitalization" });
        context.SmsCategories.AddRange(
            new SmsCategory { CategoryId = 1, CategoryName = "Emergency adults", ProjectId = 1 },
            new SmsCategory { CategoryId = 2, CategoryName = "Emergency children", ProjectId = 1 },
            new SmsCategory { CategoryId = 3, CategoryName = "Hospitalization", ProjectId = 2 });
        context.SmsTemplates.AddRange(
            CreateTemplateForCategory(1, 1),
            CreateTemplateForCategory(2, 1),
            CreateTemplateForCategory(3, 2),
            CreateTemplateForCategory(4, 3));
    }

    private static SmsTemplate CreateTemplateForCategory(int templateId, int categoryId) => new()
    {
        TemplateId = templateId,
        TemplateCode = $"Description {templateId}",
        TemplateName = $"Template {templateId}",
        CategoryId = categoryId
    };
}
