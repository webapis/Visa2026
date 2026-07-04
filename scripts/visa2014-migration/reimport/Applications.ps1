#Requires -Version 5.1
<#
.SYNOPSIS
  Delete VISA2014-imported Application data, clear migration logs/id-map, reimport Application (in-process).

.EXAMPLE
  .\scripts\visa2014-migration\reimport\Applications.ps1
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = "Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    [string]$LegacySource = "calik-energi",
    [int]$BatchSize = 50,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipImport
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

$dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
$mapPath = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/$LegacySource/Application.json"
$logDir = Join-Path $dataImporterRoot "legacy/visa2014/import-logs"
$sqlScript = Join-Path $PSScriptRoot "..\cleanup\ImportedApplications.sql"

Write-Host "=== Stop running importers ===" -ForegroundColor Cyan
Get-Process -Name "Visa2026.DataImporter" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "=== Delete imported Application rows (IsManualEntry=1) ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -i $sqlScript -W -b
if ($LASTEXITCODE -ne 0) { throw "SQL cleanup failed (exit $LASTEXITCODE)" }

Write-Host "=== Clear migration artifacts ===" -ForegroundColor Cyan
if (Test-Path $mapPath) {
    Remove-Item $mapPath -Force
    Write-Host "Removed id-map: $mapPath"
}
if (Test-Path $logDir) {
    Get-ChildItem $logDir -Filter "*.log" -ErrorAction SilentlyContinue | Remove-Item -Force
    Write-Host "Cleared import-logs/*.log"
}
Get-ChildItem (Join-Path $repoRoot "Visa2026.DataImporter/bin/$Configuration/net8.0") -Filter "import_*.log" -ErrorAction SilentlyContinue | Remove-Item -Force

if ($SkipImport) {
    Write-Host "SkipImport set - done after cleanup."
    exit 0
}

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.slnx -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Host "=== Reimport Application (in-process) ===" -ForegroundColor Cyan
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$logFile = Join-Path $logDir "reimport-Application-$stamp.log"

$importArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
    "--import-visa2014", "--inprocess", "--entity", "Application",
    "--legacy-source", $LegacySource,
    "--id-map-output", $mapPath,
    "--target-connection", $TargetConnection,
    "--batch-size", $BatchSize,
    "--no-wait"
)

Write-Host "Logging to $logFile"
& dotnet @importArgs 2>&1 | Tee-Object -FilePath $logFile
