using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class StaticsControllerTests
{
    [Fact]
    public async Task GetStatics_UsesStaticValueAndValueNameFromCurrentSchema()
    {
        await using var context = CreateContext();
        context.SmsPlaceholderCatalogs.Add(new SmsPlaceholderCatalog
        {
            PlaceholderId = 12,
            PlaceholderName = "{DepartmentLink}",
            DisplayName = "Department information link"
        });
        context.SmsStatics.AddRange(
            new SmsStatic
            {
                StaticId = 1,
                PlaceholderId = 12,
                ValueName = "Tetanus",
                StaticValue = "https://example.test/tetanus",
                IsActive = true
            },
            new SmsStatic
            {
                StaticId = 2,
                PlaceholderId = 12,
                ValueName = "Inactive",
                StaticValue = "https://example.test/inactive",
                IsActive = false
            });
        await context.SaveChangesAsync();

        var result = await new StaticsController(context)
            .GetStatics(CancellationToken.None);

        var item = Assert.Single(GetOkStatics(result));
        Assert.Equal("Tetanus", item.FieldName);
        Assert.Equal("https://example.test/tetanus", item.StaticValue);
    }

    [Fact]
    public async Task GetStatics_NoRows_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();

        var result = await new StaticsController(context)
            .GetStatics(CancellationToken.None);

        Assert.Empty(GetOkStatics(result));
    }

    private static List<StaticValueDto> GetOkStatics(
        ActionResult<IEnumerable<StaticValueDto>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IEnumerable<StaticValueDto>>(ok.Value).ToList();
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }
}
