using Microsoft.Extensions.Logging;
using RulesetEngine.Application.DTOs;
using RulesetEngine.Domain.Interfaces;
using RulesetEngine.Domain.Services;

namespace RulesetEngine.Application.Services;

public interface IRuleEvaluationService
{
    Task<EvaluationResultDto> EvaluateAsync(OrderDto order);
}

public class RuleEvaluationService : IRuleEvaluationService
{
    private readonly RuleEvaluationEngine _evaluationEngine;
    private readonly IRulesetRepository _rulesetRepository;
    private readonly IEvaluationLogRepository _evaluationLogRepository;
    private readonly ILogger<RuleEvaluationService> _logger;

    public RuleEvaluationService(
        RuleEvaluationEngine evaluationEngine,
        IRulesetRepository rulesetRepository,
        IEvaluationLogRepository evaluationLogRepository,
        ILogger<RuleEvaluationService> logger)
    {
        _evaluationEngine = evaluationEngine;
        _rulesetRepository = rulesetRepository;
        _evaluationLogRepository = evaluationLogRepository;
        _logger = logger;
    }

    public async Task<EvaluationResultDto> EvaluateAsync(OrderDto order)
    {
        try
        {
            _logger.LogInformation("Starting evaluation for Order ID: {OrderId}", Sanitize(order?.OrderId));

            if (order == null || string.IsNullOrWhiteSpace(order.OrderId))
            {
                _logger.LogWarning("Received invalid order data");
                return new EvaluationResultDto
                {
                    Matched = false,
                    Reason = "Order data is invalid",
                    ProductionPlant = null
                };
            }

            var rulesets = (await _rulesetRepository.GetActiveRulesetsAsync()).ToList();
            _logger.LogDebug("Loaded {RulesetCount} active rulesets", rulesets.Count);

            var context = ExtractContext(order);
            var domainResult = _evaluationEngine.Evaluate(context, rulesets);

            var result = new EvaluationResultDto
            {
                Matched = domainResult.Matched,
                ProductionPlant = domainResult.ProductionPlant,
                MatchedRuleset = domainResult.MatchedRuleset,
                MatchedRule = domainResult.MatchedRule,
                Reason = domainResult.Reason
            };

            await LogEvaluationAsync(order, result);

            _logger.LogInformation(
                "Evaluation completed for Order ID: {OrderId}. Plant: {Plant}, Matched: {Matched}",
                Sanitize(order.OrderId),
                result.ProductionPlant ?? "NONE",
                result.Matched);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating order {OrderId}", Sanitize(order?.OrderId));
            throw;
        }
    }

    private static EvaluationContext ExtractContext(OrderDto order)
    {
        var context = new EvaluationContext
        {
            OrderId = order.OrderId ?? string.Empty,
            Fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        };

        context.Fields["PublisherNumber"] = order.PublisherNumber;
        context.Fields["PublisherName"] = order.PublisherName;
        context.Fields["OrderMethod"] = order.OrderMethod;

        if (order.Shipments?.Any() == true)
        {
            var shipment = order.Shipments.First();
            context.Fields["IsCountry"] = shipment.ShipTo?.IsoCountry;
        }

        if (order.Items?.Any() == true)
        {
            var item = order.Items.First();
            context.Fields["PrintQuantity"] = item.PrintQuantity;
            context.Fields["Sku"] = item.Sku;

            if (item.Components?.Any() == true)
            {
                var component = item.Components.First();
                context.Fields["BindTypeCode"] = component.Attributes?.BindTypeCode;
                context.Fields["ComponentCode"] = component.Code;
            }
        }

        return context;
    }

    private async Task LogEvaluationAsync(OrderDto order, EvaluationResultDto result)
    {
        try
        {
            var log = new Domain.Entities.EvaluationLog
            {
                OrderId = order?.OrderId ?? string.Empty,
                MatchedRuleset = result.MatchedRuleset,
                MatchedRule = result.MatchedRule,
                ProductionPlant = result.ProductionPlant,
                Matched = result.Matched,
                Reason = result.Reason,
                OrderDataJson = System.Text.Json.JsonSerializer.Serialize(order),
                EvaluatedAt = DateTime.UtcNow
            };

            await _evaluationLogRepository.AddAsync(log);
            await _evaluationLogRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging evaluation for order {OrderId}", Sanitize(order?.OrderId));
        }
    }

    private static string Sanitize(string? value)
        => value?.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ") ?? string.Empty;
}
