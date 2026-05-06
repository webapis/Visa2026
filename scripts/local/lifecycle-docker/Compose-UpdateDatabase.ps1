#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] One-off XAF database update using the app image/stack.

.DESCRIPTION
  Runs:
    docker compose ... run --rm --no-deps app --updateDatabase --forceUpdate [--silent]

  Use this when logs show schema drift (e.g., "Invalid column name") or when you need to
  force ModuleUpdaters to run.

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-local).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.prod.yml).

.PARAMETER EnvFile
  Env file relative to repo root (default: .env.local).

.PARAMETER Silent
  Pass --silent to the updater (if supported by the app).

.EXAMPLE
  .\scripts\local\lifecycle-docker\Compose-UpdateDatabase.ps1

.EXAMPLE
  .\scripts\local\lifecycle-docker\Compose-UpdateDatabase.ps1 -ComposeProject visa2026-prod -EnvFile .env.prod -Silent
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-local",
    [string]$ComposeFile = "docker-compose.prod.yml",
    [string]$EnvFile = ".env.local",
    [switch]$Silent
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

$args = @(
    'compose', '-p', $ComposeProject,
    '--env-file', $envPath,
    '-f', $composePath,
    'run', '--rm', '--no-deps',
    'app',
    '--updateDatabase', '--forceUpdate'
)
if ($Silent) { $args += '--silent' }

Write-Host ("docker {0}" -f ($args -join ' ')) -ForegroundColor DarkGray
& docker @args
if ($LASTEXITCODE -ne 0) {
    throw "Database update failed (exit $LASTEXITCODE). Check output above, then run .\\scripts\\local\\lifecycle-docker\\Docker-AppLogs.ps1."
}

Write-Host "Database update completed." -ForegroundColor Green

