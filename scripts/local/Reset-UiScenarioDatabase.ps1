#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL] Drop and re-create the UI scenario LocalDB (Visa2026UiScenario).

.DESCRIPTION
  Modes:
  - Greenfield (default): DROP + XAF --updateDatabase (LookupCatalogs + StandardUser, no transactional data).
  - RestoreBaseline: DROP + restore tools/UiScenarioRunner/baseline/Visa2026UiScenario-lookup-baseline.bak.
  - DropOnly: DROP only (used internally).

.PARAMETER Mode
  Greenfield | RestoreBaseline | DropOnly

.PARAMETER BaselineBackupFile
  Override path to the lookup baseline .bak (gitignored).

.EXAMPLE
  .\scripts\local\Reset-UiScenarioDatabase.ps1

.EXAMPLE
  .\scripts\local\Reset-UiScenarioDatabase.ps1 -Mode RestoreBaseline
#>
[CmdletBinding()]
param(
    [ValidateSet('Greenfield', 'RestoreBaseline', 'DropOnly')]
    [string]$Mode = 'Greenfield',

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

if ($Mode -eq 'DropOnly') {
    Remove-UiScenarioDatabase
    exit 0
}

Remove-UiScenarioDatabase

if ($Mode -eq 'RestoreBaseline') {
    $bakPath = if ($BaselineBackupFile -ne '') {
        if ([System.IO.Path]::IsPathRooted($BaselineBackupFile)) { $BaselineBackupFile } else { Join-Path $RepoRoot $BaselineBackupFile }
    } else {
        Get-UiScenarioBaselinePath -RepoRoot $RepoRoot
    }

    if (-not (Test-Path -LiteralPath $bakPath)) {
        Write-Warning "Baseline not found: $bakPath - falling back to Greenfield seed."
        Invoke-UiScenarioGreenfieldSeed -RepoRoot $RepoRoot -Configuration $Configuration -SkipBuild:$SkipBuild
        exit 0
    }

    Write-Host "==> Restoring lookup baseline from $bakPath" -ForegroundColor Cyan
    & (Join-Path $RepoRoot 'migration-scripts/Restore-BackupToLocalDb.ps1') `
        -BackupFile $bakPath `
        -ServerInstance $script:UiScenarioServerInstance `
        -TargetDatabase $script:UiScenarioDatabaseName
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Baseline restored into $($script:UiScenarioDatabaseName)." -ForegroundColor Green
    exit 0
}

Invoke-UiScenarioGreenfieldSeed -RepoRoot $RepoRoot -Configuration $Configuration -SkipBuild:$SkipBuild
