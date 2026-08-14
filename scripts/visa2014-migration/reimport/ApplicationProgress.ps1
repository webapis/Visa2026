#Requires -Version 5.1
<#
.SYNOPSIS
  Delete imported ApplicationProgress rows, clear id-map, reimport ApplicationProgress (dev only).

.EXAMPLE
  .\scripts\visa2014-migration\reimport\ApplicationProgress.ps1
  .\scripts\visa2014-migration\reimport\ApplicationProgress.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = "Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    [string]$LegacySource = "calik-energi",
    [int]$BatchSize = 50,
    [int]$MaxRows = 0,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

$dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
$mapRoot = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/$LegacySource"
$logDir = Join-Path $dataImporterRoot "legacy/visa2014/import-logs"
$sqlScript = Join-Path $PSScriptRoot "..\cleanup\ImportedApplicationProgress.sql"
New-Item -ItemType Directory -Force -Path $logDir, $mapRoot | Out-Null

Write-Host "=== Stop running importers / Blazor host (file locks) ===" -ForegroundColor Cyan
Get-Process -Name "Visa2026.DataImporter" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "Visa2026.Blazor.Server" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "=== Delete imported ApplicationProgress rows ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -i $sqlScript -W -b
if ($LASTEXITCODE -ne 0) { throw "SQL cleanup failed (exit $LASTEXITCODE)" }

$progressMap = Join-Path $mapRoot "ApplicationProfileInstanceProgress.json"
if (Test-Path $progressMap) {
    Remove-Item $progressMap -Force
    Write-Host "Removed id-map: $progressMap"
}

Write-Host "=== Build DataImporter ($Configuration) ===" -ForegroundColor Cyan
dotnet build (Join-Path $repoRoot "Visa2026.DataImporter\Visa2026.DataImporter.csproj") -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

if ($DryRun) {
    Write-Host "DryRun: transform + payload check only." -ForegroundColor Yellow
    $dryArgs = @(
        "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
        "--import-visa2014", "--entity", "ApplicationProfileInstanceProgress",
        "--legacy-source", $LegacySource,
        "--target-connection", $TargetConnection,
        "--dry-run", "--verbose"
    )
    if ($MaxRows -gt 0) { $dryArgs += @("--max-rows", $MaxRows) }
    & dotnet @dryArgs
    exit $LASTEXITCODE
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path $logDir "reimport-ApplicationProgress-$stamp.log"
Write-Host "=== Reimport ApplicationProgress (in-process) ===" -ForegroundColor Cyan
Write-Host "Logging to $logFile"

$importArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
    "--import-visa2014", "--inprocess",
    "--entity", "ApplicationProfileInstanceProgress",
    "--legacy-source", $LegacySource,
    "--target-connection", $TargetConnection,
    "--batch-size", $BatchSize,
    "--id-map-output", $progressMap,
    "--application-id-map", (Join-Path $mapRoot "ApplicationProfileInstance.json"),
    "--verbose"
)
if ($MaxRows -gt 0) { $importArgs += @("--max-rows", $MaxRows) }

& dotnet @importArgs 2>&1 | Tee-Object -FilePath $logFile
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "=== Reimport complete ===" -ForegroundColor Green