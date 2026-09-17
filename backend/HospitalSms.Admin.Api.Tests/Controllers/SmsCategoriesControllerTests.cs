using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Admin.Api.Models;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class SmsCategoriesControllerTests
{
    [Fact]
    public async Task GetCategories_ProjectsCurrentSchemaAndPreservesCategoryTypeDisplayName()
    {
        await using var context = CreateContext();
        context.SmsProjects.Add(new SmsProject
        {
            ProjectId = 2,
            ProjectName = "Hospitalization"
        });
        context.SmsCategories.Add(new SmsCategory
        {
            CategoryId = 5,
            CategoryName = "Central hospitalization",
            ProjectId = 2
        });
        await context.SaveChangesAsync();

        var result = await new SmsCategoriesController(context)
            .GetCategories(new SmsCategoryQuery(), CancellationToken.None);

        var category = Assert.Single(GetOkCategories(result));
        Assert.Equal(5, category.CategoryId);
        Assert.Equal(2, category.ProjectId);
        Assert.Equal("Hospitalization", category.CategoryType);
    }

    [Fact]
    public async Task GetCategories_NoRows_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();

        var result = await new SmsCategoriesController(context)
            .GetCategories(new SmsCategoryQuery(), CancellationToken.None);

        Assert.Empty(GetOkCategories(result));
    }

    [Fact]
    public async Task GetCategories_ProjectFilter_ReturnsOnlyCategoriesForRequestedProject()
    {
        await using var context = CreateContext();
        SeedProjectsAndCategories(context);
        await context.SaveChangesAsync();

        var controller = new SmsCategoriesController(context);
        var projectOneResult = await controller.GetCategories(
            new SmsCategoryQuery { ProjectId = 1 },
            CancellationToken.None);
        var projectTwoResult = await controller.GetCategories(
            new SmsCategoryQuery { ProjectId = 2 },
            CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3, 4 }, GetOkCategories(projectOneResult).Select(x => x.CategoryId));
        Assert.Equal(new[] { 5, 6, 7, 8, 9 }, GetOkCategories(projectTwoResult).Select(x => x.CategoryId));
    }

    [Fact]
    public async Task GetCategories_InvalidProjectId_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var result = await new SmsCategoriesController(context).GetCategories(
            new SmsCategoryQuery { ProjectId = 0 },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task GetCategories_UnknownProjectId_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await new SmsCategoriesController(context).GetCategories(
            new SmsCategoryQuery { ProjectId = 999 },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetCategories_ExistingProjectWithoutCategories_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();
        context.SmsProjects.Add(new SmsProject { ProjectId = 3, ProjectName = "Empty" });
        await context.SaveChangesAsync();

        var result = await new SmsCategoriesController(context).GetCategories(
            new SmsCategoryQuery { ProjectId = 3 },
            CancellationToken.None);

        Assert.Empty(GetOkCategories(result));
    }

    [Fact]
    public async Task GetCategories_IsActiveFilter_IsApplied()
    {
        await using var context = CreateContext();
        context.SmsProjects.Add(new SmsProject { ProjectId = 1, ProjectName = "Emergency" });
        context.SmsCategories.AddRange(
            new SmsCategory { CategoryId = 1, CategoryName = "Active", ProjectId = 1, IsActive = true },
            new SmsCategory { CategoryId = 2, CategoryName = "Inactive", ProjectId = 1, IsActive = false });
        await context.SaveChangesAsync();

        var result = await new SmsCategoriesController(context).GetCategories(
            new SmsCategoryQuery { ProjectId = 1, IsActive = false },
            CancellationToken.None);

        Assert.Equal(2, Assert.Single(GetOkCategories(result)).CategoryId);
    }

    [Fact]
    public async Task CreateCategory_LegacyCategoryType_ResolvesActualProjectId()
    {
        await using var context = CreateContext();
        context.SmsProjects.Add(new SmsProject
        {
            ProjectId = 1,
            ProjectName = "Emergency"
        });
        await context.SaveChangesAsync();

        var result = await new SmsCategoriesController(context).CreateCategory(
            new SaveSmsCategoryRequest
            {
                CategoryName = "General emergency",
                CategoryType = "Emergency"
            },
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        var saved = Assert.Single(await context.SmsCategories.ToListAsync());
        Assert.Equal(1, saved.ProjectId);
    }

    private static List<SmsCategoryDto> GetOkCategories(
        ActionResult<IEnumerable<SmsCategoryDto>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IEnumerable<SmsCategoryDto>>(ok.Value).ToList();
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static void SeedProjectsAndCategories(HospitalSmsDbContext context)
    {
        context.SmsProjects.AddRange(
            new SmsProject { ProjectId = 1, ProjectName = "Emergency" },
            new SmsProject { ProjectId = 2, ProjectName = "Hospitalization" });

        context.SmsCategories.AddRange(
            Enumerable.Range(1, 4).Select(id => new SmsCategory
            {
                CategoryId = id,
                CategoryName = $"Category {id}",
                ProjectId = 1
            }).Concat(Enumerable.Range(5, 5).Select(id => new SmsCategory
            {
                CategoryId = id,
                CategoryName = $"Category {id}",
                ProjectId = 2
            })));
    }
}
