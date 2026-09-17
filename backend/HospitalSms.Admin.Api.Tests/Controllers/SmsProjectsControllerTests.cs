using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class SmsProjectsControllerTests
{
    [Fact]
    public async Task GetProjects_ReturnsOnlyProjectedFieldsOrderedByProjectId()
    {
        await using var context = CreateContext();
        context.SmsProjects.AddRange(
            new SmsProject
            {
                ProjectId = 2,
                ProjectName = "Hospitalization",
                IsActive = false,
                CreateDate = new DateTime(2026, 1, 1)
            },
            new SmsProject
            {
                ProjectId = 1,
                ProjectName = "Emergency",
                IsActive = true,
                CreateDate = new DateTime(2026, 1, 2)
            });
        await context.SaveChangesAsync();

        var result = await new SmsProjectsController(context)
            .GetProjects(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var projects = Assert.IsAssignableFrom<IEnumerable<SmsProjectDto>>(ok.Value).ToList();
        Assert.Collection(
            projects,
            project =>
            {
                Assert.Equal(1, project.ProjectId);
                Assert.Equal("Emergency", project.ProjectName);
                Assert.True(project.IsActive);
            },
            project =>
            {
                Assert.Equal(2, project.ProjectId);
                Assert.Equal("Hospitalization", project.ProjectName);
                Assert.False(project.IsActive);
            });
    }

    [Fact]
    public async Task GetProjects_NoRows_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();

        var result = await new SmsProjectsController(context)
            .GetProjects(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<SmsProjectDto>>(ok.Value));
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }
}
