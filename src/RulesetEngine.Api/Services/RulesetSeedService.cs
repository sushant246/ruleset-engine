using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RulesetEngine.Domain.Entities;
using RulesetEngine.Infrastructure.Database;

namespace RulesetEngine.Api.Services;

/// <summary>Service for seeding ruleset data from configuration files</summary>
public class RulesetSeedService
{
    private readonly ILogger<RulesetSeedService> _logger;

    public RulesetSeedService(ILogger<RulesetSeedService> logger)
    {
        _logger = logger;
    }

    /// <summary>Seeds rulesets from a JSON configuration file</summary>
    public async Task SeedFromJsonAsync(RulesetDbContext dbContext, string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("❌ Configuration file not found at {JsonPath}", jsonPath);
                return;
            }

            _logger.LogInformation("📖 Reading JSON file from: {JsonPath}", jsonPath);
            var json = await File.ReadAllTextAsync(jsonPath);
            _logger.LogInformation("✅ JSON file read successfully. Length: {Length} bytes", json.Length);

            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            var config = JsonSerializer.Deserialize<RulesetConfiguration>(json, options);

            if (config?.Rulesets == null || config.Rulesets.Count == 0)
            {
                _logger.LogWarning("❌ No rulesets found in configuration file");
                return;
            }

            _logger.LogInformation("📋 Found {RulesetCount} rulesets in config", config.Rulesets.Count);

            var rulesets = new List<Ruleset>();

            foreach (var rulesetConfig in config.Rulesets)
            {
                _logger.LogInformation("  📌 Processing Ruleset: {Name}", rulesetConfig.Name);

                var ruleset = new Ruleset
                {
                    Name = rulesetConfig.Name,
                    Description = rulesetConfig.Description,
                    Priority = rulesetConfig.Priority,
                    IsActive = rulesetConfig.IsActive,
                    ConditionLogic = rulesetConfig.ConditionLogic ?? "AND",
                    CreatedAt = DateTime.UtcNow,
                    Rules = new List<Rule>()
                };

                if (rulesetConfig.Rules != null && rulesetConfig.Rules.Count > 0)
                {
                    _logger.LogInformation("    ✏️ Adding {RuleCount} rules", rulesetConfig.Rules.Count);

                    foreach (var ruleConfig in rulesetConfig.Rules)
                    {
                        var rule = new Rule
                        {
                            Name = ruleConfig.Name,
                            Priority = ruleConfig.Priority,
                            ConditionLogic = ruleConfig.ConditionLogic ?? "AND",
                            Ruleset = ruleset,
                            Result = new RuleResult
                            {
                                ProductionPlant = ruleConfig.ProductionPlant
                            },
                            Conditions = new List<Condition>()
                        };

                        if (ruleConfig.Conditions != null && ruleConfig.Conditions.Count > 0)
                        {
                            _logger.LogInformation("      🔧 Adding {ConditionCount} conditions to rule", ruleConfig.Conditions.Count);

                            foreach (var conditionConfig in ruleConfig.Conditions)
                            {
                                var condition = new Condition
                                {
                                    Field = conditionConfig.Field,
                                    Operator = conditionConfig.Operator,
                                    Value = conditionConfig.Value,
                                    Rule = rule
                                };

                                rule.Conditions.Add(condition);
                            }
                        }

                        ruleset.Rules.Add(rule);
                    }
                }

                rulesets.Add(ruleset);
            }

            _logger.LogInformation("🔄 Adding {RulesetCount} rulesets to database context...", rulesets.Count);
            await dbContext.Rulesets.AddRangeAsync(rulesets);

            _logger.LogInformation("💾 Saving changes to database...");
            await dbContext.SaveChangesAsync();

            // Verify the data was actually saved
            var savedCount = await dbContext.Rulesets.CountAsync();
            var savedRuleCount = await dbContext.Rules.CountAsync();
            var savedConditionCount = await dbContext.Conditions.CountAsync();

            _logger.LogInformation("✅ Successfully seeded database!");
            _logger.LogInformation("   📊 Rulesets: {RulesetCount}", savedCount);
            _logger.LogInformation("   📊 Rules: {RuleCount}", savedRuleCount);
            _logger.LogInformation("   📊 Conditions: {ConditionCount}", savedConditionCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error seeding rulesets from {JsonPath}", jsonPath);
            throw;
        }
    }
}

/// <summary>Configuration models for JSON deserialization</summary>
public class RulesetConfiguration
{
    public List<RulesetConfigModel> Rulesets { get; set; } = new();
}

public class RulesetConfigModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string? ConditionLogic { get; set; }
    public List<RuleConfigModel> Rules { get; set; } = new();
}

public class RuleConfigModel
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public string? ConditionLogic { get; set; }
    public string ProductionPlant { get; set; } = string.Empty;
    public List<ConditionConfigModel> Conditions { get; set; } = new();
}

public class ConditionConfigModel
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class SampleOrder
{
    public string OrderId { get; set; } = string.Empty;
    public string PublisherNumber { get; set; } = string.Empty;
    public string PublisherName { get; set; } = string.Empty;
    public string OrderMethod { get; set; } = string.Empty;
    public List<Shipment> Shipments { get; set; } = new();
    public List<Item> Items { get; set; } = new();
}

public class Shipment
{
    public Address ShipTo { get; set; } = new();
}

public class Address
{
    public string IsoCountry { get; set; } = string.Empty;
}

public class Item
{
    public string Sku { get; set; } = string.Empty;
    public int PrintQuantity { get; set; }
    public List<Component> Components { get; set; } = new();
}

public class Component
{
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
}
