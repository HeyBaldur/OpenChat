using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenChat.Ai.Interfaces;
using OpenChat.Ai.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace OpenChat.API.Controllers;

// TODO: remove when chat integration is verified
[ApiController]
[Route("api/tools/test")]
[Authorize]
public class ToolsTestController : ControllerBase
{
    private readonly IToolRegistry _registry;

    public ToolsTestController(IToolRegistry registry)
    {
        _registry = registry;
    }

    [HttpGet("list")]
    public IActionResult List()
    {
        var tools = _registry.GetAll().Select(t => new
        {
            name = t.Name,
            description = t.Description,
            parameters = t.JsonSchema
        });
        return Ok(tools);
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ToolExecuteRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Tool))
            return BadRequest(new { message = "Field 'tool' is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;

        var tool = _registry.GetByName(request.Tool);
        if (tool is null)
            return NotFound(new { message = $"Tool '{request.Tool}' not found." });

        var sw = Stopwatch.StartNew();
        var result = await tool.ExecuteAsync(request.Arguments, userId, ct);
        sw.Stop();

        return Ok(new
        {
            success = result.Success,
            content = result.Content,
            sourceUrl = result.SourceUrl,
            errorReason = result.ErrorReason,
            executionMs = sw.ElapsedMilliseconds
        });
    }
}
