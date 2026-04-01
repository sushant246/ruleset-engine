using Microsoft.Extensions.Logging;
using RulesetEngine.Domain.Entities;

namespace RulesetEngine.Domain.Services;

public class RuleEvaluationEngine
{
    private readonly ILogger<RuleEvaluationEngine> _logger;

    public RuleEvaluationEngine(ILogger<RuleEvaluationEngine> logger)
    {
        _logger = logger;
    }

    public EvaluationResult Evaluate(EvaluationContext context, IList<Ruleset> rulesets)
    {
        var activeRulesets = rulesets
            .Where(r => r.IsActive)
            .ToList();

        foreach (var ruleset in activeRulesets)
        {
            _logger.LogDebug("Evaluating ruleset: {RulesetName}", ruleset.Name);

            if (!EvaluateConditions(context, ruleset.Conditions))
            {
                _logger.LogDebug("Ruleset {RulesetName} conditions not matched", ruleset.Name);
                continue;
            }

            foreach (var rule in ruleset.Rules)
            {
                _logger.LogDebug("Evaluating rule: {RuleName} in ruleset: {RulesetName}", rule.Name, ruleset.Name);

                if (EvaluateConditions(context, rule.Conditions))
                {
                    _logger.LogInformation(
                        "Matched ruleset: {RulesetName}, rule: {RuleName}, plant: {Plant}",
                        ruleset.Name, rule.Name, rule.Result?.ProductionPlant);

                    return new EvaluationResult
                    {
                        Matched = true,
                        ProductionPlant = rule.Result?.ProductionPlant,
                        MatchedRuleset = ruleset.Name,
                        MatchedRule = rule.Name,
                        Reason = $"Matched ruleset '{ruleset.Name}', rule '{rule.Name}'"
                    };
                }
            }
        }

        return new EvaluationResult
        {
            Matched = false,
            ProductionPlant = null,
            MatchedRuleset = null,
            MatchedRule = null,
            Reason = "No matching ruleset or rule found"
        };
    }

    /// <summary>
    /// Evaluates a list of conditions with AND logic (all must match)
    /// </summary>
    private bool EvaluateConditions(EvaluationContext context, IList<Condition> conditions)
    {
        // If no conditions, always match
        if (!conditions.Any())
            return true;

        // ALL conditions must be true (AND logic)
        return conditions.All(c => EvaluateCondition(context, c));
    }

    /// <summary>
    /// Evaluates a simple condition (single field comparison)
    /// </summary>
    private bool EvaluateCondition(EvaluationContext context, Condition condition)
    {
        if (!context.Fields.TryGetValue(condition.Field, out var fieldValue))
        {
            _logger.LogDebug("Field '{Field}' not found in context", condition.Field);
            return false;
        }

        var fieldStr = fieldValue?.ToString() ?? string.Empty;

        return condition.Operator.ToLowerInvariant() switch
        {
            "equals" => string.Equals(fieldStr, condition.Value, StringComparison.OrdinalIgnoreCase),
            "notequals" => !string.Equals(fieldStr, condition.Value, StringComparison.OrdinalIgnoreCase),
            "contains" => fieldStr.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            "startswith" => fieldStr.StartsWith(condition.Value, StringComparison.OrdinalIgnoreCase),
            "endswith" => fieldStr.EndsWith(condition.Value, StringComparison.OrdinalIgnoreCase),
            "greaterthan" => CompareNumeric(fieldStr, condition.Value) > 0,
            "greaterthanorequal" => CompareNumeric(fieldStr, condition.Value) >= 0,
            "lessthan" => CompareNumeric(fieldStr, condition.Value) < 0,
            "lessthanorequal" => CompareNumeric(fieldStr, condition.Value) <= 0,
            _ => false
        };
    }

    private static int CompareNumeric(string left, string right)
    {
        if (decimal.TryParse(left, out var leftVal) && decimal.TryParse(right, out var rightVal))
            return leftVal.CompareTo(rightVal);
        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }
}