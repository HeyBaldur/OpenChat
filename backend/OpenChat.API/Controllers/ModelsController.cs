using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenChat.API.Services;

namespace OpenChat.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ModelsController : ControllerBase
{
    private readonly IModelCatalogService _catalog;

    public ModelsController(IModelCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<IActionResult> GetModels(CancellationToken ct)
    {
        try
        {
            var models = await _catalog.GetModelsAsync(ct);
            return Ok(models);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new { error = "Ollama is not running or unreachable." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(503, new { error = "Ollama did not respond in time." });
        }
    }
}
