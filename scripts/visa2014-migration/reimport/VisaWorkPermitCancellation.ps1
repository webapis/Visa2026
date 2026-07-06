#Requires -Version 5.1
<#
.SYNOPSIS
  Dev reimport: Visa + WorkPermitItem with legacy IsCancelled backfill, then ApplicationItem FK relink.

.EXAMPLE
  .\scripts\visa2014-migration\reimport\VisaWorkPermitCancellation.ps1 -DryRun
  .\scripts\visa2014-migration\reimport\VisaWorkPermitCancellation.ps1
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = "Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    [string]$LegacySource = "calik-energi",
    [int]$BatchSize = 50,
    [int]$MaxRows = 0,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$DryRun,
    [switch]$SkipApplicationItems
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

$dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
$mapRoot = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/$LegacySource"
$logDir = Join-Path $dataImporterRoot "legacy/visa2014/import-logs"
$sqlScript = Join-Path $PSScriptRoot "..\cleanup\ImportedVisaWorkPermitCancellationBackfill.sql"
New-Item -ItemType Directory -Force -Path $logDir, $mapRoot | Out-Null

$idMapFiles = @("Visa.json", "VisaDocument.json", "WorkPermitItem.json")

Write-Host "=== Stop running importers ===" -ForegroundColor Cyan
Get-Process -Name "Visa2026.DataImporter" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "=== SQL cleanup (Visas + WorkPermitItems) ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -i $sqlScript -W -b
if ($LASTEXITCODE -ne 0) { throw "SQL cleanup failed (exit $LASTEXITCODE)" }

Write-Host "=== Clear Visa / WorkPermitItem id-maps ===" -ForegroundColor Cyan
foreach ($file in $idMapFiles) {
    $path = Join-Path $mapRoot $file
    if (Test-Path $path) {
        Remove-Item $path -Force
        Write-Host "  removed $file"
    }
}

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.DataImporter/Visa2026.DataImporter.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

if ($DryRun) {
    Write-Host "DryRun: cleanup + id-map clear done; no import." -ForegroundColor Yellow
    exit 0
}

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

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $logFile = Join-Path $logDir "reimport-$Entity-cancellation-$stamp.log"
    Write-Host "=== Import $Entity ===" -ForegroundColor Cyan
    & dotnet @importArgs 2>&1 | Tee-Object -FilePath $logFile
    if ($LASTEXITCODE -ne 0) { throw "Import $Entity failed (exit $LASTEXITCODE)" }
}

Invoke-Visa2014Import -Entity "Visa" -ExtraArgs @(
    "--id-map-output", (Join-Path $mapRoot "Visa.json"),
    "--passport-id-map", (Join-Path $mapRoot "Passport.json")
)

Invoke-Visa2014Import -Entity "WorkPermitItem" -ExtraArgs @(
    "--id-map-output", (Join-Path $mapRoot "WorkPermitItem.json"),
    "--person-id-map", (Join-Path $mapRoot "Person.json"),
    "--passport-id-map", (Join-Path $mapRoot "Passport.json"),
    "--position-history-id-map", (Join-Path $mapRoot "EmployeePositionHistory.json"),
    "--work-permit-id-map", (Join-Path $mapRoot "WorkPermit.json")
)

if (-not $SkipApplicationItems) {
    Write-Host "=== Reimport ApplicationItems (relink CurrentVisa / CurrentWorkPermitItem) ===" -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "ApplicationItems.ps1") `
        -TargetConnection $TargetConnection `
        -LegacySource $LegacySource `
        -BatchSize $BatchSize `
        -MaxRows $MaxRows `
        -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw "ApplicationItems reimport failed (exit $LASTEXITCODE)" }
}

Write-Host "=== Reconcile cancelled flags ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -E -C -W -Q "SET NOCOUNT ON; SELECT COUNT(*) AS visas_cancelled FROM Visas WHERE IsCancelled = 1; SELECT COUNT(*) AS work_permit_items_cancelled FROM WorkPermitItems WHERE IsCancelled = 1; SELECT COUNT(*) AS app_items_wp_cancelled FROM ApplicationItems WHERE IsCancelled = 1; SELECT COUNT(*) AS app_items_visa_cancelled FROM ApplicationItems WHERE VisaIsCancelled = 1;"

Write-Host "=== Visa + WorkPermit cancellation reimport complete ===" -ForegroundColor Green