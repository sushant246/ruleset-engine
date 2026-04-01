using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RulesetEngine.Application.DTOs;
using RulesetEngine.Application.Services;
using RulesetEngine.Domain.Entities;
using RulesetEngine.Domain.Services;
using RulesetEngine.Infrastructure.Database;
using RulesetEngine.Infrastructure.Repositories;
using Xunit;

namespace RulesetEngine.Tests.Integration;

/// <summary>
/// Integration tests for end-to-end evaluation workflows using real database context
/// </summary>
public class EvaluationWorkflowIntegrationTests : IDisposable
{
    private readonly RulesetDbContext _context;
    private readonly RuleEvaluationEngine _engine;
    private readonly RulesetRepository _rulesetRepository;
    private readonly EvaluationLogRepository _logRepository;
    private readonly RuleEvaluationService _service;

    public EvaluationWorkflowIntegrationTests()
    {
        // Create in-memory database for each test
        var options = new DbContextOptionsBuilder<RulesetDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new RulesetDbContext(options);
        _engine = new RuleEvaluationEngine(NullLogger<RuleEvaluationEngine>.Instance);
        _rulesetRepository = new RulesetRepository(_context);
        _logRepository = new EvaluationLogRepository(_context);

        var config = new ConfigurationBuilder()
            .Build();

        _service = new RuleEvaluationService(
            _engine,
            _rulesetRepository,
            _logRepository,
            NullLogger<RuleEvaluationService>.Instance,
            config);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region Basic Workflow Tests

    [Fact]
    public async Task EndToEnd_SimpleRulesetCreatedAndEvaluated_MatchFound()
    {
        // Arrange: Create a simple ruleset
        var ruleset = new Ruleset
        {
            Name = "Simple Ruleset",
            Description = "Test ruleset",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Simple Rule",
                    Conditions = new List<Condition>
                    {
                        new()
                        {
                            Field = "PublisherNumber",
                            Operator = "Equals",
                            Value = "TEST-PUB"
                        }
                    },
                    Result = new RuleResult { ProductionPlant = "TEST_PLANT" }
                }
            }
        };

        _context.Rulesets.Add(ruleset);
        await _context.SaveChangesAsync();

        // Act
        var order = new OrderDto
        {
            OrderId = "INT-001",
            PublisherNumber = "TEST-PUB"
        };

        var result = await _service.EvaluateAsync(order);

        // Assert
        Assert.True(result.Matched);
        Assert.Equal("TEST_PLANT", result.ProductionPlant);
        Assert.Equal("Simple Ruleset", result.MatchedRuleset);
        Assert.Equal("Simple Rule", result.MatchedRule);
    }

    [Fact]
    public async Task EndToEnd_EvaluationLogged_CanBeRetrieved()
    {
        // Arrange
        var ruleset = new Ruleset
        {
            Name = "Logging Test Ruleset",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Logging Rule",
                    Conditions = new List<Condition>(),
                    Result = new RuleResult { ProductionPlant = "LOG_PLANT" }
                }
            }
        };

        _context.Rulesets.Add(ruleset);
        await _context.SaveChangesAsync();

        // Act
        var order = new OrderDto
        {
            OrderId = "LOG-INT-001",
            PublisherNumber = "LOG-TEST"
        };

        await _service.EvaluateAsync(order);

        // Assert: Verify log was created
        var log = await _context.EvaluationLogs
            .FirstOrDefaultAsync(l => l.OrderId == "LOG-INT-001");

        Assert.NotNull(log);
        Assert.Equal("LOG-INT-001", log.OrderId);
        Assert.Equal("LOG_PLANT", log.ProductionPlant);
        Assert.NotNull(log.EvaluatedAt);
        Assert.NotNull(log.OrderDataJson);
    }

    [Fact]
    public async Task EndToEnd_MultipleRulesetsPriority_FirstMatchWins()
    {
        // Arrange: Create two rulesets that both could match
        var ruleset1 = new Ruleset
        {
            Name = "Priority Ruleset 1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "MULTI-PUB" }
                    },
                    Result = new RuleResult { ProductionPlant = "PLANT_A" }
                }
            }
        };

        var ruleset2 = new Ruleset
        {
            Name = "Priority Ruleset 2",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Rule 2",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "MULTI-PUB" }
                    },
                    Result = new RuleResult { ProductionPlant = "PLANT_B" }
                }
            }
        };

        _context.Rulesets.AddRange(ruleset1, ruleset2);
        await _context.SaveChangesAsync();

        // Act
        var order = new OrderDto
        {
            OrderId = "MULTI-INT-001",
            PublisherNumber = "MULTI-PUB"
        };

        var result = await _service.EvaluateAsync(order);

        // Assert: First ruleset should match
        Assert.True(result.Matched);
        Assert.Equal("PLANT_A", result.ProductionPlant);
        Assert.Equal("Priority Ruleset 1", result.MatchedRuleset);
    }

    #endregion

    #region Complex Condition Tests

    [Fact]
    public async Task EndToEnd_ComplexConditions_AllConditionsMustMatch()
    {
        // Arrange
        var ruleset = new Ruleset
        {
            Name = "Complex Ruleset",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Complex Rule",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "COMPLEX-PUB" },
                        new() { Field = "OrderMethod", Operator = "Equals", Value = "POD" },
                        new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" }
                    },
                    Result = new RuleResult { ProductionPlant = "COMPLEX_PLANT" }
                }
            }
        };

        _context.Rulesets.Add(ruleset);
        await _context.SaveChangesAsync();

        // Act
        var order = new OrderDto
        {
            OrderId = "COMPLEX-INT-001",
            PublisherNumber = "COMPLEX-PUB",
            OrderMethod = "POD",
            Items = new List<ItemDto>
            {
                new()
                {
                    Sku = "PB-001",
                    PrintQuantity = 10,
                    Components = new List<ComponentDto>
                    {
                        new()
                        {
                            Code = "Cover",
                            Attributes = new AttributesDto { BindTypeCode = "PB" }
                        }
                    }
                }
            }
        };

        var result = await _service.EvaluateAsync(order);

        // Assert
        Assert.True(result.Matched);
        Assert.Equal("COMPLEX_PLANT", result.ProductionPlant);
    }

    [Fact]
    public async Task EndToEnd_PartialConditionMatch_NoMatch()
    {
        // Arrange
        var ruleset = new Ruleset
        {
            Name = "Strict Ruleset",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Strict Rule",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "STRICT-PUB" },
                        new() { Field = "OrderMethod", Operator = "Equals", Value = "POD" }
                    },
                    Result = new RuleResult { ProductionPlant = "STRICT_PLANT" }
                }
            }
        };

        _context.Rulesets.Add(ruleset);
        await _context.SaveChangesAsync();

        // Act: Only match first condition
        var order = new OrderDto
        {
            OrderId = "STRICT-INT-001",
            PublisherNumber = "STRICT-PUB",
            OrderMethod = "WRONG"  // Doesn't match
        };

        var result = await _service.EvaluateAsync(order);

        // Assert
        Assert.False(result.Matched);
    }

    #endregion

    #region Data Persistence Tests

    [Fact]
    public async Task EndToEnd_MultipleEvaluations_AllLogged()
    {
        // Arrange
        var ruleset = new Ruleset
        {
            Name = "Multi-Log Ruleset",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Multi-Log Rule",
                    Conditions = new List<Condition>(),
                    Result = new RuleResult { ProductionPlant = "PLANT" }
                }
            }
        };

        _context.Rulesets.Add(ruleset);
        await _context.SaveChangesAsync();

        // Act: Multiple evaluations
        for (int i = 0; i < 5; i++)
        {
            var order = new OrderDto
            {
                OrderId = $"MULTI-LOG-{i:D3}",
                PublisherNumber = "TEST"
            };
            await _service.EvaluateAsync(order);
        }

        // Assert: All logs created
        var logs = await _context.EvaluationLogs.ToListAsync();
        Assert.Equal(5, logs.Count);

        // Verify each log has proper data
        for (int i = 0; i < 5; i++)
        {
            var log = logs.FirstOrDefault(l => l.OrderId == $"MULTI-LOG-{i:D3}");
            Assert.NotNull(log);
            Assert.Equal("PLANT", log.ProductionPlant);
        }
    }

    [Fact]
    public async Task EndToEnd_InactiveRulesets_NotEvaluated()
    {
        // Arrange
        var activeRuleset = new Ruleset
        {
            Name = "Active Ruleset",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>()
        };

        var inactiveRuleset = new Ruleset
        {
            Name = "Inactive Ruleset",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Should Not Match",
                    Conditions = new List<Condition>(),
                    Result = new RuleResult { ProductionPlant = "INACTIVE_PLANT" }
                }
            }
        };

        _context.Rulesets.AddRange(activeRuleset, inactiveRuleset);
        await _context.SaveChangesAsync();

        // Act
        var order = new OrderDto
        {
            OrderId = "INACTIVE-INT-001",
            PublisherNumber = "TEST"
        };

        var result = await _service.EvaluateAsync(order);

        // Assert: Inactive ruleset not evaluated
        Assert.False(result.Matched);
        Assert.NotEqual("INACTIVE_PLANT", result.ProductionPlant);
    }

    #endregion

    #region Database Integrity Tests

    [Fact]
    public async Task EndToEnd_RulesetWithMultipleRules_AllAccessible()
    {
        // Arrange
        var ruleset = new Ruleset
        {
            Name = "Multi-Rule Ruleset",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "PUB-1" }
                    },
                    Result = new RuleResult { ProductionPlant = "PLANT-1" }
                },
                new()
                {
                    Name = "Rule 2",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "PUB-2" }
                    },
                    Result = new RuleResult { ProductionPlant = "PLANT-2" }
                },
                new()
                {
                    Name = "Rule 3",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "PUB-3" }
                    },
                    Result = new RuleResult { ProductionPlant = "PLANT-3" }
                }
            }
        };

        _context.Rulesets.Add(ruleset);
        await _context.SaveChangesAsync();

        // Act & Assert: Each rule can match
        for (int i = 1; i <= 3; i++)
        {
            var order = new OrderDto
            {
                OrderId = $"MULTI-RULE-{i}",
                PublisherNumber = $"PUB-{i}"
            };

            var result = await _service.EvaluateAsync(order);
            Assert.True(result.Matched);
            Assert.Equal($"PLANT-{i}", result.ProductionPlant);
        }
    }

    [Fact]
    public async Task EndToEnd_RuleWithMultipleConditions_CorrectlyAssociated()
    {
        // Arrange
        var ruleset = new Ruleset
        {
            Name = "Multi-Condition Ruleset",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Multi-Condition Rule",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "MULTI-COND" },
                        new() { Field = "OrderMethod", Operator = "Equals", Value = "POD" },
                        new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" },
                        new() { Field = "PrintQuantity", Operator = "LessThanOrEqual", Value = "100" }
                    },
                    Result = new RuleResult { ProductionPlant = "MULTI_COND_PLANT" }
                }
            }
        };

        _context.Rulesets.Add(ruleset);
        await _context.SaveChangesAsync();

        // Verify conditions are correctly loaded
        var savedRuleset = await _context.Rulesets
            .Include(r => r.Rules)
            .ThenInclude(r => r.Conditions)
            .FirstAsync();

        var rule = savedRuleset.Rules.First();
        Assert.Equal(4, rule.Conditions.Count);

        // Act & Assert: Evaluation works with all conditions
        var order = new OrderDto
        {
            OrderId = "MULTI-COND-INT-001",
            PublisherNumber = "MULTI-COND",
            OrderMethod = "POD",
            Items = new List<ItemDto>
            {
                new()
                {
                    PrintQuantity = 50,
                    Components = new List<ComponentDto>
                    {
                        new()
                        {
                            Attributes = new AttributesDto { BindTypeCode = "PB" }
                        }
                    }
                }
            }
        };

        var result = await _service.EvaluateAsync(order);
        Assert.True(result.Matched);
        Assert.Equal("MULTI_COND_PLANT", result.ProductionPlant);
    }

    #endregion

    #region Ruleset-Level Condition Tests

    [Fact]
    public async Task EndToEnd_RulesetLevelConditions_MustMatchForRuleEvaluation()
    {
        // Arrange: Ruleset with conditions (gates)
        var ruleset = new Ruleset
        {
            Name = "Gated Ruleset",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Conditions = new List<Condition>
            {
                new() { Field = "PublisherNumber", Operator = "Equals", Value = "GATED-PUB" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Gated Rule",
                    Conditions = new List<Condition>(),
                    Result = new RuleResult { ProductionPlant = "GATED_PLANT" }
                }
            }
        };

        _context.Rulesets.Add(ruleset);
        await _context.SaveChangesAsync();

        // Act: Correct publisher
        var result1 = await _service.EvaluateAsync(new OrderDto
        {
            OrderId = "GATED-PASS-001",
            PublisherNumber = "GATED-PUB"
        });

        // Assert
        Assert.True(result1.Matched);
        Assert.Equal("GATED_PLANT", result1.ProductionPlant);

        // Act: Wrong publisher
        var result2 = await _service.EvaluateAsync(new OrderDto
        {
            OrderId = "GATED-FAIL-001",
            PublisherNumber = "WRONG-PUB"
        });

        // Assert
        Assert.False(result2.Matched);
    }

    #endregion
}
