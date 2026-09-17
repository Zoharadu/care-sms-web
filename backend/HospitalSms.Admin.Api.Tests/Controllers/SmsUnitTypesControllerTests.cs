using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class SmsUnitTypesControllerTests
{
    [Fact]
    public void UnitResponse_ContainsOnlySmsUnitDictionaryColumns()
    {
        var propertyNames = typeof(UnitResponseDto)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            new[] { "HospitalId", "IsActive", "SmsUnitId", "UnitId", "UnitName" },
            propertyNames);
    }

    [Fact]
    public async Task GetUnitTypes_ReturnsUnitsFromSmsUnitInStableOrder()
    {
        await using var context = CreateContext();
        context.SmsUnits.AddRange(
            CreateUnit(21, 200, 2, "Unit B"),
            CreateUnit(20, 300, 1, "Unit C"),
            CreateUnit(20, 100, 3, "Unit A"));
        await context.SaveChangesAsync();

        var result = await new SmsUnitTypesController(context)
            .GetUnitTypes(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var units = Assert.IsAssignableFrom<IEnumerable<UnitResponseDto>>(okResult.Value).ToList();
        Assert.Collection(
            units,
            unit =>
            {
                Assert.Equal(20, unit.HospitalId);
                Assert.Equal(100, unit.UnitId);
                Assert.Equal(3, unit.SmsUnitId);
            },
            unit =>
            {
                Assert.Equal(20, unit.HospitalId);
                Assert.Equal(300, unit.UnitId);
            },
            unit =>
            {
                Assert.Equal(21, unit.HospitalId);
                Assert.Equal(200, unit.UnitId);
            });
    }

    [Fact]
    public async Task GetUnitTypes_NoUnits_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();

        var result = await new SmsUnitTypesController(context)
            .GetUnitTypes(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var units = Assert.IsAssignableFrom<IEnumerable<UnitResponseDto>>(okResult.Value);
        Assert.Empty(units);
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static SmsUnit CreateUnit(
        int hospitalId,
        int unitId,
        int smsUnitId,
        string unitName) => new()
        {
            SmsUnitId = smsUnitId,
            HospitalId = hospitalId,
            UnitId = unitId,
            UnitName = unitName
        };
}
