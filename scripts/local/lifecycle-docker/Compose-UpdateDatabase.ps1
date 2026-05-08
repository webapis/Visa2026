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
  Docker Compose project name (default: visa2026-dev).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.dev.yml).

.PARAMETER EnvFile
  Env file relative to repo root (default: .env.dev).

.PARAMETER Silent
  Pass --silent to the updater (if supported by the app).

.PARAMETER UseLocal
  Force APP_IMAGE_TAG=local for this one-off run (avoids pulling from Docker Hub). Default: on.

.PARAMETER Pull
  Pull the app image from registry for the tag in your env file (disables -UseLocal).

.EXAMPLE
  .\scripts\local\lifecycle-docker\Compose-UpdateDatabase.ps1

.EXAMPLE
  .\scripts\local\lifecycle-docker\Compose-UpdateDatabase.ps1 -ComposeProject visa2026-prod -EnvFile .env.prod -Silent
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-dev",
    [string]$ComposeFile = "docker-compose.dev.yml",
    [string]$EnvFile = ".env.dev",
    [switch]$Silent,
    [switch]$UseLocal = $true,
    [switch]$Pull
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
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
    throw "Use only one of -Pull or -UseLocal."
}

$baseArgs = @(
    'compose', '-p', $ComposeProject,
    '--env-file', $envPath
)

$tempEnv = $null
if ($UseLocal -and -not $Pull) {
    $tempEnv = Join-Path $env:TEMP ("visa2026-compose-tags-{0}.env" -f [Guid]::NewGuid().ToString("n"))
    Set-Content -Path $tempEnv -Value @("APP_IMAGE_TAG=local") -Encoding utf8
    $baseArgs += @('--env-file', $tempEnv)
}

$pullMode = "never"
if ($Pull) { $pullMode = "always" }

$args = $baseArgs + @(
    '-f', $composePath,
    'run', '--rm', '--no-deps',
    '--pull', $pullMode,
    'app',
    '--updateDatabase', '--forceUpdate'
)
if ($Silent) { $args += '--silent' }

Write-Host ("docker {0}" -f ($args -join ' ')) -ForegroundColor DarkGray
try {
    & docker @args
    if ($LASTEXITCODE -ne 0) {
        throw "Database update failed (exit $LASTEXITCODE). Check output above, then run .\\scripts\\local\\lifecycle-docker\\Docker-AppLogs.ps1."
    }
}
finally {
    if ($tempEnv -and (Test-Path $tempEnv)) { Remove-Item -LiteralPath $tempEnv -Force -ErrorAction SilentlyContinue }
}

Write-Host "Database update completed." -ForegroundColor Green

