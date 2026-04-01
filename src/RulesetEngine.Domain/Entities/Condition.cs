namespace RulesetEngine.Domain.Entities;

public class Condition
{
    public int Id { get; set; }

    // Comparison operator (Equals, Contains, GreaterThan, etc.)
    public string Operator { get; set; } = string.Empty;

    // Field and Value for simple condition comparison
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    // Navigation properties
    public int? RulesetId { get; set; }
    public Ruleset? Ruleset { get; set; }
    public int? RuleId { get; set; }
    public Rule? Rule { get; set; }
}
