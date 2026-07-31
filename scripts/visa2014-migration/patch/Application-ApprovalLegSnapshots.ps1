#Requires -Version 5.1
<#
.SYNOPSIS
  Backfill Application.ApprovalLegSnapshots from ApprovalLegProfile (fills Ministrlik on progress).

.DESCRIPTION
  Safe for already-migrated data: does NOT delete or regenerate ApplicationProgress rows.
  Use when progress history exists but Ministrlik / status suffix is blank.

  Prefer this over patch/ApplicationProgress-MinistryLegs.ps1 when ministry progress steps
  already exist and only snapshots are missing.

.EXAMPLE
  .\scripts\visa2014-migration\patch\Application-ApprovalLegSnapshots.ps1 -DryRun
  .\scripts\visa2014-migration\patch\Application-ApprovalLegSnapshots.ps1
  .\scripts\visa2014-migration\patch\Application-ApprovalLegSnapshots.ps1 -TargetConnection "Server=localhost\SQLEXPRESS;Database=Visa2026DbProd;..."
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = "Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

Write-Host "=== Build DataImporter ($Configuration) ===" -ForegroundColor Cyan
dotnet build (Join-Path $repoRoot "Visa2026.DataImporter\Visa2026.DataImporter.csproj") -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$args = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--no-build", "--",
    "--backfill-application-approval-leg-snapshots",
    "--target-connection", $TargetConnection,
    "--verbose"
)
if ($DryRun) { $args += "--dry-run" }

Write-Host "=== ApprovalLegSnapshot backfill (Ministrlik) ===" -ForegroundColor Cyan
& dotnet @args
exit $LASTEXITCODE