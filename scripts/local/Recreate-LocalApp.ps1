#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Recreate the local Visa2026 app container using the existing image (no rebuild).

.DESCRIPTION
  Convenience wrapper around docker compose that:
  - Uses the same defaults as Rebuild-And-DeployLocal.ps1 / Build-DockerImages.ps1
  - Does NOT rebuild images (reuses whatever is already tagged)
  - Force-recreates only the 'app' service in the visa2026-dev project

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-dev).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.dev.yml).

.PARAMETER EnvFile
  Env file relative to repo root (default: .env.dev).

.EXAMPLE
  .\scripts\local\Recreate-LocalApp.ps1

.EXAMPLE
  .\scripts\local\Recreate-LocalApp.ps1 -ComposeProject visa2026-dev -EnvFile .env.dev
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-dev",
    [string]$ComposeFile = "docker-compose.dev.yml",
    [string]$EnvFile = ".env.dev"
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

Write-Host "Recreating local app container without rebuild..." -ForegroundColor Cyan

docker compose `
    -p $ComposeProject `
    --env-file $envPath `
    -f $composePath `
    up -d --no-build --force-recreate app

if ($LASTEXITCODE -ne 0) {
    throw "docker compose up failed (exit $LASTEXITCODE)."
}

Write-Host "Done." -ForegroundColor Green

