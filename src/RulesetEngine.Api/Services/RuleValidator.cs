using RulesetEngine.Domain.Entities;

namespace RulesetEngine.Api.Services;

/// <summary>
/// Sample SINGLETON service - stateless validator
/// Thread-safe, pure logic with no mutable state
/// Single instance reused across all requests for optimal performance
/// </summary>
public class RuleValidator
{
    private readonly ILogger<RuleValidator> _logger;

    public RuleValidator(ILogger<RuleValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a single rule
    /// </summary>
    public bool ValidateRule(Rule rule)
    {
        if (rule == null)
        {
            _logger.LogWarning("Null rule provided to validator");
            return false;
        }

        if (string.IsNullOrWhiteSpace(rule.Name))
        {
            _logger.LogWarning("Rule has empty name");
            return false;
        }

        if (rule.Conditions == null || rule.Conditions.Count == 0)
        {
            _logger.LogWarning("Rule has no conditions");
            return false;
        }

        _logger.LogInformation("Rule '{RuleName}' validation passed", rule.Name);
        return true;
    }

    /// <summary>
    /// Validates a list of rules
    /// </summary>
    public List<string> ValidateRules(List<Rule> rules)
    {
        var errors = new List<string>();

        foreach (var rule in rules)
        {
            if (!ValidateRule(rule))
            {
                errors.Add($"Rule validation failed: {rule?.Name ?? "Unknown"}");
            }
        }

        return errors;
    }
}
