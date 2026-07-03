#Requires -Version 5.1
<#
.SYNOPSIS
  Backfill ApprovalLeg snapshots and regenerate ApplicationProgress for via-ministry apps missing ministry steps.

.EXAMPLE
  .\scripts\visa2014-migration\patch\ApplicationProgress-MinistryLegs.ps1
  .\scripts\visa2014-migration\patch\ApplicationProgress-MinistryLegs.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = "Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    [string]$LegacySource = "calik-energi",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

Write-Host "=== Stop running importers / Blazor host ===" -ForegroundColor Cyan
Get-Process -Name "Visa2026.DataImporter" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "Visa2026.Blazor.Server" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "=== Build DataImporter ($Configuration) ===" -ForegroundColor Cyan
dotnet build (Join-Path $repoRoot "Visa2026.DataImporter\Visa2026.DataImporter.csproj") -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$args = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
    "--correct-application-progress-ministry-legs",
    "--legacy-source", $LegacySource,
    "--target-connection", $TargetConnection,
    "--verbose"
)
if ($DryRun) { $args += "--dry-run" }

& dotnet @args
exit $LASTEXITCODE