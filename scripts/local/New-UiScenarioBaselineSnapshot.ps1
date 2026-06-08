#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL] Create or refresh the UI scenario lookup baseline .bak snapshot.

.DESCRIPTION
  1. Greenfield seed (LookupCatalogs + security users, no Person/Application rows).
  2. BACKUP DATABASE to tools/UiScenarioRunner/baseline/Visa2026UiScenario-lookup-baseline.bak.

  Re-run after schema bumps or LookupCatalogs changes that affect scenario prerequisites.
  The .bak is gitignored; commit only this script and baseline/README.md.

.EXAMPLE
  .\scripts\local\New-UiScenarioBaselineSnapshot.ps1
#>
[CmdletBinding()]
param(
    [string]$BaselineBackupFile = '',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

. (Join-Path $PSScriptRoot 'UiScenarioDatabase.ps1')

Ensure-UiScenarioLocalDb

Write-Host '==> Greenfield seed before baseline capture' -ForegroundColor Cyan
Invoke-UiScenarioGreenfieldSeed -RepoRoot $RepoRoot -Configuration $Configuration -SkipBuild:$SkipBuild

$bakPath = if ($BaselineBackupFile -ne '') {
    if ([System.IO.Path]::IsPathRooted($BaselineBackupFile)) { $BaselineBackupFile } else { Join-Path $RepoRoot $BaselineBackupFile }
} else {
    Get-UiScenarioBaselinePath -RepoRoot $RepoRoot
}

Backup-UiScenarioDatabase -BackupFile $bakPath
Write-Host "Lookup baseline ready: $bakPath" -ForegroundColor Green
Write-Host 'Use Invoke-UiScenarioRun.ps1 -UseBaselineSnapshot for faster suite runs.' -ForegroundColor DarkGray
