using Microsoft.AspNetCore.Mvc;
using RulesetEngine.Application.DTOs;
using RulesetEngine.Application.Services;

namespace RulesetEngine.Api.Controllers;

[ApiController]
[Route("api/logs")]
[Produces("application/json")]
public class LogsController : ControllerBase
{
    private readonly IRulesetManagementService _managementService;

    public LogsController(IRulesetManagementService managementService)
    {
        _managementService = managementService;
    }

    /// <summary>Returns the most recent evaluation log entries.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EvaluationLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 100)
    {
        var logs = await _managementService.GetRecentLogsAsync(count);
        return Ok(logs);
    }
}
