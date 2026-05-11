using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenChat.API.Models;
using OpenChat.API.Services;
using System.Security.Claims;

namespace OpenChat.API.Controllers;

[Authorize]
[ApiController]
[Route("api/allowlist")]
public class AllowlistController : ControllerBase
{
    private readonly IAllowlistService _service;

    public AllowlistController(IAllowlistService service)
    {
        _service = service;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _service.GetAllForUserAsync(userId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _service.GetByIdAsync(id, userId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AllowedDomainRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE")
        {
            return Conflict(new { message = "Domain already exists in the allowlist." });
        }
        catch (ArgumentException ex) when (ex.Message == "INVALID_DOMAIN")
        {
            return BadRequest(new { message = "Invalid domain format. Use bare domain without protocol or path (e.g. example.com)." });
        }
        catch (ArgumentException ex) when (ex.Message == "RESERVED_DOMAIN")
        {
            return BadRequest(new { message = "Reserved or private domains are not allowed." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AllowedDomainRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE")
        {
            return Conflict(new { message = "Domain already exists in the allowlist." });
        }
        catch (ArgumentException ex) when (ex.Message == "INVALID_DOMAIN")
        {
            return BadRequest(new { message = "Invalid domain format. Use bare domain without protocol or path (e.g. example.com)." });
        }
        catch (ArgumentException ex) when (ex.Message == "RESERVED_DOMAIN")
        {
            return BadRequest(new { message = "Reserved or private domains are not allowed." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException ex) when (ex.Message == "SYSTEM_DEFAULT")
        {
            return StatusCode(403, new { message = "System default domains cannot be deleted. Disable it instead." });
        }
    }

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> Toggle(string id)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            var result = await _service.ToggleAsync(id, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestUrl([FromQuery] string url)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { message = "url query parameter is required." });

        var allowed = await _service.IsDomainAllowedAsync(url, userId);
        return Ok(new { allowed });
    }
}
