using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Data;

public class TemplateTriggerRuleConstraintTests
{
    [Fact]
    public void Model_UsesRequiredKeysRelationshipsAndUniqueIndexesAsConcurrencyBackstop()
    {
        using var context = CreateContext();

        var templateTrigger = context.Model.FindEntityType(typeof(SmsTemplateTrigger));
        Assert.NotNull(templateTrigger);
        Assert.Equal(
            nameof(SmsTemplateTrigger.TtId),
            Assert.Single(templateTrigger!.FindPrimaryKey()!.Properties).Name);
        Assert.Contains(
            templateTrigger.GetIndexes(),
            index => index.IsUnique
                     && index.Properties.Count == 1
                     && index.Properties[0].Name == nameof(SmsTemplateTrigger.TemplateId));
        Assert.Contains(
            templateTrigger.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(SmsTemplate)
                          && foreignKey.Properties.Count == 1
                          && foreignKey.Properties[0].Name == nameof(SmsTemplateTrigger.TemplateId));

        var rule = context.Model.FindEntityType(typeof(SmsRule));
        Assert.NotNull(rule);
        Assert.Equal(
            nameof(SmsRule.RuleId),
            Assert.Single(rule!.FindPrimaryKey()!.Properties).Name);
        Assert.False(rule.FindProperty(nameof(SmsRule.TtId))!.IsNullable);
        Assert.Contains(
            rule.GetIndexes(),
            index => index.IsUnique
                     && index.Properties.Count == 1
                     && index.Properties[0].Name == nameof(SmsRule.TtId));
        Assert.Contains(
            rule.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(SmsTemplateTrigger)
                          && foreignKey.Properties.Count == 1
                          && foreignKey.Properties[0].Name == nameof(SmsRule.TtId));
    }

    [Fact]
    public void CleanupScript_RollsBackWhenDuplicateValidationFails()
    {
        var solutionDirectory = FindSolutionDirectory();
        var scriptPath = Path.Combine(
            solutionDirectory.FullName,
            "HospitalSms.Infrastructure",
            "Data",
            "Scripts",
            "20260824_EnforceSingleTemplateTriggerAndRule.sql");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("SET XACT_ABORT ON", script, StringComparison.Ordinal);
        Assert.Contains("BEGIN TRANSACTION", script, StringComparison.Ordinal);
        Assert.Contains("Duplicate template_id values remain", script, StringComparison.Ordinal);
        Assert.Contains("Multiple rules remain for the same tt_id", script, StringComparison.Ordinal);
        Assert.Contains("ROLLBACK TRANSACTION", script, StringComparison.Ordinal);
        Assert.Contains("THROW;", script, StringComparison.Ordinal);
        Assert.True(
            script.IndexOf("THROW 51001", StringComparison.Ordinal)
            < script.IndexOf("CREATE UNIQUE INDEX UX_sms_template_trigger_template_id", StringComparison.Ordinal));
        Assert.True(
            script.IndexOf("COMMIT TRANSACTION", StringComparison.Ordinal)
            < script.IndexOf("BEGIN CATCH", StringComparison.Ordinal));
    }

    private static HospitalSmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HospitalSmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HospitalSmsDbContext(options);
    }

    private static DirectoryInfo FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "HospitalSms.sln")))
        {
            directory = directory.Parent;
        }

        return directory
            ?? throw new DirectoryNotFoundException("Could not locate HospitalSms.sln.");
    }
}
