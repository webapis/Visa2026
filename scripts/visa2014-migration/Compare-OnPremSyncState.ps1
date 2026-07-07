#Requires -Version 5.1
<#
.SYNOPSIS
  On-prem prod sync dashboard: legacy VISA2015 (.15) vs Visa2026DbProd (.25) scalar + FileData waves.

.DESCRIPTION
  Scalar: Legacy total, migrated (prod) total, not completed, id-map entries, sync status.
  FileData: document/photo waves (bootstrap from calik-energi .bak + optional -IncludeFileWaves).
  Reads sync watermark from sync-state/<legacy-source>.json when present.

  Real-time polling during sync: Watch-OnPremSyncState.ps1

.EXAMPLE
  $env:VISA2026_PROD_SQL_CONNECTION = 'Server=10.100.128.25\SQLEXPRESS;Database=Visa2026DbProd;...'
  .\scripts\visa2014-migration\Compare-OnPremSyncState.ps1

.EXAMPLE
  .\scripts\visa2014-migration\Compare-OnPremSyncState.ps1 -LegacySource calik-energi-onprem-prod -ShowNotes
#>
[CmdletBinding()]
param(
    [string]$LegacyServer = '10.100.128.15',
    [string]$LegacyDatabase = 'VISA2015',
    [string]$LegacyUser = 'ReadOnlyUser',
    [string]$LegacyPassword = '',
    [string]$TargetConnection = '',
    [string]$TargetServer = '10.100.128.25\SQLEXPRESS',
    [string]$TargetDatabase = 'Visa2026DbProd',
    [string]$TargetUser = 'sa',
    [string]$TargetPassword = '',
    [string]$LegacySource = 'calik-energi-onprem-prod',
    [switch]$ShowNotes
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_lib\Get-RepoRoot.ps1')
. (Join-Path $PSScriptRoot '_lib\OnPremSyncState.ps1')

$repoRoot = Get-Visa2026RepoRoot
$config = Resolve-OnPremSyncStateConfig `
    -LegacyServer $LegacyServer `
    -LegacyDatabase $LegacyDatabase `
    -LegacyUser $LegacyUser `
    -LegacyPassword $LegacyPassword `
    -TargetConnection $TargetConnection `
    -TargetServer $TargetServer `
    -TargetDatabase $TargetDatabase `
    -TargetUser $TargetUser `
    -TargetPassword $TargetPassword `
    -LegacySource $LegacySource `
    -RepoRoot $repoRoot

Write-Host "=== On-prem sync state ===" -ForegroundColor Cyan
Write-Host "Legacy: $($config.LegacyServer) / $($config.LegacyDatabase) (ReadOnlyUser)" -ForegroundColor DarkGray
Write-Host "Target: $($config.TargetServer) / $($config.TargetDatabase)" -ForegroundColor DarkGray
Write-Host "Id-maps: $($config.MapRoot)" -ForegroundColor DarkGray

$watermark = Get-OnPremSyncWatermark -Config $config
if ($watermark) {
    Write-Host "Sync watermark: $($config.SyncStatePath)" -ForegroundColor DarkGray
    Write-Host "  LastSuccessfulRunUtc: $watermark" -ForegroundColor DarkGray
}

Write-Host ''
Write-Host '--- Scalar business objects ---' -ForegroundColor Cyan
$scalar = Get-OnPremScalarSyncSnapshot -Config $config | ForEach-Object {
    $obj = [ordered]@{
        BO            = $_.BO
        Legacy        = $_.Legacy
        Migrated      = $_.Migrated
        NotCompleted  = $_.NotCompleted
        IdMap         = $_.IdMap
        ScalarSync    = $_.SyncState
    }
    if ($ShowNotes -and $_.Note) { $obj.Note = $_.Note }
    [pscustomobject]$obj
}
$scalar | Format-Table -AutoSize

Write-Host ''
Write-Host '--- FileData / document waves ---' -ForegroundColor Cyan
Write-Host 'File bytes on prod came from calik-energi bootstrap; prod -IncludeFileWaves delta not in scalar sync.' -ForegroundColor DarkGray
$files = Get-OnPremFileSyncSnapshot -Config $config | ForEach-Object {
    [pscustomobject]@{
        BO            = $_.BO
        LegacyScope   = $_.Legacy
        Migrated      = $_.Migrated
        NotCompleted  = $_.NotCompleted
        FileIdMap     = $_.IdMap
        FileDataSync  = $_.SyncState
    }
}
$files | Format-Table -AutoSize

Write-Host ''
Write-Host 'NotCompleted = max(0, Legacy - Migrated) for scalar; file LegacyScope uses legacy SQL or id-map when legacy SQL omitted.' -ForegroundColor DarkGray
Write-Host 'Real-time watch: .\scripts\visa2014-migration\Watch-OnPremSyncState.ps1' -ForegroundColor DarkGray
