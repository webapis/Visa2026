#Requires -Version 5.1
<#
.SYNOPSIS
  Snapshot the current Import run (sync-run-status.json + target DbCounts) into history/.

.DESCRIPTION
  Use after a completed/failed Import when the orchestrator did not archive yet,
  or to refresh DbCounts for the latest RunId (-Force).

.EXAMPLE
  .\scripts\visa2014-migration\Archive-OnPremImportRun.ps1 -Profile Demo
#>
[CmdletBinding()]
param(
    [ValidateSet('Production', 'Staging', 'Demo')]
    [string]$Profile = 'Demo',
    [string]$SyncHostRoot = '',
    [string]$RunId = '',
    [switch]$Force,
    [switch]$SkipDbCounts
)

$ErrorActionPreference = 'Stop'

function Resolve-LibPath {
    param([string]$Name)
    foreach ($c in @(
            (Join-Path $PSScriptRoot "_lib\$Name"),
            (Join-Path $PSScriptRoot "..\_lib\$Name")
        )) {
        if (Test-Path -LiteralPath $c) { return $c }
    }
    throw "Lib not found: $Name"
}

. (Resolve-LibPath 'Get-OnPremSyncHostRoot.ps1')
. (Resolve-LibPath 'OnPremSyncRunStatus.ps1')
. (Resolve-LibPath 'OnPremImportRunArchive.ps1')

if ([string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    $SyncHostRoot = Get-DefaultOnPremSyncHostRoot -Profile $Profile
}

$dir = Save-OnPremImportRunArchive `
    -SyncHostRoot $SyncHostRoot `
    -Profile $Profile `
    -RunId $RunId `
    -SkipDbCounts:$SkipDbCounts `
    -Force:$Force

if (-not $dir) { exit 1 }
Write-Host "INF Done: $dir" -ForegroundColor Green
exit 0
