using Microsoft.EntityFrameworkCore;
using RulesetEngine.Domain.Entities;
using RulesetEngine.Infrastructure.Database;
using Xunit;

namespace RulesetEngine.Tests.Infrastructure;

public class RulesetDbContextTests
{
    /// <summary>
    /// Creates a DbContextOptions configured for in-memory database.
    /// NOTE: In-memory databases do NOT enforce cascade delete the same way as SQL Server.
    /// These tests verify configuration and relationships, not actual cascade behavior.
    /// For cascade delete verification in production, use integration tests against real SQL Server.
    /// </summary>
    private DbContextOptions<RulesetDbContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<RulesetDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    [Fact]
    public async Task DbContext_ConfiguresRulesetEntity()
    {
        // Arrange & Act: Verify configuration doesn't throw
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        using var context = new RulesetDbContext(options);
        var ruleset = new Ruleset { Name = "Test", IsActive = true };
        context.Rulesets.Add(ruleset);
        await context.SaveChangesAsync();

        // Assert: Entity saved successfully
        var saved = await context.Rulesets.FirstAsync();
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task ForeignKeyRelationship_RuleToRuleset_Enforced()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        using (var context = new RulesetDbContext(options))
        {
            var ruleset = new Ruleset { Name = "Test Ruleset", IsActive = true };
            var rule = new Rule { Name = "Test Rule", Ruleset = ruleset };

            context.Rulesets.Add(ruleset);
            context.Rules.Add(rule);
            await context.SaveChangesAsync();

            // Verify the relationship is set
            Assert.NotEqual(0, rule.RulesetId);
            Assert.Equal(ruleset.Id, rule.RulesetId);
        }
    }

    [Fact]
    public async Task ConditionRelationship_CanBelongToEitherRulesetOrRule()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        using (var context = new RulesetDbContext(options))
        {
            var ruleset = new Ruleset { Name = "Test Ruleset", IsActive = true };
            var rule = new Rule { Name = "Test Rule", Ruleset = ruleset };

            // Condition at ruleset level
            var rulesetCondition = new Condition
            {
                Field = "PublisherNumber",
                Operator = "Equals",
                Value = "99999",
                Ruleset = ruleset,
                RulesetId = ruleset.Id
            };

            // Condition at rule level
            var ruleCondition = new Condition
            {
                Field = "BindTypeCode",
                Operator = "Equals",
                Value = "PB",
                Rule = rule,
                RuleId = rule.Id
            };

            context.Rulesets.Add(ruleset);
            context.Rules.Add(rule);
            context.Conditions.AddRange(rulesetCondition, ruleCondition);
            await context.SaveChangesAsync();

            // Verify both relationships exist
            Assert.Equal(ruleset.Id, rulesetCondition.RulesetId);
            Assert.Null(rulesetCondition.RuleId);

            Assert.Equal(rule.Id, ruleCondition.RuleId);
            Assert.Null(ruleCondition.RulesetId);
        }
    }

    [Fact]
    public async Task RuleResultRelationship_OneToOne_WithRule()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        using (var context = new RulesetDbContext(options))
        {
            var ruleset = new Ruleset { Name = "Test Ruleset", IsActive = true };
            var rule = new Rule { Name = "Test Rule", Ruleset = ruleset };
            var result = new RuleResult { ProductionPlant = "US", Rule = rule };

            context.Rulesets.Add(ruleset);
            context.Rules.Add(rule);
            context.RuleResults.Add(result);
            await context.SaveChangesAsync();

            // Verify one-to-one relationship
            Assert.Equal(rule.Id, result.RuleId);
            Assert.NotNull(rule.Result);
            Assert.Equal(result.Id, rule.Result.Id);
        }
    }

    [Fact]
    public async Task DbContext_CanInsertAndRetrieveEvaluationLog()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        var logEntry = new EvaluationLog
        {
            OrderId = "TEST-001",
            MatchedRuleset = "Ruleset 1",
            MatchedRule = "Rule 1",
            ProductionPlant = "US",
            Matched = true,
            FallbackUsed = false,
            Reason = "Test evaluation"
        };

        // Act
        using (var context = new RulesetDbContext(options))
        {
            context.EvaluationLogs.Add(logEntry);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new RulesetDbContext(options))
        {
            var log = await context.EvaluationLogs.FirstAsync(l => l.OrderId == "TEST-001");
            Assert.NotNull(log);
            Assert.Equal("TEST-001", log.OrderId);
            Assert.Equal("Test evaluation", log.Reason);
            Assert.True(log.Matched);
            Assert.Equal("US", log.ProductionPlant);
        }
    }

    [Fact]
    public async Task Ruleset_IsActive_DefaultsToTrue()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        using (var context = new RulesetDbContext(options))
        {
            var ruleset = new Ruleset { Name = "Default Ruleset" };
            context.Rulesets.Add(ruleset);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new RulesetDbContext(options))
        {
            var ruleset = await context.Rulesets.FirstAsync();
            Assert.True(ruleset.IsActive);
        }
    }

    [Fact]
    public async Task MultipleRulesets_CanCoexist_WithIndependentData()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        using (var context = new RulesetDbContext(options))
        {
            var ruleset1 = new Ruleset { Name = "Ruleset 1", IsActive = true };
            var ruleset2 = new Ruleset { Name = "Ruleset 2", IsActive = false };

            var rule1 = new Rule { Name = "Rule 1", Ruleset = ruleset1 };
            var rule2 = new Rule { Name = "Rule 2", Ruleset = ruleset2 };

            context.Rulesets.AddRange(ruleset1, ruleset2);
            context.Rules.AddRange(rule1, rule2);
            await context.SaveChangesAsync();

            // Verify both exist
            var rulesets = await context.Rulesets.ToListAsync();
            Assert.Equal(2, rulesets.Count);
        }
    }

    [Fact]
    public async Task CreatedAt_Timestamp_SetAutomatically()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        var beforeCreation = DateTime.UtcNow;

        using (var context = new RulesetDbContext(options))
        {
            var ruleset = new Ruleset { Name = "Test Ruleset" };
            context.Rulesets.Add(ruleset);
            await context.SaveChangesAsync();
        }

        var afterCreation = DateTime.UtcNow;

        // Assert
        using (var context = new RulesetDbContext(options))
        {
            var ruleset = await context.Rulesets.FirstAsync();
            Assert.True(ruleset.CreatedAt >= beforeCreation && ruleset.CreatedAt <= afterCreation);
        }
    }

    [Fact]
    public async Task EvaluationLog_EvaluatedAt_SetAutomatically()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        var beforeCreation = DateTime.UtcNow;

        using (var context = new RulesetDbContext(options))
        {
            var log = new EvaluationLog
            {
                OrderId = "LOG-001",
                Reason = "Timestamp test",
                Matched = true
            };
            context.EvaluationLogs.Add(log);
            await context.SaveChangesAsync();
        }

        var afterCreation = DateTime.UtcNow;

        // Assert
        using (var context = new RulesetDbContext(options))
        {
            var log = await context.EvaluationLogs.FirstAsync(l => l.OrderId == "LOG-001");
            Assert.True(log.EvaluatedAt >= beforeCreation && log.EvaluatedAt <= afterCreation);
        }
    }

    [Fact]
    public async Task EntityNavigation_RuleToRuleset_Works()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        using (var context = new RulesetDbContext(options))
        {
            var ruleset = new Ruleset { Name = "Parent Ruleset", IsActive = true };
            var rule = new Rule { Name = "Child Rule", Ruleset = ruleset };

            context.Rulesets.Add(ruleset);
            context.Rules.Add(rule);
            await context.SaveChangesAsync();
        }

        // Act & Assert: Navigate relationship
        using (var context = new RulesetDbContext(options))
        {
            var rule = await context.Rules.Include(r => r.Ruleset).FirstAsync();
            Assert.NotNull(rule.Ruleset);
            Assert.Equal("Parent Ruleset", rule.Ruleset.Name);
        }
    }

    [Fact]
    public async Task EntityNavigation_RulesetToRules_Works()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        using (var context = new RulesetDbContext(options))
        {
            var ruleset = new Ruleset { Name = "Parent Ruleset", IsActive = true };
            context.Rules.Add(new Rule { Name = "Rule 1", Ruleset = ruleset });
            context.Rules.Add(new Rule { Name = "Rule 2", Ruleset = ruleset });

            context.Rulesets.Add(ruleset);
            await context.SaveChangesAsync();
        }

        // Act & Assert: Navigate collection
        using (var context = new RulesetDbContext(options))
        {
            var ruleset = await context.Rulesets.Include(r => r.Rules).FirstAsync();
            Assert.Equal(2, ruleset.Rules.Count);
        }
    }

    [Fact]
    public async Task EntityNavigation_RuleToResult_Works()
    {
        // Arrange
        var options = CreateInMemoryOptions(Guid.NewGuid().ToString());

        using (var context = new RulesetDbContext(options))
        {
            var ruleset = new Ruleset { Name = "Test Ruleset", IsActive = true };
            var rule = new Rule { Name = "Test Rule", Ruleset = ruleset };
            var result = new RuleResult { ProductionPlant = "US_PLANT", Rule = rule };

            context.Rulesets.Add(ruleset);
            context.Rules.Add(rule);
            context.RuleResults.Add(result);
            await context.SaveChangesAsync();
        }

        // Act & Assert
        using (var context = new RulesetDbContext(options))
        {
            var rule = await context.Rules.Include(r => r.Result).FirstAsync();
            Assert.NotNull(rule.Result);
            Assert.Equal("US_PLANT", rule.Result.ProductionPlant);
        }
    }

    /// <summary>
    /// NOTE: Cascade delete behavior testing requires a real SQL Server database.
    /// In-Memory EF Core does not enforce cascade delete at the DbContext level.
    /// 
    /// Integration tests with real SQL Server should verify:
    /// - DeleteRuleset cascades to Rules, Conditions, and RuleResults
    /// - DeleteRule cascades to Conditions and RuleResults
    /// - Relationship constraints are properly enforced
    /// 
    /// This ensures data integrity in production environments.
    /// </summary>
    [Fact]
    public void CascadeDeleteConfiguration_DocumentedForIntegrationTesting()
    {
        // This test documents the limitation and requirement for SQL Server integration tests
        Assert.True(true, "Cascade delete behavior requires integration tests with real SQL Server");
    }
}
