#Requires -Version 5.1
<#
.SYNOPSIS
  Supplement soft-deleted WorkHistoryOfEmployee rows for WorkPermit.Position, then re-import missing WorkPermitItems.

.EXAMPLE
  .\scripts\visa2014-migration\patch\WorkPermitItem-SupplementPositions.ps1 -DryRun
  .\scripts\visa2014-migration\patch\WorkPermitItem-SupplementPositions.ps1
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

function Invoke-Visa2014Import {
    param(
        [string]$Entity,
        [string[]]$ExtraArgs
    )
    $importArgs = @(
        "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
        "--import-visa2014", "--inprocess", "--entity", $Entity,
        "--legacy-source", $LegacySource,
        "--target-connection", $TargetConnection,
        "--batch-size", $BatchSize,
        "--no-wait"
    ) + $ExtraArgs
    if ($MaxRows -gt 0) { $importArgs += @("--max-rows", $MaxRows) }
    if ($DryRun) { $importArgs += "--dry-run" }

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $logFile = Join-Path $logDir "patch-$Entity-$stamp.log"
    Write-Host "=== Patch $Entity ===" -ForegroundColor Cyan
    Write-Host "Logging to $logFile"
    & dotnet @importArgs 2>&1 | Tee-Object -FilePath $logFile
    if ($LASTEXITCODE -ne 0) { throw "Import $Entity failed (exit $LASTEXITCODE)" }
}

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.DataImporter/Visa2026.DataImporter.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Invoke-Visa2014Import -Entity "EmployeePositionHistory" -ExtraArgs @(
    "--supplement-permit-positions",
    "--id-map-output", (Join-Path $mapRoot "EmployeePositionHistory.json"),
    "--person-id-map", (Join-Path $mapRoot "Person.json")
)

Invoke-Visa2014Import -Entity "WorkPermitItem" -ExtraArgs @(
    "--id-map-output", (Join-Path $mapRoot "WorkPermitItem.json"),
    "--person-id-map", (Join-Path $mapRoot "Person.json"),
    "--passport-id-map", (Join-Path $mapRoot "Passport.json"),
    "--position-history-id-map", (Join-Path $mapRoot "EmployeePositionHistory.json"),
    "--work-permit-id-map", (Join-Path $mapRoot "WorkPermit.json")
)

Write-Host "Done. Re-run ApplicationItems reimport if new WorkPermitItem rows were posted." -ForegroundColor Green