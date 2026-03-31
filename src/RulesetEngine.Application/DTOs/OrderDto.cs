namespace RulesetEngine.Application.DTOs;

public class ShipToDto
{
    public string? IsoCountry { get; set; }
}

public class ShipmentDto
{
    public ShipToDto? ShipTo { get; set; }
}

public class AttributesDto
{
    public string? BindTypeCode { get; set; }
}

public class ComponentDto
{
    public string? Code { get; set; }
    public AttributesDto? Attributes { get; set; }
}

public class ItemDto
{
    public string? Sku { get; set; }
    public int PrintQuantity { get; set; }
    public List<ComponentDto>? Components { get; set; }
}

public class OrderDto
{
    public string? OrderId { get; set; }
    public string? PublisherNumber { get; set; }
    public string? PublisherName { get; set; }
    public string? OrderMethod { get; set; }
    public List<ShipmentDto>? Shipments { get; set; }
    public List<ItemDto>? Items { get; set; }
}

public class EvaluationResultDto
{
    public bool Matched { get; set; }
    public string? ProductionPlant { get; set; }
    public string? MatchedRuleset { get; set; }
    public string? MatchedRule { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool FallbackUsed { get; set; }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
}

// ── Ruleset management DTOs ──────────────────────────────────────────────────

public class ConditionDto
{
    public int Id { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class RuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string ConditionLogic { get; set; } = "AND";
    public List<ConditionDto> Conditions { get; set; } = new();
    public string? ProductionPlant { get; set; }
}

public class RulesetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string ConditionLogic { get; set; } = "AND";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ConditionDto> Conditions { get; set; } = new();
    public List<RuleDto> Rules { get; set; } = new();
}

public class SaveConditionRequest
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class SaveRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string ConditionLogic { get; set; } = "AND";
    public List<SaveConditionRequest> Conditions { get; set; } = new();
    public string ProductionPlant { get; set; } = string.Empty;
}

public class SaveRulesetRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public string ConditionLogic { get; set; } = "AND";
    public List<SaveConditionRequest> Conditions { get; set; } = new();
    public List<SaveRuleRequest> Rules { get; set; } = new();
}

// ── Evaluation log DTO ───────────────────────────────────────────────────────

public class EvaluationLogDto
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? MatchedRuleset { get; set; }
    public string? MatchedRule { get; set; }
    public string? ProductionPlant { get; set; }
    public bool Matched { get; set; }
    public bool FallbackUsed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
}

