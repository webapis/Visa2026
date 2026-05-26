#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  [ON-PREM WINDOWS SERVER] Pull and start Visa2026 prod compose stack via WSL Docker.

.DESCRIPTION
  Expects C:\visa2026\docker-compose.prod.yml and C:\visa2026\.env.prod.
  Agent skill: setup-docker-engine (see docs/legacy/ON_PREM_WINDOWS_SERVER.md).

.PARAMETER DeployRoot
  Windows folder with compose + env (default C:\visa2026).

.PARAMETER DistroName
  WSL distribution (default Ubuntu).

.PARAMETER ComposeProject
  docker compose project name (default visa2026-prod).

.PARAMETER Pull
  Run docker compose pull before up.

.PARAMETER AppOnly
  Pull/up only the app service (--no-deps).

.PARAMETER OpenHttpFirewall
  Create inbound TCP rule for APP_PORT from .env.prod (default 80).

.EXAMPLE
  .\scripts\on-prem\Start-Visa2026Compose.ps1

.EXAMPLE
  .\scripts\on-prem\Start-Visa2026Compose.ps1 -Pull -AppOnly
#>
[CmdletBinding()]
param(
    [string]$DeployRoot = 'C:\visa2026',
    [string]$DistroName = 'Ubuntu',
    [string]$ComposeFile = 'docker-compose.prod.yml',
    [string]$EnvFileName = '.env.prod',
    [string]$ComposeProject = 'visa2026-prod',
    [switch]$Pull,
    [switch]$AppOnly,
    [switch]$OpenHttpFirewall
)

$ErrorActionPreference = 'Stop'
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host ('==> ' + $Message) -ForegroundColor Cyan
}

function Get-EnvFileAppPort {
    param([string]$EnvPath)
    $port = 80
    if (-not (Test-Path -LiteralPath $EnvPath)) { return $port }
    foreach ($line in Get-Content -LiteralPath $EnvPath) {
        if ($line -match '^\s*APP_PORT\s*=\s*(\d+)\s*$') {
            return [int]$Matches[1]
        }
    }
    return $port
}

$composePath = Join-Path $DeployRoot $ComposeFile
$envPath = Join-Path $DeployRoot $EnvFileName

if (-not (Test-Path -LiteralPath $composePath)) {
    throw ('Missing compose file: ' + $composePath)
}
if (-not (Test-Path -LiteralPath $envPath)) {
    throw ('Missing env file: ' + $envPath + ' (copy from .env.prod.example and fill secrets)')
}

if ($DeployRoot -notmatch '^([A-Za-z]):\\(.*)$') {
    throw 'DeployRoot must be a Windows path like C:\visa2026'
}
$wslPath = '/mnt/' + $Matches[1].ToLower() + '/' + ($Matches[2] -replace '\\', '/')

Write-Step 'Visa2026 compose via WSL Docker'
Write-Host ('DeployRoot: ' + $DeployRoot)
Write-Host ('WSL path:   ' + $wslPath)

$null = & wsl.exe -d $DistroName -u root -- docker --version 2>&1
if ($LASTEXITCODE -ne 0) {
    throw 'Docker not available in WSL. Run Install-WslDockerEngine.ps1 first.'
}

$composeCmd = 'cd ' + $wslPath + ' && docker compose -p ' + $ComposeProject + ' --env-file ' + $EnvFileName + ' -f ' + $ComposeFile

if ($Pull) {
    Write-Step 'docker compose pull'
    if ($AppOnly) {
        $bash = $composeCmd + ' pull app'
    }
    else {
        $bash = $composeCmd + ' pull'
    }
    & wsl.exe -d $DistroName -u root -e bash -lc $bash
    if ($LASTEXITCODE -ne 0) { throw 'docker compose pull failed' }
}

Write-Step 'docker compose up -d'
if ($AppOnly) {
    $bash = $composeCmd + ' up -d --no-deps app'
}
else {
    $bash = $composeCmd + ' up -d'
}
& wsl.exe -d $DistroName -u root -e bash -lc $bash
if ($LASTEXITCODE -ne 0) { throw 'docker compose up failed' }

Write-Step 'docker compose ps'
& wsl.exe -d $DistroName -u root -e bash -lc ($composeCmd + ' ps')
if ($LASTEXITCODE -ne 0) { throw 'docker compose ps failed' }

if ($OpenHttpFirewall) {
    $httpPort = Get-EnvFileAppPort -EnvPath $envPath
    $ruleName = 'Visa2026-HTTP-In-TCP'
    $existing = Get-NetFirewallRule -Name $ruleName -ErrorAction SilentlyContinue
    if (-not $existing) {
        Write-Step ('Firewall: allow inbound TCP ' + $httpPort)
        New-NetFirewallRule -Name $ruleName -DisplayName 'Visa2026 HTTP (compose app)' `
            -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort $httpPort | Out-Null
    }
    else {
        Write-Host ('Firewall rule exists: ' + $ruleName)
    }
}

$appPort = Get-EnvFileAppPort -EnvPath $envPath
Write-Host ''
Write-Host ('Open in browser: http://<server-ip>:' + $appPort) -ForegroundColor Green
Write-Host 'Logs: wsl -d Ubuntu -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod logs app --tail 100"'
