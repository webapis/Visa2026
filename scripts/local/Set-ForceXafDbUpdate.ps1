#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL / OPS] Set or clear FORCE_XAF_DB_UPDATE in an env file and optionally recreate the compose app.

.DESCRIPTION
  FORCE_XAF_DB_UPDATE=true forces XAF DatabaseUpdateMode.UpdateDatabaseAlways for one-shot ModuleUpdaters
  (e.g. ReportsUpdater). Remove or disable after a successful start.

.PARAMETER Enable
  Add or set FORCE_XAF_DB_UPDATE=true in the env file.

.PARAMETER Disable
  Remove any FORCE_XAF_DB_UPDATE= line from the env file (compose then passes an empty value → off).

.PARAMETER EnvFile
  Path relative to repo root or absolute (default: .env.prod).

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-dev).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.dev.yml).

.PARAMETER NoCompose
  Only edit the env file; do not run docker compose.

.EXAMPLE
  .\scripts\local\Set-ForceXafDbUpdate.ps1 -Enable

.EXAMPLE
  .\scripts\local\Set-ForceXafDbUpdate.ps1 -Disable

.EXAMPLE
  .\scripts\local\Set-ForceXafDbUpdate.ps1 -Enable -EnvFile .env.dev -ComposeProject visa2026-dev
#>
[CmdletBinding(DefaultParameterSetName = 'Enable')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Enable')]
    [switch]$Enable,

    [Parameter(Mandatory = $true, ParameterSetName = 'Disable')]
    [switch]$Disable,

    [string]$EnvFile = ".env.prod",
    [string]$ComposeProject = "visa2026-dev",
    [string]$ComposeFile = "docker-compose.dev.yml",
    [switch]$NoCompose
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$envPath = if ([System.IO.Path]::IsPathRooted($EnvFile)) { $EnvFile } else { Join-Path $RepoRoot $EnvFile }
$composePath = Join-Path $RepoRoot $ComposeFile

if (-not (Test-Path -LiteralPath $envPath)) {
    throw "Env file not found: $envPath"
}
if (-not (Test-Path -LiteralPath $composePath)) {
    throw "Compose file not found: $composePath"
}

$lines = Get-Content -LiteralPath $envPath
$newLines = [System.Collections.ArrayList]::new()
$found = $false
foreach ($line in $lines) {
    if ($line -match '^\s*FORCE_XAF_DB_UPDATE\s*=') {
        $found = $true
        if ($Enable) {
            [void]$newLines.Add("FORCE_XAF_DB_UPDATE=true")
        }
    }
    else {
        [void]$newLines.Add($line)
    }
}

if ($Enable -and -not $found) {
    [void]$newLines.Add("FORCE_XAF_DB_UPDATE=true")
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$text = (($newLines | ForEach-Object { $_ }) -join "`n") + "`n"
[System.IO.File]::WriteAllText($envPath, $text, $utf8NoBom)

if ($Enable) {
    Write-Host "FORCE_XAF_DB_UPDATE enabled in $envPath" -ForegroundColor Green
}
else {
    Write-Host "FORCE_XAF_DB_UPDATE removed from $envPath" -ForegroundColor Green
}

if ($NoCompose) {
    Write-Host "Skipped docker compose (-NoCompose). Recreate the app when ready." -ForegroundColor DarkGray
    return
}

Set-Location $RepoRoot
Write-Host "Recreating compose service 'app' (project $ComposeProject, APP_IMAGE_TAG=local)..." -ForegroundColor Cyan
$tempEnv = Join-Path $env:TEMP ("visa2026-compose-tags-{0}.env" -f [Guid]::NewGuid().ToString("n"))
try {
    Set-Content -Path $tempEnv -Value @("APP_IMAGE_TAG=local") -Encoding utf8
    docker compose -p $ComposeProject --env-file $envPath --env-file $tempEnv -f $composePath up -d --force-recreate --no-deps app
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose failed (exit $LASTEXITCODE)."
    }
}
finally {
    if (Test-Path $tempEnv) { Remove-Item -LiteralPath $tempEnv -Force -ErrorAction SilentlyContinue }
}
Write-Host "Done." -ForegroundColor Green
