namespace RulesetEngine.Domain.Entities;

public class EvaluationLog
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? MatchedRuleset { get; set; }
    public string? MatchedRule { get; set; }
    public string? ProductionPlant { get; set; }
    public bool Matched { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? OrderDataJson { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}
