#Requires -Version 5.1
<#
.SYNOPSIS
  PATCH Application.ApprovalLegProfile on already-imported applications from legacy ministry inference.

.EXAMPLE
  .\scripts\visa2014-migration\Patch-Visa2014ApplicationApprovalLegProfile.ps1 -DryRun
  .\scripts\visa2014-migration\Patch-Visa2014ApplicationApprovalLegProfile.ps1 -ApiBaseUrl "https://localhost:5001"
#>
[CmdletBinding()]
param(
    [string]$LegacySource = "calik-energi",
    [string]$ApiBaseUrl = "https://localhost:5001",
    [string]$ImportUser = "Admin",
    [string]$ImportPassword = $(if ($env:VISA2026_IMPORT_PASSWORD) 
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
{ $env:VISA2026_IMPORT_PASSWORD } else { "" }),
    [int]$MaxRows = 0,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$DryRun,
    [switch]$NoWait
)

$ErrorActionPreference = "Stop"
Set-Location $repoRoot

if (-not $DryRun -and [string]::IsNullOrWhiteSpace($ImportPassword)) {
    throw "Set VISA2026_IMPORT_PASSWORD or pass -ImportPassword for OData PATCH."
}

$mapPath = Join-Path $repoRoot "Visa2026.DataImporter/legacy/visa2014/id-maps/$LegacySource/ApplicationProfileInstance.json"
if (-not (Test-Path $mapPath)) {
    throw "Application id-map not found: $mapPath"
}

$patchArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
    "--patch-visa2014-application-approval-leg-profile",
    "--legacy-source", $LegacySource,
    "--application-id-map", $mapPath,
    "--api-base-url", $ApiBaseUrl,
    "--user", $ImportUser,
    "--password", $ImportPassword
)
if ($MaxRows -gt 0) { $patchArgs += @("--max-rows", $MaxRows) }
if ($DryRun) { $patchArgs += "--dry-run" }
if ($NoWait) { $patchArgs += "--no-wait" }

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.slnx -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Host "=== PATCH Application.ApprovalLegProfile ===" -ForegroundColor Cyan
& dotnet @patchArgs
exit $LASTEXITCODE
