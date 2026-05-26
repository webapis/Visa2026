#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  [ON-PREM WINDOWS SERVER] Enable or disable FORCE_XAF_DB_UPDATE in C:\visa2026\.env.prod and recreate app via WSL.

.DESCRIPTION
  One-shot XAF ModuleUpdaters when UpdateOldDatabase would skip them. Uses WSL docker compose
  (same paths as Start-Visa2026Compose.ps1). Remove the flag after a healthy start.

.PARAMETER Enable
  Set FORCE_XAF_DB_UPDATE=true in the env file.

.PARAMETER Disable
  Remove FORCE_XAF_DB_UPDATE from the env file.

.PARAMETER DeployRoot
  Windows deploy folder (default C:\visa2026).

.PARAMETER DistroName
  WSL distribution (default Ubuntu).

.PARAMETER NoCompose
  Only edit .env.prod; do not recreate the app container.

.EXAMPLE
  .\Set-OnPremForceXafDbUpdate.ps1 -Enable

.EXAMPLE
  .\Set-OnPremForceXafDbUpdate.ps1 -Disable
#>
[CmdletBinding(DefaultParameterSetName = 'Enable')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Enable')]
    [switch]$Enable,

    [Parameter(Mandatory = $true, ParameterSetName = 'Disable')]
    [switch]$Disable,

    [string]$DeployRoot = 'C:\visa2026',
    [string]$DistroName = 'Ubuntu',
    [string]$ComposeFile = 'docker-compose.prod.yml',
    [string]$EnvFileName = '.env.prod',
    [string]$ComposeProject = 'visa2026-prod',
    [switch]$NoCompose
)

$ErrorActionPreference = 'Stop'
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

$envPath = Join-Path $DeployRoot $EnvFileName
if (-not (Test-Path -LiteralPath $envPath)) {
    throw ('Env file not found: ' + $envPath)
}

$lines = Get-Content -LiteralPath $envPath
$newLines = [System.Collections.ArrayList]::new()
$found = $false
foreach ($line in $lines) {
    if ($line -match '^\s*FORCE_XAF_DB_UPDATE\s*=') {
        $found = $true
        if ($Enable) {
            [void]$newLines.Add('FORCE_XAF_DB_UPDATE=true')
        }
    }
    else {
        [void]$newLines.Add($line)
    }
}
if ($Enable -and -not $found) {
    [void]$newLines.Add('FORCE_XAF_DB_UPDATE=true')
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$text = (($newLines | ForEach-Object { $_ }) -join "`n") + "`n"
[System.IO.File]::WriteAllText($envPath, $text, $utf8NoBom)

if ($Enable) {
    Write-Host ('FORCE_XAF_DB_UPDATE enabled in ' + $envPath) -ForegroundColor Green
}
else {
    Write-Host ('FORCE_XAF_DB_UPDATE removed from ' + $envPath) -ForegroundColor Green
}

if ($NoCompose) {
    Write-Host 'Skipped compose recreate (-NoCompose).' -ForegroundColor DarkGray
    return
}

if ($DeployRoot -notmatch '^([A-Za-z]):\\(.*)$') {
    throw 'DeployRoot must be a Windows path like C:\visa2026'
}
$wslPath = '/mnt/' + $Matches[1].ToLower() + '/' + ($Matches[2] -replace '\\', '/')
$bash = 'cd ' + $wslPath + ' && docker compose -p ' + $ComposeProject + ' --env-file ' + $EnvFileName + ' -f ' + $ComposeFile + ' up -d --force-recreate --no-deps app'

Write-Host 'Recreating app container via WSL...' -ForegroundColor Cyan
& wsl.exe -d $DistroName -u root -e bash -lc $bash
if ($LASTEXITCODE -ne 0) {
    throw 'docker compose recreate app failed'
}
Write-Host 'Done. Remove FORCE_XAF_DB_UPDATE after verifying updaters ran.' -ForegroundColor Green
