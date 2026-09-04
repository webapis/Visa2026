#Requires -Version 5.1
<#
.SYNOPSIS
  Import VISA2014 Invitation headers then InvitationItem rows (in-process).

.EXAMPLE
  .\scripts\visa2014-migration\import\Invitations.ps1 -MaxRows 50
  .\scripts\visa2014-migration\import\Invitations.ps1
  .\scripts\visa2014-migration\import\Invitations.ps1 -DryRun
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
    [switch]$HeadersOnly,
    [switch]$ItemsOnly
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
    $logFile = Join-Path $logDir "import-$Entity-$stamp.log"
    Write-Host "=== Import $Entity (in-process) ===" -ForegroundColor Cyan
    Write-Host "Logging to $logFile"
    & dotnet @importArgs 2>&1 | Tee-Object -FilePath $logFile
    if ($LASTEXITCODE -ne 0) { throw "Import $Entity failed (exit $LASTEXITCODE)" }
}

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.DataImporter/Visa2026.DataImporter.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

if (-not $ItemsOnly) {
    Invoke-Visa2014Import -Entity "Invitation" -ExtraArgs @(
        "--id-map-output", (Join-Path $mapRoot "Invitation.json"),
        "--application-id-map", (Join-Path $mapRoot "ApplicationProfileInstance.json")
    )
}

if (-not $HeadersOnly) {
    Invoke-Visa2014Import -Entity "InvitationItem" -ExtraArgs @(
        "--id-map-output", (Join-Path $mapRoot "InvitationItem.json"),
        "--person-id-map", (Join-Path $mapRoot "Person.json"),
        "--passport-id-map", (Join-Path $mapRoot "Passport.json"),
        "--invitation-id-map", (Join-Path $mapRoot "Invitation.json")
    )
}

Write-Host "Done." -ForegroundColor Green