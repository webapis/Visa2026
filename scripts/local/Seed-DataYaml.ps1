#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL] Run Visa2026.DataImporter (default: data.yaml scenarios) against the compose app.

.DESCRIPTION
  Starts a one-off db-updater container (compose profile tools). Requires the stack app + SQL to be up
  so API_BASE_URL=http://app:8080 works inside the compose network.

  By default the importer uses data.yaml bundled in the importer image (same as repo Visa2026.DataImporter/data.yaml
  at image build time). To use a YAML file from your PC, pass -HostYamlPath.

  Lookup catalogs are NOT imported here — they sync when the app starts (LookupCatalogSyncUpdater).
  On a fresh database, ensure app + SQL are up and the app has completed database update before running this script.

.PARAMETER HostYamlPath
  Optional absolute or repo-relative path to a YAML file; bind-mounted and passed as the importer's first argument.

.PARAMETER EnvFile
  Env file relative to repo root or absolute (default: .env.prod).

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-dev).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.dev.yml).

.EXAMPLE
  .\scripts\local\Seed-DataYaml.ps1

.EXAMPLE
  .\scripts\local\Seed-DataYaml.ps1 -HostYamlPath .\Visa2026.DataImporter\data.yaml
#>
param(
    [string]$HostYamlPath = "",
    [string]$EnvFile = ".env.prod",
    [string]$ComposeProject = "visa2026-dev",
    [string]$ComposeFile = "docker-compose.dev.yml"
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

$mountArgs = @()
$serviceAndImporterArgs = @("db-updater")
if (-not [string]::IsNullOrWhiteSpace($HostYamlPath)) {
    $resolved = if ([System.IO.Path]::IsPathRooted($HostYamlPath)) { $HostYamlPath } else { Join-Path $RepoRoot $HostYamlPath }
    if (-not (Test-Path -LiteralPath $resolved)) {
        throw "YAML file not found: $resolved"
    }
    $resolved = (Resolve-Path -LiteralPath $resolved).Path
    $mountArgs += "-v", "${resolved}:/app/custom-data.yaml:ro"
    $serviceAndImporterArgs = @("db-updater", "/app/custom-data.yaml")
}

Write-Host "Running db-updater (imports bundled data.yaml by default). Ensure app + sqlserver are up for project $ComposeProject." -ForegroundColor Cyan

Set-Location $RepoRoot
# docker compose ... run --rm [-v host:container:ro] db-updater [optional-yaml-path]
$allArgs = @(
    "compose", "-p", $ComposeProject,
    "--env-file", $envPath,
    "-f", $composePath,
    "--profile", "tools",
    "run", "--rm"
) + $mountArgs + $serviceAndImporterArgs

& docker @allArgs
if ($LASTEXITCODE -ne 0) {
    throw "docker compose run db-updater failed (exit $LASTEXITCODE)."
}
