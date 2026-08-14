#Requires -Version 5.1
<#
.SYNOPSIS
  Import VISA2014 ApplicationPerson roster (Wave 2b) after Application + Person id-maps exist.

.EXAMPLE
  .\scripts\visa2014-migration\import\ApplicationPeople.ps1 -MaxRows 50
  .\scripts\visa2014-migration\import\ApplicationPeople.ps1
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

$dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
$mapRoot = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/$LegacySource"
$logDir = Join-Path $dataImporterRoot "legacy/visa2014/import-logs"
New-Item -ItemType Directory -Force -Path $logDir, $mapRoot | Out-Null

$importArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
    "--import-visa2014", "--inprocess", "--entity", "ApplicationProfileInstancePerson",
    "--legacy-source", $LegacySource,
    "--target-connection", $TargetConnection,
    "--batch-size", $BatchSize,
    "--id-map-output", (Join-Path $mapRoot "ApplicationProfileInstancePerson.json"),
    "--application-id-map", (Join-Path $mapRoot "ApplicationProfileInstance.json"),
    "--person-id-map", (Join-Path $mapRoot "Person.json"),
    "--no-wait"
)
if ($MaxRows -gt 0) { $importArgs += @("--max-rows", $MaxRows) }
if ($DryRun) { $importArgs += "--dry-run" }

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.slnx -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path $logDir "import-ApplicationPerson-$stamp.log"
Write-Host "=== Import ApplicationPerson (in-process, Wave 2b) ===" -ForegroundColor Cyan
Write-Host "Logging to $logFile"
& dotnet @importArgs 2>&1 | Tee-Object -FilePath $logFile
exit $LASTEXITCODE