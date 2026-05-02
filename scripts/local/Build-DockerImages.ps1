#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Build Visa2026 Docker images locally with the same build-args as CI
  (.github/workflows/publish-to-docker-hub.yml).

.DESCRIPTION
  - App image: root Dockerfile, tags webapia/visa2026:local and webapia/visa2026:<AssemblyVersion>
  - Importer: Visa2026.DataImporter/Dockerfile, tag webapia/visa2026-importer:local

  Set APP_IMAGE_TAG=local (and IMPORTER_IMAGE_TAG=local) in .env.local to run compose against these builds,
  or use -DeployLocal to pass temporary overrides and recreate the running app container.

.NOTES
  Location: scripts/local/ — for developer PCs only, not for droplet deployment.

.PARAMETER AppOnly
  Build only the main Blazor app image.

.PARAMETER ImporterOnly
  Build only the data importer image.

.PARAMETER ImagePrefix
  Repository prefix for tags (default: webapia, matching the publish workflow).

.PARAMETER DeployLocal
  After a successful build, run docker compose with temporary tag overrides and
  force-recreate the app service so it uses the new :local images (compose file
  must use the same image repository names as ImagePrefix). Importer-only:
  no default compose service runs the importer; see script output.

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-local).

.PARAMETER ComposeFile
  Compose file relative to repo root (default: docker-compose.prod.yml).

.PARAMETER EnvFile
  Env file relative to repo root (default: .env.local).
#>
param(
    [switch]$AppOnly,
    [switch]$ImporterOnly,
    [string]$ImagePrefix = "webapia",
    [switch]$DeployLocal,
    [string]$ComposeProject = "visa2026-local",
    [string]$ComposeFile = "docker-compose.prod.yml",
    [string]$EnvFile = ".env.local"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$licensePath = Join-Path $RepoRoot "DevExpress.Key\DevExpress_License.txt"
if (-not (Test-Path $licensePath)) {
    throw "Missing DevExpress.Key\DevExpress_License.txt; required for docker build (same as CI)."
}

function Get-AssemblyVersion {
    $csprojPath = Join-Path $RepoRoot "Visa2026.Module\Visa2026.Module.csproj"
    $xml = [xml](Get-Content -Raw $csprojPath)
    $version = $xml.Project.PropertyGroup.AssemblyVersion
    if (-not $version) {
        throw "Could not read AssemblyVersion from Visa2026.Module\Visa2026.Module.csproj"
    }
    return [string]$version
}

function Get-GitShaShort {
    $sha = git -C $RepoRoot rev-parse --short=7 HEAD 2>$null
    if (-not $sha) { return "local" }
    return $sha
}

$buildApp = -not $ImporterOnly
$buildImporter = -not $AppOnly

if ($AppOnly -and $ImporterOnly) {
    throw "Use only one of -AppOnly or -ImporterOnly, not both."
}

if ($buildApp) {
    $appVersion = Get-AssemblyVersion
    $gitSha = Get-GitShaShort
    $appImage = "${ImagePrefix}/visa2026"

    Write-Host "Building $appImage (APP_VERSION=$appVersion GIT_SHA=$gitSha)..." -ForegroundColor Cyan
    docker build $RepoRoot `
        --build-arg "APP_VERSION=$appVersion" `
        --build-arg "GIT_SHA=$gitSha" `
        -t "${appImage}:local" `
        -t "${appImage}:$appVersion"
}

if ($buildImporter) {
    $importerImage = "${ImagePrefix}/visa2026-importer"
    Write-Host "Building $importerImage..." -ForegroundColor Cyan
    docker build -f (Join-Path $RepoRoot "Visa2026.DataImporter\Dockerfile") $RepoRoot `
        -t "${importerImage}:local"
}

if ($DeployLocal) {
    $envPath = Join-Path $RepoRoot $EnvFile
    if (-not (Test-Path $envPath)) {
        throw "DeployLocal requires env file at: $envPath"
    }

    $composePath = Join-Path $RepoRoot $ComposeFile
    if (-not (Test-Path $composePath)) {
        throw "DeployLocal requires compose file at: $composePath"
    }

    $overrideLines = @()
    if ($buildApp) { $overrideLines += "APP_IMAGE_TAG=local" }
    if ($buildImporter) { $overrideLines += "IMPORTER_IMAGE_TAG=local" }

    $tempEnv = Join-Path $env:TEMP ("visa2026-compose-tags-{0}.env" -f [Guid]::NewGuid().ToString("n"))
    try {
        Set-Content -Path $tempEnv -Value $overrideLines -Encoding utf8

        if ($buildApp) {
            Write-Host "Recreating compose service 'app' with :local image tags..." -ForegroundColor Cyan
            docker compose -p $ComposeProject `
                --env-file $envPath `
                --env-file $tempEnv `
                -f $composePath `
                up -d --force-recreate --no-deps app
        }
        elseif ($ImporterOnly) {
            Write-Host "Importer image updated. The default stack does not run db-updater continuously." -ForegroundColor Yellow
            Write-Host "Set IMPORTER_IMAGE_TAG=local in $EnvFile (or use --profile tools with the same override) before:" -ForegroundColor Yellow
            Write-Host "  docker compose -p $ComposeProject --env-file $EnvFile -f $ComposeFile --profile tools run --rm db-updater ..." -ForegroundColor Yellow
            Write-Host "(See docs/ENVIRONMENTS.md for db-updater arguments such as --seed-lookups-only.)" -ForegroundColor Yellow
        }
    }
    finally {
        if (Test-Path $tempEnv) { Remove-Item -LiteralPath $tempEnv -Force -ErrorAction SilentlyContinue }
    }
}

Write-Host "Done." -ForegroundColor Green
if (-not $DeployLocal) {
    $tip = "Tip: use -DeployLocal to recreate the running app container with :local tags, or set APP_IMAGE_TAG=local and IMPORTER_IMAGE_TAG=local in {0} then: docker compose up -d --force-recreate app." -f $EnvFile
    Write-Host $tip -ForegroundColor DarkGray
}
