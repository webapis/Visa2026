#Requires -Version 5.1
<#
.SYNOPSIS
  PATCH Application.ApplicationProfile on already-imported applications (Wave 2 backfill).

.EXAMPLE
  .\scripts\visa2014-migration\patch\Application-Profile.ps1 -LegacySource calik-energi-local-pg -DryRun
  .\scripts\visa2014-migration\patch\Application-Profile.ps1 -LegacySource calik-energi-local-pg -TargetConnection $env:ConnectionStrings__DefaultConnection
#>
[CmdletBinding()]
param(
    [string]$LegacySource = "calik-energi-local-pg",
    [string]$TargetConnection = $(if ($env:ConnectionStrings__DefaultConnection) { $env:ConnectionStrings__DefaultConnection } else { "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres" }),
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$DryRun
)

. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
$ErrorActionPreference = "Stop"
Set-Location $repoRoot

$mapPath = Join-Path $repoRoot "Visa2026.DataImporter/legacy/visa2014/id-maps/$LegacySource/ApplicationProfileInstance.json"
$binMapPath = Join-Path $repoRoot "Visa2026.DataImporter/bin/$Configuration/net8.0/legacy/visa2014/id-maps/$LegacySource/ApplicationProfileInstance.json"
if ((Test-Path $binMapPath) -and ((Get-Item $binMapPath).Length -gt 10)) {
    $mapPath = $binMapPath
}
elseif (-not (Test-Path $mapPath) -or ((Get-Item $mapPath).Length -le 10)) {
    throw "Application id-map not found or empty. Expected: $mapPath or $binMapPath"
}

$patchArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
    "--patch-visa2014-application-profile",
    "--legacy-source", $LegacySource,
    "--application-id-map", $mapPath,
    "--target-connection", $TargetConnection
)
$skipReport = Join-Path $repoRoot "docs/VISA2014_MIGRATION/analysis/application-profile-patch-skips.md"
$patchArgs += "--skip-report", $skipReport
if ($DryRun) { $patchArgs += "--dry-run" }

Write-Host "=== Build DataImporter ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.DataImporter/Visa2026.DataImporter.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Host "=== PATCH Application.ApplicationProfile (Wave 2) ===" -ForegroundColor Cyan
& dotnet @patchArgs
exit $LASTEXITCODE
