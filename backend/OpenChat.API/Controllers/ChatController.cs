using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenChat.API.Models;
using OpenChat.API.Repositories;
using OpenChat.API.Services;
using System.Security.Claims;
using System.Text.Json;

namespace OpenChat.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IChatRepository _chatRepo;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ChatController(IChatService chatService, IChatRepository chatRepo)
    {
        _chatService = chatService;
        _chatRepo = chatRepo;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });
        var response = await _chatService.ProcessMessageAsync(request);
        return Ok(response);
    }

    [HttpPost("stream")]
    public async Task StreamChat([FromBody] ChatRequest request)
    {
        request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = 400;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["Connection"] = "keep-alive";

        var ct = HttpContext.RequestAborted;

        try
        {
            await foreach (var evt in _chatService.StreamMessageAsync(request, ct))
            {
                if (ct.IsCancellationRequested) break;

                string eventName;
                string dataJson;

                switch (evt.Type)
                {
                    case AgenticEventType.Token:
                        eventName = "token";
                        dataJson = JsonSerializer.Serialize(evt.TokenText, JsonOpts);
                        break;
                    case AgenticEventType.ToolStart:
                        eventName = "tool_start";
                        dataJson = JsonSerializer.Serialize(new { tool = evt.ToolName, args = evt.ToolArguments }, JsonOpts);
                        break;
                    case AgenticEventType.ToolEnd:
                        eventName = "tool_end";
                        dataJson = JsonSerializer.Serialize(new { tool = evt.ToolName, ok = evt.ToolSuccess, sourceUrl = evt.SourceUrl, errorReason = evt.ErrorReason, preview = evt.ContentPreview }, JsonOpts);
                        break;
                    case AgenticEventType.Done:
                        eventName = "done";
                        dataJson = JsonSerializer.Serialize(new { conversationId = evt.ConversationId, conversationTitle = evt.ConversationTitle, promptTokens = evt.PromptTokens, completionTokens = evt.CompletionTokens, tokensUsed = (evt.PromptTokens ?? 0) + (evt.CompletionTokens ?? 0), toolCallsUsed = evt.ToolCallsUsed }, JsonOpts);
                        break;
                    case AgenticEventType.Error:
                        eventName = "error";
                        dataJson = JsonSerializer.Serialize(new { message = evt.ErrorMessage }, JsonOpts);
                        break;
                    default:
                        continue;
                }

                await Response.WriteAsync($"event: {eventName}\ndata: {dataJson}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    [HttpGet("conversation/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(string conversationId)
    {
        var messages = await _chatRepo.GetByConversationAsync(conversationId, 100);
        return Ok(messages);
    }
}
