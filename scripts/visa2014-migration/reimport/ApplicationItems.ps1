#Requires -Version 5.1
<#
.SYNOPSIS
  Delete imported ApplicationItem rows, rebuild parent id-maps from target DB, reimport ApplicationItem, run post-import corrections.

.EXAMPLE
  .\scripts\visa2014-migration\reimport\ApplicationItems.ps1
  .\scripts\visa2014-migration\reimport\ApplicationItems.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = "Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    [string]$LegacySource = "calik-energi",
    [int]$BatchSize = 50,
    [int]$MaxRows = 0,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$DryRun,
    [switch]$SkipCorrections
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

$dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
$mapRoot = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/$LegacySource"
$logDir = Join-Path $dataImporterRoot "legacy/visa2014/import-logs"
$sqlScript = Join-Path $PSScriptRoot "..\cleanup\ImportedApplicationItems.sql"
New-Item -ItemType Directory -Force -Path $logDir, $mapRoot | Out-Null

Write-Host "=== Stop running importers ===" -ForegroundColor Cyan
Get-Process -Name "Visa2026.DataImporter" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "=== Delete imported ApplicationItem rows ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -i $sqlScript -W -b
if ($LASTEXITCODE -ne 0) { throw "SQL cleanup failed (exit $LASTEXITCODE)" }

$itemMap = Join-Path $mapRoot "ApplicationItem.json"
if (Test-Path $itemMap) {
    Remove-Item $itemMap -Force
    Write-Host "Removed id-map: $itemMap"
}

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.slnx -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Host "=== Rebuild parent id-maps from target DB ===" -ForegroundColor Cyan
$rebuildArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
    "--rebuild-visa2014-id-maps",
    "--legacy-source", $LegacySource,
    "--target-connection", $TargetConnection,
    "--verbose"
)
& dotnet @rebuildArgs
if ($LASTEXITCODE -ne 0) { throw "Id-map rebuild failed (exit $LASTEXITCODE)" }

if ($DryRun) {
    Write-Host "DryRun: skipping ApplicationItem import and corrections." -ForegroundColor Yellow
    exit 0
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path $logDir "reimport-ApplicationItem-$stamp.log"
Write-Host "=== Reimport ApplicationItem (in-process) ===" -ForegroundColor Cyan
Write-Host "Logging to $logFile"

$importArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
    "--import-visa2014", "--inprocess", "--entity", "ApplicationItem",
    "--legacy-source", $LegacySource,
    "--target-connection", $TargetConnection,
    "--batch-size", $BatchSize,
    "--id-map-output", $itemMap,
    "--application-id-map", (Join-Path $mapRoot "Application.json"),
    "--person-id-map", (Join-Path $mapRoot "Person.json"),
    "--passport-id-map", (Join-Path $mapRoot "Passport.json"),
    "--visa-id-map", (Join-Path $mapRoot "Visa.json"),
    "--position-history-id-map", (Join-Path $mapRoot "EmployeePositionHistory.json"),
    "--address-id-map", (Join-Path $mapRoot "AddressOfResidence.json"),
    "--education-id-map", (Join-Path $mapRoot "Education.json"),
    "--employee-salary-id-map", (Join-Path $mapRoot "EmployeeSalary.json"),
    "--work-permit-item-id-map", (Join-Path $mapRoot "WorkPermitItem.json"),
    "--invitation-item-id-map", (Join-Path $mapRoot "InvitationItem.json"),
    "--no-wait"
)
if ($MaxRows -gt 0) { $importArgs += @("--max-rows", $MaxRows) }

& dotnet @importArgs 2>&1 | Tee-Object -FilePath $logFile
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($SkipCorrections) {
    Write-Host "SkipCorrections set - done after import." -ForegroundColor Yellow
    exit 0
}

Write-Host "=== Post-import corrections ===" -ForegroundColor Cyan
$corrections = @(
    @{ Name = "PersonAddressPia"; Flag = "--correct-person-address-of-residence" },
    @{ Name = "ApplicationItemPersonCurrent"; Flag = "--correct-application-item-person-current" }
)
foreach ($corr in $corrections) {
    $corrLog = Join-Path $logDir "reimport-post-$($corr.Name)-$stamp.log"
    Write-Host "--- $($corr.Name) ---" -ForegroundColor Cyan
    $corrArgs = @(
        "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
        $corr.Flag, "--legacy-source", $LegacySource,
        "--target-connection", $TargetConnection, "--verbose"
    )
    & dotnet @corrArgs 2>&1 | Tee-Object -FilePath $corrLog
    if ($LASTEXITCODE -ne 0) { throw "Correction $($corr.Name) failed (exit $LASTEXITCODE)" }
}

Write-Host "=== Reimport complete ===" -ForegroundColor Green