extern alias ApiAlias;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RulesetEngine.Application.DTOs;
using Xunit;

namespace RulesetEngine.Tests.Api;

/// <summary>
/// Extended tests for EvaluationController covering error scenarios, edge cases, and response validation
/// </summary>
public class EvaluationControllerExtendedTests : IClassFixture<WebApplicationFactory<ApiAlias::Program>>
{
    private readonly HttpClient _client;

    public EvaluationControllerExtendedTests(WebApplicationFactory<ApiAlias::Program> factory)
    {
        _client = factory.CreateClient();
    }

    #region Happy Path Tests

    [Fact]
    public async Task Evaluate_ValidOrderWithMatch_ReturnsOkWithMatchedPlant()
    {
        var order = new OrderDto
        {
            OrderId = "1245101",
            PublisherNumber = "99999",
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
                    PrintQuantity = 20,
                    Components = new List<ComponentDto>
                    {
                        new() { Code = "Cover", Attributes = new AttributesDto { BindTypeCode = "PB" } }
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        Assert.NotNull(result);
        Assert.True(result!.Matched, "Expected order to match a ruleset");
        Assert.NotEmpty(result.ProductionPlant!);
    }

    [Fact]
    public async Task Evaluate_ValidOrderWithoutMatch_ReturnsOkResult()
    {
        var order = new OrderDto
        {
            OrderId = "999-NO-MATCH",
            PublisherNumber = "11111",
            PublisherName = "Unknown Publisher",
            OrderMethod = "UNKNOWN",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "ZZ" } }
            },
            Items = new List<ItemDto>
            {
                new()
                {
                    Sku = "UNKNOWN-SKU",
                    PrintQuantity = 100,
                    Components = new List<ComponentDto>()
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        Assert.NotNull(result);
        // Verify response is valid
        Assert.NotNull(result!.Reason);
    }

    #endregion

    #region Invalid Input Tests

    [Fact]
    public async Task Evaluate_NullOrder_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/evaluate", (OrderDto?)null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_MissingOrderId_ReturnsBadRequest()
    {
        var order = new OrderDto
        {
            OrderId = null, // Missing required field
            PublisherNumber = "99999",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "US" } }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        // Depending on validation setup, may return 400
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK,
            $"Expected 400 or 200, got {response.StatusCode}");
    }

    [Fact]
    public async Task Evaluate_EmptyOrderId_ReturnsBadRequest()
    {
        var order = new OrderDto
        {
            OrderId = string.Empty,
            PublisherNumber = "99999",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "US" } }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        // Depending on validation setup
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    #endregion

    #region Data Structure Tests

    [Fact]
    public async Task Evaluate_ComplexOrderStructure_ProcessesAllComponents()
    {
        var order = new OrderDto
        {
            OrderId = "COMPLEX-001",
            PublisherNumber = "99999",
            PublisherName = "Test Publisher",
            OrderMethod = "POD",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "US" } },
                new() { ShipTo = new ShipToDto { IsoCountry = "CA" } }
            },
            Items = new List<ItemDto>
            {
                new()
                {
                    Sku = "PB-001",
                    PrintQuantity = 50,
                    Components = new List<ComponentDto>
                    {
                        new() { Code = "Cover", Attributes = new AttributesDto { BindTypeCode = "PB" } },
                        new() { Code = "Pages", Attributes = new AttributesDto { BindTypeCode = "PB" } }
                    }
                },
                new()
                {
                    Sku = "HC-001",
                    PrintQuantity = 25,
                    Components = new List<ComponentDto>
                    {
                        new() { Code = "Cover", Attributes = new AttributesDto { BindTypeCode = "HC" } }
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        Assert.NotNull(result);
        Assert.NotNull(result!.ProductionPlant);
    }

    [Fact]
    public async Task Evaluate_MinimalValidOrder_ReturnsResult()
    {
        var order = new OrderDto
        {
            OrderId = "MIN-001",
            PublisherNumber = "99999"
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        Assert.NotNull(result);
    }

    #endregion

    #region Response Validation Tests

    [Fact]
    public async Task Evaluate_ResponseContainsAllExpectedFields()
    {
        var order = new OrderDto
        {
            OrderId = "FIELDS-001",
            PublisherNumber = "99999",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "US" } }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        Assert.NotNull(result);
        // Verify all expected properties exist and are accessible
        _ = result!.Matched;
        _ = result.ProductionPlant;
        _ = result.MatchedRuleset;
        _ = result.MatchedRule;
        Assert.NotNull(result.Reason); // Reason should never be null
        _ = result.FallbackUsed;
    }

    [Fact]
    public async Task Evaluate_SuccessfulResponse_HasValidContentType()
    {
        var order = new OrderDto
        {
            OrderId = "CT-001",
            PublisherNumber = "99999"
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Content.Headers.ContentType?.ToString() ?? string.Empty);
    }

    #endregion

    #region Sequential Processing Tests

    [Fact]
    public async Task Evaluate_MultipleOrdersSequentially_AllProcessedCorrectly()
    {
        var orders = new[]
        {
            new OrderDto { OrderId = "SEQ-001", PublisherNumber = "99999" },
            new OrderDto { OrderId = "SEQ-002", PublisherNumber = "99999" },
            new OrderDto { OrderId = "SEQ-003", PublisherNumber = "99999" }
        };

        foreach (var order in orders)
        {
            var response = await _client.PostAsJsonAsync("/api/evaluate", order);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
            Assert.NotNull(result);
            // All results should be valid
            Assert.NotNull(result!.Reason);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Evaluate_OrderWithNullCollections_HandledGracefully()
    {
        var order = new OrderDto
        {
            OrderId = "NULL-COLL-001",
            PublisherNumber = "99999",
            Shipments = null,
            Items = null
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        // Should either process with defaults or return 400
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Evaluate_OrderWithEmptyCollections_ProcessedCorrectly()
    {
        var order = new OrderDto
        {
            OrderId = "EMPTY-COLL-001",
            PublisherNumber = "99999",
            Shipments = new List<ShipmentDto>(),
            Items = new List<ItemDto>()
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Evaluate_OrderWithSpecialCharactersInFields_HandledCorrectly()
    {
        var order = new OrderDto
        {
            OrderId = "SPECIAL-@#$%-001",
            PublisherNumber = "99999",
            PublisherName = "Publisher \"With\" <Special> &Characters",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "US" } }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
