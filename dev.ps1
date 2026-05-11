# dev.ps1 - OpenChat Development Server Manager
# Usage: .\dev.ps1
# Controls: R = Restart API | U = Restart UI | Q = Quit all

$API_DIR = Join-Path $PSScriptRoot "backend\OpenChat.API"
$UI_DIR  = Join-Path $PSScriptRoot "frontend\openchat-ui"

$script:apiJob = $null
$script:uiJob  = $null

function Write-Log {
    param([string]$Prefix, [ConsoleColor]$Color, [string]$Message)
    $ts = Get-Date -Format "HH:mm:ss"
    Write-Host "$ts " -ForegroundColor DarkGray -NoNewline
    Write-Host "[$Prefix] " -ForegroundColor $Color -NoNewline
    Write-Host $Message
}

function Kill-Dotnet {
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
}

function Kill-Node {
    Get-Process -Name "node" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
}

function Start-API {
    $script:apiJob = Start-Job -Name "API" -ScriptBlock {
        param($dir)
        Set-Location $dir
        & dotnet watch run 2>&1
    } -ArgumentList $API_DIR
    Write-Log "API" Cyan "Iniciando... (dotnet watch run)"
}

function Stop-API {
    if ($script:apiJob) {
        Stop-Job   $script:apiJob -ErrorAction SilentlyContinue
        Remove-Job $script:apiJob -Force -ErrorAction SilentlyContinue
        $script:apiJob = $null
    }
    Kill-Dotnet
    Write-Log "API" Yellow "Detenido"
}

function Start-UI {
    $script:uiJob = Start-Job -Name "UI" -ScriptBlock {
        param($dir)
        Set-Location $dir
        & npm start 2>&1
    } -ArgumentList $UI_DIR
    Write-Log "UI " Green "Iniciando... (npm start)"
}

function Stop-UI {
    if ($script:uiJob) {
        Stop-Job   $script:uiJob -ErrorAction SilentlyContinue
        Remove-Job $script:uiJob -Force -ErrorAction SilentlyContinue
        $script:uiJob = $null
    }
    Kill-Node
    Write-Log "UI " Yellow "Detenido"
}

function Flush-Output {
    if ($script:apiJob) {
        $lines = Receive-Job $script:apiJob -ErrorAction SilentlyContinue
        foreach ($line in $lines) {
            if ($line) { Write-Log "API" Cyan $line }
        }
        if ($script:apiJob.State -eq "Failed") {
            Write-Log "API" Red "El proceso termino inesperadamente. Presiona R para reiniciar."
        }
    }
    if ($script:uiJob) {
        $lines = Receive-Job $script:uiJob -ErrorAction SilentlyContinue
        foreach ($line in $lines) {
            if ($line) { Write-Log "UI " Green $line }
        }
        if ($script:uiJob.State -eq "Failed") {
            Write-Log "UI " Red "El proceso termino inesperadamente. Presiona U para reiniciar."
        }
    }
}

# Banner
Clear-Host
Write-Host "============================================" -ForegroundColor White
Write-Host "      OpenChat - Dev Server Manager         " -ForegroundColor White
Write-Host "============================================" -ForegroundColor White
Write-Host "  [R] Restart API     [U] Restart UI        " -ForegroundColor White
Write-Host "  [Q] Quit all                              " -ForegroundColor White
Write-Host "============================================" -ForegroundColor White
Write-Host ""

# Kill stale processes from previous run
Kill-Dotnet
Kill-Node
Start-Sleep -Milliseconds 500

Start-API
Start-UI
Write-Host ""

# Main loop
try {
    while ($true) {
        Flush-Output

        if ([Console]::KeyAvailable) {
            $key = [Console]::ReadKey($true)
            switch ($key.Key.ToString().ToUpper()) {
                "R" {
                    Write-Host ""
                    Write-Log "API" Yellow "Reiniciando API..."
                    Stop-API
                    Start-Sleep -Milliseconds 1200
                    Start-API
                    Write-Host ""
                }
                "U" {
                    Write-Host ""
                    Write-Log "UI " Yellow "Reiniciando UI..."
                    Stop-UI
                    Start-Sleep -Milliseconds 1200
                    Start-UI
                    Write-Host ""
                }
                "Q" {
                    Write-Host ""
                    Write-Log "SYS" Red "Deteniendo todos los servicios..."
                    Stop-API
                    Stop-UI
                    Write-Host ""
                    Write-Host "Hasta luego." -ForegroundColor DarkGray
                    exit 0
                }
            }
        }

        Start-Sleep -Milliseconds 150
    }
}
finally {
    Stop-API
    Stop-UI
}
