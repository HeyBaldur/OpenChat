# ════════════════════════════════════════════════════════════════════
#  OpenChatAi — Manual Content Inspection
# ════════════════════════════════════════════════════════════════════
#
#  Prompts for your JWT token securely (input is hidden), fetches
#  angular.dev/guide/signals through the fetch_url tool, saves the
#  extracted markdown to a file and opens it in Notepad for review.
#
#  USAGE:
#    .\inspect-content.ps1
#
#  Optional flags:
#    -Url       Override the URL to fetch (defaults to Angular signals)
#    -OutFile   Override the output filename
#    -BaseUrl   Override the API base URL
#
#  TIP: get your token from the browser ->
#       F12 -> Application -> Local Storage -> openchat-auth-token
# ════════════════════════════════════════════════════════════════════

param(
    [string]$Url     = "https://angular.dev/guide/signals",
    [string]$OutFile = "angular-signals-extracted.md",
    [string]$BaseUrl = "http://localhost:5124"
)


Write-Host ""
Write-Host "============================================================"
Write-Host " OpenChatAi - Manual Content Inspection"
Write-Host "============================================================"
Write-Host ""
Write-Host "Target URL : $Url"
Write-Host "Output     : $OutFile"
Write-Host "API        : $BaseUrl"
Write-Host ""


# ─── Prompt for the JWT token (input hidden) ────────────────────────
Write-Host "Paste your JWT token (input will be hidden):" -ForegroundColor Yellow
$secureToken = Read-Host -AsSecureString

# Convert SecureString back to plain text just for the HTTP header
$bstr  = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
$token = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host ""
    Write-Host "  ERROR: empty token. Aborting." -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "Token received ($($token.Length) chars). Fetching..." -ForegroundColor DarkGray
Write-Host ""


# ─── Build and send the request ─────────────────────────────────────
$bodyObj = @{
    tool      = "fetch_url"
    arguments = @{ url = $Url }
}
$body = $bodyObj | ConvertTo-Json -Compress

$headers = @{
    Authorization  = "Bearer $token"
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod `
        -Uri "$BaseUrl/api/tools/test/execute" `
        -Method POST `
        -Headers $headers `
        -Body $body `
        -ErrorAction Stop
} catch {
    Write-Host "  ERROR calling API: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "  HTTP Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
    Write-Host ""
    exit 1
}


# ─── Report ─────────────────────────────────────────────────────────
Write-Host "------------------------------------------------------------"
Write-Host " Response summary"
Write-Host "------------------------------------------------------------"
Write-Host "  success     : $($response.success)"
Write-Host "  sourceUrl   : $($response.sourceUrl)"
if ($response.errorReason) {
    Write-Host "  errorReason : $($response.errorReason)" -ForegroundColor Yellow
}
if ($response.content) {
    Write-Host "  content     : $($response.content.Length) chars"
}
Write-Host ""

if (-not $response.success) {
    Write-Host "  Fetch failed. Nothing to save." -ForegroundColor Red
    Write-Host ""
    exit 1
}

if ([string]::IsNullOrWhiteSpace($response.content)) {
    Write-Host "  Empty content received. Nothing to save." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}


# ─── Save and open ──────────────────────────────────────────────────
$response.content | Out-File -FilePath $OutFile -Encoding UTF8

$fullPath = (Resolve-Path $OutFile).Path
Write-Host "  Saved to    : $fullPath" -ForegroundColor Green
Write-Host ""

# Show first 40 lines in console (handy for quickly pasting back)
Write-Host "------------------------------------------------------------"
Write-Host " First 40 lines of extracted content"
Write-Host "------------------------------------------------------------"
$response.content -split "`r?`n" | Select-Object -First 40 | ForEach-Object {
    Write-Host $_
}
Write-Host ""
Write-Host "------------------------------------------------------------"
Write-Host ""

Write-Host "Opening in Notepad..." -ForegroundColor DarkGray
Start-Process notepad.exe $fullPath

# Clean up token from memory (defensive)
$token = $null
[System.GC]::Collect()
