namespace RulesetEngine.Domain.Entities;

public class Rule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RulesetId { get; set; }
    public Ruleset? Ruleset { get; set; }
    public List<Condition> Conditions { get; set; } = new();
    public RuleResult? Result { get; set; }
}
