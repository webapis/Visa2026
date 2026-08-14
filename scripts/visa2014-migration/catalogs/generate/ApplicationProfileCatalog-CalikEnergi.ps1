#Requires -Version 5.1

<#
.SYNOPSIS
  Wave 0b — export proposed ApplicationProfile tenant catalog from legacy VISA2015 (Çalik).

.DESCRIPTION
  Reads dbo.Application on 10.100.128.15 / VISA2015. Via-ministry types group by
  (ApplicationType, ProjectContract) when contract is set; otherwise type-only.
  Direct migration: ApplicationType only. Writes Excel for developer sign-off before
  tenant JSON (LookupCatalogs/tenant/).

  Requires VISA2014_SQL_PASSWORD in the environment (or password in --connection).

.EXAMPLE
  $env:VISA2014_SQL_PASSWORD = '***'
  .\scripts\visa2014-migration\catalogs\generate\ApplicationProfileCatalog-CalikEnergi.ps1

.EXAMPLE
  .\scripts\visa2014-migration\catalogs\generate\ApplicationProfileCatalog-CalikEnergi.ps1 -LegacySource calik-energi-local-pg
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
$defaultOut = Join-Path $repoRoot 'Visa2026.DataImporter\legacy\visa2014\preview-export\ApplicationProfileCatalog-proposal.calik-energi.xlsx'
if (-not $OutputPath) { $OutputPath = $defaultOut }
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

Write-Host '=== Wave 0b: ApplicationProfile catalog proposal (legacy VISA2015) ===' -ForegroundColor Cyan
Write-Host "INF Legacy source: $LegacySource"
Write-Host "INF Output: $OutputPath"

$dotnetArgs = @(
    'run', '--project', $importerProj, '--no-build',
    '--export-visa2014-preview',
    '--entity', 'ApplicationProfileCatalog',
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

Write-Host "OK Review workbook: $OutputPath" -ForegroundColor Green
Write-Host 'INF Fill Decision / SignOff on ApplicationProfiles sheet, then proceed to tenant JSON (Wave 1).'
