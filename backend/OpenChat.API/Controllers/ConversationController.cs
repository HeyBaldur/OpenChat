using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenChat.Application.Interfaces.Services;
using OpenChat.Domain.Dtos;
using OpenChat.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace OpenChat.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly IConversationRepository _convRepo;
    private readonly IChatRepository _chatRepo;
    private readonly IModelCatalogService _catalog;

    public ConversationController(
        IConversationRepository convRepo,
        IChatRepository chatRepo,
        IModelCatalogService catalog)
    {
        _convRepo = convRepo;
        _chatRepo = chatRepo;
        _catalog = catalog;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var tokenUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (userId != tokenUserId) return Forbid();

        var conversations = await _convRepo.GetByUserAsync(tokenUserId);
        return Ok(conversations);
    }

    [HttpDelete("{conversationId}")]
    public async Task<IActionResult> Delete(string conversationId)
    {
        await _chatRepo.DeleteByConversationAsync(conversationId);
        await _convRepo.DeleteAsync(conversationId);
        return NoContent();
    }

    [HttpPut("{conversationId}/model")]
    public async Task<IActionResult> UpdateModel(
        string conversationId,
        [FromBody] UpdateConversationModelRequest request,
        CancellationToken ct)
    {
        try
        {
            var models = await _catalog.GetModelsAsync(ct);
            if (!models.Any(m => m.Name == request.Model))
                return BadRequest(new { error = $"Model '{request.Model}' is not installed in Ollama." });
        }
        catch (HttpRequestException)
        {
            // Ollama unreachable — skip validation and apply the update
        }
        catch (TaskCanceledException)
        {
            // Same fallback
        }

        await _convRepo.UpdateModelAsync(conversationId, request.Model);
        return NoContent();
    }
}
