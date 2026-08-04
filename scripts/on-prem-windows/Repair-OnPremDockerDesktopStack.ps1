#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Repair Visa2026 on 10.100.128.25-class hosts: Docker Desktop memory, port layout, container restart.

.DESCRIPTION
  - Sets Docker Desktop MemoryMiB (default 12288)
  - Maps staging to :8080 (prod :80)
  - Restarts prod/staging app containers
  - Disables broken legacy WSL keepalive tasks
  - Writes C:\visa2026\OFFICER_URLS.txt

.EXAMPLE
  .\Repair-OnPremDockerDesktopStack.ps1
#>
[CmdletBinding()]
param(
    [string]$DockerBin = "E:\Docker\resources\bin",
    [string]$ProdRoot = "E:\visa2026-prod",
    [string]$StagingRoot = "E:\visa2026-staging",
    [int]$MemoryMiB = 12288,
    [string]$ServerHost = "10.100.128.25",
    [string]$DockerSettingsStore = "C:\Users\adm43418\AppData\Roaming\Docker\settings-store.json"
)

$ErrorActionPreference = "Stop"
$docker = Join-Path $DockerBin "docker.exe"
$compose = Join-Path $DockerBin "docker-compose.exe"
if (-not (Test-Path -LiteralPath $docker)) {
    throw "Docker CLI not found: $docker"
}

function Set-EnvAppPort {
    param([string]$EnvPath, [int]$Port)
    $text = [IO.File]::ReadAllText($EnvPath)
    if ($text -match 'APP_PORT=\d+') {
        $text = [regex]::Replace($text, 'APP_PORT=\d+', "APP_PORT=$Port")
    }
    else {
        $text = $text.TrimEnd() + "`r`nAPP_PORT=$Port`r`n"
    }
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [IO.File]::WriteAllText($EnvPath, $text, $utf8)
}

function Test-HttpOk {
    param([string]$Url)
    try {
        $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 45
        return [PSCustomObject]@{ Url = $Url; Ok = $true; Status = $r.StatusCode }
    }
    catch {
        return [PSCustomObject]@{ Url = $Url; Ok = $false; Status = $_.Exception.Message }
    }
}

Write-Host "==> Docker Desktop memory -> ${MemoryMiB} MiB" -ForegroundColor Cyan
if (Test-Path -LiteralPath $DockerSettingsStore) {
    $settings = Get-Content -LiteralPath $DockerSettingsStore -Raw | ConvertFrom-Json
    $settings | Add-Member -NotePropertyName MemoryMiB -NotePropertyValue $MemoryMiB -Force
    $settings | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $DockerSettingsStore -Encoding UTF8
    Write-Host "  Updated $DockerSettingsStore" -ForegroundColor Green
}
else {
    Write-Warning "Docker settings-store not found at $DockerSettingsStore - skip memory change."
}

function Test-DockerReady {
    $old = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    try {
        $null = & $docker info 2>&1
        return $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $old
    }
}

Write-Host "==> Restart Docker Desktop service (applies memory)" -ForegroundColor Cyan
Stop-Process -Name "Docker Desktop" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "com.docker.backend" -Force -ErrorAction SilentlyContinue
sc.exe stop com.docker.service | Out-Null
Start-Sleep -Seconds 8
sc.exe start com.docker.service | Out-Null
$dockerDesktop = "E:\Docker\Docker Desktop.exe"
if (Test-Path -LiteralPath $dockerDesktop) {
    Start-Process -FilePath $dockerDesktop -WindowStyle Hidden
}
$deadline = (Get-Date).AddMinutes(5)
do {
    Start-Sleep -Seconds 8
    $ready = Test-DockerReady
} while (-not $ready -and (Get-Date) -lt $deadline)
if (-not (Test-DockerReady)) {
    throw "Docker daemon did not become ready within 5 minutes."
}
Write-Host "  Docker daemon ready." -ForegroundColor Green

Write-Host "==> Staging APP_PORT -> 8080" -ForegroundColor Cyan
$stagingEnv = Join-Path $StagingRoot ".env.prod"
if (-not (Test-Path -LiteralPath $stagingEnv)) {
    throw "Missing $stagingEnv"
}
Set-EnvAppPort -EnvPath $stagingEnv -Port 8080
$env:DOCKER_CONFIG = Join-Path $StagingRoot ".docker"
$env:Path = (Join-Path $StagingRoot "bin") + ";" + (($env:Path -split ';' | Where-Object { $_ -and ($_ -notmatch 'Docker') }) -join ';')
Push-Location $StagingRoot
& $compose -p visa2026-staging --env-file .env.prod -f docker-compose.prod.yml up -d --force-recreate app
Pop-Location

Write-Host "==> Restart prod app" -ForegroundColor Cyan
$env:DOCKER_CONFIG = Join-Path $ProdRoot ".docker"
Push-Location $ProdRoot
& $compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml restart app
Pop-Location
Start-Sleep -Seconds 15

Write-Host "==> Disable legacy WSL keepalive tasks" -ForegroundColor Cyan
foreach ($task in @("Visa2026-WslKeepAlive", "Visa2026-WslPersistent", "Visa2026-Startup")) {
    schtasks /Query /TN $task 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        schtasks /Change /TN $task /DISABLE | Out-Null
        Write-Host "  Disabled $task" -ForegroundColor Yellow
    }
}

$noticePath = "C:\visa2026\OFFICER_URLS.txt"
$notice = @"
Visa2026 officer URLs (Docker) - updated $(Get-Date -Format 'yyyy-MM-dd HH:mm')

Production:  http://$ServerHost/LoginPage
Staging:     http://${ServerHost}:8080/LoginPage

Do NOT use port 8080 for IIS (removed). Hard-refresh (Ctrl+F5) if the page sticks on loading.

Demo slot (:8081) is not deployed in Docker yet.
"@
$utf8 = New-Object System.Text.UTF8Encoding $false
[IO.File]::WriteAllText($noticePath, $notice, $utf8)
Write-Host "Wrote $noticePath" -ForegroundColor Green

Write-Host "==> Smoke tests" -ForegroundColor Cyan
& $docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
foreach ($url in @(
        "http://127.0.0.1/LoginPage",
        "http://${ServerHost}/LoginPage",
        "http://127.0.0.1:8080/LoginPage",
        "http://${ServerHost}:8080/LoginPage"
    )) {
    $r = Test-HttpOk -Url $url
    $color = if ($r.Ok) { "Green" } else { "Red" }
    Write-Host ("  {0} {1}" -f $(if ($r.Ok) { "OK" } else { "FAIL" }), $r.Url) -ForegroundColor $color
}

Write-Host "Repair complete." -ForegroundColor Green
