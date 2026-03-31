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
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
}
