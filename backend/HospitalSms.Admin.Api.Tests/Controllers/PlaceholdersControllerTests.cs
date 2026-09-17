using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Admin.Api.Models;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class PlaceholdersControllerTests
{
    [Fact]
    public async Task GetPlaceholders_ReturnsOnlyRequestedFieldsInIdOrder()
    {
        await using var context = CreateContext();
        context.SmsPlaceholderCatalogs.AddRange(
            CreatePlaceholder(2, "{HospitalName}", "Hospital name", "HOSPITAL_DB"),
            CreatePlaceholder(1, "{PatientName}", "Patient name", "PATIENT_DB"));
        await context.SaveChangesAsync();

        var result = await new PlaceholdersController(context)
            .GetPlaceholders(new PlaceholderQuery(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var placeholders = Assert.IsAssignableFrom<IEnumerable<PlaceholderCatalogDto>>(okResult.Value).ToList();
        Assert.Collection(
            placeholders,
            placeholder =>
            {
                Assert.Equal(1, placeholder.PlaceholderId);
                Assert.Equal("{PatientName}", placeholder.PlaceholderName);
                Assert.Equal("Patient name", placeholder.DisplayName);
            },
            placeholder => Assert.Equal(2, placeholder.PlaceholderId));

        var json = JsonSerializer.Serialize(
            placeholders,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"placeholderId\"", json);
        Assert.Contains("\"placeholderName\"", json);
        Assert.Contains("\"displayName\"", json);
        Assert.DoesNotContain("sourceType", json);
        Assert.DoesNotContain("createDate", json);
    }

    [Fact]
    public async Task GetPlaceholders_NoRows_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();

        var result = await new PlaceholdersController(context)
            .GetPlaceholders(new PlaceholderQuery(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var placeholders = Assert.IsAssignableFrom<IEnumerable<PlaceholderCatalogDto>>(okResult.Value);
        Assert.Empty(placeholders);
    }

    [Fact]
    public async Task GetPlaceholders_ValidScope_ReturnsProjectAndCategoryPlaceholdersWithoutDuplicates()
    {
        await using var context = CreateContext();
        SeedProjectCategoryAndCatalog(context);
        context.SmsPlaceholderScopes.AddRange(
            CreateScope(1, placeholderId: 1, projectId: 1, categoryId: null),
            CreateScope(2, placeholderId: 2, projectId: 1, categoryId: 1),
            CreateScope(3, placeholderId: 2, projectId: 1, categoryId: 1));
        await context.SaveChangesAsync();

        var result = await new PlaceholdersController(context).GetPlaceholders(
            new PlaceholderQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, GetOkPlaceholders(result).Select(x => x.PlaceholderId));
    }

    [Fact]
    public async Task GetPlaceholders_Filter_ExcludesInactiveOtherProjectAndOtherCategoryScopes()
    {
        await using var context = CreateContext();
        SeedProjectCategoryAndCatalog(context);
        context.SmsPlaceholderScopes.AddRange(
            CreateScope(1, placeholderId: 1, projectId: 1, categoryId: null, isActive: false),
            CreateScope(2, placeholderId: 2, projectId: 2, categoryId: null),
            CreateScope(3, placeholderId: 3, projectId: 1, categoryId: 2));
        await context.SaveChangesAsync();

        var result = await new PlaceholdersController(context).GetPlaceholders(
            new PlaceholderQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);

        Assert.Empty(GetOkPlaceholders(result));
    }

    [Fact]
    public async Task GetPlaceholders_CategoryNotOwnedByProject_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        SeedProjectCategoryAndCatalog(context);
        await context.SaveChangesAsync();

        var result = await new PlaceholdersController(context).GetPlaceholders(
            new PlaceholderQuery { ProjectId = 1, CategoryId = 3 },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Theory]
    [InlineData(999, 1)]
    [InlineData(1, 999)]
    public async Task GetPlaceholders_UnknownIdentifier_ReturnsNotFound(
        int projectId,
        int categoryId)
    {
        await using var context = CreateContext();
        SeedProjectCategoryAndCatalog(context);
        await context.SaveChangesAsync();

        var result = await new PlaceholdersController(context).GetPlaceholders(
            new PlaceholderQuery { ProjectId = projectId, CategoryId = categoryId },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    public async Task GetPlaceholders_NonPositiveIdentifier_ReturnsBadRequest(
        int projectId,
        int categoryId)
    {
        await using var context = CreateContext();

        var result = await new PlaceholdersController(context).GetPlaceholders(
            new PlaceholderQuery { ProjectId = projectId, CategoryId = categoryId },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(null, 1)]
    public async Task GetPlaceholders_OnlyOneIdentifier_ReturnsBadRequest(
        int? projectId,
        int? categoryId)
    {
        await using var context = CreateContext();

        var result = await new PlaceholdersController(context).GetPlaceholders(
            new PlaceholderQuery { ProjectId = projectId, CategoryId = categoryId },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task GetPlaceholders_ValidProjectAndCategoryWithoutScopes_ReturnsEmptyArray()
    {
        await using var context = CreateContext();
        SeedProjectCategoryAndCatalog(context);
        await context.SaveChangesAsync();

        var result = await new PlaceholdersController(context).GetPlaceholders(
            new PlaceholderQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);

        Assert.Empty(GetOkPlaceholders(result));
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static SmsPlaceholderCatalog CreatePlaceholder(
        int id,
        string name,
        string displayName,
        string sourceType) => new()
        {
            PlaceholderId = id,
            PlaceholderName = name,
            DisplayName = displayName,
            SourceType = sourceType
        };

    private static List<PlaceholderCatalogDto> GetOkPlaceholders(
        ActionResult<IEnumerable<PlaceholderCatalogDto>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IEnumerable<PlaceholderCatalogDto>>(ok.Value).ToList();
    }

    private static void SeedProjectCategoryAndCatalog(HospitalSmsDbContext context)
    {
        context.SmsProjects.AddRange(
            new SmsProject { ProjectId = 1, ProjectName = "Emergency" },
            new SmsProject { ProjectId = 2, ProjectName = "Hospitalization" });
        context.SmsCategories.AddRange(
            new SmsCategory { CategoryId = 1, CategoryName = "Emergency adults", ProjectId = 1 },
            new SmsCategory { CategoryId = 2, CategoryName = "Emergency children", ProjectId = 1 },
            new SmsCategory { CategoryId = 3, CategoryName = "Hospitalization", ProjectId = 2 });
        context.SmsPlaceholderCatalogs.AddRange(
            CreatePlaceholder(1, "{HospitalName}", "Hospital name", "DB"),
            CreatePlaceholder(2, "{UnitName}", "Unit name", "DB"),
            CreatePlaceholder(3, "{DoctorName}", "Doctor name", "DB"));
    }

    private static SmsPlaceholderScope CreateScope(
        int id,
        int placeholderId,
        int projectId,
        int? categoryId,
        bool isActive = true) => new()
        {
            PlaceholderScopeId = id,
            PlaceholderId = placeholderId,
            ProjectId = projectId,
            CategoryId = categoryId,
            IsActive = isActive
        };
}
