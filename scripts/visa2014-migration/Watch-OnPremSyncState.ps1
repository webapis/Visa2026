#Requires -Version 5.1
<#
.SYNOPSIS
  Poll legacy vs prod row counts and log sync state per business object in real time.

.DESCRIPTION
  Runs Compare-OnPremSyncState logic on an interval while a sync is in progress.
  Logs CSV snapshots (append) and optionally refreshes a console dashboard.
  DeltaMigrated shows change in prod count since the previous sample.

.EXAMPLE
  # Second terminal while OnPrem-Sync.ps1 runs:
  .\scripts\visa2014-migration\Watch-OnPremSyncState.ps1 -IntervalSeconds 30

.EXAMPLE
  .\scripts\visa2014-migration\Watch-OnPremSyncState.ps1 -IncludeFileData -LogPath C:\temp\sync-watch.csv
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
    [switch]$LoadProdConnectionFromSsh,
    [string]$SshHost = 'visa2026-onprem',
    [int]$IntervalSeconds = 30,
    [int]$DurationMinutes = 0,
    [string]$LogPath = '',
    [switch]$IncludeFileData,
    [switch]$ClearScreen,
    [switch]$NoConsole
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_lib\Get-RepoRoot.ps1')
. (Join-Path $PSScriptRoot '_lib\OnPremSyncState.ps1')

$repoRoot = Get-Visa2026RepoRoot
if ($LoadProdConnectionFromSsh) {
    Set-OnPremProdConnectionFromSsh -SshHost $SshHost | Out-Null
}
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

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $logDir = Join-Path $repoRoot 'Visa2026.DataImporter/legacy/visa2014/import-logs'
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    $LogPath = Join-Path $logDir "sync-state-watch-$(Get-Date -Format yyyyMMdd-HHmmss).csv"
}
else {
    $logParent = Split-Path -Parent $LogPath
    if ($logParent -and -not (Test-Path -LiteralPath $logParent)) {
        New-Item -ItemType Directory -Force -Path $logParent | Out-Null
    }
}

$csvHeader = 'SampleUtc,Kind,BO,Legacy,Migrated,NotCompleted,IdMap,SyncState,DeltaMigrated,ElapsedSec'
if (-not (Test-Path -LiteralPath $LogPath)) {
    Set-Content -LiteralPath $LogPath -Value $csvHeader -Encoding UTF8
}

$previousMigrated = @{}
$sampleIndex = 0
$startedAt = Get-Date
$deadline = if ($DurationMinutes -gt 0) { $startedAt.AddMinutes($DurationMinutes) } else { $null }

function Write-SyncWatchBanner {
    param([datetime]$SampleTime, [int]$Index, [string]$Watermark)

    $wm = if ($Watermark) { $Watermark } else { '(none)' }
    Write-Host "=== On-prem sync watch #$Index @ $($SampleTime.ToString('yyyy-MM-dd HH:mm:ss')) ===" -ForegroundColor Cyan
    Write-Host "Legacy: $($config.LegacyServer) / $($config.LegacyDatabase)  ->  Target: $($config.TargetServer) / $($config.TargetDatabase)" -ForegroundColor DarkGray
    Write-Host "Id-maps: $($config.MapRoot)" -ForegroundColor DarkGray
    Write-Host "Sync watermark LastSuccessfulRunUtc: $wm" -ForegroundColor DarkGray
    Write-Host "Log: $LogPath" -ForegroundColor DarkGray
    Write-Host "Interval: ${IntervalSeconds}s  |  Ctrl+C to stop" -ForegroundColor DarkGray
}

if (-not $NoConsole) {
    Write-Host "INF Watching sync state (logging to $LogPath)" -ForegroundColor Green
}

Test-OnPremSqlConnections -Config $config

$watchFailed = $false
try {
    while ($true) {
        if ($deadline -and (Get-Date) -gt $deadline) {
            Write-Host "INF Duration limit reached ($DurationMinutes min)." -ForegroundColor Yellow
            break
        }

        $sampleIndex++
        $sampleTime = Get-Date
        $sampleUtc = $sampleTime.ToUniversalTime().ToString('o')
        $elapsedSec = [int](($sampleTime - $startedAt).TotalSeconds)
        $watermark = Get-OnPremSyncWatermark -Config $config

        $rows = Get-OnPremSyncStateSnapshot -Config $config -IncludeFileData:$IncludeFileData

        $csvLines = New-Object System.Collections.Generic.List[string]
        foreach ($row in $rows) {
            $boKey = "$($row.Kind)|$($row.BO)"
            $delta = 0
            if ($null -ne $row.Migrated) {
                if ($previousMigrated.ContainsKey($boKey)) {
                    $delta = $row.Migrated - $previousMigrated[$boKey]
                }
                $previousMigrated[$boKey] = $row.Migrated
            }

            $row | Add-Member -NotePropertyName DeltaMigrated -NotePropertyValue $delta -Force
            $row | Add-Member -NotePropertyName SampleUtc -NotePropertyValue $sampleUtc -Force
            $row | Add-Member -NotePropertyName ElapsedSec -NotePropertyValue $elapsedSec -Force

            $legacyStr = if ($null -eq $row.Legacy) { '' } else { $row.Legacy }
            $migratedStr = if ($null -eq $row.Migrated) { '' } else { $row.Migrated }
            $notCompletedStr = if ($null -eq $row.NotCompleted) { '' } else { $row.NotCompleted }
            $idMapStr = if ($null -eq $row.IdMap) { '' } else { $row.IdMap }
            $syncStateEsc = ($row.SyncState -replace '"', '""')

            $csvLines.Add(
                "$sampleUtc,$($row.Kind),$($row.BO),$legacyStr,$migratedStr,$notCompletedStr,$idMapStr,`"$syncStateEsc`",$delta,$elapsedSec"
            )
        }

        Add-Content -LiteralPath $LogPath -Value $csvLines -Encoding UTF8

        if (-not $NoConsole) {
            if ($ClearScreen) { Clear-Host }
            Write-SyncWatchBanner -SampleTime $sampleTime -Index $sampleIndex -Watermark $watermark
            Write-Host ''
            $rows | Where-Object { $_.Kind -eq 'Scalar' } |
                Select-Object BO, Legacy, Migrated, NotCompleted, IdMap, DeltaMigrated, SyncState |
                Format-Table -AutoSize
            if ($IncludeFileData) {
                Write-Host ''
                Write-Host '--- FileData ---' -ForegroundColor Cyan
                $rows | Where-Object { $_.Kind -eq 'FileData' } |
                    Select-Object BO, Legacy, Migrated, NotCompleted, IdMap, DeltaMigrated, SyncState |
                    Format-Table -AutoSize
            }
        }

        if ($deadline -and (Get-Date) -gt $deadline) { break }
        Start-Sleep -Seconds $IntervalSeconds
    }
}
catch {
    $watchFailed = $true
    Write-Host "ERR $($_.Exception.Message)" -ForegroundColor Red
    throw
}
finally {
    if (-not $watchFailed -and -not $NoConsole) {
        Write-Host "INF Stopped. Log: $LogPath" -ForegroundColor Green
    }
}
