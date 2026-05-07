#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Force-recreate the 'sqlserver' service for a compose stack.

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-dev).

.PARAMETER EnvFile
  Env file relative to repo root or absolute (default: .env.dev).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.dev.yml).

.EXAMPLE
  .\scripts\local\Recreate-SqlServer.ps1

.EXAMPLE
  .\scripts\local\Recreate-SqlServer.ps1 -ComposeProject visa2026-dev -EnvFile .env.dev
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-dev",
    [string]$EnvFile = ".env.dev",
    [string]$ComposeFile = "docker-compose.dev.yml"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$envPath = if ([System.IO.Path]::IsPathRooted($EnvFile)) { $EnvFile } else { Join-Path $RepoRoot $EnvFile }
$composePath = Join-Path $RepoRoot $ComposeFile

if (-not (Test-Path -LiteralPath $envPath)) { throw "Env file not found: $envPath" }
if (-not (Test-Path -LiteralPath $composePath)) { throw "Compose file not found: $composePath" }

Write-Host "Recreating compose service 'sqlserver' (project $ComposeProject)..." -ForegroundColor Cyan
docker compose -p $ComposeProject --env-file $envPath -f $composePath up -d --force-recreate --no-deps sqlserver
if ($LASTEXITCODE -ne 0) { throw "docker compose failed (exit $LASTEXITCODE)." }
Write-Host "Done." -ForegroundColor Green

