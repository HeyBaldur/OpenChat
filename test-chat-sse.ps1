# ════════════════════════════════════════════════════════════════════
#  OpenChatAi — Chat SSE Live Test
# ════════════════════════════════════════════════════════════════════
#
#  Connects to /Chat/stream and prints every SSE event as it arrives.
#  Shows the agentic loop in action: tool_start, tool_end, token, done.
#
#  USAGE:
#    .\test-chat-sse.ps1
#    .\test-chat-sse.ps1 -Message "your question"
#    .\test-chat-sse.ps1 -Model "llama3:latest"
#    .\test-chat-sse.ps1 -ConversationId "<existing-id>"  (optional)
# ════════════════════════════════════════════════════════════════════

param(
    [string]$Message        = "How do I use Angular signals? Give me a code example.",
    [string]$Model          = "qwen2.5:7b",
    [string]$ConversationId = $null,
    [string]$BaseUrl        = "http://localhost:5124",
    [string]$Endpoint       = "/Chat/stream"
)


function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "------------------------------------------------------------"
    Write-Host " $Text"
    Write-Host "------------------------------------------------------------"
}


function Get-UserIdFromJwt {
    param([string]$Jwt)

    try {
        $parts = $Jwt.Split(".")
        if ($parts.Count -lt 2) { return $null }

        $payload = $parts[1]
        # Pad base64url to base64
        switch ($payload.Length % 4) {
            2 { $payload += "==" }
            3 { $payload += "="  }
        }
        $payload = $payload.Replace("-", "+").Replace("_", "/")

        $bytes = [Convert]::FromBase64String($payload)
        $json  = [System.Text.Encoding]::UTF8.GetString($bytes)
        $obj   = $json | ConvertFrom-Json

        # Common claim names for user id
        $candidates = @(
            "nameid",
            "sub",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )
        foreach ($claim in $candidates) {
            if ($obj.PSObject.Properties.Name -contains $claim) {
                return $obj.$claim
            }
        }
        return $null
    } catch {
        return $null
    }
}


# ════════════════════════════════════════════════════════════════════
# 1. Banner
# ════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "============================================================"
Write-Host " OpenChatAi - Chat SSE Live Test"
Write-Host "============================================================"
Write-Host ""
Write-Host "Message  : $Message"
Write-Host "Model    : $Model"
if ($ConversationId) {
    Write-Host "ConvId   : $ConversationId"
} else {
    Write-Host "ConvId   : (new conversation)"
}
Write-Host "Endpoint : $BaseUrl$Endpoint"
Write-Host ""


# ════════════════════════════════════════════════════════════════════
# 2. Token prompt + UserId extraction
# ════════════════════════════════════════════════════════════════════
Write-Host "Paste your JWT token (input will be hidden):" -ForegroundColor Yellow
$secureToken = Read-Host -AsSecureString
$bstr  = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
$token = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host ""
    Write-Host "  ERROR: empty token. Aborting." -ForegroundColor Red
    Write-Host ""
    exit 1
}

$userId = Get-UserIdFromJwt -Jwt $token
if ([string]::IsNullOrWhiteSpace($userId)) {
    Write-Host ""
    Write-Host "  ERROR: could not extract userId from JWT." -ForegroundColor Red
    Write-Host "  Token format may be unexpected. Pass -UserId manually if needed." -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "  Detected UserId: $userId" -ForegroundColor DarkGray


# ════════════════════════════════════════════════════════════════════
# 3. Build request body
# ════════════════════════════════════════════════════════════════════
$bodyObj = @{
    userId  = $userId
    message = $Message
    model   = $Model
}
if ($ConversationId) {
    $bodyObj.conversationId = $ConversationId
}
$body = $bodyObj | ConvertTo-Json -Compress


# ════════════════════════════════════════════════════════════════════
# 4. Setup HttpClient for streaming
# ════════════════════════════════════════════════════════════════════
Add-Type -AssemblyName System.Net.Http

$client   = $null
$response = $null
$reader   = $null

$stats = @{
    ToolStart = 0
    ToolEnd   = 0
    Tokens    = 0
    Done      = 0
    Error     = 0
}

try {
    $client = New-Object System.Net.Http.HttpClient
    $client.Timeout = [System.Threading.Timeout]::InfiniteTimeSpan
    $client.DefaultRequestHeaders.Authorization = `
        New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
    $client.DefaultRequestHeaders.Accept.Add(
        (New-Object System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"))
    )

    $request = New-Object System.Net.Http.HttpRequestMessage(
        [System.Net.Http.HttpMethod]::Post,
        "$BaseUrl$Endpoint"
    )
    $request.Content = New-Object System.Net.Http.StringContent(
        $body,
        [System.Text.Encoding]::UTF8,
        "application/json"
    )

    Write-Host ""
    Write-Host "Opening SSE stream..." -ForegroundColor DarkGray

    $response = $client.SendAsync(
        $request,
        [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead
    ).GetAwaiter().GetResult()

    if (-not $response.IsSuccessStatusCode) {
        Write-Host ""
        Write-Host "  HTTP $($response.StatusCode) - $($response.ReasonPhrase)" -ForegroundColor Red
        $errBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Write-Host "  Response body:" -ForegroundColor Red
        Write-Host $errBody -ForegroundColor DarkRed
        exit 1
    }

    $stream = $response.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
    $reader = New-Object System.IO.StreamReader($stream)

    Write-Header "SSE EVENTS (live)"

    $currentEvent = $null
    $inTokenStream = $false

    while (-not $reader.EndOfStream) {
        $line = $reader.ReadLine()

        # Blank line terminates an SSE message block
        if ([string]::IsNullOrEmpty($line)) {
            $currentEvent = $null
            continue
        }

        if ($line.StartsWith("event:")) {
            $currentEvent = $line.Substring(6).Trim()
            continue
        }

        if ($line.StartsWith("data:")) {
            $data = $line.Substring(5).Trim()

            if ($currentEvent -eq "tool_start") {
                if ($inTokenStream) {
                    Write-Host ""
                    $inTokenStream = $false
                }
                Write-Host ""
                Write-Host "  >>> TOOL_START" -ForegroundColor Cyan
                Write-Host "      $data" -ForegroundColor DarkCyan
                $stats.ToolStart++
            }
            elseif ($currentEvent -eq "tool_end") {
                Write-Host "  <<< TOOL_END" -ForegroundColor Green
                Write-Host "      $data" -ForegroundColor DarkGreen
                Write-Host ""
                $stats.ToolEnd++
            }
            elseif ($currentEvent -eq "token") {
                $stats.Tokens++
                $inTokenStream = $true
                $tokenText = $data
                try {
                    $parsed = $data | ConvertFrom-Json -ErrorAction Stop
                    if ($parsed -is [string]) { $tokenText = $parsed }
                } catch {
                    # leave as raw
                }
                Write-Host -NoNewline $tokenText -ForegroundColor White
            }
            elseif ($currentEvent -eq "done") {
                if ($inTokenStream) {
                    Write-Host ""
                    $inTokenStream = $false
                }
                Write-Host ""
                Write-Host "  === DONE" -ForegroundColor Magenta
                Write-Host "      $data" -ForegroundColor DarkMagenta
                $stats.Done++
            }
            elseif ($currentEvent -eq "error") {
                if ($inTokenStream) {
                    Write-Host ""
                    $inTokenStream = $false
                }
                Write-Host ""
                Write-Host "  !!! ERROR" -ForegroundColor Red
                Write-Host "      $data" -ForegroundColor DarkRed
                $stats.Error++
            }
            else {
                # Unknown event — print raw in gray
                Write-Host -NoNewline "$data " -ForegroundColor DarkGray
            }
        }
    }

    if ($inTokenStream) { Write-Host "" }
}
catch {
    Write-Host ""
    Write-Host "  EXCEPTION: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "  Inner: $($_.Exception.InnerException.Message)" -ForegroundColor DarkRed
    }
}
finally {
    if ($reader)   { $reader.Dispose() }
    if ($response) { $response.Dispose() }
    if ($client)   { $client.Dispose() }
    $token = $null
    [System.GC]::Collect()
}


# ════════════════════════════════════════════════════════════════════
# 5. Summary
# ════════════════════════════════════════════════════════════════════
Write-Header "EVENT SUMMARY"
Write-Host ("  tool_start  : {0}" -f $stats.ToolStart) -ForegroundColor Cyan
Write-Host ("  tool_end    : {0}" -f $stats.ToolEnd)   -ForegroundColor Green
Write-Host ("  token       : {0}" -f $stats.Tokens)    -ForegroundColor White
Write-Host ("  done        : {0}" -f $stats.Done)      -ForegroundColor Magenta
Write-Host ("  error       : {0}" -f $stats.Error)     -ForegroundColor Red
Write-Host ""

if ($stats.Error -gt 0) {
    Write-Host "  WARN: stream contained error events." -ForegroundColor Yellow
}
if ($stats.Done -eq 0) {
    Write-Host "  WARN: no 'done' event received; stream may have closed early." -ForegroundColor Yellow
}
if ($stats.ToolStart -ne $stats.ToolEnd) {
    Write-Host "  WARN: tool_start count ($($stats.ToolStart)) != tool_end count ($($stats.ToolEnd))" -ForegroundColor Yellow
}

Write-Host ""