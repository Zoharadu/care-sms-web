using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Admin.Api.Identity;
using HospitalSms.Application.Abstractions;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class RuleSavingApiIntegrationTests
{
    private const string ConnectedUser = @"EXAMPLE\test.user";

    [Fact]
    public async Task PutRejectHospitalUnits_ReplacesAndClearsExclusionsOverHttp()
    {
        await using var api = await TestApi.Start();
        await api.WithContext(async context =>
        {
            context.SmsTemplates.Add(new SmsTemplate
            {
                TemplateId = 23,
                TemplateCode = "APPOINTMENT_REMINDER",
                TemplateName = "Appointment reminder",
                CategoryId = 1
            });
            context.SmsHospitals.Add(new SmsHospital
            {
                HospitalId = 1,
                HospitalTypeId = 1,
                HospitalDescription = "H1",
                HospitalName = "Hospital 1"
            });
            context.SmsUnits.AddRange(
                new SmsUnit { HospitalId = 1, UnitId = 10, UnitName = "Unit 10" },
                new SmsUnit { HospitalId = 1, UnitId = 11, UnitName = "Unit 11" });
            context.SmsRejectHospitalUnits.Add(
                new SmsRejectHospitalUnit { TemplateId = 23, HospitalId = 1, UnitId = 10 });
            await context.SaveChangesAsync();
        });

        var replaceResponse = await api.Client.PutAsJsonAsync(
            "/api/SmsRejectHospitalUnits/replace",
            new ReplaceRejectHospitalUnitsRequest
            {
                TemplateId = 23,
                Items =
                [
                    new RejectHospitalUnitItemRequest { HospitalId = 1, UnitId = 10 },
                    new RejectHospitalUnitItemRequest { HospitalId = 1, UnitId = 11 },
                    new RejectHospitalUnitItemRequest { HospitalId = 1, UnitId = 11 }
                ]
            });

        replaceResponse.EnsureSuccessStatusCode();
        var replaced = await replaceResponse.Content
            .ReadFromJsonAsync<ReplaceRejectHospitalUnitsResponse>();
        Assert.NotNull(replaced);
        Assert.Equal(23, replaced!.TemplateId);
        Assert.Equal(2, replaced.SavedCount);
        Assert.Equal(2, replaced.Items.Count);
        await api.WithContext(async context =>
            Assert.Equal(2, await context.SmsRejectHospitalUnits.CountAsync()));

        var clearResponse = await api.Client.PutAsJsonAsync(
            "/api/SmsRejectHospitalUnits/replace",
            new ReplaceRejectHospitalUnitsRequest { TemplateId = 23, Items = [] });

        clearResponse.EnsureSuccessStatusCode();
        var cleared = await clearResponse.Content
            .ReadFromJsonAsync<ReplaceRejectHospitalUnitsResponse>();
        Assert.NotNull(cleared);
        Assert.Equal(0, cleared!.SavedCount);
        Assert.Empty(cleared.Items);
        await api.WithContext(async context =>
            Assert.Empty(await context.SmsRejectHospitalUnits.ToListAsync()));
    }

    [Fact]
    public async Task PutRule_UpdatesOnlySentFieldsAndNeverCreatesMissingRuleOverHttp()
    {
        await using var api = await TestApi.Start();
        await api.WithContext(async context =>
        {
            context.SmsTemplateTriggers.AddRange(
                new SmsTemplateTrigger { TtId = 15, TemplateId = 1, TriggerId = 100 },
                new SmsTemplateTrigger { TtId = 12, TemplateId = 2, TriggerId = 101 });
            context.SmsRules.Add(new SmsRule
            {
                RuleId = 4,
                TtId = 15,
                MaxTimeTokefInMinutes = 50,
                IsConstant = true,
                IsRecurring = false,
                CreateDate = new DateTime(2026, 8, 20)
            });
            await context.SaveChangesAsync();
        });

        var rule4Response = await api.Client.PutAsJsonAsync(
            "/api/SmsRules/4",
            new
            {
                ttId = 15,
                dependOnTtId = 12,
                dependencyMaxMinutes = 30
            });

        rule4Response.EnsureSuccessStatusCode();
        var rule4 = await rule4Response.Content.ReadFromJsonAsync<SmsRuleResponse>();
        Assert.NotNull(rule4);
        Assert.Equal(12, rule4!.DependOnTtId);
        Assert.Equal(30, rule4.DependencyMaxMinutes);
        Assert.Equal(50, rule4.MaxTimeTokefInMinutes);
        Assert.True(rule4.IsConstant);
        Assert.False(rule4.IsRecurring);

        var rule5Response = await api.Client.PutAsJsonAsync(
            "/api/SmsRules/4",
            new
            {
                ttId = 15,
                isRecurring = true,
                recurringIntervalDays = 2,
                recurringTimeOfDay = "13:00",
                recurringStopCondition = "discharged"
            });

        rule5Response.EnsureSuccessStatusCode();
        var rule5 = await rule5Response.Content.ReadFromJsonAsync<SmsRuleResponse>();
        Assert.NotNull(rule5);
        Assert.True(rule5!.IsRecurring);
        Assert.Equal("13:00", rule5.RecurringTimeOfDay);
        Assert.Equal(12, rule5.DependOnTtId);
        Assert.Equal(30, rule5.DependencyMaxMinutes);
        Assert.Equal(50, rule5.MaxTimeTokefInMinutes);

        var invalidTimeResponse = await api.Client.PutAsJsonAsync(
            "/api/SmsRules/4",
            new { ttId = 15, recurringTimeOfDay = "25:70" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidTimeResponse.StatusCode);

        var missingRuleResponse = await api.Client.PutAsJsonAsync(
            "/api/SmsRules/999",
            new { ttId = 15, dependOnTtId = 12 });
        Assert.Equal(HttpStatusCode.NotFound, missingRuleResponse.StatusCode);
        await api.WithContext(async context =>
            Assert.Single(await context.SmsRules.ToListAsync()));
    }

    [Fact]
    public async Task Swagger_ExposesPutContractsAndRule5Validation()
    {
        await using var api = await TestApi.Start();
        using var response = await api.Client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var swagger = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        var paths = swagger.RootElement.GetProperty("paths");
        var exclusions = paths.GetProperty("/api/SmsRejectHospitalUnits/replace");
        Assert.True(exclusions.TryGetProperty("put", out _));
        Assert.True(exclusions.TryGetProperty("post", out _));

        var rules = paths.GetProperty("/api/SmsRules/{ruleId}");
        Assert.True(rules.TryGetProperty("put", out _));

        var schemas = swagger.RootElement
            .GetProperty("components")
            .GetProperty("schemas");
        var createSchema = schemas
            .GetProperty(nameof(CreateSmsRuleRequest))
            .GetProperty("properties");
        var updateSchema = schemas
            .GetProperty(nameof(UpdateSmsRuleRequest))
            .GetProperty("properties");
        Assert.Equal(
            "^(?:[01]\\d|2[0-3]):[0-5]\\d$",
            updateSchema.GetProperty("recurringTimeOfDay").GetProperty("pattern").GetString());
        Assert.Equal(
            1,
            updateSchema.GetProperty("recurringIntervalDays").GetProperty("minimum").GetInt32());
        Assert.Equal(
            SmsRuleStopConditions.ValidationPattern,
            createSchema.GetProperty("recurringStopCondition").GetProperty("pattern").GetString());
        Assert.Equal(
            SmsRuleStopConditions.ValidationPattern,
            updateSchema.GetProperty("recurringStopCondition").GetProperty("pattern").GetString());
    }

    [Fact]
    public async Task AnonymousTemplatePostAndPut_ReturnUnauthorizedWithoutChangingData()
    {
        await using var api = await TestApi.Start();
        await api.WithContext(async context =>
        {
            context.SmsTemplates.Add(new SmsTemplate
            {
                TemplateId = 10,
                TemplateCode = "EXISTING",
                TemplateName = "Existing template",
                CategoryId = 3
            });
            await context.SaveChangesAsync();
        });
        using var anonymousClient = api.CreateClient(userName: null);

        using var postResponse = await anonymousClient.PostAsJsonAsync(
            "/api/SmsTemplates",
            new CreateSmsTemplateDto
            {
                TemplateName = "Anonymous template",
                Description = "ANONYMOUS",
                CategoryId = 3,
                IsActive = true,
                IsEditable = true
            });
        using var putResponse = await anonymousClient.PutAsJsonAsync(
            "/api/SmsTemplates/10",
            new UpdateSmsTemplateDto
            {
                TemplateId = 10,
                TemplateName = "Anonymous update",
                Description = "ANONYMOUS_UPDATE",
                CategoryId = 3,
                IsActive = true,
                IsEditable = true
            });

        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);
        await api.WithContext(async context =>
        {
            var templates = await context.SmsTemplates.AsNoTracking().ToListAsync();
            var template = Assert.Single(templates);
            Assert.Equal("Existing template", template.TemplateName);
            Assert.Null(template.UpdateUser);
        });
    }

    [Fact]
    public async Task AuthenticatedTemplatePost_SavesFullDomainUser()
    {
        await using var api = await TestApi.Start();

        using var response = await api.Client.PostAsJsonAsync(
            "/api/SmsTemplates",
            new CreateSmsTemplateDto
            {
                TemplateName = "Authenticated template",
                Description = "AUTHENTICATED",
                CategoryId = 3,
                IsActive = true,
                IsEditable = true
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await api.WithContext(async context =>
        {
            var template = await context.SmsTemplates.AsNoTracking().SingleAsync();
            Assert.Equal(ConnectedUser, template.UpdateUser);
        });
    }

    [Fact]
    public async Task AuthenticatedUserEndpoint_UsesWindowsPrincipal()
    {
        await using var api = await TestApi.Start();

        using var response = await api.Client.GetAsync("/api/User");

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserDetails>();
        Assert.NotNull(user);
        Assert.Equal(ConnectedUser, user!.FullName);
    }

    [Fact]
    public async Task FrontendCorsPreflight_AllowsCredentialsBeforeAuthorization()
    {
        await using var api = await TestApi.Start();
        using var anonymousClient = api.CreateClient(userName: null);
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/SmsTemplates");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await anonymousClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            "http://localhost:4200",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal(
            "true",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
    }

    private sealed class TestApi : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private TestApi(WebApplication app, HttpClient client)
        {
            _app = app;
            Client = client;
        }

        public HttpClient Client { get; }

        public static async Task<TestApi> Start()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Development"
            });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services
                .AddControllers()
                .AddApplicationPart(typeof(SmsRulesController).Assembly)
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddSingleton<IActiveDirectoryService, TestActiveDirectoryService>();
            builder.Services
                .AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });
            var databaseName = Guid.NewGuid().ToString();
            builder.Services.AddDbContext<HospitalSmsDbContext>(options =>
                options.UseInMemoryDatabase(databaseName)
                    .ConfigureWarnings(warnings =>
                        warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            var app = builder.Build();
            app.UseSwagger();
            app.UseCors("Frontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();

            var server = app.Services.GetRequiredService<IServer>();
            var address = server.Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .Single();
            var client = new HttpClient { BaseAddress = new Uri(address) };
            client.DefaultRequestHeaders.Add(
                TestAuthenticationHandler.UserHeaderName,
                ConnectedUser);
            return new TestApi(app, client);
        }

        public HttpClient CreateClient(string? userName)
        {
            var client = new HttpClient { BaseAddress = Client.BaseAddress };
            if (!string.IsNullOrWhiteSpace(userName))
            {
                client.DefaultRequestHeaders.Add(
                    TestAuthenticationHandler.UserHeaderName,
                    userName);
            }

            return client;
        }

        public async Task WithContext(Func<HospitalSmsDbContext, Task> action)
        {
            await using var scope = _app.Services.CreateAsyncScope();
            await action(scope.ServiceProvider.GetRequiredService<HospitalSmsDbContext>());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
