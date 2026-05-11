using Microsoft.Extensions.Configuration;
using OpenChat.Application.Constants;
using OpenChat.Application.Interfaces.External;
using OpenChat.Application.Interfaces.Services;
using OpenChat.Application.Models;
using OpenChat.Domain.Dtos;
using OpenChat.Domain.Entities;
using OpenChat.Domain.Interfaces.Repositories;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenChat.Application.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepo;
    private readonly IConversationRepository _convRepo;
    private readonly ILogRepository _logRepo;
    private readonly IOllamaService _ollama;
    private readonly IAgenticChatService _agenticService;
    private readonly IConfiguration _config;

    public ChatService(
        IChatRepository chatRepo,
        IConversationRepository convRepo,
        ILogRepository logRepo,
        IOllamaService ollama,
        IAgenticChatService agenticService,
        IConfiguration config)
    {
        _chatRepo = chatRepo;
        _convRepo = convRepo;
        _logRepo = logRepo;
        _ollama = ollama;
        _agenticService = agenticService;
        _config = config;
    }

    public async Task<ChatResponse> ProcessMessageAsync(ChatRequest request)
    {
        var (conversation, effectiveModel) = await ResolveConversationAsync(request);
        var history = await _chatRepo.GetByConversationAsync(conversation.Id!);

        await _chatRepo.AddMessageAsync(new ChatMessage
        {
            UserId = request.UserId,
            ConversationId = conversation.Id!,
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        });

        var result = await _ollama.GenerateResponseAsync(request.Message, history, effectiveModel);

        var assistantMessage = new ChatMessage
        {
            UserId = request.UserId,
            ConversationId = conversation.Id!,
            Role = "assistant",
            Content = result.Response,
            Timestamp = DateTime.UtcNow
        };
        await _chatRepo.AddMessageAsync(assistantMessage);
        await PersistTokensAsync(conversation, request.UserId, result.PromptTokens, result.CompletionTokens, effectiveModel);

        return new ChatResponse
        {
            ConversationId = conversation.Id!,
            ConversationTitle = conversation.Title,
            Reply = result.Response,
            Timestamp = assistantMessage.Timestamp,
            TokensUsed = result.TotalTokens
        };
    }

    public async IAsyncEnumerable<AgenticStreamEvent> StreamMessageAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var (conversation, effectiveModel) = await ResolveConversationAsync(request);
        var history = await _chatRepo.GetByConversationAsync(conversation.Id!);

        await _chatRepo.AddMessageAsync(new ChatMessage
        {
            UserId = request.UserId,
            ConversationId = conversation.Id!,
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        });

        var fullText = new StringBuilder();
        int promptTokens = 0;
        int completionTokens = 0;
        var toolCallsUsed = new List<ToolCallRecord>();
        bool stopped = false;

        bool supportsTools = !string.IsNullOrEmpty(effectiveModel)
            && ModelCapabilities.SupportsToolCalling(effectiveModel);

        if (supportsTools)
        {
            var messages = _ollama.BuildConversationMessages(request.Message, history, withToolGuidance: true);

            await foreach (var evt in _agenticService.StreamWithToolsAsync(messages, effectiveModel!, request.UserId, ct))
            {
                if (evt.Type == AgenticEventType.Token)
                    fullText.Append(evt.TokenText ?? string.Empty);

                if (evt.Type == AgenticEventType.Done)
                {
                    promptTokens = evt.PromptTokens ?? 0;
                    completionTokens = evt.CompletionTokens ?? 0;
                    toolCallsUsed = evt.ToolCallsUsed ?? [];

                    yield return AgenticStreamEvent.Done(
                        promptTokens, completionTokens, toolCallsUsed,
                        conversation.Id!, conversation.Title);
                }
                else
                {
                    yield return evt;
                }
            }

            stopped = ct.IsCancellationRequested;
        }
        else
        {
            await foreach (var chunk in _ollama.StreamResponseAsync(request.Message, history, effectiveModel, ct))
            {
                if (ct.IsCancellationRequested) break;

                if (!chunk.Done)
                {
                    var text = chunk.Message?.Content ?? string.Empty;
                    if (text.Length > 0)
                    {
                        fullText.Append(text);
                        yield return AgenticStreamEvent.Token(text);
                    }
                }
                else
                {
                    promptTokens = chunk.PromptEvalCount;
                    completionTokens = chunk.EvalCount;
                }
            }

            stopped = ct.IsCancellationRequested;
            yield return AgenticStreamEvent.Done(
                promptTokens, completionTokens, [],
                conversation.Id!, conversation.Title);
        }

        var reply = fullText.ToString().Trim();
        if (string.IsNullOrEmpty(reply)) yield break;

        await _chatRepo.AddMessageAsync(new ChatMessage
        {
            UserId = request.UserId,
            ConversationId = conversation.Id!,
            Role = "assistant",
            Content = reply,
            Timestamp = DateTime.UtcNow,
            Stopped = stopped,
            ToolCallsUsed = toolCallsUsed.Count > 0 ? toolCallsUsed : null
        });

        await PersistTokensAsync(conversation, request.UserId, promptTokens, completionTokens, effectiveModel);
    }

    private async Task<(Conversation conversation, string? effectiveModel)> ResolveConversationAsync(ChatRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ConversationId))
        {
            var existing = await _convRepo.GetByIdAsync(request.ConversationId)
                ?? throw new InvalidOperationException("Conversation not found.");

            var effectiveModel = request.Model ?? existing.Model;

            if (!string.IsNullOrEmpty(request.Model) && request.Model != existing.Model)
                await _convRepo.UpdateModelAsync(existing.Id!, request.Model);

            return (existing, effectiveModel);
        }

        var title = request.Message.Length > 50
            ? request.Message[..50].TrimEnd() + "…"
            : request.Message;

        var created = await _convRepo.CreateAsync(request.UserId, title, request.Model);
        return (created, request.Model);
    }

    private async Task PersistTokensAsync(
        Conversation conversation, string userId,
        int prompt, int completion, string? effectiveModel)
    {
        int total = prompt + completion;
        await _logRepo.AddAsync(new Log
        {
            ConversationId = conversation.Id!,
            UserId = userId,
            Model = effectiveModel ?? _config["Ollama:Model"] ?? "llama3",
            PromptTokens = prompt,
            CompletionTokens = completion,
            TotalTokens = total,
            Timestamp = DateTime.UtcNow
        });
        await _convRepo.AddTokensAsync(conversation.Id!, total);
    }
}
