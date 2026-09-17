using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Admin.Api.Models;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class SmsRulesControllerTests
{
    [Fact]
    public async Task CreateRule_ValidRequest_SavesRuleIncludingMaxTimeTokef()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.AddRange(
            CreateTemplateTrigger(10, 1, 100),
            CreateTemplateTrigger(11, 2, 101));
        await context.SaveChangesAsync();

        var request = new CreateSmsRuleRequest
        {
            TtId = 10,
            DependOnTtId = 11,
            DependencyMaxMinutes = 30,
            MaxTimeTokefInMinutes = 120,
            IsConstant = false,
            IsRecurring = true,
            RecurringIntervalDays = 2,
            RecurringTimeOfDay = "08:30",
            RecurringStopCondition = SmsRuleStopConditions.Discharged
        };

        var result = await CreateController(context).CreateRule(request, CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.True(response.RuleId > 0);
        Assert.Equal(120, response.MaxTimeTokefInMinutes);
        Assert.Equal(SmsRuleStopConditions.Discharged, response.RecurringStopCondition);

        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.Equal(10, savedRule.TtId);
        Assert.Equal(11, savedRule.DependOnTtId);
        Assert.Equal(30, savedRule.DependencyMaxMinutes);
        Assert.Equal(120, savedRule.MaxTimeTokefInMinutes);
        Assert.Equal("08:30", savedRule.RecurringTimeOfDay);
        Assert.Equal(SmsRuleStopConditions.Discharged, savedRule.RecurringStopCondition);
        Assert.NotEqual(default, savedRule.CreateDate);
        Assert.Null(savedRule.UpdateDate);
    }

    [Fact]
    public async Task CreateRule_NeverStopCondition_SavesAndReturnsExactValue()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(10, 1, 100));
        await context.SaveChangesAsync();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest
            {
                TtId = 10,
                IsRecurring = true,
                RecurringIntervalDays = 5,
                RecurringTimeOfDay = "20:00",
                RecurringStopCondition = SmsRuleStopConditions.Never
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(SmsRuleStopConditions.Never, response.RecurringStopCondition);
        Assert.Equal(
            SmsRuleStopConditions.Never,
            Assert.Single(await context.SmsRules.ToListAsync()).RecurringStopCondition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("Never")]
    [InlineData("DISCHARGED")]
    public async Task CreateRule_InvalidOrMissingStopConditionWhenRecurring_ReturnsBadRequest(
        string? stopCondition)
    {
        await using var context = CreateContext();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest
            {
                TtId = 10,
                IsRecurring = true,
                RecurringIntervalDays = 5,
                RecurringTimeOfDay = "20:00",
                RecurringStopCondition = stopCondition
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
        Assert.Empty(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public async Task CreateRule_OmittedOptionalValues_UsesRequestDefaults()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(10, 1, 100));
        await context.SaveChangesAsync();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest { TtId = 10 },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(0, response.DependencyMaxMinutes);
        Assert.Equal(60, response.MaxTimeTokefInMinutes);
    }

    [Fact]
    public async Task CreateRule_ValidTimeRange_SavesAndReturnsBothValues()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(10, 1, 100));
        await context.SaveChangesAsync();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest
            {
                TtId = 10,
                StartTimeRange = "22:00",
                EndTimeRange = "06:00"
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal("22:00", response.StartTimeRange);
        Assert.Equal("06:00", response.EndTimeRange);

        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.Equal("22:00", savedRule.StartTimeRange);
        Assert.Equal("06:00", savedRule.EndTimeRange);
    }

    [Fact]
    public async Task CreateRule_OnlyOneTimeRangeValue_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest
            {
                TtId = 10,
                StartTimeRange = "08:00"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
        Assert.Empty(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public async Task CreateRule_InvalidTimeRange_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest
            {
                TtId = 10,
                StartTimeRange = "25:00",
                EndTimeRange = "17:00"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
        Assert.Empty(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public async Task CreateRule_TemplateAlreadyHasRule_ReturnsConflictWithoutUpdatingRule()
    {
        await using var context = CreateContext();
        var originalCreateDate = new DateTime(2026, 8, 20, 8, 0, 0);
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(10, 1, 100));
        context.SmsRules.Add(new SmsRule
        {
            RuleId = 1,
            TtId = 10,
            MaxTimeTokefInMinutes = 60,
            IsRecurring = false,
            CreateDate = originalCreateDate
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest
            {
                TtId = 10,
                MaxTimeTokefInMinutes = 180,
                IsRecurring = true,
                RecurringIntervalDays = 3,
                RecurringStopCondition = SmsRuleStopConditions.Discharged
            },
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("A rule already exists for this template.", problem.Title);
        Assert.Contains("PUT /api/SmsRules/1", problem.Detail);

        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.Equal(originalCreateDate, savedRule.CreateDate);
        Assert.Equal(60, savedRule.MaxTimeTokefInMinutes);
        Assert.False(savedRule.IsRecurring);
        Assert.Null(savedRule.UpdateDate);
    }

    [Fact]
    public async Task UpdateRule_ExistingRule_UpdatesWithoutCreatingAnotherRule()
    {
        await using var context = CreateContext();
        var createDate = new DateTime(2026, 8, 20, 8, 0, 0);
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(10, 1, 100));
        context.SmsRules.Add(new SmsRule
        {
            RuleId = 1,
            TtId = 10,
            MaxTimeTokefInMinutes = 60,
            CreateDate = createDate
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            1,
            new UpdateSmsRuleRequest
            {
                TtId = 10,
                MaxTimeTokefInMinutes = 120,
                IsRecurring = true,
                RecurringIntervalDays = 2,
                RecurringTimeOfDay = "13:00",
                RecurringStopCondition = "discharged"
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(1, response.RuleId);
        Assert.Equal(120, response.MaxTimeTokefInMinutes);
        Assert.True(response.IsRecurring);
        Assert.Equal(SmsRuleStopConditions.Discharged, response.RecurringStopCondition);
        Assert.NotNull(response.UpdateDate);

        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.Equal(createDate, savedRule.CreateDate);
        Assert.Equal(120, savedRule.MaxTimeTokefInMinutes);
        Assert.Equal(SmsRuleStopConditions.Discharged, savedRule.RecurringStopCondition);
    }

    [Fact]
    public async Task UpdateRule_BothTimeRangeValues_UpdatesRangeAndServerAudit()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                StartTimeRange = "09:15",
                EndTimeRange = "18:45"
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal("09:15", response.StartTimeRange);
        Assert.Equal("18:45", response.EndTimeRange);
        Assert.Equal(TestCurrentUserService.DefaultUserName, response.UpdateUser);
        Assert.NotNull(response.UpdateDate);

        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.Equal("09:15", savedRule.StartTimeRange);
        Assert.Equal("18:45", savedRule.EndTimeRange);
        Assert.Equal(TestCurrentUserService.DefaultUserName, savedRule.UpdateUser);
    }

    [Fact]
    public async Task UpdateRule_BothTimeRangeValuesAreNull_ClearsRange()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                StartTimeRange = null,
                EndTimeRange = null
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Null(response.StartTimeRange);
        Assert.Null(response.EndTimeRange);

        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.Null(savedRule.StartTimeRange);
        Assert.Null(savedRule.EndTimeRange);
    }

    [Fact]
    public async Task UpdateRule_TimeRangeFieldsOmitted_PreservesExistingRange()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest { TtId = 15 },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal("08:00", response.StartTimeRange);
        Assert.Equal("17:00", response.EndTimeRange);
    }

    [Fact]
    public async Task UpdateRule_OnlyOneTimeRangeFieldSpecified_ReturnsBadRequestWithoutChanges()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                StartTimeRange = "09:00"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.Equal("08:00", savedRule.StartTimeRange);
        Assert.Equal("17:00", savedRule.EndTimeRange);
        Assert.Null(savedRule.UpdateDate);
    }

    [Fact]
    public async Task UpdateRule_InvalidTimeRange_ReturnsBadRequestWithoutChanges()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                StartTimeRange = "25:00",
                EndTimeRange = "17:00"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.Equal("08:00", savedRule.StartTimeRange);
        Assert.Equal("17:00", savedRule.EndTimeRange);
        Assert.Null(savedRule.UpdateDate);
    }

    [Fact]
    public async Task UpdateRule_Rule4Only_UpdatesDependencyAndPreservesRules3And5()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.AddRange(
            CreateTemplateTrigger(15, 1, 100),
            CreateTemplateTrigger(12, 2, 101));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                DependOnTtId = 12,
                DependencyMaxMinutes = 30
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(12, response.DependOnTtId);
        Assert.Equal(30, response.DependencyMaxMinutes);
        Assert.Equal(50, response.MaxTimeTokefInMinutes);
        Assert.True(response.IsConstant);
        Assert.True(response.IsRecurring);
        Assert.Equal(2, response.RecurringIntervalDays);
        Assert.Equal("13:00", response.RecurringTimeOfDay);
        Assert.Equal("discharged", response.RecurringStopCondition);
        Assert.Single(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public async Task UpdateRule_ExplicitNullDependency_ClearsRule4AndPreservesRule5()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.AddRange(
            CreateTemplateTrigger(15, 1, 100),
            CreateTemplateTrigger(12, 2, 101));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                DependOnTtId = null
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Null(response.DependOnTtId);
        Assert.Equal(0, response.DependencyMaxMinutes);
        Assert.True(response.IsRecurring);
        Assert.Equal(2, response.RecurringIntervalDays);
        Assert.Equal("13:00", response.RecurringTimeOfDay);
        Assert.Single(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public async Task UpdateRule_Rule5Only_EnablesRecurringAndPreservesRules3And4()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.AddRange(
            CreateTemplateTrigger(15, 1, 100),
            CreateTemplateTrigger(12, 2, 101));
        var rule = CreateConfiguredRule();
        rule.IsRecurring = false;
        context.SmsRules.Add(rule);
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                IsRecurring = true,
                RecurringIntervalDays = 2,
                RecurringTimeOfDay = "13:00",
                RecurringStopCondition = SmsRuleStopConditions.Discharged
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.True(response.IsRecurring);
        Assert.Equal(2, response.RecurringIntervalDays);
        Assert.Equal("13:00", response.RecurringTimeOfDay);
        Assert.Equal("discharged", response.RecurringStopCondition);
        Assert.Equal(12, response.DependOnTtId);
        Assert.Equal(30, response.DependencyMaxMinutes);
        Assert.Equal(50, response.MaxTimeTokefInMinutes);
        Assert.True(response.IsConstant);
        Assert.Single(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public async Task UpdateRule_DisablingRule5_PreservesRecurringConfiguration()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                IsRecurring = false
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.False(response.IsRecurring);
        Assert.Equal(2, response.RecurringIntervalDays);
        Assert.Equal("13:00", response.RecurringTimeOfDay);
        Assert.Equal("discharged", response.RecurringStopCondition);
    }

    [Theory]
    [InlineData("25:70")]
    [InlineData("24:00")]
    [InlineData("7:00")]
    [InlineData("13:0")]
    public async Task UpdateRule_InvalidRecurringTime_ReturnsBadRequestWithoutChanges(
        string timeOfDay)
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        var rule = CreateConfiguredRule();
        rule.IsRecurring = false;
        context.SmsRules.Add(rule);
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                IsRecurring = true,
                RecurringIntervalDays = 2,
                RecurringTimeOfDay = timeOfDay,
                RecurringStopCondition = "discharged"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.False(savedRule.IsRecurring);
        Assert.Equal("13:00", savedRule.RecurringTimeOfDay);
    }

    [Fact]
    public async Task UpdateRule_NonPositiveRecurringInterval_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                RecurringIntervalDays = 0
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task UpdateRule_UnsupportedStopCondition_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                RecurringStopCondition = "unknown"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Theory]
    [InlineData(SmsRuleStopConditions.Discharged)]
    [InlineData(SmsRuleStopConditions.Never)]
    public async Task UpdateRule_SupportedStopCondition_UpdatesAndReturnsExactValue(
        string stopCondition)
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                RecurringStopCondition = stopCondition
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(stopCondition, response.RecurringStopCondition);
        Assert.Equal(
            stopCondition,
            Assert.Single(await context.SmsRules.ToListAsync()).RecurringStopCondition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UpdateRule_MissingStopConditionWhileRecurring_ReturnsBadRequestWithoutChanges(
        string? stopCondition)
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                RecurringStopCondition = stopCondition
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
        Assert.Equal(
            SmsRuleStopConditions.Discharged,
            Assert.Single(await context.SmsRules.ToListAsync()).RecurringStopCondition);
    }

    [Fact]
    public async Task UpdateRule_EnablingRecurringWithNoExistingStopCondition_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        var rule = CreateConfiguredRule();
        rule.IsRecurring = false;
        rule.RecurringStopCondition = null;
        context.SmsRules.Add(rule);
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                IsRecurring = true
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
        var savedRule = Assert.Single(await context.SmsRules.ToListAsync());
        Assert.False(savedRule.IsRecurring);
        Assert.Null(savedRule.RecurringStopCondition);
    }

    [Fact]
    public async Task UpdateRule_DisablingRecurring_AllowsExplicitNullStopCondition()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                IsRecurring = false,
                RecurringStopCondition = null
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.False(response.IsRecurring);
        Assert.Null(response.RecurringStopCondition);
        Assert.Null(Assert.Single(await context.SmsRules.ToListAsync()).RecurringStopCondition);
    }

    [Fact]
    public async Task UpdateRule_StopConditionOmitted_PreservesExistingValue()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        context.SmsRules.Add(CreateConfiguredRule());
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            4,
            new UpdateSmsRuleRequest
            {
                TtId = 15,
                MaxTimeTokefInMinutes = 90
            },
            CancellationToken.None);

        var response = GetOkResponse(result);
        Assert.Equal(SmsRuleStopConditions.Discharged, response.RecurringStopCondition);
        Assert.Equal(
            SmsRuleStopConditions.Discharged,
            Assert.Single(await context.SmsRules.ToListAsync()).RecurringStopCondition);
    }

    [Fact]
    public async Task UpdateRule_UnknownRuleId_ReturnsNotFoundAndDoesNotCreateRule()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(15, 1, 100));
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            999,
            new UpdateSmsRuleRequest { TtId = 15 },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public void UpdateRuleRequest_DistinguishesOmittedFieldsFromExplicitNull()
    {
        const string json = """
            {
              "ttId": 15,
              "dependOnTtId": null,
              "startTimeRange": null,
              "endTimeRange": null,
              "isRecurring": false
            }
            """;

        var request = JsonSerializer.Deserialize<UpdateSmsRuleRequest>(
            json,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.NotNull(request);
        Assert.True(request!.DependOnTtIdSpecified);
        Assert.Null(request.DependOnTtId);
        Assert.True(request.StartTimeRangeSpecified);
        Assert.True(request.EndTimeRangeSpecified);
        Assert.Null(request.StartTimeRange);
        Assert.Null(request.EndTimeRange);
        Assert.True(request.IsRecurringSpecified);
        Assert.False(request.IsRecurring);
        Assert.False(request.DependencyMaxMinutesSpecified);
        Assert.False(request.MaxTimeTokefInMinutesSpecified);
        Assert.False(request.RecurringTimeOfDaySpecified);
        Assert.False(request.RecurringStopConditionSpecified);
    }

    [Fact]
    public void RuleTimeRangeContracts_SerializeUsingCamelCase()
    {
        var json = JsonSerializer.Serialize(
            new SmsRuleResponse
            {
                StartTimeRange = "08:00",
                EndTimeRange = "17:00"
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"startTimeRange\":\"08:00\"", json);
        Assert.Contains("\"endTimeRange\":\"17:00\"", json);
        Assert.DoesNotContain("StartTimeRange", json);
        Assert.DoesNotContain("EndTimeRange", json);
    }

    [Theory]
    [InlineData(typeof(SmsRule))]
    [InlineData(typeof(CreateSmsRuleRequest))]
    [InlineData(typeof(UpdateSmsRuleRequest))]
    [InlineData(typeof(SmsRuleResponse))]
    [InlineData(typeof(SmsTriggerSettingsDto))]
    public void ActiveRuleContracts_UseRule6TimeRangeInsteadOfObsoleteFallbackFields(
        Type contractType)
    {
        Assert.NotNull(contractType.GetProperty("StartTimeRange"));
        Assert.NotNull(contractType.GetProperty("EndTimeRange"));
        Assert.Null(contractType.GetProperty("OnetimeFallbackEnabled"));
        Assert.Null(contractType.GetProperty("OnetimeFallbackTime"));
    }

    [Fact]
    public async Task UpdateRule_TargetTemplateAlreadyHasRule_ReturnsConflict()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.AddRange(
            CreateTemplateTrigger(10, 1, 100),
            CreateTemplateTrigger(20, 2, 101));
        context.SmsRules.AddRange(
            new SmsRule { RuleId = 1, TtId = 10, CreateDate = DateTime.Now },
            new SmsRule { RuleId = 2, TtId = 20, CreateDate = DateTime.Now });
        await context.SaveChangesAsync();

        var result = await CreateController(context).UpdateRule(
            1,
            new UpdateSmsRuleRequest { TtId = 20 },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(2, await context.SmsRules.CountAsync());
        Assert.Equal(10, (await context.SmsRules.FindAsync(1))!.TtId);
    }

    [Fact]
    public async Task CreateRule_NegativeMaxTimeTokef_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest { MaxTimeTokefInMinutes = -1 },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
        Assert.Empty(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public async Task CreateRule_UnknownTemplateTrigger_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest { TtId = 999 },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public async Task CreateRule_UnknownDependencyTemplateTrigger_ReturnsNotFound()
    {
        await using var context = CreateContext();
        context.SmsTemplateTriggers.Add(CreateTemplateTrigger(10, 1, 100));
        await context.SaveChangesAsync();

        var result = await CreateController(context).CreateRule(
            new CreateSmsRuleRequest { TtId = 10, DependOnTtId = 999 },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(await context.SmsRules.ToListAsync());
    }

    [Fact]
    public async Task GetRules_ValidScope_ReturnsOnlyMatchingHierarchyWithNullableRuleAndNamedExclusions()
    {
        await using var context = CreateContext();
        SeedReadHierarchy(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetRules(
            new SmsRulesQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);

        var items = GetOkHierarchy(result);
        Assert.Collection(
            items,
            item =>
            {
                Assert.Equal(1, item.ProjectId);
                Assert.Equal(1, item.Category.CategoryId);
                Assert.Equal(101, item.Template.TemplateId);
                Assert.Equal(1001, item.TemplateTrigger.TtId);
                Assert.Equal("Admission", item.TemplateTrigger.TriggerName);
                Assert.Equal(501, item.Rule?.RuleId);
                var exclusion = Assert.Single(item.ExcludedHospitalUnits);
                Assert.Equal(20, exclusion.HospitalId);
                Assert.Equal("Hospital A", exclusion.HospitalName);
                Assert.Equal(200, exclusion.UnitId);
                Assert.Equal("Emergency", exclusion.UnitName);
            },
            item =>
            {
                Assert.Equal(102, item.Template.TemplateId);
                Assert.Equal(1002, item.TemplateTrigger.TtId);
                Assert.Equal("Discharge", item.TemplateTrigger.TriggerName);
                Assert.Null(item.Rule);
                Assert.Empty(item.ExcludedHospitalUnits);
            });
    }

    [Fact]
    public async Task GetRules_ValidCategoryWithoutTemplateTriggers_ReturnsEmptyArray()
    {
        await using var context = CreateContext();
        SeedReadHierarchy(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetRules(
            new SmsRulesQuery { ProjectId = 1, CategoryId = 2 },
            CancellationToken.None);

        Assert.Empty(GetOkHierarchy(result));
    }

    [Fact]
    public async Task GetRulesAndGetRulesByTemplate_ReturnTimeRangeAndStopCondition()
    {
        await using var context = CreateContext();
        SeedReadHierarchy(context);
        await context.SaveChangesAsync();

        var allRulesResult = await CreateController(context).GetRules(
            new SmsRulesQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);
        var allRulesItem = GetOkHierarchy(allRulesResult)
            .Single(item => item.Rule?.RuleId == 501);

        var templateRulesResult = await CreateController(context).GetRulesByTemplate(
            101,
            new SmsRulesQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);
        var templateRulesItem = Assert.Single(GetOkHierarchy(templateRulesResult));

        Assert.Equal("08:00", allRulesItem.Rule?.StartTimeRange);
        Assert.Equal("17:00", allRulesItem.Rule?.EndTimeRange);
        Assert.Equal("08:00", templateRulesItem.Rule?.StartTimeRange);
        Assert.Equal("17:00", templateRulesItem.Rule?.EndTimeRange);
        Assert.Equal(SmsRuleStopConditions.Never, allRulesItem.Rule?.RecurringStopCondition);
        Assert.Equal(SmsRuleStopConditions.Never, templateRulesItem.Rule?.RecurringStopCondition);
    }

    [Fact]
    public void Model_MapsTimeRangesToNullableSqlTimeColumns()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(SmsRule));
        var table = StoreObjectIdentifier.Table("sms_rules", "dbo");

        AssertTimeRangeMapping(entity, table, nameof(SmsRule.StartTimeRange), "start_time_range");
        AssertTimeRangeMapping(entity, table, nameof(SmsRule.EndTimeRange), "end_time_range");
    }

    [Fact]
    public void SmsRuleQuery_UsesTimeRangeColumnsAndConvertedTimeValue()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HospitalSmsRuleQueryTest;Trusted_Connection=True")
            .Options;

        using var context = new HospitalSmsDbContext(options);
        var sql = context.SmsRules
            .Where(rule => rule.StartTimeRange == "08:00")
            .ToQueryString();

        Assert.Contains("[start_time_range]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[end_time_range]", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("08:00", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onetime_fallback", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(1, null)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public async Task GetRules_InvalidIdentifiers_ReturnsBadRequest(
        int? projectId,
        int? categoryId)
    {
        await using var context = CreateContext();

        var result = await CreateController(context).GetRules(
            new SmsRulesQuery { ProjectId = projectId, CategoryId = categoryId },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Theory]
    [InlineData(999, 1)]
    [InlineData(1, 999)]
    public async Task GetRules_UnknownProjectOrCategory_ReturnsNotFound(
        int projectId,
        int categoryId)
    {
        await using var context = CreateContext();
        SeedReadHierarchy(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetRules(
            new SmsRulesQuery { ProjectId = projectId, CategoryId = categoryId },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRules_CategoryFromAnotherProject_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        SeedReadHierarchy(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetRules(
            new SmsRulesQuery { ProjectId = 1, CategoryId = 5 },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task GetRulesByTemplate_ValidScope_ReturnsOnlyRequestedTemplate()
    {
        await using var context = CreateContext();
        SeedReadHierarchy(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetRulesByTemplate(
            102,
            new SmsRulesQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);

        var item = Assert.Single(GetOkHierarchy(result));
        Assert.Equal(102, item.Template.TemplateId);
        Assert.Null(item.Rule);
    }

    [Fact]
    public async Task GetRulesByTemplate_TemplateFromAnotherCategory_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        SeedReadHierarchy(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetRulesByTemplate(
            201,
            new SmsRulesQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task GetRulesByTemplate_UnknownTemplate_ReturnsNotFound()
    {
        await using var context = CreateContext();
        SeedReadHierarchy(context);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetRulesByTemplate(
            999,
            new SmsRulesQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRulesByTemplate_NonPositiveTemplateId_ReturnsBadRequest()
    {
        await using var context = CreateContext();

        var result = await CreateController(context).GetRulesByTemplate(
            0,
            new SmsRulesQuery { ProjectId = 1, CategoryId = 1 },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static SmsRulesController CreateController(HospitalSmsDbContext context) =>
        new(context, new TestCurrentUserService());

    private static SmsRuleResponse GetOkResponse(ActionResult<SmsRuleResponse> result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<SmsRuleResponse>(okResult.Value);
    }

    private static List<SmsRuleHierarchyDto> GetOkHierarchy(
        ActionResult<IEnumerable<SmsRuleHierarchyDto>> result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IEnumerable<SmsRuleHierarchyDto>>(okResult.Value).ToList();
    }

    private static SmsTemplateTrigger CreateTemplateTrigger(
        int ttId,
        int templateId,
        int triggerId) => new()
        {
            TtId = ttId,
            TemplateId = templateId,
            TriggerId = triggerId
        };

    private static SmsRule CreateConfiguredRule() => new()
    {
        RuleId = 4,
        TtId = 15,
        DependOnTtId = 12,
        DependencyMaxMinutes = 30,
        MaxTimeTokefInMinutes = 50,
        IsConstant = true,
        StartTimeRange = "08:00",
        EndTimeRange = "17:00",
        IsRecurring = true,
        RecurringIntervalDays = 2,
        RecurringTimeOfDay = "13:00",
        RecurringStopCondition = SmsRuleStopConditions.Discharged,
        CreateDate = new DateTime(2026, 8, 20, 8, 0, 0)
    };

    private static void SeedReadHierarchy(HospitalSmsDbContext context)
    {
        context.SmsProjects.AddRange(
            new SmsProject { ProjectId = 1, ProjectName = "Project A" },
            new SmsProject { ProjectId = 2, ProjectName = "Project B" });
        context.SmsCategories.AddRange(
            new SmsCategory { CategoryId = 1, CategoryName = "Category A", ProjectId = 1 },
            new SmsCategory { CategoryId = 2, CategoryName = "Empty category", ProjectId = 1 },
            new SmsCategory { CategoryId = 5, CategoryName = "Category B", ProjectId = 2 });
        context.SmsTemplates.AddRange(
            CreateTemplate(101, 1),
            CreateTemplate(102, 1),
            CreateTemplate(201, 5));
        context.SmsTriggerCatalogs.AddRange(
            new SmsTriggerCatalog
            {
                TriggerId = 10,
                TriggerCode = "ADMISSION",
                TriggerName = "Admission"
            },
            new SmsTriggerCatalog
            {
                TriggerId = 11,
                TriggerCode = "DISCHARGE",
                TriggerName = "Discharge"
            });
        context.SmsTemplateTriggers.AddRange(
            CreateTemplateTrigger(1001, 101, 10),
            CreateTemplateTrigger(1002, 102, 11),
            CreateTemplateTrigger(2001, 201, 10));
        context.SmsRules.AddRange(
            new SmsRule
            {
                RuleId = 501,
                TtId = 1001,
                MaxTimeTokefInMinutes = 60,
                StartTimeRange = "08:00",
                EndTimeRange = "17:00",
                IsRecurring = true,
                RecurringIntervalDays = 1,
                RecurringTimeOfDay = "09:00",
                RecurringStopCondition = SmsRuleStopConditions.Never,
                CreateDate = new DateTime(2026, 8, 25)
            },
            new SmsRule
            {
                RuleId = 502,
                TtId = 2001,
                MaxTimeTokefInMinutes = 120,
                CreateDate = new DateTime(2026, 8, 25)
            });
        context.SmsHospitals.AddRange(
            CreateHospital(20, "Hospital A"),
            CreateHospital(21, "Hospital B"));
        context.SmsUnits.AddRange(
            new SmsUnit { HospitalId = 20, UnitId = 200, UnitName = "Emergency" },
            new SmsUnit { HospitalId = 21, UnitId = 210, UnitName = "Ward" });
        context.SmsRejectHospitalUnits.AddRange(
            new SmsRejectHospitalUnit { TemplateId = 101, HospitalId = 20, UnitId = 200 },
            new SmsRejectHospitalUnit { TemplateId = 201, HospitalId = 21, UnitId = 210 });
    }

    private static SmsTemplate CreateTemplate(int templateId, int categoryId) => new()
    {
        TemplateId = templateId,
        TemplateCode = $"Template {templateId}",
        TemplateName = $"Template {templateId}",
        CategoryId = categoryId
    };

    private static SmsHospital CreateHospital(int hospitalId, string hospitalName) => new()
    {
        HospitalId = hospitalId,
        HospitalTypeId = 1,
        HospitalDescription = $"H{hospitalId}",
        HospitalName = hospitalName
    };

    private static void AssertTimeRangeMapping(
        IEntityType? entity,
        StoreObjectIdentifier table,
        string propertyName,
        string columnName)
    {
        var property = entity?.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(columnName, property!.GetColumnName(table));
        Assert.Equal(
            "time(7)",
            property.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value);
        Assert.Equal(5, property.GetMaxLength());
        Assert.True(property.IsNullable);
        Assert.Equal(typeof(TimeSpan?), property.GetValueConverter()?.ProviderClrType);
    }
}
