namespace RulesetEngine.Domain.Entities;

public class RuleResult
{
    public int Id { get; set; }
    public string ProductionPlant { get; set; } = string.Empty;
    public int RuleId { get; set; }
    public Rule? Rule { get; set; }
}
