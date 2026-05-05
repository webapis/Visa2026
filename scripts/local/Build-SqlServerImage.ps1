#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Build a local SQL Server Docker image for Visa2026.

.DESCRIPTION
  Builds a small wrapper image around Microsoft's official SQL Server Linux image.
  Useful if you want a stable local tag (e.g. visa2026-sqlserver:local) or later want to
  customize the image (tools/init scripts/entrypoint) in docker/sqlserver/Dockerfile.

.PARAMETER BaseImage
  Base SQL Server image to use (default: mcr.microsoft.com/mssql/server:2025-latest).

.PARAMETER Tag
  Target tag for the built image (default: visa2026-sqlserver:local).

.EXAMPLE
  .\scripts\local\Build-SqlServerImage.ps1

.EXAMPLE
  .\scripts\local\Build-SqlServerImage.ps1 -BaseImage mcr.microsoft.com/mssql/server:2022-latest -Tag visa2026-sqlserver:2022-local
#>
[CmdletBinding()]
param(
    [string]$BaseImage = "mcr.microsoft.com/mssql/server:2025-latest",
    [string]$Tag = "visa2026-sqlserver:local"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$dockerfile = Join-Path $RepoRoot "docker\sqlserver\Dockerfile"
if (-not (Test-Path -LiteralPath $dockerfile)) {
    throw "Dockerfile not found: $dockerfile"
}

$env:DOCKER_BUILDKIT = "1"

Write-Host "Building SQL Server image: $Tag (BASE_IMAGE=$BaseImage)..." -ForegroundColor Cyan
docker build `
    -f $dockerfile `
    --build-arg "BASE_IMAGE=$BaseImage" `
    -t $Tag `
    $RepoRoot

if ($LASTEXITCODE -ne 0) {
    throw "docker build failed (exit $LASTEXITCODE)."
}

Write-Host "Done." -ForegroundColor Green
Write-Host "If you want compose to use this image, change sqlserver.image in docker-compose*.yml to '$Tag' (or parameterize via env)." -ForegroundColor DarkGray

