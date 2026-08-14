#Requires -Version 5.1

<#
.SYNOPSIS
  Wave 1 — export signed-off ApplicationProfile tenant JSON from legacy VISA2015 (Çalik).

.DESCRIPTION
  After Wave 0 Excel sign-off, writes LookupCatalogs/tenant/application-profile.calik-energi.json
  (one profile per translated ApplicationType). Synced on deploy via ApplicationProfileTenantCatalogSeedUpdater.

  Requires VISA2014_SQL_PASSWORD in the environment (or password in --connection).

.EXAMPLE
  $env:VISA2014_SQL_PASSWORD = '***'
  .\scripts\visa2014-migration\catalogs\generate\ApplicationProfileTenant-CalikEnergi.ps1

.EXAMPLE
  .\scripts\visa2014-migration\catalogs\generate\ApplicationProfileTenant-CalikEnergi.ps1 -LegacySource calik-energi-local-pg
#>
param(
    [string]$LegacySource = 'calik-energi-local-pg',
    [string]$OutputPath
)

. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$importerProj = Join-Path $repoRoot 'Visa2026.DataImporter\Visa2026.DataImporter.csproj'
$defaultOut = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant\application-profile.calik-energi.json'
if (-not $OutputPath) { $OutputPath = $defaultOut }
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

Write-Host '=== Wave 1: ApplicationProfile tenant JSON (legacy VISA2015) ===' -ForegroundColor Cyan
Write-Host "INF Legacy source: $LegacySource"
Write-Host "INF Output: $OutputPath"

$dotnetArgs = @(
    'run', '--project', $importerProj, '--no-build',
    '--export-visa2014-application-profile-tenant-json',
    '--legacy-source', $LegacySource,
    '--output', $OutputPath
)

Push-Location $repoRoot
try {
    & dotnet build $importerProj -c Debug --no-restore | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & dotnet build $importerProj -c Debug
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

Write-Host "OK Tenant JSON: $OutputPath" -ForegroundColor Green
Write-Host 'INF Deploy sync: ApplicationProfileTenantCatalogSeedUpdater (after ApplicationProfileSeedUpdater).'
