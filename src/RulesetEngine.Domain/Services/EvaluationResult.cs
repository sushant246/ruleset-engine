namespace RulesetEngine.Domain.Services;

public class EvaluationResult
{
    public bool Matched { get; set; }
    public string? ProductionPlant { get; set; }
    public string? MatchedRuleset { get; set; }
    public string? MatchedRule { get; set; }
    public string Reason { get; set; } = string.Empty;
}
