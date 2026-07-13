#Requires -Version 5.1
<#
.SYNOPSIS
  Compare two archived on-prem Import reimport runs (DbCount + wave outcomes).

.DESCRIPTION
  Reads immutable snapshots under <SyncHostRoot>\history\runs\<RunId>\.
  Default Left/Right = second-latest / latest archived runs.
  Writes compare HTML under history\ and prints anomalies.
  Exit 2 when -FailOnAnomaly and anomalies are found.

  On .25 (or after Install-OnPremSyncHost copies scripts):
    C:\visa2026-sync-demo\tools\scripts\Compare-OnPremImportRuns.ps1 -Profile Demo

.EXAMPLE
  .\scripts\visa2014-migration\Compare-OnPremImportRuns.ps1 -Profile Demo

.EXAMPLE
  .\scripts\visa2014-migration\Compare-OnPremImportRuns.ps1 -Profile Demo `
    -Left 20260710-214856 -Right 20260712-212802 -FailOnAnomaly
#>
[CmdletBinding()]
param(
    [ValidateSet('Production', 'Staging', 'Demo')]
    [string]$Profile = 'Demo',
    [string]$SyncHostRoot = '',
    [string]$Left = '',
    [string]$Right = '',
    [int]$AbsoluteCountThreshold = 20,
    [double]$RelativePercentThreshold = 1.0,
    [switch]$FailOnAnomaly
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
. (Resolve-LibPath 'OnPremImportRunArchive.ps1')

if ([string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    $SyncHostRoot = Get-DefaultOnPremSyncHostRoot -Profile $Profile
}

$runs = @(Get-OnPremImportRunArchiveList -SyncHostRoot $SyncHostRoot)
if ($runs.Count -eq 0) {
    throw "No archived Import runs under $(Get-OnPremImportRunHistoryRoot -SyncHostRoot $SyncHostRoot)\runs. Run an Import first (or Archive-OnPremImportRun.ps1)."
}

if ([string]::IsNullOrWhiteSpace($Right)) {
    $Right = $runs[0].RunId
}
if ([string]::IsNullOrWhiteSpace($Left)) {
    if ($runs.Count -lt 2) {
        throw "Only one archived run ($Right). Need a second run to compare (or pass -Left)."
    }
    $Left = $runs[1].RunId
}

Write-Host "=== Compare Import reimports ($Profile) ===" -ForegroundColor Cyan
Write-Host "INF Left:  $Left"
Write-Host "INF Right: $Right"
Write-Host "INF Thresholds: |??|>=$AbsoluteCountThreshold and |??%|>=$RelativePercentThreshold%"

$leftArch = Read-OnPremImportRunArchive -SyncHostRoot $SyncHostRoot -RunId $Left
$rightArch = Read-OnPremImportRunArchive -SyncHostRoot $SyncHostRoot -RunId $Right
$cmp = Compare-OnPremImportRunArchives `
    -Left $leftArch `
    -Right $rightArch `
    -AbsoluteCountThreshold $AbsoluteCountThreshold `
    -RelativePercentThreshold $RelativePercentThreshold

$html = Write-OnPremImportCompareHtml -SyncHostRoot $SyncHostRoot -CompareResult $cmp

Write-Host ""
Write-Host "--- Target DB counts ---" -ForegroundColor Cyan
$cmp.BoRows | Format-Table BO, Left, Right, Delta, AbsPct, Anomaly -AutoSize

Write-Host "--- Waves ---" -ForegroundColor Cyan
$cmp.WaveRows | Format-Table Wave, LeftStatus, RightStatus, LeftFailed, RightFailed, Regressed -AutoSize

Write-Host ""
if ($cmp.AnomalyCount -eq 0) {
    Write-Host "INF No anomalies under thresholds." -ForegroundColor Green
}
else {
    Write-Host "WRN Anomalies ($($cmp.AnomalyCount)):" -ForegroundColor Yellow
    $cmp.Anomalies | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

Write-Host "INF HTML: $html" -ForegroundColor DarkGray
Write-Host ("INF Index: {0}" -f (Join-Path (Get-OnPremImportRunHistoryRoot -SyncHostRoot $SyncHostRoot) 'index.html')) -ForegroundColor DarkGray

if ($FailOnAnomaly -and $cmp.AnomalyCount -gt 0) {
    exit 2
}
exit 0
