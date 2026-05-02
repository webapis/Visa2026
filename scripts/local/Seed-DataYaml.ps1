#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL] Run Visa2026.DataImporter --import-yaml-only against the compose app (data.yaml scenarios).

.DESCRIPTION
  Starts a one-off db-updater container (compose profile tools). Requires the stack app + SQL to be up
  so API_BASE_URL=http://app:8080 works inside the compose network.

  By default the importer uses data.yaml bundled in the importer image (same as repo Visa2026.DataImporter/data.yaml
  at image build time). To use a YAML file from your PC, pass -HostYamlPath.

  Lookup seeding is NOT run in this mode. If the database is fresh, run seed-lookups first:
    docker compose -p <project> --env-file <env> -f <compose> --profile tools run --rm db-updater -- --seed-lookups-only

.PARAMETER HostYamlPath
  Optional absolute or repo-relative path to a YAML file; it is bind-mounted into the container and passed to --import-yaml-only.

.PARAMETER EnvFile
  Env file relative to repo root or absolute (default: .env.prod).

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-local).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.prod.yml).

.EXAMPLE
  .\scripts\local\Seed-DataYaml.ps1

.EXAMPLE
  .\scripts\local\Seed-DataYaml.ps1 -HostYamlPath .\Visa2026.DataImporter\data.yaml
#>
param(
    [string]$HostYamlPath = "",
    [string]$EnvFile = ".env.prod",
    [string]$ComposeProject = "visa2026-local",
    [string]$ComposeFile = "docker-compose.prod.yml"
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
$serviceAndImporterArgs = @("db-updater", "--import-yaml-only")
if (-not [string]::IsNullOrWhiteSpace($HostYamlPath)) {
    $resolved = if ([System.IO.Path]::IsPathRooted($HostYamlPath)) { $HostYamlPath } else { Join-Path $RepoRoot $HostYamlPath }
    if (-not (Test-Path -LiteralPath $resolved)) {
        throw "YAML file not found: $resolved"
    }
    $resolved = (Resolve-Path -LiteralPath $resolved).Path
    $mountArgs += "-v", "${resolved}:/app/custom-data.yaml:ro"
    $serviceAndImporterArgs = @("db-updater", "--import-yaml-only", "/app/custom-data.yaml")
}

Write-Host "Running db-updater (import-yaml-only). Ensure app + sqlserver are up for project $ComposeProject." -ForegroundColor Cyan

Set-Location $RepoRoot
# docker compose ... run --rm [-v host:container:ro] db-updater --import-yaml-only [path]
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
