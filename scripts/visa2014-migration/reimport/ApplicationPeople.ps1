#Requires -Version 5.1
<#
.SYNOPSIS
  Delete imported ApplicationPerson rows (PG), clear id-map, reimport Wave 2b roster.

.EXAMPLE
  .\scripts\visa2014-migration\reimport\ApplicationPeople.ps1
  .\scripts\visa2014-migration\reimport\ApplicationPeople.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres",
    [string]$LegacySource = "calik-energi",
    [int]$BatchSize = 50,
    [int]$MaxRows = 0,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

$sqlScript = Join-Path $PSScriptRoot "..\cleanup\ImportedApplicationPeople.postgres.sql"
$dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
$mapRoot = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/$LegacySource"
$logDir = Join-Path $dataImporterRoot "legacy/visa2014/import-logs"
New-Item -ItemType Directory -Force -Path $logDir, $mapRoot | Out-Null

Write-Host "=== Delete imported ApplicationPerson rows (Postgres) ===" -ForegroundColor Cyan
if (-not $DryRun) {
    $env:PGPASSWORD = 'Visa2026Local'
    & psql -h localhost -p 5432 -U postgres -d visa2026 -v ON_ERROR_STOP=1 -f $sqlScript
    if ($LASTEXITCODE -ne 0) { throw "Cleanup SQL failed" }
    $mapPath = Join-Path $mapRoot "ApplicationProfileInstancePerson.json"
    if (Test-Path $mapPath) { Remove-Item $mapPath -Force }
}

if ($DryRun) {
    Write-Host "DryRun: skipping ApplicationPerson import." -ForegroundColor Yellow
    exit 0
}

& (Join-Path $PSScriptRoot "..\import\ApplicationPeople.ps1") `
    -TargetConnection $TargetConnection `
    -LegacySource $LegacySource `
    -BatchSize $BatchSize `
    -MaxRows $MaxRows `
    -Configuration $Configuration
exit $LASTEXITCODE