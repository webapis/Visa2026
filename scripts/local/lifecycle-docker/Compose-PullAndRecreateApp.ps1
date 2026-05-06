#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Force-recreate only the 'app' service (defaults to :local).

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-local).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.prod.yml).

.PARAMETER EnvFile
  Env file relative to repo root (default: .env.local).

.PARAMETER Pull
  Pull from the registry before recreating (uses the tag from your env file).

.PARAMETER UseLocal
  Back-compat alias for the default behavior (use locally built :local tag).

.EXAMPLE
  .\scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1

.EXAMPLE
  .\scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1 -ComposeProject visa2026-prod -EnvFile .env.prod

.EXAMPLE
  .\scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1 -UseLocal

.EXAMPLE
  .\scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1 -Pull
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-local",
    [string]$ComposeFile = "docker-compose.prod.yml",
    [string]$EnvFile = ".env.local",
    [switch]$Pull,
    [switch]$UseLocal
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$envPath = Join-Path $RepoRoot $EnvFile
if (-not (Test-Path -LiteralPath $envPath)) {
    throw "Env file not found: $envPath"
}

$composePath = Join-Path $RepoRoot $ComposeFile
if (-not (Test-Path -LiteralPath $composePath)) {
    throw "Compose file not found: $composePath"
}

if ($Pull -and $UseLocal) {
    throw "Use only one of -Pull or -UseLocal (default is local)."
}

if ($Pull) {
    Write-Host "Pulling 'app' image..." -ForegroundColor Cyan
    docker compose -p $ComposeProject --env-file $envPath -f $composePath pull app
    if ($LASTEXITCODE -ne 0) { throw "docker compose pull failed (exit $LASTEXITCODE)." }
} else {
    Write-Host "Using local image tag ':local' (skipping pull)..." -ForegroundColor Cyan
}

Write-Host "Recreating 'app' container..." -ForegroundColor Cyan
if (-not $Pull) {
    $tempEnv = Join-Path $env:TEMP ("visa2026-compose-tags-{0}.env" -f [Guid]::NewGuid().ToString("n"))
    try {
        Set-Content -Path $tempEnv -Value @("APP_IMAGE_TAG=local") -Encoding utf8
        docker compose -p $ComposeProject --env-file $envPath --env-file $tempEnv -f $composePath up -d --force-recreate --no-deps app
        if ($LASTEXITCODE -ne 0) { throw "docker compose up failed (exit $LASTEXITCODE)." }
    }
    finally {
        if (Test-Path $tempEnv) { Remove-Item -LiteralPath $tempEnv -Force -ErrorAction SilentlyContinue }
    }
} else {
    docker compose -p $ComposeProject --env-file $envPath -f $composePath up -d --force-recreate --no-deps app
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed (exit $LASTEXITCODE)." }
}

Write-Host "Done." -ForegroundColor Green

