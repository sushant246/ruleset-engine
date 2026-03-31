using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RulesetEngine.Application.DTOs;
using Xunit;

namespace RulesetEngine.Tests.Api;

public class EvaluationControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EvaluationControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Evaluate_ValidOrder_ReturnsOk()
    {
        var order = CreateSampleOrder("1245101", "99999");

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Evaluate_MatchingOrder_ReturnsCorrectPlant()
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

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        Assert.NotNull(result);
        Assert.True(result.Matched);
        Assert.Equal("US", result.ProductionPlant);
        Assert.Equal("Ruleset Two", result.MatchedRuleset);
    }

    [Fact]
    public async Task Evaluate_NoMatchingOrder_ReturnsNotMatchedResult()
    {
        var order = new OrderDto
        {
            OrderId = "9999999",
            PublisherNumber = "UNKNOWN",
            OrderMethod = "POD",
            Shipments = new List<ShipmentDto>
            {
                new() { ShipTo = new ShipToDto { IsoCountry = "AU" } }
            },
            Items = new List<ItemDto>
            {
                new()
                {
                    Sku = "XX-001",
                    PrintQuantity = 5,
                    Components = new List<ComponentDto>
                    {
                        new()
                        {
                            Code = "Cover",
                            Attributes = new AttributesDto { BindTypeCode = "XX" }
                        }
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/evaluate", order);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        Assert.NotNull(result);
        Assert.False(result.Matched);
        Assert.Null(result.ProductionPlant);
    }

    [Fact]
    public async Task Evaluate_InvalidJson_ReturnsBadRequest()
    {
        var content = new System.Net.Http.StringContent("invalid json", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/evaluate", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
}
