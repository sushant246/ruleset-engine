namespace RulesetEngine.Domain.Entities;

public class Rule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    /// <summary>
    /// Logical operator used to combine this rule's conditions. "AND" (default) or "OR".
    /// </summary>
    public string ConditionLogic { get; set; } = "AND";
    public int RulesetId { get; set; }
    public Ruleset? Ruleset { get; set; }
    public List<Condition> Conditions { get; set; } = new();
    public RuleResult? Result { get; set; }
}
