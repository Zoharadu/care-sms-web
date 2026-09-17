using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class UnitsControllerTests
{
    [Fact]
    public async Task GetUnits_HospitalWithUnits_ReturnsUnitsOrderedByName()
    {
        await using var context = CreateContext();
        context.SmsUnits.AddRange(
            CreateUnit(20, 2002, "פנימית", 2),
            CreateUnit(20, 2001, "מיון", 1),
            CreateUnit(20, 2003, "לא פעילה", 3, isActive: false));
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetUnits(20, CancellationToken.None);

        var units = GetOkUnits(result);
        Assert.Equal(3, units.Count);

        var activeUnit = Assert.Single(units.Where(unit => unit.UnitId == 2001));
        Assert.Equal(1, activeUnit.SmsUnitId);
        Assert.Equal("מיון", activeUnit.UnitName);
        Assert.True(activeUnit.IsActive);

        var inactiveUnit = Assert.Single(units.Where(unit => unit.UnitId == 2003));
        Assert.False(inactiveUnit.IsActive);
    }

    [Fact]
    public async Task GetUnits_HospitalWithoutUnits_ReturnsEmptyArray()
    {
        await using var context = CreateContext();
        var result = await CreateController(context).GetUnits(20, CancellationToken.None);

        Assert.Empty(GetOkUnits(result));
    }

    [Fact]
    public async Task GetUnits_NonPositiveHospitalId_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).GetUnits(0, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("positive integer", problem.Detail);
    }

    [Fact]
    public async Task GetUnits_MissingHospitalId_ReturnsBadRequestWithClearMessage()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).GetUnits(null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Invalid hospitalId.", problem.Title);
        Assert.Contains("required", problem.Detail);
    }

    [Fact]
    public async Task GetUnits_DoesNotReturnUnitsFromAnotherHospital()
    {
        await using var context = CreateContext();
        context.SmsUnits.AddRange(
            CreateUnit(20, 2001, "מיון", 1),
            CreateUnit(21, 2101, "יחידה אחרת", 2));
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetUnits(20, CancellationToken.None);

        var unit = Assert.Single(GetOkUnits(result));
        Assert.Equal(20, unit.HospitalId);
        Assert.Equal(2001, unit.UnitId);
    }

    [Fact]
    public void SmsUnitQuery_UsesActualColumnsWithoutDependingOnRemovedColumns()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HospitalSmsQueryTest;Trusted_Connection=True")
            .Options;

        using var context = new HospitalSmsDbContext(options);
        var sql = context.SmsUnits
            .Where(unit => unit.HospitalId == 20)
            .ToQueryString();

        Assert.Contains("[sms_unit_id]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[hospital_id] = 20", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("category_id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unit_type", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static UnitsController CreateController(HospitalSmsDbContext context) => new(context);

    private static List<UnitResponseDto> GetOkUnits(ActionResult<IEnumerable<UnitResponseDto>> result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IEnumerable<UnitResponseDto>>(okResult.Value).ToList();
    }

    private static SmsUnit CreateUnit(
        int hospitalId,
        int unitId,
        string unitName,
        int smsUnitId,
        bool isActive = true) => new()
        {
            SmsUnitId = smsUnitId,
            HospitalId = hospitalId,
            UnitId = unitId,
            UnitName = unitName,
            IsActive = isActive
        };
}
