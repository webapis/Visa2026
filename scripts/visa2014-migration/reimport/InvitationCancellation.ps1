#Requires -Version 5.1
<#
.SYNOPSIS
  Dev reimport: InvitationItem with legacy IsCancelled backfill, then ApplicationItem FK relink.

.EXAMPLE
  .\scripts\visa2014-migration\reimport\InvitationCancellation.ps1 -DryRun
  .\scripts\visa2014-migration\reimport\InvitationCancellation.ps1
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
$sqlScript = Join-Path $PSScriptRoot "..\cleanup\ImportedInvitationItemCancellationBackfill.sql"
New-Item -ItemType Directory -Force -Path $logDir, $mapRoot | Out-Null

Write-Host "=== Stop running importers ===" -ForegroundColor Cyan
Get-Process -Name "Visa2026.DataImporter" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "=== SQL cleanup (InvitationItems) ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -i $sqlScript -W -b
if ($LASTEXITCODE -ne 0) { throw "SQL cleanup failed (exit $LASTEXITCODE)" }

Write-Host "=== Clear InvitationItem id-map ===" -ForegroundColor Cyan
$invitationItemMap = Join-Path $mapRoot "InvitationItem.json"
if (Test-Path $invitationItemMap) {
    Remove-Item $invitationItemMap -Force
    Write-Host "  removed InvitationItem.json"
}

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.DataImporter/Visa2026.DataImporter.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

if ($DryRun) {
    Write-Host "DryRun: cleanup + id-map clear done; no import." -ForegroundColor Yellow
    exit 0
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path $logDir "reimport-InvitationItem-cancellation-$stamp.log"
Write-Host "=== Import InvitationItem ===" -ForegroundColor Cyan
$importArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
    "--import-visa2014", "--inprocess", "--entity", "InvitationItem",
    "--legacy-source", $LegacySource,
    "--target-connection", $TargetConnection,
    "--batch-size", $BatchSize,
    "--no-wait",
    "--id-map-output", (Join-Path $mapRoot "InvitationItem.json"),
    "--person-id-map", (Join-Path $mapRoot "Person.json"),
    "--passport-id-map", (Join-Path $mapRoot "Passport.json"),
    "--invitation-id-map", (Join-Path $mapRoot "Invitation.json")
)
if ($MaxRows -gt 0) { $importArgs += @("--max-rows", $MaxRows) }

& dotnet @importArgs 2>&1 | Tee-Object -FilePath $logFile
if ($LASTEXITCODE -ne 0) { throw "Import InvitationItem failed (exit $LASTEXITCODE)" }

Write-Host "=== Skip ApplicationItem relink (Phase B hard-remove; roster uses ResolvedLinks) ===" -ForegroundColor Yellow

Write-Host "=== Reconcile cancelled flags ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -E -C -W -Q "SET NOCOUNT ON; SELECT COUNT(*) AS invitation_items_cancelled FROM InvitationItems WHERE IsCancelled = 1; SELECT COUNT(*) AS app_items_inv_cancelled FROM ApplicationItems WHERE InvitationItemIsCancelled = 1;"

Write-Host "=== Invitation cancellation reimport complete ===" -ForegroundColor Green