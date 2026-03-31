namespace RulesetEngine.Domain.Services;

public class EvaluationContext
{
    public string OrderId { get; set; } = string.Empty;
    public Dictionary<string, object?> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
