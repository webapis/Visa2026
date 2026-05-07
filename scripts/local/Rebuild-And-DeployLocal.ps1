#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Build Visa2026 images locally and recreate the local compose app container.

.DESCRIPTION
  Wrapper around scripts/local/Build-DockerImages.ps1 that builds the :local tag(s) and then
  force-recreates the compose service 'app' so it runs the freshly built image.

  This uses the same compose/env defaults as the repo docs:
  - project: visa2026-dev
  - env: .env.dev
  - compose file: docker-compose.dev.yml

  Note: If your .env.dev pins APP_IMAGE_TAG to a version, this script still deploys :local
  by using Build-DockerImages.ps1 -DeployLocal (temporary env override).

.PARAMETER AppOnly
  Build only the main Blazor app image.

.PARAMETER ImporterOnly
  Build only the importer image. (No compose service runs it continuously; build only.)

.PARAMETER ImagePrefix
  Repository prefix for tags (default: webapia).

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-dev).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.dev.yml).

.PARAMETER EnvFile
  Env file relative to repo root (default: .env.dev).

.PARAMETER NoRecreate
  Build only; do not recreate compose services.

.EXAMPLE
  .\scripts\local\Rebuild-And-DeployLocal.ps1

.EXAMPLE
  .\scripts\local\Rebuild-And-DeployLocal.ps1 -AppOnly
#>
[CmdletBinding()]
param(
    [switch]$AppOnly,
    [switch]$ImporterOnly,
    [string]$ImagePrefix = "webapia",
    [string]$ComposeProject = "visa2026-dev",
    [string]$ComposeFile = "docker-compose.dev.yml",
    [string]$EnvFile = ".env.dev",
    [switch]$NoRecreate
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$buildScript = Join-Path $RepoRoot "scripts\local\Build-DockerImages.ps1"
if (-not (Test-Path -LiteralPath $buildScript)) {
    throw "Build script not found: $buildScript"
}

if ($AppOnly -and $ImporterOnly) {
    throw "Use only one of -AppOnly or -ImporterOnly, not both."
}

Write-Host "Running local Docker build (and deploy)..." -ForegroundColor Cyan
& $buildScript `
    -AppOnly:$AppOnly `
    -ImporterOnly:$ImporterOnly `
    -ImagePrefix $ImagePrefix `
    -ComposeProject $ComposeProject `
    -ComposeFile $ComposeFile `
    -EnvFile $EnvFile `
    -DeployLocal:(-not $NoRecreate)
if ($LASTEXITCODE -ne 0) {
    throw "Build-DockerImages.ps1 failed (exit $LASTEXITCODE)."
}

Write-Host "Done." -ForegroundColor Green

