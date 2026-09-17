using System.Text.Json;
using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class SmsUnitCategoriesControllerTests
{
    [Fact]
    public async Task GetUnitCategories_ReturnsAllRowsInStableOrder()
    {
        await using var context = CreateContext();
        context.SmsUnitCategories.AddRange(
            CreateUnitCategory(2, 20, 4, isActive: false),
            CreateUnitCategory(1, 10, 3, isActive: true));
        await context.SaveChangesAsync();

        var result = await CreateController(context)
            .GetUnitCategories(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<SmsUnitCategoryResponseDto>>(ok.Value)
            .ToList();

        Assert.Collection(
            items,
            item =>
            {
                Assert.Equal(1, item.UcId);
                Assert.Equal(10, item.SmsUnitId);
                Assert.Equal(3, item.CategoryId);
                Assert.True(item.IsActive);
                Assert.Equal(new DateTime(2026, 9, 6, 8, 0, 0), item.CreateDate);
            },
            item =>
            {
                Assert.Equal(2, item.UcId);
                Assert.False(item.IsActive);
            });
    }

    [Fact]
    public async Task GetUnitCategories_NoRows_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();

        var result = await CreateController(context)
            .GetUnitCategories(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<SmsUnitCategoryResponseDto>>(ok.Value));
    }

    [Fact]
    public async Task GetUnitCategoryDetails_NoRows_ReturnsOkWithEmptyArray()
    {
        await using var context = CreateContext();

        var result = await CreateController(context)
            .GetUnitCategoryDetails(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<SmsUnitCategoryDetailsDto>>(ok.Value));
    }

    [Fact]
    public async Task Post_NewAssociation_CreatesActiveRowWithServerAuditUser()
    {
        await using var context = CreateContext();
        await SeedValidReferences(context);

        var result = await CreateController(context, @"EXAMPLE\creator")
            .CreateUnitCategory(CreateValidRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<SmsUnitCategoryWriteResponseDto>(created.Value);
        Assert.True(response.UcId > 0);
        Assert.Equal(101, response.SmsUnitId);
        Assert.Equal(3, response.CategoryId);
        Assert.True(response.IsActive);

        var saved = Assert.Single(await context.SmsUnitCategories.ToListAsync());
        Assert.Equal(@"EXAMPLE\creator", saved.UpdateUser);
        Assert.True(saved.IsActive);
        Assert.NotEqual(default, saved.CreateDate);
    }

    [Fact]
    public async Task Post_ExistingActiveAssociation_IsIdempotentAndDoesNotCreateDuplicate()
    {
        await using var context = CreateContext();
        await SeedValidReferences(context);
        var existing = CreateUnitCategory(7, 101, 3, isActive: true);
        existing.UpdateUser = @"EXAMPLE\original";
        context.SmsUnitCategories.Add(existing);
        await context.SaveChangesAsync();

        var result = await CreateController(context, @"EXAMPLE\second")
            .CreateUnitCategory(CreateValidRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SmsUnitCategoryWriteResponseDto>(ok.Value);
        Assert.Equal(7, response.UcId);
        Assert.True(response.IsActive);
        var saved = Assert.Single(await context.SmsUnitCategories.ToListAsync());
        Assert.Equal(@"EXAMPLE\original", saved.UpdateUser);
    }

    [Fact]
    public async Task Post_ExistingInactiveAssociation_ReactivatesInsteadOfCreatingDuplicate()
    {
        await using var context = CreateContext();
        await SeedValidReferences(context);
        var existing = CreateUnitCategory(8, 101, 3, isActive: false);
        existing.UpdateUser = @"EXAMPLE\previous";
        context.SmsUnitCategories.Add(existing);
        await context.SaveChangesAsync();

        var result = await CreateController(context, @"EXAMPLE\reactivator")
            .CreateUnitCategory(CreateValidRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SmsUnitCategoryWriteResponseDto>(ok.Value);
        Assert.Equal(8, response.UcId);
        Assert.True(response.IsActive);
        var saved = Assert.Single(await context.SmsUnitCategories.ToListAsync());
        Assert.True(saved.IsActive);
        Assert.Equal(@"EXAMPLE\reactivator", saved.UpdateUser);
    }

    [Fact]
    public async Task Post_CategoryDoesNotBelongToProject_ReturnsBadRequestWithoutWriting()
    {
        await using var context = CreateContext();
        await SeedValidReferences(context);
        var request = CreateValidRequest();
        request.ProjectId = 99;

        var result = await CreateController(context)
            .CreateUnitCategory(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("does not belong", problem.Detail);
        Assert.Empty(await context.SmsUnitCategories.ToListAsync());
    }

    [Fact]
    public async Task Post_UnitDoesNotBelongToHospital_ReturnsBadRequestWithoutWriting()
    {
        await using var context = CreateContext();
        await SeedValidReferences(context);
        var request = CreateValidRequest();
        request.HospitalId = 99;

        var result = await CreateController(context)
            .CreateUnitCategory(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("does not belong", problem.Detail);
        Assert.Empty(await context.SmsUnitCategories.ToListAsync());
    }

    [Theory]
    [InlineData(0, 3, 20, 101)]
    [InlineData(2, 0, 20, 101)]
    [InlineData(2, 3, 0, 101)]
    [InlineData(2, 3, 20, 0)]
    [InlineData(-1, 3, 20, 101)]
    public async Task Post_NonPositiveIdentifier_ReturnsBadRequest(
        int projectId,
        int categoryId,
        int hospitalId,
        int smsUnitId)
    {
        await using var context = CreateContext();
        var request = new CreateSmsUnitCategoryRequest
        {
            ProjectId = projectId,
            CategoryId = categoryId,
            HospitalId = hospitalId,
            SmsUnitId = smsUnitId
        };

        var result = await CreateController(context)
            .CreateUnitCategory(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("positive", problem.Detail);
        Assert.Empty(await context.SmsUnitCategories.ToListAsync());
    }

    [Fact]
    public async Task Post_RequestCannotSpoofAuditUser()
    {
        await using var context = CreateContext();
        await SeedValidReferences(context);
        var request = JsonSerializer.Deserialize<CreateSmsUnitCategoryRequest>(
            """
            {
              "projectId": 2,
              "categoryId": 3,
              "hospitalId": 20,
              "smsUnitId": 101,
              "createUser": "forged.creator",
              "updateUser": "forged.updater"
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Null(typeof(CreateSmsUnitCategoryRequest).GetProperty("CreateUser"));
        Assert.Null(typeof(CreateSmsUnitCategoryRequest).GetProperty("UpdateUser"));

        await CreateController(context, @"EXAMPLE\trusted.user")
            .CreateUnitCategory(request, CancellationToken.None);

        var saved = Assert.Single(await context.SmsUnitCategories.ToListAsync());
        Assert.Equal(@"EXAMPLE\trusted.user", saved.UpdateUser);
    }

    [Fact]
    public async Task Post_MissingWindowsIdentity_IsRejectedWithoutWriting()
    {
        await using var context = CreateContext();
        await SeedValidReferences(context);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateController(context, userName: null).CreateUnitCategory(
                CreateValidRequest(),
                CancellationToken.None));

        Assert.Empty(await context.SmsUnitCategories.ToListAsync());
    }

    [Fact]
    public async Task PatchStatus_AuthenticatedUser_UpdatesStatusAndAuditUser()
    {
        await using var context = CreateContext();
        context.SmsUnitCategories.Add(CreateUnitCategory(1, 10, 3, isActive: true));
        await context.SaveChangesAsync();

        var controller = CreateController(context, @"EXAMPLE\test.user");
        var result = await controller.UpdateStatus(
            1,
            new UpdateSmsUnitCategoryStatusRequest { IsActive = false },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var saved = Assert.Single(await context.SmsUnitCategories.ToListAsync());
        Assert.False(saved.IsActive);
        Assert.Equal(@"EXAMPLE\test.user", saved.UpdateUser);
    }

    [Fact]
    public async Task PatchStatus_InactiveAssociation_CanBeActivated()
    {
        await using var context = CreateContext();
        context.SmsUnitCategories.Add(CreateUnitCategory(1, 10, 3, isActive: false));
        await context.SaveChangesAsync();

        var result = await CreateController(context, @"EXAMPLE\test.user").UpdateStatus(
            1,
            new UpdateSmsUnitCategoryStatusRequest { IsActive = true },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var saved = Assert.Single(await context.SmsUnitCategories.ToListAsync());
        Assert.True(saved.IsActive);
        Assert.Equal(@"EXAMPLE\test.user", saved.UpdateUser);
    }

    [Fact]
    public async Task PatchStatus_MissingWindowsIdentity_ReturnsUnauthorizedWithoutMutation()
    {
        await using var context = CreateContext();
        context.SmsUnitCategories.Add(CreateUnitCategory(1, 10, 3, isActive: true));
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateController(context, userName: null).UpdateStatus(
                1,
                new UpdateSmsUnitCategoryStatusRequest { IsActive = false },
                CancellationToken.None));

        var saved = Assert.Single(await context.SmsUnitCategories.ToListAsync());
        Assert.True(saved.IsActive);
        Assert.Null(saved.UpdateUser);
    }

    [Fact]
    public async Task PatchStatus_MissingIsActive_ReturnsBadRequestWithoutMutation()
    {
        await using var context = CreateContext();
        context.SmsUnitCategories.Add(CreateUnitCategory(1, 10, 3, isActive: true));
        await context.SaveChangesAsync();

        var result = await CreateController(context, @"EXAMPLE\test.user").UpdateStatus(
            1,
            new UpdateSmsUnitCategoryStatusRequest(),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("isActive", problem.Detail);
        Assert.True(Assert.Single(await context.SmsUnitCategories.ToListAsync()).IsActive);
    }

    [Fact]
    public async Task PatchStatus_UnknownId_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await CreateController(context, @"EXAMPLE\test.user").UpdateStatus(
            999,
            new UpdateSmsUnitCategoryStatusRequest { IsActive = false },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void WriteEndpoints_AreAuthorizedAndRequestsCannotSupplyAuditUsers()
    {
        var patchAction = typeof(SmsUnitCategoriesController)
            .GetMethod(nameof(SmsUnitCategoriesController.UpdateStatus));
        var postAction = typeof(SmsUnitCategoriesController)
            .GetMethod(nameof(SmsUnitCategoriesController.CreateUnitCategory));

        Assert.NotNull(patchAction);
        Assert.NotNull(postAction);
        Assert.NotEmpty(patchAction!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        Assert.NotEmpty(postAction!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        Assert.Null(typeof(UpdateSmsUnitCategoryStatusRequest).GetProperty("UpdateUser"));
        Assert.Null(typeof(CreateSmsUnitCategoryRequest).GetProperty("CreateUser"));
        Assert.Null(typeof(CreateSmsUnitCategoryRequest).GetProperty("UpdateUser"));
    }

    [Fact]
    public void ResponseDto_ContainsOnlyRequestedColumns()
    {
        var propertyNames = typeof(SmsUnitCategoryResponseDto)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            new[] { "CategoryId", "CreateDate", "IsActive", "SmsUnitId", "UcId" },
            propertyNames);
    }

    [Fact]
    public void DetailsDto_PreservesExistingFieldsAndAddsViewIdentifiers()
    {
        var propertyNames = typeof(SmsUnitCategoryDetailsDto)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "CategoryId",
                "CategoryName",
                "HospitalId",
                "HospitalName",
                "IsActive",
                "ProjectId",
                "ProjectName",
                "SmsUnitId",
                "UcId",
                "UnitId",
                "UnitName"
            },
            propertyNames);

        Assert.Equal(typeof(int?), typeof(SmsUnitCategoryDetailsDto).GetProperty("ProjectId")!.PropertyType);
        Assert.Equal(typeof(int), typeof(SmsUnitCategoryDetailsDto).GetProperty("CategoryId")!.PropertyType);
        Assert.Equal(typeof(int?), typeof(SmsUnitCategoryDetailsDto).GetProperty("HospitalId")!.PropertyType);
        Assert.Equal(typeof(int), typeof(SmsUnitCategoryDetailsDto).GetProperty("SmsUnitId")!.PropertyType);
        Assert.Equal(typeof(int?), typeof(SmsUnitCategoryDetailsDto).GetProperty("UnitId")!.PropertyType);
    }

    [Fact]
    public void WriteContracts_ContainOnlyServerSupportedFields()
    {
        var createFields = typeof(CreateSmsUnitCategoryRequest)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();
        var updateFields = typeof(UpdateSmsUnitCategoryStatusRequest)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();
        var responseFields = typeof(SmsUnitCategoryWriteResponseDto)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            new[] { "CategoryId", "HospitalId", "ProjectId", "SmsUnitId" },
            createFields);
        Assert.Equal(new[] { "IsActive" }, updateFields);
        Assert.Equal(
            new[] { "CategoryId", "IsActive", "SmsUnitId", "UcId" },
            responseFields);
    }

    [Fact]
    public void EntityMapping_UsesSmsUnitCategorySchemaAndNullableUnicodeAuditUser()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(SmsUnitCategory));
        Assert.NotNull(entity);
        var table = StoreObjectIdentifier.Table("sms_unit_category", "dbo");

        Assert.Equal("uc_id", entity!.FindProperty(nameof(SmsUnitCategory.UcId))!.GetColumnName(table));
        Assert.Equal("sms_unit_id", entity.FindProperty(nameof(SmsUnitCategory.SmsUnitId))!.GetColumnName(table));
        Assert.Equal("category_id", entity.FindProperty(nameof(SmsUnitCategory.CategoryId))!.GetColumnName(table));

        var updateUser = entity.FindProperty(nameof(SmsUnitCategory.UpdateUser));
        Assert.NotNull(updateUser);
        Assert.Equal("update_user", updateUser!.GetColumnName(table));
        Assert.Equal(50, updateUser.GetMaxLength());
        Assert.True(updateUser.IsNullable);
        Assert.True(updateUser.IsUnicode());
    }

    [Fact]
    public void DetailsViewMapping_UsesExpectedReadOnlyViewAndColumns()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(SmsUnitCategoryView));
        Assert.NotNull(entity);
        Assert.Null(entity!.FindPrimaryKey());
        Assert.Equal("sms_unit_category_v", entity.GetViewName());
        Assert.Equal("dbo", entity.GetViewSchema());

        var view = StoreObjectIdentifier.View("sms_unit_category_v", "dbo");
        Assert.Equal("uc_id", entity.FindProperty(nameof(SmsUnitCategoryView.UcId))!.GetColumnName(view));
        Assert.Equal("project_id", entity.FindProperty(nameof(SmsUnitCategoryView.ProjectId))!.GetColumnName(view));
        Assert.Equal("project_name", entity.FindProperty(nameof(SmsUnitCategoryView.ProjectName))!.GetColumnName(view));
        Assert.Equal("category_id", entity.FindProperty(nameof(SmsUnitCategoryView.CategoryId))!.GetColumnName(view));
        Assert.Equal("category_name", entity.FindProperty(nameof(SmsUnitCategoryView.CategoryName))!.GetColumnName(view));
        Assert.Equal("hospital_id", entity.FindProperty(nameof(SmsUnitCategoryView.HospitalId))!.GetColumnName(view));
        Assert.Equal("hospital_name", entity.FindProperty(nameof(SmsUnitCategoryView.HospitalName))!.GetColumnName(view));
        Assert.Equal("sms_unit_id", entity.FindProperty(nameof(SmsUnitCategoryView.SmsUnitId))!.GetColumnName(view));
        Assert.Equal("unit_id", entity.FindProperty(nameof(SmsUnitCategoryView.UnitId))!.GetColumnName(view));
        Assert.Equal("unit_name", entity.FindProperty(nameof(SmsUnitCategoryView.UnitName))!.GetColumnName(view));
        Assert.Equal("is_active", entity.FindProperty(nameof(SmsUnitCategoryView.IsActive))!.GetColumnName(view));

        Assert.True(entity.FindProperty(nameof(SmsUnitCategoryView.ProjectId))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(SmsUnitCategoryView.CategoryId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(SmsUnitCategoryView.HospitalId))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(SmsUnitCategoryView.SmsUnitId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(SmsUnitCategoryView.UnitId))!.IsNullable);
    }

    [Fact]
    public void DetailsViewQuery_ReadsFromSmsUnitCategoryView()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HospitalSmsViewQueryTest;Trusted_Connection=True")
            .Options;

        using var context = new HospitalSmsDbContext(options);
        var sql = context.SmsUnitCategoryViews
            .AsNoTracking()
            .OrderBy(item => item.UcId)
            .ToQueryString();

        Assert.Contains("[dbo].[sms_unit_category_v]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[project_id]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[project_name]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[category_id]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[category_name]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[hospital_id]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[hospital_name]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[sms_unit_id]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[unit_id]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[unit_name]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WHERE", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static async Task SeedValidReferences(HospitalSmsDbContext context)
    {
        context.SmsCategories.Add(new SmsCategory
        {
            CategoryId = 3,
            CategoryName = "Maternity",
            ProjectId = 2
        });
        context.SmsUnits.Add(new SmsUnit
        {
            SmsUnitId = 101,
            HospitalId = 20,
            UnitId = 1549947,
            UnitName = "Maternity ER"
        });
        await context.SaveChangesAsync();
    }

    private static CreateSmsUnitCategoryRequest CreateValidRequest() => new()
    {
        ProjectId = 2,
        CategoryId = 3,
        HospitalId = 20,
        SmsUnitId = 101
    };

    private static SmsUnitCategoriesController CreateController(
        HospitalSmsDbContext context,
        string? userName = TestCurrentUserService.DefaultUserName)
    {
        return new SmsUnitCategoriesController(
            context,
            new TestCurrentUserService(userName));
    }

    private static SmsUnitCategory CreateUnitCategory(
        int ucId,
        int smsUnitId,
        int categoryId,
        bool isActive) => new()
        {
            UcId = ucId,
            SmsUnitId = smsUnitId,
            CategoryId = categoryId,
            IsActive = isActive,
            CreateDate = new DateTime(2026, 9, 6, 8, 0, 0)
        };
}
