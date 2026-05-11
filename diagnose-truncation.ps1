# ════════════════════════════════════════════════════════════════════
#  OpenChatAi — Truncation Diagnostic
# ════════════════════════════════════════════════════════════════════
#
#  Verifies whether the WebFetcher is truncating important content
#  before it reaches the LLM. Specifically tests the Angular releases
#  page (which contains version info deep in the body).
#
#  USAGE:
#    .\diagnose-truncation.ps1
#    .\diagnose-truncation.ps1 -Url "https://example.com/page" -Keyword "something"
# ════════════════════════════════════════════════════════════════════

param(
    [string]$Url     = "https://angular.dev/reference/releases",
    [string]$BaseUrl = "http://localhost:5124"
)


Write-Host ""
Write-Host "============================================================"
Write-Host " OpenChatAi - Truncation Diagnostic"
Write-Host "============================================================"
Write-Host ""
Write-Host "Target URL: $Url"
Write-Host ""


# ─── Prompt for token (hidden input) ────────────────────────────────
Write-Host "Paste your JWT token (input will be hidden):" -ForegroundColor Yellow
$secureToken = Read-Host -AsSecureString
$bstr  = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
$token = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host ""
    Write-Host "  ERROR: empty token." -ForegroundColor Red
    Write-Host ""
    exit 1
}


# ─── Build request body ─────────────────────────────────────────────
$bodyObj = @{
    tool      = "fetch_url"
    arguments = @{ url = $Url }
}
$body = $bodyObj | ConvertTo-Json -Compress


# ─── Call the API ───────────────────────────────────────────────────
Write-Host ""
Write-Host "Fetching..." -ForegroundColor DarkGray
Write-Host ""

try {
    $r = Invoke-RestMethod `
        -Uri "$BaseUrl/api/tools/test/execute" `
        -Method POST `
        -Headers @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" } `
        -Body $body `
        -ErrorAction Stop
} catch {
    Write-Host "  ERROR calling API: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "  HTTP Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
    exit 1
}


# ─── Validate response ──────────────────────────────────────────────
if (-not $r.success) {
    Write-Host "  Fetch failed." -ForegroundColor Red
    Write-Host "  Reason: $($r.errorReason)"
    Write-Host "  Content: $($r.content)"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($r.content)) {
    Write-Host "  Empty content received." -ForegroundColor Yellow
    exit 1
}


# ─── Analysis ───────────────────────────────────────────────────────
Write-Host "============================================================"
Write-Host " ANALYSIS"
Write-Host "============================================================"
Write-Host ""

# 1. Size of extracted content
Write-Host "[1] Content size delivered to LLM:" -ForegroundColor Cyan
Write-Host "    $($r.content.Length) chars"
$wasTruncated = $r.content -match "\[Content truncated"
if ($wasTruncated) {
    Write-Host "    TRUNCATED at extractor limit" -ForegroundColor Yellow
} else {
    Write-Host "    Not truncated by extractor (under the limit)" -ForegroundColor Green
}
Write-Host ""

# 2. Search for key markers
$markers = @(
    @{ Pattern = "Actively supported";    Description = "Version table heading" }
    @{ Pattern = "21\.0\.0|v21";           Description = "Angular 21 version mention" }
    @{ Pattern = "20\.0\.0|v20";           Description = "Angular 20 version mention" }
    @{ Pattern = "19\.0\.0|v19";           Description = "Angular 19 version mention" }
    @{ Pattern = "Support window";         Description = "Support window heading" }
    @{ Pattern = "major\.minor\.patch";    Description = "Versioning explanation (early content)" }
    @{ Pattern = "Versioning and releases";Description = "Page title / breadcrumb" }
)

Write-Host "[2] Key content markers:" -ForegroundColor Cyan
foreach ($m in $markers) {
    $found = $r.content -match $m.Pattern
    if ($found) {
        Write-Host ("    [OK]  {0,-45} ({1})" -f $m.Description, $m.Pattern) -ForegroundColor Green
    } else {
        Write-Host ("    [NO]  {0,-45} ({1})" -f $m.Description, $m.Pattern) -ForegroundColor Red
    }
}
Write-Host ""

# 3. Last 500 chars (where the cut happens)
Write-Host "[3] Last 500 chars (where the extraction ends):" -ForegroundColor Cyan
Write-Host "    " + ("-" * 56)
$tailStart = [Math]::Max(0, $r.content.Length - 500)
$tail = $r.content.Substring($tailStart)
$tail -split "`r?`n" | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
Write-Host "    " + ("-" * 56)
Write-Host ""

# 4. Verdict
Write-Host "============================================================"
Write-Host " VERDICT"
Write-Host "============================================================"
Write-Host ""

$hasTableHeader  = $r.content -match "Actively supported"
$hasCurrentVer   = $r.content -match "21\.0\.0|v21"
$hasEarlyContent = $r.content -match "major\.minor\.patch"

if ($hasCurrentVer -and $hasTableHeader) {
    Write-Host "  Content IS reaching the model with the current version info." -ForegroundColor Green
    Write-Host "  The truncation is NOT the problem."
    Write-Host "  The model is choosing to ignore the table and use its memory."
    Write-Host ""
    Write-Host "  Next step: improve prompt hardening or the model's reasoning."
}
elseif ($hasEarlyContent -and -not $hasTableHeader) {
    Write-Host "  CONFIRMED: truncation IS the problem." -ForegroundColor Red
    Write-Host ""
    Write-Host "  The page header content (versioning explanation) was extracted,"
    Write-Host "  but the 'Actively supported versions' table is missing."
    Write-Host "  This means the truncation happens BEFORE the table."
    Write-Host ""
    Write-Host "  The model only sees the page's introduction, not the version table."
    Write-Host "  It fills in the gap with outdated training knowledge."
    Write-Host ""
    Write-Host "  Fix needed: increase truncation limit OR smarter extraction."
}
elseif (-not $hasEarlyContent -and -not $hasTableHeader) {
    Write-Host "  WEIRD: neither early nor late content is present." -ForegroundColor Yellow
    Write-Host "  The extractor may be removing too much content."
    Write-Host "  Inspect the full output manually."
}
else {
    Write-Host "  Partial / unexpected state. Review the markers above." -ForegroundColor Yellow
}

Write-Host ""

# 5. Optional: save full content to file for manual inspection
$outFile = "angular-releases-extracted.md"
$r.content | Out-File -FilePath $outFile -Encoding UTF8
Write-Host "  Full extracted content saved to: $((Resolve-Path $outFile).Path)" -ForegroundColor DarkGray
Write-Host ""

# Cleanup
$token = $null
[System.GC]::Collect()
