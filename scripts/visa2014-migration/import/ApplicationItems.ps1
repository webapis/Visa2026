#Requires -Version 5.1
<#
.SYNOPSIS
  Import VISA2014 ApplicationItem rows (in-process) after Application id-map exists.

.EXAMPLE
  .\scripts\visa2014-migration\import/ApplicationItems.ps1 -MaxRows 50
  .\scripts\visa2014-migration\import/ApplicationItems.ps1
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
New-Item -ItemType Directory -Force -Path $logDir, $mapRoot | Out-Null

$importArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
    "--import-visa2014", "--inprocess", "--entity", "ApplicationItem",
    "--legacy-source", $LegacySource,
    "--target-connection", $TargetConnection,
    "--batch-size", $BatchSize,
    "--id-map-output", (Join-Path $mapRoot "ApplicationItem.json"),
    "--application-id-map", (Join-Path $mapRoot "Application.json"),
    "--person-id-map", (Join-Path $mapRoot "Person.json"),
    "--passport-id-map", (Join-Path $mapRoot "Passport.json"),
    "--visa-id-map", (Join-Path $mapRoot "Visa.json"),
    "--position-history-id-map", (Join-Path $mapRoot "EmployeePositionHistory.json"),
    "--address-id-map", (Join-Path $mapRoot "AddressOfResidence.json"),
    "--education-id-map", (Join-Path $mapRoot "Education.json"),
    "--employee-salary-id-map", (Join-Path $mapRoot "EmployeeSalary.json"),
    "--no-wait"
)
if ($MaxRows -gt 0) { $importArgs += @("--max-rows", $MaxRows) }
if ($DryRun) { $importArgs += "--dry-run" }

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.slnx -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path $logDir "import-ApplicationItem-$stamp.log"
Write-Host "=== Import ApplicationItem (in-process) ===" -ForegroundColor Cyan
Write-Host "Logging to $logFile"
& dotnet @importArgs 2>&1 | Tee-Object -FilePath $logFile
exit $LASTEXITCODE
