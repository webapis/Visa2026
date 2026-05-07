#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Optional hot reload: Blazor app with dotnet watch inside Docker (docker-compose.watch.yml).

.DESCRIPTION
  Uses the .NET SDK image, mounts the repo into the container, and runs `dotnet watch run`.
  This is separate from production-like stacks (docker-compose.prod.yml / Hub or local built images).

  Use a dedicated compose project name (default visa2026-watch) so it does not replace
  visa2026-dev. Default app port is 8081 unless APP_PORT is set in the env file.

.NOTES
  Location: scripts/local/ — for developer PCs only. Requires NuGet reachable from the container.

.PARAMETER EnvFile
  Env file relative to repo root. Prefer .env.dev for DB_NAME=Visa2026DbDev alignment.

.PARAMETER Project
  Docker Compose project name (default: visa2026-watch).

.PARAMETER Detach
  Run in background (-d). Default is attached (foreground) so you see watch output.
#>
param(
    [string]$EnvFile = ".env.dev",
    [string]$Project = "visa2026-watch",
    [switch]$Detach
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$envPath = Join-Path $RepoRoot $EnvFile
if (-not (Test-Path $envPath)) {
    throw "Env file not found: $envPath. Create it from .env.dev.example."
}

$watchFile = Join-Path $RepoRoot "docker-compose.watch.yml"
if (-not (Test-Path $watchFile)) {
    throw "Missing docker-compose.watch.yml at repo root."
}

$dockerArgs = @(
    "compose", "-p", $Project,
    "--env-file", $envPath,
    "-f", $watchFile,
    "up"
)
if ($Detach) { $dockerArgs += "-d" }

Write-Host "Starting watch stack (project=$Project, env=$EnvFile)..." -ForegroundColor Cyan
Write-Host "App URL is typically http://localhost:8081 unless APP_PORT overrides." -ForegroundColor DarkGray
& docker @dockerArgs
