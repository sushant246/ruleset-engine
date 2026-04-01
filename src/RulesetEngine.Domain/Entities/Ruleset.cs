namespace RulesetEngine.Domain.Entities;

public class Ruleset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string ConditionLogic { get; set; } = "AND";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<Condition> Conditions { get; set; } = new();
    public List<Rule> Rules { get; set; } = new();
}
