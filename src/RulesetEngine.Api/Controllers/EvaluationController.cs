using Microsoft.AspNetCore.Mvc;
using RulesetEngine.Application.DTOs;
using RulesetEngine.Application.Services;

namespace RulesetEngine.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class EvaluationController : ControllerBase
{
    private readonly IRuleEvaluationService _ruleEvaluationService;
    private readonly ILogger<EvaluationController> _logger;

    public EvaluationController(
        IRuleEvaluationService ruleEvaluationService,
        ILogger<EvaluationController> logger)
    {
        _ruleEvaluationService = ruleEvaluationService;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates an order against configured rulesets to determine production plant
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/evaluate
    ///     {
    ///       "orderId": "1245101",
    ///       "publisherNumber": "99999",
    ///       "publisherName": "BookWorld Ltd",
    ///       "orderMethod": "POD",
    ///       "shipments": [
    ///         {
    ///           "shipTo": { "isoCountry": "US" }
    ///         }
    ///       ],
    ///       "items": [
    ///         {
    ///           "sku": "PB-001",
    ///           "printQuantity": 10,
    ///           "components": [
    ///             {
    ///               "code": "Cover",
    ///               "attributes": { "bindTypeCode": "PB" }
    ///             }
    ///           ]
    ///         }
    ///       ]
    ///     }
    /// </remarks>
    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(EvaluationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Evaluate([FromBody] OrderDto order)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid order model received");
            return BadRequest(new ErrorResponse
            {
                Message = "Invalid order data",
                Details = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _ruleEvaluationService.EvaluateAsync(order);
        return Ok(result);
    }
}
