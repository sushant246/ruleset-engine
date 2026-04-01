using Microsoft.Extensions.Logging.Abstractions;
using RulesetEngine.Domain.Entities;
using RulesetEngine.Domain.Services;
using Xunit;

namespace RulesetEngine.Tests.Domain;

public class RuleEvaluationEngineTests
{
    private readonly RuleEvaluationEngine _engine;

    public RuleEvaluationEngineTests()
    {
        _engine = new RuleEvaluationEngine(NullLogger<RuleEvaluationEngine>.Instance);
    }

    [Fact]
    public void Evaluate_NoRulesets_ReturnsNoMatch()
    {
        var context = new EvaluationContext
        {
            OrderId = "TEST-001",
            Fields = new Dictionary<string, object?> { ["PublisherNumber"] = "99999" }
        };

        var result = _engine.Evaluate(context, new List<Ruleset>());

        Assert.False(result.Matched);
        Assert.Null(result.ProductionPlant);
        Assert.Equal("No matching ruleset or rule found", result.Reason);
    }

    [Fact]
    public void Evaluate_MatchingRulesetAndRule_ReturnsMatch()
    {
        var ruleset = CreateRuleset("Publisher 99999",
            new List<Condition>
            {
                new() { Field = "PublisherNumber", Operator = "Equals", Value = "99999" },
                new() { Field = "OrderMethod", Operator = "Equals", Value = "POD" }
            },
            new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" },
                        new() { Field = "IsCountry", Operator = "Equals", Value = "US" },
                        new() { Field = "PrintQuantity", Operator = "LessThanOrEqual", Value = "20" }
                    },
                    Result = new RuleResult { ProductionPlant = "US" }
                }
            });

        var context = new EvaluationContext
        {
            OrderId = "TEST-001",
            Fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["PublisherNumber"] = "99999",
                ["OrderMethod"] = "POD",
                ["BindTypeCode"] = "PB",
                ["IsCountry"] = "US",
                ["PrintQuantity"] = 10
            }
        };

        var result = _engine.Evaluate(context, new List<Ruleset> { ruleset });

        Assert.True(result.Matched);
        Assert.Equal("US", result.ProductionPlant);
        Assert.Equal("Publisher 99999", result.MatchedRuleset);
        Assert.Equal("Rule 1", result.MatchedRule);
    }

    [Fact]
    public void Evaluate_RulesetConditionNotMet_ReturnsNoMatch()
    {
        var ruleset = CreateRuleset("Publisher 99999",
            new List<Condition>
            {
                new() { Field = "PublisherNumber", Operator = "Equals", Value = "99999" }
            },
            new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" }
                    },
                    Result = new RuleResult { ProductionPlant = "US" }
                }
            });

        var context = new EvaluationContext
        {
            OrderId = "TEST-002",
            Fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["PublisherNumber"] = "DIFFERENT",
                ["BindTypeCode"] = "PB"
            }
        };

        var result = _engine.Evaluate(context, new List<Ruleset> { ruleset });

        Assert.False(result.Matched);
        Assert.Null(result.ProductionPlant);
    }

    [Fact]
    public void Evaluate_RuleConditionNotMet_ReturnsNoMatch()
    {
        var ruleset = CreateRuleset("Publisher 99999",
            new List<Condition>
            {
                new() { Field = "PublisherNumber", Operator = "Equals", Value = "99999" }
            },
            new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" }
                    },
                    Result = new RuleResult { ProductionPlant = "US" }
                }
            });

        var context = new EvaluationContext
        {
            OrderId = "TEST-003",
            Fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["PublisherNumber"] = "99999",
                ["BindTypeCode"] = "CV"
            }
        };

        var result = _engine.Evaluate(context, new List<Ruleset> { ruleset });

        Assert.False(result.Matched);
    }

    [Fact]
    public void Evaluate_GreaterThanOrEqual_MatchesCorrectly()
    {
        var ruleset = CreateRuleset("Ruleset",
            new List<Condition>(),
            new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PrintQuantity", Operator = "GreaterThanOrEqual", Value = "20" }
                    },
                    Result = new RuleResult { ProductionPlant = "KGL" }
                }
            });

        var context = new EvaluationContext
        {
            OrderId = "TEST-004",
            Fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["PrintQuantity"] = 25
            }
        };

        var result = _engine.Evaluate(context, new List<Ruleset> { ruleset });

        Assert.True(result.Matched);
        Assert.Equal("KGL", result.ProductionPlant);
    }

    [Fact]
    public void Evaluate_InactiveRuleset_Skipped()
    {
        var ruleset = CreateRuleset("Inactive Ruleset", new List<Condition>(), new List<Rule>
        {
            new()
            {
                Name = "Rule 1",
                Conditions = new List<Condition>(),
                Result = new RuleResult { ProductionPlant = "US" }
            }
        });
        ruleset.IsActive = false;

        var context = new EvaluationContext
        {
            OrderId = "TEST-005",
            Fields = new Dictionary<string, object?>()
        };

        var result = _engine.Evaluate(context, new List<Ruleset> { ruleset });

        Assert.False(result.Matched);
    }

    [Fact]
    public void Evaluate_MultipleRulesets_MatchesFirstRulesetInSequence()
    {
        var ruleset1 = CreateRuleset("Ruleset One",
            new List<Condition>
            {
                new() { Field = "PublisherNumber", Operator = "Equals", Value = "99990" }
            },
            new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" }
                    },
                    Result = new RuleResult { ProductionPlant = "PLANT_A" }
                }
            });

        var ruleset2 = CreateRuleset("Ruleset Two",
            new List<Condition>
            {
                new() { Field = "PublisherNumber", Operator = "Equals", Value = "99990" }
            },
            new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" }
                    },
                    Result = new RuleResult { ProductionPlant = "PLANT_B" }
                }
            });

        var context = new EvaluationContext
        {
            OrderId = "TEST-006",
            Fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["PublisherNumber"] = "99990",
                ["BindTypeCode"] = "PB"
            }
        };

        var result = _engine.Evaluate(context, new List<Ruleset> { ruleset1, ruleset2 });

        Assert.True(result.Matched);
        Assert.Equal("PLANT_A", result.ProductionPlant);
        Assert.Equal("Ruleset One", result.MatchedRuleset);
    }

    [Theory]
    [InlineData("Equals", "PB", "PB", true)]
    [InlineData("Equals", "PB", "CV", false)]
    [InlineData("NotEquals", "PB", "CV", true)]
    [InlineData("NotEquals", "PB", "PB", false)]
    [InlineData("Contains", "PB", "PBCV", true)]
    [InlineData("StartsWith", "PB", "PBCV", true)]
    [InlineData("EndsWith", "CV", "PBCV", true)]
    [InlineData("GreaterThan", "10", "15", true)]
    [InlineData("LessThan", "10", "5", true)]
    public void Evaluate_AllOperators_WorkCorrectly(string op, string value, string fieldValue, bool expectedMatch)
    {
        var ruleset = CreateRuleset("Ruleset", new List<Condition>(), new List<Rule>
        {
            new()
            {
                Name = "Rule",
                Conditions = new List<Condition>
                {
                    new() { Field = "TestField", Operator = op, Value = value }
                },
                Result = new RuleResult { ProductionPlant = "TEST" }
            }
        });

        var context = new EvaluationContext
        {
            OrderId = "TEST",
            Fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["TestField"] = fieldValue
            }
        };

        var result = _engine.Evaluate(context, new List<Ruleset> { ruleset });

        Assert.Equal(expectedMatch, result.Matched);
    }

    // ── Composite condition (AND/OR) tests ───────────────────────────────────

    [Fact]
    public void Evaluate_RulesetAndLogic_NoMatchWhenOnlyOneConditionMet()
    {
        var ruleset = CreateRuleset("AND Ruleset",
            new List<Condition>
            {
                new() { Field = "PublisherNumber", Operator = "Equals", Value = "99999" },
                new() { Field = "OrderMethod", Operator = "Equals", Value = "POD" }
            },
            new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>(),
                    Result = new RuleResult { ProductionPlant = "AND_PLANT" }
                }
            });

        // Only PublisherNumber matches — OrderMethod is wrong
        var context = new EvaluationContext
        {
            OrderId = "AND-001",
            Fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["PublisherNumber"] = "99999",
                ["OrderMethod"] = "ONLINE"
            }
        };

        var result = _engine.Evaluate(context, new List<Ruleset> { ruleset });

        Assert.False(result.Matched);
    }

    [Fact]
    public void Evaluate_EmptyConditions_AlwaysMatch()
    {
        var ruleset = CreateRuleset("Empty Conditions",
            new List<Condition>(),
            new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>(),
                    Result = new RuleResult { ProductionPlant = "CATCH_ALL" }
                }
            });

        var context = new EvaluationContext
        {
            OrderId = "EMPTY-001",
            Fields = new Dictionary<string, object?>()
        };

        var result = _engine.Evaluate(context, new List<Ruleset> { ruleset });

        Assert.True(result.Matched);
        Assert.Equal("CATCH_ALL", result.ProductionPlant);
    }

    private static Ruleset CreateRuleset(string name, List<Condition> conditions, List<Rule> rules)
    {
        return new Ruleset
        {
            Id = 1,
            Name = name,
            IsActive = true,
            Conditions = conditions,
            Rules = rules
        };
    }
}