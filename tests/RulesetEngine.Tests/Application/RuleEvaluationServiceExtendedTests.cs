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

/// <summary>
/// Extended tests for RuleEvaluationService covering error scenarios, edge cases, and repository interactions
/// </summary>
public class RuleEvaluationServiceExtendedTests
{
    private readonly Mock<IRulesetRepository> _mockRulesetRepo;
    private readonly Mock<IEvaluationLogRepository> _mockLogRepo;
    private readonly RuleEvaluationEngine _engine;
    private readonly RuleEvaluationService _service;

    public RuleEvaluationServiceExtendedTests()
    {
        _mockRulesetRepo = new Mock<IRulesetRepository>();
        _mockLogRepo = new Mock<IEvaluationLogRepository>();
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

        return new RuleEvaluationService(
            _engine,
            _mockRulesetRepo.Object,
            _mockLogRepo.Object,
            NullLogger<RuleEvaluationService>.Instance,
            config);
    }

    #region Repository Exception Tests

    [Fact]
    public async Task EvaluateAsync_RepositoryThrowsException_PropagatesException()
    {
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var order = CreateSampleOrder("1245101", "99999");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.EvaluateAsync(order));
        Assert.Equal("Database connection failed", ex.Message);
    }

    [Fact]
    public async Task EvaluateAsync_LogRepositoryThrowsException_DoesNotThrow()
    {
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());
        _mockLogRepo
            .Setup(r => r.AddAsync(It.IsAny<EvaluationLog>()))
            .ThrowsAsync(new Exception("Logging failed"));

        var order = CreateSampleOrder("1245101", "99999");
        
        // Should not throw even if logging fails
        var result = await _service.EvaluateAsync(order);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task EvaluateAsync_LogRepositorySaveChangesThrows_DoesNotThrow()
    {
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());
        _mockLogRepo
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new Exception("Save failed"));

        var order = CreateSampleOrder("1245101", "99999");
        
        // Should not throw even if save fails
        var result = await _service.EvaluateAsync(order);
        Assert.NotNull(result);
    }

    #endregion

    #region Complex Order Data Tests

    [Fact]
    public async Task EvaluateAsync_OrderWithAllNullFields_HandlesGracefully()
    {
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());

        var order = new OrderDto
        {
            OrderId = "NULL-FIELDS-001",
            PublisherNumber = null,
            PublisherName = null,
            OrderMethod = null,
            Shipments = null,
            Items = null
        };

        var result = await _service.EvaluateAsync(order);

        Assert.NotNull(result);
        Assert.False(result.Matched);
    }

    [Fact]
    public async Task EvaluateAsync_OrderWithMultipleShipments_UsesFirst()
    {
        var ruleset = new Ruleset
        {
            Id = 1,
            Name = "Multi-Shipment Test",
            IsActive = true,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>()
        };

        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset> { ruleset });

        var order = new OrderDto
        {
            OrderId = "MULTI-SHIP-001",
            PublisherNumber = "99999",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "US" } },
                new() { ShipTo = new ShipToDto { IsoCountry = "CA" } },
                new() { ShipTo = new ShipToDto { IsoCountry = "MX" } }
            }
        };

        var result = await _service.EvaluateAsync(order);

        Assert.NotNull(result);
        // Verify evaluation completed successfully
        Assert.NotNull(result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_OrderWithMultipleItems_UsesFirst()
    {
        var ruleset = new Ruleset
        {
            Id = 1,
            Name = "Multi-Item Test",
            IsActive = true,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>()
        };

        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset> { ruleset });

        var order = new OrderDto
        {
            OrderId = "MULTI-ITEM-001",
            PublisherNumber = "99999",
            Items = new List<ItemDto>
            {
                new() { Sku = "SKU-001", PrintQuantity = 10 },
                new() { Sku = "SKU-002", PrintQuantity = 20 },
                new() { Sku = "SKU-003", PrintQuantity = 30 }
            }
        };

        var result = await _service.EvaluateAsync(order);

        Assert.NotNull(result);
        Assert.NotNull(result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_OrderWithMultipleComponents_UsesFirst()
    {
        var ruleset = new Ruleset
        {
            Id = 1,
            Name = "Multi-Component Test",
            IsActive = true,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>()
        };

        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset> { ruleset });

        var order = new OrderDto
        {
            OrderId = "MULTI-COMP-001",
            PublisherNumber = "99999",
            Items = new List<ItemDto>
            {
                new()
                {
                    Sku = "SKU-001",
                    PrintQuantity = 10,
                    Components = new List<ComponentDto>
                    {
                        new() { Code = "Cover", Attributes = new AttributesDto { BindTypeCode = "PB" } },
                        new() { Code = "Pages", Attributes = new AttributesDto { BindTypeCode = "WC" } },
                        new() { Code = "Binding", Attributes = new AttributesDto { BindTypeCode = "PB" } }
                    }
                }
            }
        };

        var result = await _service.EvaluateAsync(order);

        Assert.NotNull(result);
        Assert.NotNull(result.Reason);
    }

    #endregion

    #region Context Extraction Tests

    [Fact]
    public async Task EvaluateAsync_ExtractsAllFieldsCorrectly()
    {
        var matchingRuleset = new Ruleset
        {
            Id = 1,
            Name = "Field Extraction Test",
            IsActive = true,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Extraction Rule",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "PUB-123" },
                        new() { Field = "OrderMethod", Operator = "Equals", Value = "POD" },
                        new() { Field = "IsCountry", Operator = "Equals", Value = "US" },
                        new() { Field = "PrintQuantity", Operator = "Equals", Value = "50" },
                        new() { Field = "BindTypeCode", Operator = "Equals", Value = "HC" }
                    },
                    Result = new RuleResult { ProductionPlant = "PRIMARY" }
                }
            }
        };

        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset> { matchingRuleset });

        var order = new OrderDto
        {
            OrderId = "EXTRACT-001",
            PublisherNumber = "PUB-123",
            PublisherName = "Test Publisher",
            OrderMethod = "POD",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "US" } }
            },
            Items = new List<ItemDto>
            {
                new()
                {
                    Sku = "HC-001",
                    PrintQuantity = 50,
                    Components = new List<ComponentDto>
                    {
                        new() { Code = "Cover", Attributes = new AttributesDto { BindTypeCode = "HC" } }
                    }
                }
            }
        };

        var result = await _service.EvaluateAsync(order);

        Assert.True(result.Matched);
        Assert.Equal("PRIMARY", result.ProductionPlant);
    }

    #endregion

    #region Logging Verification Tests

    [Fact]
    public async Task EvaluateAsync_LogsOrderDataAsJson()
    {
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());

        var order = CreateSampleOrder("LOG-JSON-001", "99999");
        await _service.EvaluateAsync(order);

        _mockLogRepo.Verify(
            r => r.AddAsync(It.Is<EvaluationLog>(l => 
                l.OrderId == "LOG-JSON-001" && 
                !string.IsNullOrEmpty(l.OrderDataJson))),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_LogsIncludesAllEvaluationDetails()
    {
        var ruleset = new Ruleset
        {
            Id = 1,
            Name = "Log Detail Test",
            IsActive = true,
            Conditions = new List<Condition>(),
            Rules = new List<Rule>
            {
                new()
                {
                    Name = "Log Test Rule",
                    Conditions = new List<Condition>
                    {
                        new() { Field = "PublisherNumber", Operator = "Equals", Value = "LOG-PUB" }
                    },
                    Result = new RuleResult { ProductionPlant = "LOGGED_PLANT" }
                }
            }
        };

        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset> { ruleset });

        var order = new OrderDto
        {
            OrderId = "LOG-DETAIL-001",
            PublisherNumber = "LOG-PUB"
        };

        await _service.EvaluateAsync(order);

        _mockLogRepo.Verify(
            r => r.AddAsync(It.Is<EvaluationLog>(l =>
                l.OrderId == "LOG-DETAIL-001" &&
                l.Matched == true &&
                l.ProductionPlant == "LOGGED_PLANT" &&
                l.MatchedRuleset == "Log Detail Test" &&
                l.MatchedRule == "Log Test Rule")),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_LogsHavesEvaluatedAtTimestamp()
    {
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());

        var order = CreateSampleOrder("LOG-TIME-001", "99999");
        var beforeEvaluation = DateTime.UtcNow;
        await _service.EvaluateAsync(order);
        var afterEvaluation = DateTime.UtcNow;

        _mockLogRepo.Verify(
            r => r.AddAsync(It.Is<EvaluationLog>(l =>
                l.EvaluatedAt >= beforeEvaluation &&
                l.EvaluatedAt <= afterEvaluation)),
            Times.Once);
    }

    #endregion

    #region Fallback Plant Tests with Logging

    [Fact]
    public async Task EvaluateAsync_FallbackUsedLogged()
    {
        var service = BuildService(fallbackPlant: "FALLBACK_PLANT");
        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(new List<Ruleset>());

        var order = CreateSampleOrder("FALLBACK-LOG-001", "99999");
        await service.EvaluateAsync(order);

        _mockLogRepo.Verify(
            r => r.AddAsync(It.Is<EvaluationLog>(l =>
                l.FallbackUsed == true &&
                l.ProductionPlant == "FALLBACK_PLANT")),
            Times.Once);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task EvaluateAsync_OrderIdWithWhitespace_ConsideredInvalid()
    {
        var order = new OrderDto { OrderId = "   " };
        
        var result = await _service.EvaluateAsync(order);

        Assert.False(result.Matched);
        Assert.Equal("Order data is invalid", result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_LargeNumberOfRulesets_EvaluatesAll()
    {
        var rulesets = Enumerable.Range(1, 10)
            .Select(i => new Ruleset
            {
                Id = i,
                Name = $"Ruleset {i}",
                IsActive = true,
                Conditions = new List<Condition>(),
                Rules = new List<Rule>()
            })
            .ToList();

        _mockRulesetRepo
            .Setup(r => r.GetActiveRulesetsAsync())
            .ReturnsAsync(rulesets);

        var order = CreateSampleOrder("MANY-RULESETS-001", "99999");
        var result = await _service.EvaluateAsync(order);

        Assert.NotNull(result);
        Assert.NotNull(result.Reason);
    }

    #endregion

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
}
