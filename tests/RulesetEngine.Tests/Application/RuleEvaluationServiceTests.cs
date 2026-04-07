using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RulesetEngine.Application.DTOs;
using RulesetEngine.Application.Services;
using RulesetEngine.Domain.Entities;
using RulesetEngine.Domain.Interfaces;
using RulesetEngine.Domain.Services;
using Xunit;

namespace RulesetEngine.Tests.Application;

public class RuleEvaluationServiceTests
{
    private readonly Mock<IRulesetRepository> _mockRulesetRepo;
    private readonly Mock<IEvaluationLogRepository> _mockLogRepo;
    private readonly Mock<IRulesetCacheService> _mockCacheService;
    private readonly RuleEvaluationEngine _engine;
    private readonly RuleEvaluationService _service;

    public RuleEvaluationServiceTests()
    {
        _mockRulesetRepo = new Mock<IRulesetRepository>();
        _mockLogRepo = new Mock<IEvaluationLogRepository>();
        _mockCacheService = new Mock<IRulesetCacheService>();
        _engine = new RuleEvaluationEngine(NullLogger<RuleEvaluationEngine>.Instance);
        _service = BuildService(fallbackPlant: null);

        _mockLogRepo
            .Setup(r => r.AddAsync(It.IsAny<EvaluationLog>()))
            .ReturnsAsync((EvaluationLog log) => log);
        _mockLogRepo
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);
    }

    private RuleEvaluationService BuildService(string? fallbackPlant)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(fallbackPlant == null
                ? Array.Empty<KeyValuePair<string, string?>>()
                : new[] { new KeyValuePair<string, string?>("RulesetEngine:FallbackProductionPlant", fallbackPlant) })
            .Build();

        _mockLogRepo
            .Setup(r => r.AddAsync(It.IsAny<EvaluationLog>()))
            .ReturnsAsync((EvaluationLog log) => log);
        _mockLogRepo
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // ✅ FIX: Configure the cache service to call the repository and cache the result
        _mockCacheService
            .Setup(c => c.GetActiveRulesetsAsync(It.IsAny<IRulesetRepository>()))
            .Returns(async (IRulesetRepository repo) => await repo.GetActiveRulesetsAsync());

        return new RuleEvaluationService(
            _engine,
            _mockRulesetRepo.Object,
            _mockCacheService.Object,
            _mockLogRepo.Object,
            NullLogger<RuleEvaluationService>.Instance,
            config);
    }

    [Fact]
    public async Task EvaluateAsync_NullOrder_ReturnsInvalidResult()
    {
        var result = await _service.EvaluateAsync(null!);

        Assert.False(result.Matched);
        Assert.Equal("Order data is invalid", result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_EmptyOrderId_ReturnsInvalidResult()
    {
        var result = await _service.EvaluateAsync(new OrderDto { OrderId = "" });

        Assert.False(result.Matched);
        Assert.Equal("Order data is invalid", result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_NoRulesets_ReturnsNoMatch()
    {
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());

        var order = CreateSampleOrder("1245101", "99999");
        var result = await _service.EvaluateAsync(order);

        Assert.False(result.Matched);
    }

    [Fact]
    public async Task EvaluateAsync_MatchingRuleset_ReturnsPlant()
    {
        var ruleset = new Ruleset
        {
            Id = 1,
            Name = "Ruleset Two",
            IsActive = true,
            Conditions = new List<Condition>
            {
                new() { Field = "PublisherNumber", Operator = "Equals", Value = "99999" },
                new() { Field = "OrderMethod", Operator = "Equals", Value = "POD" }
            },
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" },
                        new() { Field = "IsCountry", Operator = "Equals", Value = "US" },
                        new() { Field = "PrintQuantity", Operator = "LessThanOrEqual", Value = "20" }
                    },
                    Result = new RuleResult { ProductionPlant = "US" }
                }
            }
        };

        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset> { ruleset });

        var order = CreateSampleOrder("1245101", "99999");
        var result = await _service.EvaluateAsync(order);

        Assert.True(result.Matched);
        Assert.Equal("US", result.ProductionPlant);
        Assert.Equal("Ruleset Two", result.MatchedRuleset);
        Assert.Equal("Rule 1", result.MatchedRule);
    }

    [Fact]
    public async Task EvaluateAsync_LogsEvaluation()
    {
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());

        var order = CreateSampleOrder("1245101", "99999");
        await _service.EvaluateAsync(order);

        _mockLogRepo.Verify(r => r.AddAsync(It.IsAny<EvaluationLog>()), Times.Once);
        _mockLogRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    private static OrderDto CreateSampleOrder(string orderId, string publisherNumber)
    {
        return new OrderDto
        {
            OrderId = orderId,
            PublisherNumber = publisherNumber,
            PublisherName = "BookWorld Ltd",
            OrderMethod = "POD",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "US" } }
            },
            Items = new List<ItemDto>
            {
                new()
                {
                    Sku = "PB-001",
                    PrintQuantity = 10,
                    Components = new List<ComponentDto>
                    {
                        new()
                        {
                            Code = "Cover",
                            Attributes = new AttributesDto { BindTypeCode = "PB" }
                        }
                    }
                }
            }
        };
    }

    // ── Fallback plant tests ─────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_NoMatch_WithFallback_ReturnsFallbackPlant()
    {
        var service = BuildService(fallbackPlant: "DEFAULT_PLANT");
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());

        var order = CreateSampleOrder("FALLBACK-001", "UNKNOWN");
        var result = await service.EvaluateAsync(order);

        Assert.False(result.Matched);
        Assert.True(result.FallbackUsed);
        Assert.Equal("DEFAULT_PLANT", result.ProductionPlant);
        Assert.Contains("DEFAULT_PLANT", result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_NoMatch_WithoutFallback_ReturnsNullPlant()
    {
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());

        var order = CreateSampleOrder("NOFALLBACK-001", "UNKNOWN");
        var result = await _service.EvaluateAsync(order);

        Assert.False(result.Matched);
        Assert.False(result.FallbackUsed);
        Assert.Null(result.ProductionPlant);
    }

    [Fact]
    public async Task EvaluateAsync_MatchFound_FallbackNotUsed()
    {
        var service = BuildService(fallbackPlant: "DEFAULT_PLANT");
        var ruleset = new Ruleset
        {
            Id = 1, Name = "Test", IsActive = true,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Rule 1",
                    Conditions = new List<Condition>(),
                    Result = new RuleResult { ProductionPlant = "MATCHED_PLANT" }
                }
            }
        };
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset> { ruleset });

        var order = CreateSampleOrder("MATCH-001", "ANY");
        var result = await service.EvaluateAsync(order);

        Assert.True(result.Matched);
        Assert.False(result.FallbackUsed);
        Assert.Equal("MATCHED_PLANT", result.ProductionPlant);
    }
}
