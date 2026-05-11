using OpenChat.API.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace OpenChat.API.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;
    private readonly string _systemPrompt;

    private const string DefaultSystemPrompt =
        "You are a helpful, conversational assistant. " +
        "Reply clearly, accurately, and directly. " +
        "Format your responses using markdown when appropriate: use **bold** for key terms, " +
        "bullet lists for enumerations, and code blocks for code or structured data. " +
        "LANGUAGE RULE (highest priority): Always reply in the exact same language the user writes in. " +
        "This rule overrides everything else.";

    private const string ToolGuidanceAddendum = """


You have access to a fetch_url tool that lets you read web pages from allowed domains to get up-to-date, authoritative information.

WHEN TO USE fetch_url
=====================

ALWAYS use fetch_url (no exceptions) for:

1. **Version, release, or "what's latest" questions** about ANY software, framework, library, language, OS, browser, or tool.

   Examples that MUST trigger fetch_url — no exceptions:
   - "What's the latest version of X?"
   - "When was X released?"
   - "What's new in X version Y?"
   - "Is X still supported?"
   - "What's the current stable version of X?"

   NEVER answer these from memory. Your training data is months or years old and version numbers change constantly.

   Known release pages (use these directly):
   - Angular  → https://angular.dev/reference/releases
   - .NET     → https://learn.microsoft.com/en-us/dotnet/core/releases-and-support
   - Node.js  → https://nodejs.org/en/about/previous-releases
   - React    → https://github.com/facebook/react/releases
   - Vue      → https://github.com/vuejs/core/releases
   - Python   → https://www.python.org/downloads/
   - For any other tech: fetch their official releases or changelog page.

ALSO use fetch_url for:

2. Specific features, APIs, or syntax of any programming language, framework, library, or tool.
3. Anything involving "new", "experimental", "deprecated", "preview", or "recently added".
4. Configuration, installation, or setup details of specific tools.
5. Any topic where you suspect your training knowledge might be outdated.

Do NOT use it for:

1. Greetings, small talk, or conversation that doesn't need facts.
2. General programming concepts (e.g. "what is recursion", "explain polymorphism") that don't depend on a specific framework version.
3. Subjective opinions ("which is better, X or Y").
4. Math, logic, or pure reasoning problems.

HOW TO DISCOVER THE RIGHT URL
=============================

You do NOT know the exact structure of every site. To find the right URL, follow this strategy:

1. Identify the official documentation site for the topic. Common ones:
   - Angular        → angular.dev
   - React          → react.dev
   - Vue            → vuejs.org
   - .NET / C#      → learn.microsoft.com/dotnet
   - TypeScript     → typescriptlang.org
   - JavaScript/Web → developer.mozilla.org
   - Node.js        → nodejs.org
   - Python         → docs.python.org
   - Django         → docs.djangoproject.com
   - MongoDB        → www.mongodb.com or docs.mongodb.com
   - Docker         → docs.docker.com
   - Kubernetes     → kubernetes.io
   - Tailwind CSS   → tailwindcss.com
   - Next.js        → nextjs.org
   - Go             → go.dev
   - Rust           → doc.rust-lang.org
   - npm packages   → docs.npmjs.com
   - General Q&A   → stackoverflow.com
   - Source/repos   → github.com
   - General topics → wikipedia.org

⚠️ IMPORTANT — Domain names that CHANGED (your training is likely outdated):

- Angular docs: use **angular.dev** — NOT angular.io (deprecated since 2023)
- React docs: use **react.dev** — NOT reactjs.org (legacy domain)
- Microsoft docs: use **learn.microsoft.com** — NOT docs.microsoft.com (migrated in 2022)
- Vue docs: use **vuejs.org** (current Vue 3) — NOT vue2.vuejs.org (legacy)

If you have any uncertainty about which domain is current for a topic, start with the known-correct ones above. Never use the old/deprecated domains; they will be blocked by the allowlist.

2. Construct the most likely URL based on the topic. Use lowercase, hyphens for words, and the site's URL conventions.

3. If your fetched URL returns an error (404, blocked, or unrelated content), do NOT give up. Try:
   a. A more general path (e.g. /guide/forms instead of /guide/forms/signals)
   b. A different known doc site for the same topic
   c. Stack Overflow or Wikipedia as fallback
   d. Last resort: answer from general knowledge but say so explicitly

4. For features that are NEW or EXPERIMENTAL, the URL is often deep inside the doc tree. Examples:
   - Angular Signal Forms (experimental, v21) → /guide/forms/signals/overview
   - React Server Components                  → /reference/rsc/server-components
   - .NET pattern matching (C# 12+)           → /dotnet/csharp/fundamentals/functional/pattern-matching

   If your first guess doesn't work, try variations: /guide/X, /docs/X, /api/X, /reference/X.

GOOD vs BAD URLs
================

Good (specific, deep, likely to contain the answer):
  https://angular.dev/reference/releases
  https://angular.dev/guide/signals
  https://react.dev/reference/react/useState
  https://learn.microsoft.com/en-us/dotnet/core/releases-and-support
  https://developer.mozilla.org/en-US/docs/Web/JavaScript/Closures
  https://docs.python.org/3/library/asyncio.html

Bad (too generic, won't have specific info):
  https://angular.dev/
  https://react.dev/learn
  https://docs.python.org/

AFTER FETCHING
==============

When you receive content from fetch_url:

1. Read it carefully BEFORE answering.
2. Base your answer PRIMARILY on the fetched content.
3. Quote or paraphrase specific facts from it.
4. If the fetched content does NOT cover the specific question, say so explicitly: "The documentation I fetched covers X but does not detail Y. Based on general knowledge: ..."
5. Always cite the source URL in your final answer.
6. Never silently mix outdated training knowledge with fetched content. If they contradict, trust the fetched content (it's newer).

ANTI-HALLUCINATION RULE
=======================

NEVER pretend to use fetch_url. If you say "let me check the docs" or "checking the official site", you MUST have actually invoked fetch_url in this turn. If you haven't invoked it, do not narrate as if you did.

If you respond WITHOUT using fetch_url for a version, release, or recency question, you MUST say explicitly at the start of your answer:

"Note: I'm answering from my training knowledge, which may be outdated. For the current version, check [official release URL]."

WHEN A FETCH FAILS
==================

If fetch_url returns an error:

1. **Read the error message carefully.** It usually contains a hint about what went wrong and a suggested fix (e.g. "try angular.dev instead of angular.io").

2. **Try AT LEAST ONE more URL** before giving up. You have multiple tool calls available per turn. Common recovery strategies:
   - Use a different domain (the error message often suggests one)
   - Try a more general path (e.g. /reference/releases instead of /release-notes)
   - Try the home page of the doc site

3. **Only fall back to training knowledge after 2-3 failed attempts.** And when you do, you MUST start your answer with:

   "⚠️ I could not access live documentation. The following is from my training data and may be outdated:"

4. **Never recommend a URL that you yourself could not access.** If angular.io was blocked for you, don't tell the user to visit angular.io either.
""";


    public OllamaService(HttpClient httpClient, IConfiguration config)
    {
        _defaultModel = config["Ollama:Model"] ?? "llama3";
        _systemPrompt = config["Ollama:SystemPrompt"] ?? DefaultSystemPrompt;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config["Ollama:BaseUrl"] ?? "http://localhost:11434");
        _httpClient.Timeout = TimeSpan.FromSeconds(180);
    }

    public async Task<OllamaResult> GenerateResponseAsync(
        string userMessage,
        List<ChatMessage> history,
        string? modelOverride = null,
        CancellationToken ct = default)
    {
        var model = modelOverride ?? _defaultModel;
        var response = await _httpClient.PostAsJsonAsync("/api/chat", new OllamaChatRequest
        {
            Model = model,
            Messages = BuildConversationMessages(userMessage, history),
            Stream = false
        }, ct);
        response.EnsureSuccessStatusCode();

        var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);
        return new OllamaResult
        {
            Response = chatResponse?.Message?.Content?.Trim() ?? "Sorry, I couldn't generate a response.",
            PromptTokens = chatResponse?.PromptEvalCount ?? 0,
            CompletionTokens = chatResponse?.EvalCount ?? 0
        };
    }

    public async IAsyncEnumerable<OllamaChatChunk> StreamResponseAsync(
        string userMessage,
        List<ChatMessage> history,
        string? modelOverride = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var model = modelOverride ?? _defaultModel;
        await foreach (var chunk in StreamChatAsync(BuildConversationMessages(userMessage, history), model, ct))
            yield return chunk;
    }

    public async Task<OllamaToolChatResponse> ChatWithToolsAsync(
        List<OllamaChatMessage> messages,
        string model,
        object[] tools,
        CancellationToken ct = default)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync("/api/chat", new OllamaChatRequest
        {
            Model = model,
            Messages = messages,
            Stream = false,
            Tools = tools.Length > 0 ? tools : null
        }, ct);
        httpResponse.EnsureSuccessStatusCode();

        var raw = await httpResponse.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);

        var toolCalls = raw?.Message?.ToolCalls?
            .Where(tc => tc.Function is not null)
            .Select(tc => new OllamaToolCall
            {
                Id = Guid.NewGuid().ToString(),
                Name = tc.Function!.Name,
                Arguments = tc.Function.Arguments
            })
            .ToList() ?? [];

        return new OllamaToolChatResponse
        {
            HasToolCalls = toolCalls.Count > 0,
            ToolCalls = toolCalls,
            Content = raw?.Message?.Content?.Trim() ?? string.Empty,
            AssistantMessage = raw?.Message ?? new OllamaChatMessage { Role = "assistant", Content = string.Empty },
            PromptTokens = raw?.PromptEvalCount ?? 0,
            CompletionTokens = raw?.EvalCount ?? 0
        };
    }

    public async IAsyncEnumerable<OllamaChatChunk> StreamChatAsync(
        List<OllamaChatMessage> messages,
        string model,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(new OllamaChatRequest
            {
                Model = model,
                Messages = messages,
                Stream = true
            })
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            var chunk = JsonSerializer.Deserialize<OllamaChatChunk>(line);
            if (chunk is not null) yield return chunk;
        }
    }

    public List<OllamaChatMessage> BuildConversationMessages(
        string userMessage,
        List<ChatMessage> history,
        bool withToolGuidance = false)
    {
        var systemContent = withToolGuidance
            ? _systemPrompt + ToolGuidanceAddendum
            : _systemPrompt;

        var messages = new List<OllamaChatMessage>
        {
            new() { Role = "system", Content = systemContent }
        };

        foreach (var msg in history.TakeLast(20))
        {
            messages.Add(new OllamaChatMessage
            {
                Role = msg.Role == "user" ? "user" : "assistant",
                Content = msg.Content
            });
        }

        messages.Add(new OllamaChatMessage { Role = "user", Content = userMessage });
        return messages;
    }
}
