using HospitalSms.Admin.Api.Controllers;
using HospitalSms.Application.Dtos;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class ConcurrentTemplateTriggerRuleCreationTests
{
    [Fact]
    public async Task ConcurrentTemplateTriggerRequests_CreateOnlyOneAssociation()
    {
        var options = CreateOptions();
        await using (var seedContext = new HospitalSmsDbContext(options))
        {
            seedContext.SmsTemplates.Add(CreateTemplate(1));
            seedContext.SmsTriggerCatalogs.Add(CreateCatalog(10));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new UniqueConstraintTestDbContext(options);
        await using var secondContext = new UniqueConstraintTestDbContext(options);
        var request = new CreateSmsTemplateTriggerRequest { TemplateId = 1, TriggerId = 10 };

        var results = await Task.WhenAll(
            new SmsTriggersController(firstContext, new TestCurrentUserService())
                .CreateTemplateTrigger(request, CancellationToken.None),
            new SmsTriggersController(secondContext, new TestCurrentUserService())
                .CreateTemplateTrigger(request, CancellationToken.None));

        Assert.Single(results, result => result.Result is CreatedAtActionResult);
        Assert.Single(results, result => result.Result is ConflictObjectResult);

        await using var verificationContext = new HospitalSmsDbContext(options);
        Assert.Single(await verificationContext.SmsTemplateTriggers.ToListAsync());
    }

    [Fact]
    public async Task ConcurrentRuleRequests_CreateOnlyOneRule()
    {
        var options = CreateOptions();
        await using (var seedContext = new HospitalSmsDbContext(options))
        {
            seedContext.SmsTemplateTriggers.Add(new SmsTemplateTrigger
            {
                TtId = 10,
                TemplateId = 1,
                TriggerId = 100
            });
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new UniqueConstraintTestDbContext(options);
        await using var secondContext = new UniqueConstraintTestDbContext(options);
        var request = new CreateSmsRuleRequest { TtId = 10 };

        var results = await Task.WhenAll(
            new SmsRulesController(firstContext, new TestCurrentUserService())
                .CreateRule(request, CancellationToken.None),
            new SmsRulesController(secondContext, new TestCurrentUserService())
                .CreateRule(request, CancellationToken.None));

        Assert.Single(results, result => result.Result is OkObjectResult);
        Assert.Single(results, result => result.Result is ConflictObjectResult);

        await using var verificationContext = new HospitalSmsDbContext(options);
        Assert.Single(await verificationContext.SmsRules.ToListAsync());
    }

    private static DbContextOptions<HospitalSmsDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString(),
                new InMemoryDatabaseRoot())
            .Options;
    }

    private static SmsTemplate CreateTemplate(int templateId) => new()
    {
        TemplateId = templateId,
        TemplateCode = $"TEMPLATE_{templateId}",
        TemplateName = $"Template {templateId}",
        CategoryId = 1
    };

    private static SmsTriggerCatalog CreateCatalog(int triggerId) => new()
    {
        TriggerId = triggerId,
        TriggerCode = $"TRIGGER_{triggerId}",
        TriggerName = $"Trigger {triggerId}"
    };

    private sealed class UniqueConstraintTestDbContext : HospitalSmsDbContext
    {
        private static readonly SemaphoreSlim SaveGate = new(1, 1);
        private readonly DbContextOptions<HospitalSmsDbContext> _options;

        public UniqueConstraintTestDbContext(DbContextOptions<HospitalSmsDbContext> options)
            : base(options)
        {
            _options = options;
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            var addedTemplateIds = ChangeTracker.Entries<SmsTemplateTrigger>()
                .Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity.TemplateId)
                .ToList();
            var addedRuleTtIds = ChangeTracker.Entries<SmsRule>()
                .Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity.TtId)
                .ToList();

            await SaveGate.WaitAsync(cancellationToken);
            try
            {
                await using var verificationContext = new HospitalSmsDbContext(_options);
                var duplicateTemplateTrigger = addedTemplateIds.Count > 0
                    && await verificationContext.SmsTemplateTriggers
                        .AnyAsync(
                            templateTrigger => addedTemplateIds.Contains(templateTrigger.TemplateId),
                            cancellationToken);
                var duplicateRule = addedRuleTtIds.Count > 0
                    && await verificationContext.SmsRules
                        .AnyAsync(
                            rule => addedRuleTtIds.Contains(rule.TtId),
                            cancellationToken);

                if (duplicateTemplateTrigger || duplicateRule)
                {
                    throw new DbUpdateException("Simulated unique constraint violation.");
                }

                return await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                SaveGate.Release();
            }
        }
    }
}
