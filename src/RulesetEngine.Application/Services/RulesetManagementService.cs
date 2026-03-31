using Microsoft.Extensions.Logging;
using RulesetEngine.Application.DTOs;
using RulesetEngine.Domain.Entities;
using RulesetEngine.Domain.Interfaces;

namespace RulesetEngine.Application.Services;

public interface IRulesetManagementService
{
    Task<IEnumerable<RulesetDto>> GetAllAsync();
    Task<RulesetDto?> GetByIdAsync(int id);
    Task<RulesetDto> CreateAsync(SaveRulesetRequest request);
    Task<RulesetDto?> UpdateAsync(int id, SaveRulesetRequest request);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<EvaluationLogDto>> GetRecentLogsAsync(int count = 100);
}

public class RulesetManagementService : IRulesetManagementService
{
    private readonly IRulesetRepository _rulesetRepository;
    private readonly IEvaluationLogRepository _logRepository;
    private readonly ILogger<RulesetManagementService> _logger;

    public RulesetManagementService(
        IRulesetRepository rulesetRepository,
        IEvaluationLogRepository logRepository,
        ILogger<RulesetManagementService> logger)
    {
        _rulesetRepository = rulesetRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<RulesetDto>> GetAllAsync()
    {
        var rulesets = await _rulesetRepository.GetActiveRulesetsAsync();
        return rulesets.Select(MapToDto);
    }

    public async Task<RulesetDto?> GetByIdAsync(int id)
    {
        var ruleset = await _rulesetRepository.GetByIdAsync(id);
        return ruleset == null ? null : MapToDto(ruleset);
    }

    public async Task<RulesetDto> CreateAsync(SaveRulesetRequest request)
    {
        var ruleset = MapFromRequest(request);
        var created = await _rulesetRepository.AddAsync(ruleset);
        _logger.LogInformation("Created ruleset: {RulesetName} (Id={Id})", Sanitize(created.Name), created.Id);
        return MapToDto(created);
    }

    public async Task<RulesetDto?> UpdateAsync(int id, SaveRulesetRequest request)
    {
        var existing = await _rulesetRepository.GetByIdAsync(id);
        if (existing == null)
            return null;

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Priority = request.Priority;
        existing.IsActive = request.IsActive;
        existing.ConditionLogic = request.ConditionLogic;

        existing.Conditions = request.Conditions.Select(c => new Condition
        {
            Field = c.Field,
            Operator = c.Operator,
            Value = c.Value
        }).ToList();

        existing.Rules = request.Rules.Select(r => new Rule
        {
            Name = r.Name,
            Priority = r.Priority,
            ConditionLogic = r.ConditionLogic,
            Conditions = r.Conditions.Select(c => new Condition
            {
                Field = c.Field,
                Operator = c.Operator,
                Value = c.Value
            }).ToList(),
            Result = new RuleResult { ProductionPlant = r.ProductionPlant }
        }).ToList();

        await _rulesetRepository.UpdateAsync(existing);
        _logger.LogInformation("Updated ruleset: {RulesetName} (Id={Id})", Sanitize(existing.Name), existing.Id);
        return MapToDto(existing);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _rulesetRepository.GetByIdAsync(id);
        if (existing == null)
            return false;

        await _rulesetRepository.DeleteAsync(id);
        _logger.LogInformation("Deleted ruleset Id={Id}", id);
        return true;
    }

    public async Task<IEnumerable<EvaluationLogDto>> GetRecentLogsAsync(int count = 100)
    {
        var logs = await _logRepository.GetRecentAsync(count);
        return logs.Select(l => new EvaluationLogDto
        {
            Id = l.Id,
            OrderId = l.OrderId,
            MatchedRuleset = l.MatchedRuleset,
            MatchedRule = l.MatchedRule,
            ProductionPlant = l.ProductionPlant,
            Matched = l.Matched,
            FallbackUsed = l.FallbackUsed,
            Reason = l.Reason,
            EvaluatedAt = l.EvaluatedAt
        });
    }

    // ── mapping helpers ──────────────────────────────────────────────────────

    private static string Sanitize(string? value)
        => value?.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ") ?? string.Empty;

    private static RulesetDto MapToDto(Ruleset rs) => new()
    {
        Id = rs.Id,
        Name = rs.Name,
        Description = rs.Description,
        Priority = rs.Priority,
        IsActive = rs.IsActive,
        ConditionLogic = rs.ConditionLogic,
        CreatedAt = rs.CreatedAt,
        UpdatedAt = rs.UpdatedAt,
        Conditions = rs.Conditions.Select(c => new ConditionDto
        {
            Id = c.Id,
            Field = c.Field,
            Operator = c.Operator,
            Value = c.Value
        }).ToList(),
        Rules = rs.Rules.Select(r => new RuleDto
        {
            Id = r.Id,
            Name = r.Name,
            Priority = r.Priority,
            ConditionLogic = r.ConditionLogic,
            ProductionPlant = r.Result?.ProductionPlant,
            Conditions = r.Conditions.Select(c => new ConditionDto
            {
                Id = c.Id,
                Field = c.Field,
                Operator = c.Operator,
                Value = c.Value
            }).ToList()
        }).ToList()
    };

    private static Ruleset MapFromRequest(SaveRulesetRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
        Priority = request.Priority,
        IsActive = request.IsActive,
        ConditionLogic = request.ConditionLogic,
        Conditions = request.Conditions.Select(c => new Condition
        {
            Field = c.Field,
            Operator = c.Operator,
            Value = c.Value
        }).ToList(),
        Rules = request.Rules.Select(r => new Rule
        {
            Name = r.Name,
            Priority = r.Priority,
            ConditionLogic = r.ConditionLogic,
            Conditions = r.Conditions.Select(c => new Condition
            {
                Field = c.Field,
                Operator = c.Operator,
                Value = c.Value
            }).ToList(),
            Result = new RuleResult { ProductionPlant = r.ProductionPlant }
        }).ToList()
    };
}
