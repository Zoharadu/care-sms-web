using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class SmsRejectHospitalUnitsControllerTests
{
    [Fact]
    public void Replace_IsExposedAsPutAndPostForBackwardCompatibility()
    {
        var action = typeof(SmsRejectHospitalUnitsController)
            .GetMethod(nameof(SmsRejectHospitalUnitsController.Replace));

        var put = Assert.Single(
            action!.GetCustomAttributes(typeof(HttpPutAttribute), inherit: false)
                .Cast<HttpPutAttribute>());
        var post = Assert.Single(
            action.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false)
                .Cast<HttpPostAttribute>());
        Assert.Equal("replace", put.Template);
        Assert.Equal("replace", post.Template);
    }

    [Fact]
    public async Task GetRejectHospitalUnits_ReturnsBackendRowsOrderedAndSupportsTemplateFilter()
    {
        await using var context = CreateContext();
        context.SmsRejectHospitalUnits.AddRange(
            CreateReject(2, 21, 200),
            CreateReject(1, 20, 101),
            CreateReject(1, 20, 100));
        await context.SaveChangesAsync();

        var allResult = await CreateController(context)
            .GetRejectHospitalUnits(null, CancellationToken.None);
        var filteredResult = await CreateController(context)
            .GetRejectHospitalUnits(1, CancellationToken.None);

        var allRejects = GetOkRejects(allResult);
        Assert.Equal(
            new[] { (1, 20, 100), (1, 20, 101), (2, 21, 200) },
            allRejects.Select(reject =>
                (reject.TemplateId, reject.HospitalId, reject.UnitId)).ToArray());

        var filteredRejects = GetOkRejects(filteredResult);
        Assert.Equal(2, filteredRejects.Count);
        Assert.All(filteredRejects, reject => Assert.Equal(1, reject.TemplateId));
    }

    [Fact]
    public async Task Replace_MultipleHospitalsAndUnits_SavesAllItems()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(1));
        context.SmsHospitals.AddRange(CreateHospital(20), CreateHospital(21));
        context.SmsUnits.AddRange(
            CreateUnit(20, 15500431),
            CreateUnit(20, 15500435),
            CreateUnit(21, 15501023));
        await context.SaveChangesAsync();

        var request = CreateRequest(1,
            (20, 15500431),
            (20, 15500435),
            (21, 15501023));

        var result = await CreateController(context).Replace(request, CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(1, response.TemplateId);
        Assert.Equal(3, response.SavedCount);
        Assert.Equal(3, response.Items.Count);
        Assert.Equal(3, await context.SmsRejectHospitalUnits.CountAsync());
        Assert.All(
            await context.SmsRejectHospitalUnits.ToListAsync(),
            reject =>
            {
                Assert.NotEqual(default, reject.CreateDate);
                Assert.Null(reject.UpdateDate);
            });
    }

    [Fact]
    public async Task Replace_ExistingList_ReplacesOnlyRequestedTemplate()
    {
        await using var context = CreateContext();
        context.SmsTemplates.AddRange(CreateTemplate(1), CreateTemplate(2));
        context.SmsHospitals.Add(CreateHospital(20));
        context.SmsUnits.AddRange(CreateUnit(20, 100), CreateUnit(20, 101));
        context.SmsRejectHospitalUnits.AddRange(
            CreateReject(1, 20, 100),
            CreateReject(2, 20, 100));
        await context.SaveChangesAsync();

        var result = await CreateController(context).Replace(
            CreateRequest(1, (20, 101)),
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(1, response.SavedCount);

        var templateOneReject = Assert.Single(
            await context.SmsRejectHospitalUnits.Where(reject => reject.TemplateId == 1).ToListAsync());
        Assert.Equal(101, templateOneReject.UnitId);

        var templateTwoReject = Assert.Single(
            await context.SmsRejectHospitalUnits.Where(reject => reject.TemplateId == 2).ToListAsync());
        Assert.Equal(100, templateTwoReject.UnitId);
    }

    [Fact]
    public async Task Replace_MixedHospitalAndUnitChanges_SynchronizesOnlyDifferences()
    {
        await using var context = CreateContext();
        var originalCreateDate = new DateTime(2026, 8, 20, 8, 0, 0);
        context.SmsTemplates.Add(CreateTemplate(1));
        context.SmsHospitals.AddRange(
            CreateHospital(20),
            CreateHospital(21),
            CreateHospital(22),
            CreateHospital(23));
        context.SmsUnits.AddRange(
            CreateUnit(20, 100),
            CreateUnit(20, 101),
            CreateUnit(20, 102),
            CreateUnit(21, 200),
            CreateUnit(22, 300),
            CreateUnit(23, 400));
        context.SmsRejectHospitalUnits.AddRange(
            CreateReject(1, 20, 100, originalCreateDate),
            CreateReject(1, 20, 101),
            CreateReject(1, 21, 200),
            CreateReject(1, 22, 300, originalCreateDate));
        await context.SaveChangesAsync();

        var result = await CreateController(context).Replace(
            CreateRequest(1, (20, 100), (20, 102), (22, 300), (23, 400)),
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(4, response.SavedCount);

        var savedRejects = await context.SmsRejectHospitalUnits
            .OrderBy(reject => reject.HospitalId)
            .ThenBy(reject => reject.UnitId)
            .ToListAsync();
        Assert.Equal(
            new[] { (20, 100), (20, 102), (22, 300), (23, 400) },
            savedRejects.Select(reject => (reject.HospitalId, reject.UnitId)).ToArray());
        Assert.Equal(
            originalCreateDate,
            savedRejects.Single(reject => reject.HospitalId == 20 && reject.UnitId == 100).CreateDate);
        Assert.Equal(
            originalCreateDate,
            savedRejects.Single(reject => reject.HospitalId == 22 && reject.UnitId == 300).CreateDate);
        Assert.DoesNotContain(savedRejects, reject => reject.HospitalId == 21);
        Assert.NotEqual(
            default,
            savedRejects.Single(reject => reject.HospitalId == 23 && reject.UnitId == 400).CreateDate);
    }

    [Fact]
    public async Task Replace_EmptyList_RemovesAllTemplateRejects()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(1));
        context.SmsRejectHospitalUnits.AddRange(
            CreateReject(1, 20, 100),
            CreateReject(1, 20, 101));
        await context.SaveChangesAsync();

        var result = await CreateController(context).Replace(
            CreateRequest(1),
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(0, response.SavedCount);
        Assert.Empty(response.Items);
        Assert.Empty(await context.SmsRejectHospitalUnits.ToListAsync());
    }

    [Fact]
    public async Task Replace_UnitBelongsToAnotherHospital_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(1));
        context.SmsHospitals.AddRange(CreateHospital(20), CreateHospital(21));
        context.SmsUnits.Add(CreateUnit(21, 100));
        await context.SaveChangesAsync();

        var result = await CreateController(context).Replace(
            CreateRequest(1, (20, 100)),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(await context.SmsRejectHospitalUnits.ToListAsync());
    }

    [Fact]
    public async Task Replace_DuplicateItems_SavesOneItem()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(1));
        context.SmsHospitals.Add(CreateHospital(20));
        context.SmsUnits.Add(CreateUnit(20, 100));
        await context.SaveChangesAsync();

        var result = await CreateController(context).Replace(
            CreateRequest(1, (20, 100), (20, 100), (20, 100)),
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(1, response.SavedCount);
        Assert.Single(response.Items);
        Assert.Single(await context.SmsRejectHospitalUnits.ToListAsync());
    }

    [Fact]
    public async Task Replace_UnknownTemplate_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).Replace(
            CreateRequest(999),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Replace_NonPositiveIds_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var invalidTemplateResult = await CreateController(context).Replace(
            CreateRequest(0),
            CancellationToken.None);
        var invalidItemResult = await CreateController(context).Replace(
            CreateRequest(1, (20, 0)),
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(invalidTemplateResult.Result);
        Assert.IsType<BadRequestResult>(invalidItemResult.Result);
    }

    [Fact]
    public async Task Replace_UnknownHospital_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(1));
        await context.SaveChangesAsync();

        var result = await CreateController(context).Replace(
            CreateRequest(1, (999, 100)),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Replace_UnknownUnit_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.SmsTemplates.Add(CreateTemplate(1));
        context.SmsHospitals.Add(CreateHospital(20));
        await context.SaveChangesAsync();

        var result = await CreateController(context).Replace(
            CreateRequest(1, (20, 999)),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static SmsRejectHospitalUnitsController CreateController(HospitalSmsDbContext context) => new(context);

    private static List<SmsRejectHospitalUnit> GetOkRejects(
        ActionResult<IEnumerable<SmsRejectHospitalUnit>> result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<List<SmsRejectHospitalUnit>>(okResult.Value);
    }

    private static ReplaceRejectHospitalUnitsResponse GetOkResponse(
        ActionResult<ReplaceRejectHospitalUnitsResponse> result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<ReplaceRejectHospitalUnitsResponse>(okResult.Value);
    }

    private static ReplaceRejectHospitalUnitsRequest CreateRequest(
        int templateId,
        params (int HospitalId, int UnitId)[] items) => new()
        {
            TemplateId = templateId,
            Items = items.Select(item => new RejectHospitalUnitItemRequest
            {
                HospitalId = item.HospitalId,
                UnitId = item.UnitId
            }).ToList()
        };

    private static SmsTemplate CreateTemplate(int templateId) => new()
    {
        TemplateId = templateId,
        TemplateCode = $"TEMPLATE_{templateId}",
        TemplateName = $"Template {templateId}",
        CategoryId = 1
    };

    private static SmsHospital CreateHospital(int hospitalId) => new()
    {
        HospitalId = hospitalId,
        HospitalTypeId = 1,
        HospitalDescription = $"Hospital {hospitalId}",
        HospitalName = $"Hospital {hospitalId}"
    };

    private static SmsUnit CreateUnit(int hospitalId, int unitId) => new()
    {
        HospitalId = hospitalId,
        UnitId = unitId,
        UnitName = $"Unit {unitId}"
    };

    private static SmsRejectHospitalUnit CreateReject(
        int templateId,
        int hospitalId,
        int unitId,
        DateTime? createDate = null) => new()
        {
            TemplateId = templateId,
            HospitalId = hospitalId,
            UnitId = unitId,
            CreateDate = createDate ?? DateTime.Now
        };
}
