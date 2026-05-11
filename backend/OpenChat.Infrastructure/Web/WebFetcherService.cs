using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using OpenChat.Ai.Interfaces;
using OpenChat.Ai.Models;
using OpenChat.Application.Interfaces.Services;
using OpenChat.Domain.Entities;
using OpenChat.Domain.Interfaces.Repositories;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenChat.Infrastructure.Web;

public class WebFetcherService : IWebFetcherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAllowlistService _allowlistService;
    private readonly ILogRepository _logRepository;
    private readonly ILogger<WebFetcherService> _logger;

    private static readonly string[] ReservedHostSuffixes = [".local", ".internal", ".localdomain"];

    public WebFetcherService(
        IHttpClientFactory httpClientFactory,
        IAllowlistService allowlistService,
        ILogRepository logRepository,
        ILogger<WebFetcherService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _allowlistService = allowlistService;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> FetchAndExtractAsync(string url, string userId, CancellationToken ct)
    {
        // a) Parse URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("WebFetcher: invalid URL {Url}", url);
            return ToolExecutionResult.Error("invalid_url", $"'{url}' is not a valid URL.");
        }

        // b) Scheme guard
        if (uri.Scheme is not "http" and not "https")
        {
            _logger.LogWarning("WebFetcher: disallowed scheme '{Scheme}' in {Url}", uri.Scheme, url);
            return ToolExecutionResult.Error("invalid_scheme", $"Scheme '{uri.Scheme}' is not allowed. Only http and https are supported.");
        }

        // c) SSRF guard — reserved hostnames and private IP ranges
        var ssrfReason = await CheckSsrfAsync(uri.Host);
        if (ssrfReason is not null)
        {
            _logger.LogWarning("WebFetcher: SSRF blocked for host '{Host}' ({Reason})", uri.Host, ssrfReason);
            return ToolExecutionResult.Error("internal_address", "Cannot fetch internal addresses.");
        }

        // d) Allowlist check
        bool allowed;
        try
        {
            allowed = await _allowlistService.IsDomainAllowedAsync(url, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebFetcher: allowlist check threw for {Url}", url);
            return ToolExecutionResult.Error("allowlist_error", "Could not verify domain allowlist. Please try again.");
        }

        if (!allowed)
        {
            _logger.LogWarning("WebFetcher: domain blocked — {Url}", url);
            await WriteLogAsync(url, "domain_blocked");
            return ToolExecutionResult.Error("domain_blocked", "Domain not in allowlist. Use only allowed sources.");
        }

        // e) HTTP request — headers read first, then stream body
        var client = _httpClientFactory.CreateClient("WebFetcher");

        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        requestCts.CancelAfter(TimeSpan.FromSeconds(10));

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, requestCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("WebFetcher: timeout on headers for {Url}", url);
            await WriteLogAsync(url, "timeout");
            return ToolExecutionResult.Error("timeout", "The request timed out after 10 seconds.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebFetcher: network error fetching {Url}", url);
            return ToolExecutionResult.Error("fetch_error", $"Failed to reach the page: {ex.Message}");
        }

        using (response)
        {
            // Content-Type check before reading the body
            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!IsTextContentType(contentType))
            {
                _logger.LogWarning("WebFetcher: unsupported content-type '{ContentType}' for {Url}", contentType, url);
                return ToolExecutionResult.Error("not_text", $"Content type '{contentType}' is not supported. Only HTML and plain-text pages are supported.");
            }

            // f) Status check
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                _logger.LogWarning("WebFetcher: HTTP {Status} for {Url}", status, url);
                await WriteLogAsync(url, $"http_{status}");
                return ToolExecutionResult.Error($"http_{status}", $"Page returned HTTP {status}.");
            }

            // Stream body with 2 MB cap
            string? rawHtml;
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync(requestCts.Token);
                rawHtml = await ReadWithSizeLimitAsync(stream, maxBytes: 2 * 1024 * 1024, requestCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("WebFetcher: timeout reading body for {Url}", url);
                await WriteLogAsync(url, "timeout");
                return ToolExecutionResult.Error("timeout", "The request timed out after 10 seconds.");
            }

            if (rawHtml is null)
            {
                _logger.LogWarning("WebFetcher: response exceeded 2 MB for {Url}", url);
                await WriteLogAsync(url, "too_large");
                return ToolExecutionResult.Error("too_large", "The page content exceeds the 2 MB limit.");
            }

            // g) Parse and clean HTML
            var cleanedHtml = await ExtractMainContentAsync(rawHtml);

            // h) HTML → Markdown
            var mdConfig = new ReverseMarkdown.Config
            {
                GithubFlavored     = true,
                UnknownTags        = ReverseMarkdown.Config.UnknownTagsOption.Bypass,
                SmartHrefHandling  = true,
                RemoveComments     = true
            };
            var converter = new ReverseMarkdown.Converter(mdConfig);
            var markdown = converter.Convert(cleanedHtml);

            // i) Strip invisible/dangerous Unicode characters + post-process
            markdown = StripInvisibleCharacters(markdown);
            markdown = PostProcessMarkdown(markdown);

            // j) Truncate to 8000 chars
            if (markdown.Length > 8000)
                markdown = markdown[..8000] + "\n\n[Content truncated at 8000 characters. If the answer is not above, the truncation may have hidden it. Consider fetching a more specific URL or trying the page's main URL.]";

            var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
            _logger.LogInformation("WebFetcher: success {Url} → {Chars} chars", finalUrl, markdown.Length);
            await WriteLogAsync(finalUrl, "success");

            return ToolExecutionResult.Ok(markdown, finalUrl);
        }
    }

    private static async Task<string?> ReadWithSizeLimitAsync(Stream stream, int maxBytes, CancellationToken ct)
    {
        var buffer = new byte[8192];
        var sb = new StringBuilder();
        int totalBytes = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            totalBytes += read;
            if (totalBytes > maxBytes) return null;
            sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
        }

        return sb.ToString();
    }

    private static async Task<string> ExtractMainContentAsync(string html)
    {
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html);

        // STEP 1 — Remove non-content elements wholesale
        var noiseSelectors = new[]
        {
            "script", "style", "iframe", "object", "embed", "noscript",
            "svg", "picture", "video", "audio",
            "nav", "aside", "footer",
            "form", "button",
            "[aria-hidden='true']"
        };
        foreach (var sel in noiseSelectors)
        {
            foreach (var el in document.QuerySelectorAll(sel).ToList())
                el.Remove();
        }

        // Remove global <header> elements but keep those inside <article> or <main>
        foreach (var el in document.QuerySelectorAll("header").ToList())
        {
            if (!HasAncestor(el, "article") && !HasAncestor(el, "main"))
                el.Remove();
        }

        // Remove elements hidden via inline style
        foreach (var el in document.QuerySelectorAll("[style]").ToList())
        {
            var style = el.GetAttribute("style") ?? string.Empty;
            if (style.Contains("display:none", StringComparison.OrdinalIgnoreCase) ||
                style.Contains("display: none", StringComparison.OrdinalIgnoreCase) ||
                style.Contains("visibility:hidden", StringComparison.OrdinalIgnoreCase) ||
                style.Contains("visibility: hidden", StringComparison.OrdinalIgnoreCase))
            {
                el.Remove();
            }
        }

        // STEP 2 — Unwrap custom elements (tag name contains "-"): keep children, drop wrapper
        foreach (var el in document.QuerySelectorAll("*").ToList())
        {
            if (!el.TagName.Contains('-') || el.ParentElement is null) continue;

            var parent = el.ParentElement;
            var children = el.ChildNodes.ToList();
            foreach (var child in children)
                parent.InsertBefore(child, el);
            el.Remove();
        }

        // STEP 3 — Remove HTML comments
        RemoveComments(document.DocumentElement!);

        // STEP 4 — Strip framework/decoration attributes from all remaining elements
        foreach (var el in document.QuerySelectorAll("*").ToList())
            CleanAttributes(el);

        // STEP 5 — Extract main content; prefer known doc-content containers, fall back to body
        var specificContainerSelectors = new[]
        {
            "#content", "#main-content", "#doc-content", "#docs-content",
            ".docs-content", ".content-area", ".doc-content", ".main-content"
        };

        foreach (var sel in specificContainerSelectors)
        {
            var candidate = document.QuerySelector(sel);
            if (candidate is not null && candidate.TextContent.Trim().Length >= 500)
                return candidate.OuterHtml;
        }

        var main = document.QuerySelector("main")
                ?? document.QuerySelector("article")
                ?? document.QuerySelector("[role='main']");

        if (main is not null && main.TextContent.Trim().Length >= 200)
            return main.OuterHtml;

        var body = document.Body;
        return body?.InnerHtml ?? html;
    }

    private static bool HasAncestor(IElement el, string ancestorTag)
    {
        var parent = el.ParentElement;
        while (parent is not null)
        {
            if (parent.TagName.Equals(ancestorTag, StringComparison.OrdinalIgnoreCase))
                return true;
            parent = parent.ParentElement;
        }
        return false;
    }

    private static void CleanAttributes(IElement el)
    {
        var tag = el.TagName.ToLowerInvariant();
        var attrNames = el.Attributes.Select(a => a.Name).ToList();

        foreach (var name in attrNames)
        {
            if (ShouldKeepAttribute(name, tag)) continue;
            el.RemoveAttribute(name);
        }
    }

    private static bool ShouldKeepAttribute(string name, string tag)
    {
        var n = name.ToLowerInvariant();
        return (tag is "a" && n is "href" or "title")
            || (tag is "img" && n is "src" or "alt")
            || (tag is "code" or "pre" && n is "lang");
    }

    private static string PostProcessMarkdown(string markdown)
    {
        // Collapse 3+ consecutive blank lines to 2
        markdown = Regex.Replace(markdown, @"\n{3,}", "\n\n");

        // Trim trailing whitespace per line
        markdown = string.Join('\n', markdown.Split('\n').Select(l => l.TrimEnd()));

        // Remove any residual HTML tags ReverseMarkdown left behind
        markdown = Regex.Replace(markdown, @"</?[a-zA-Z][^>]*>", string.Empty);

        return markdown.Trim();
    }

    private static void RemoveComments(INode node)
    {
        for (int i = node.ChildNodes.Length - 1; i >= 0; i--)
        {
            var child = node.ChildNodes[i];
            if (child is IComment comment)
                comment.Remove();
            else
                RemoveComments(child);
        }
    }

    private static string StripInvisibleCharacters(string text) =>
        Regex.Replace(text, @"[​-‏﻿⁠]", string.Empty);

    private static bool IsTextContentType(string contentType) =>
        contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(contentType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

    private async Task<string?> CheckSsrfAsync(string host)
    {
        var hostLower = host.ToLowerInvariant();

        if (hostLower == "localhost") return "reserved hostname";

        foreach (var suffix in ReservedHostSuffixes)
        {
            if (hostLower.EndsWith(suffix, StringComparison.Ordinal))
                return $"reserved hostname suffix '{suffix}'";
        }

        // Direct IP literal
        if (IPAddress.TryParse(host, out var literalIp))
            return IsPrivateIp(literalIp) ? $"private IP {literalIp}" : null;

        // DNS resolution check
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host);
            foreach (var addr in addresses)
            {
                if (IsPrivateIp(addr)) return $"resolved to private IP {addr}";
            }
        }
        catch
        {
            // DNS failure is not an SSRF issue — let HttpClient surface the error naturally
        }

        return null;
    }

    private static bool IsPrivateIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var b6 = ip.GetAddressBytes();
            // fe80::/10 link-local
            return b6[0] == 0xfe && (b6[1] & 0xc0) == 0x80;
        }

        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 169 && b[1] == 254)
            || b[0] == 127;
    }

    private async Task WriteLogAsync(string url, string outcome)
    {
        try
        {
            await _logRepository.AddAsync(new Log
            {
                ConversationId = url,
                UserId = "system",
                Model = "webfetcher",
                PromptTokens = 0,
                CompletionTokens = 0,
                TotalTokens = 0,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebFetcher: failed to write audit log for {Url} ({Outcome})", url, outcome);
        }
    }
}
