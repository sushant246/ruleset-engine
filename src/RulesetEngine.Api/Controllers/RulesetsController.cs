using Microsoft.AspNetCore.Mvc;
using RulesetEngine.Application.DTOs;
using RulesetEngine.Application.Services;

namespace RulesetEngine.Api.Controllers;

[ApiController]
[Route("api/rulesets")]
[Produces("application/json")]
public class RulesetsController : ControllerBase
{
    private readonly IRulesetManagementService _managementService;
    private readonly ILogger<RulesetsController> _logger;

    public RulesetsController(
        IRulesetManagementService managementService,
        ILogger<RulesetsController> logger)
    {
        _managementService = managementService;
        _logger = logger;
    }

    /// <summary>Returns all rulesets.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RulesetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var rulesets = await _managementService.GetAllAsync();
        return Ok(rulesets);
    }

    /// <summary>Returns a single ruleset by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RulesetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ruleset = await _managementService.GetByIdAsync(id);
        return ruleset == null ? NotFound() : Ok(ruleset);
    }

    /// <summary>Creates a new ruleset.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RulesetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SaveRulesetRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Message = "Invalid request" });

        var created = await _managementService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing ruleset.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RulesetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] SaveRulesetRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Message = "Invalid request" });

        var updated = await _managementService.UpdateAsync(id, request);
        return updated == null ? NotFound() : Ok(updated);
    }

    /// <summary>Deletes a ruleset.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _managementService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
