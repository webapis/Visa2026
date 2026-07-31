#Requires -Version 5.1
<#
.SYNOPSIS
  Live console table of the current on-prem sync run (waves from sync-run-status.json).

.DESCRIPTION
  Polls sync-run-status.json and prints Overall + per-wave Status / counts as Format-Table.
  Use a second terminal while OnPrem-Sync.ps1 runs.

  On the sync host (.25):
    .\Watch-OnPremSyncRun.ps1 -SyncHostRoot C:\visa2026-sync -ClearScreen

  From a workstation (SSH):
    .\scripts\visa2014-migration\Watch-OnPremSyncRun.ps1 -ViaSsh -ClearScreen

.EXAMPLE
  .\scripts\visa2014-migration\Watch-OnPremSyncRun.ps1 -ViaSsh -IntervalSeconds 10 -ClearScreen

.EXAMPLE
  # Demo import on .25:
  .\scripts\visa2014-migration\Watch-OnPremSyncRun.ps1 -Profile Demo -ViaSsh -ClearScreen

.NOTES
  Prefer Watch-OnPremImportLive.ps1 for wave deltas + live DB counts.
#>
[CmdletBinding()]
param(
    [ValidateSet('Production', 'Staging', 'Demo')]
    [string]$Profile = 'Production',
    [string]$SyncHostRoot = '',
    [string]$StatusPath = '',
    [switch]$ViaSsh,
    [string]$SshHost = 'visa2026-onprem',
    [string]$RemoteStatusPath = '',
    [string]$RemoteOutLogHint = '',
    [int]$IntervalSeconds = 10,
    [int]$DurationMinutes = 0,
    [int]$TailLogLines = 8,
    [switch]$ClearScreen,
    [switch]$NoTail
)

$ErrorActionPreference = 'Stop'
$scriptDir = $PSScriptRoot
. (Join-Path $scriptDir '_lib\Get-OnPremSyncHostRoot.ps1')
. (Join-Path $scriptDir '_lib\OnPremSyncRunStatus.ps1')

if ([string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    $SyncHostRoot = Get-DefaultOnPremSyncHostRoot -Profile $Profile
}
if ([string]::IsNullOrWhiteSpace($RemoteStatusPath)) {
    $RemoteStatusPath = Join-Path $SyncHostRoot 'sync-run-status.json'
}
if ([string]::IsNullOrWhiteSpace($RemoteOutLogHint)) {
    $RemoteOutLogHint = Join-Path $SyncHostRoot 'logs\manual-sync-pid.txt'
}

function Resolve-LocalStatusPath {
    if (-not [string]::IsNullOrWhiteSpace($StatusPath)) {
        return (Resolve-Path -LiteralPath $StatusPath).Path
    }
    if (-not [string]::IsNullOrWhiteSpace($SyncHostRoot)) {
        $candidate = Get-OnPremSyncRunStatusPath -Root $SyncHostRoot
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
        return $candidate
    }
    if (Test-Path -LiteralPath 'C:\visa2026-sync\sync-run-status.json') {
        return 'C:\visa2026-sync\sync-run-status.json'
    }
    throw 'Pass -Profile / -SyncHostRoot, -StatusPath, or -ViaSsh.'
}

function Get-RemoteStatusJson {
    $remotePs = @"
`$ErrorActionPreference = 'Continue'
`$statusPath = '$RemoteStatusPath'
if (-not (Test-Path -LiteralPath `$statusPath)) { Write-Output 'STATUS_MISSING'; exit 0 }
Get-Content -LiteralPath `$statusPath -Raw -Encoding UTF8
Write-Output '---META---'
`$pidFile = '$RemoteOutLogHint'
if (Test-Path -LiteralPath `$pidFile) {
  `$lines = Get-Content -LiteralPath `$pidFile
  Write-Output ('PidFile=' + (`$lines -join '|'))
  `$parentId = 0
  if ([int]::TryParse((`$lines | Select-Object -First 1), [ref]`$parentId)) {
    `$alive = [bool](Get-Process -Id `$parentId -ErrorAction SilentlyContinue)
    Write-Output ('ParentAlive=' + `$alive)
  }
}
`$diList = @(Get-Process -Name 'Visa2026.DataImporter' -ErrorAction SilentlyContinue)
Write-Output ('DataImporter=' + [bool]`$diList)
if (`$diList.Count -gt 0) {
  Write-Output ('DataImporterPid=' + ((`$diList | Select-Object -ExpandProperty Id) -join ','))
  `$prodDi = `$null
  foreach (`$p in `$diList) {
    try {
      `$cmd = (Get-CimInstance Win32_Process -Filter ("ProcessId=" + `$p.Id) -ErrorAction SilentlyContinue).CommandLine
      if (`$cmd -and `$cmd -match 'calik-energi-onprem-prod') { `$prodDi = `$p; break }
    } catch {}
  }
  if (-not `$prodDi) { `$prodDi = `$diList | Sort-Object WorkingSet64 -Descending | Select-Object -First 1 }
  if (`$prodDi) {
    Write-Output ('DataImporterCpu=' + [math]::Round([double]`$prodDi.CPU, 1))
    Write-Output ('DataImporterWsMb=' + [math]::Round(`$prodDi.WorkingSet64 / 1MB))
  }
}
`$statusObj = `$null
try { `$statusObj = Get-Content -LiteralPath `$statusPath -Raw -Encoding UTF8 | ConvertFrom-Json } catch {}
`$wave = if (`$statusObj) { [string]`$statusObj.CurrentWave } else { '' }
if (`$wave) {
  `$roots = @('C:\visa2026-sync\data\id-maps\calik-energi-onprem-prod', 'C:\visa2026-sync-demo\data\id-maps\calik-energi-onprem-demo')
  foreach (`$root in `$roots) {
    `$prog = Join-Path `$root (`$wave + '.sync-progress.json')
    if (Test-Path -LiteralPath `$prog) {
      Write-Output ('ProgressJson=' + (Get-Content -LiteralPath `$prog -Raw -Encoding UTF8).Trim())
      break
    }
  }
}
if (`$wave -eq 'ApplicationItem') {
  try {
    `$n = & sqlcmd -S 'localhost\SQLEXPRESS' -d Visa2026DbProd -E -C -h -1 -W -Q 'SET NOCOUNT ON; SELECT COUNT(1) FROM ApplicationItems;' 2>`$null
    `$count = (`$n | Where-Object { `$_ -match '^\s*\d+\s*$' } | Select-Object -First 1)
    if (`$count) { Write-Output ('DbRowCount=' + `$count.Trim()) }
  } catch {}
}
"@
    $b64 = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($remotePs))
    $raw = & ssh -o BatchMode=yes $SshHost "powershell -NoProfile -EncodedCommand $b64" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SSH failed ($LASTEXITCODE): $raw"
    }
    $text = ($raw | Out-String)
    if ($text -match 'STATUS_MISSING') {
        return @{ Status = $null; Meta = @{} }
    }
    $parts = $text -split '---META---', 2
    $json = $parts[0].Trim()
    $meta = @{}
    if ($parts.Count -gt 1) {
        foreach ($line in ($parts[1] -split "`r?`n")) {
            if ($line -match '^(PidFile|ParentAlive|DataImporter|DataImporterPid|DataImporterCpu|DataImporterWsMb|DbRowCount)=(.*)$') {
                $meta[$Matches[1]] = $Matches[2].Trim()
            }
            elseif ($line -match '^ProgressJson=(.*)$') {
                $meta['ProgressJson'] = $Matches[1].Trim()
            }
        }
    }
    $status = $null
    if ($json) {
        try { $status = $json | ConvertFrom-Json } catch { $status = $null }
    }
    return @{ Status = $status; Meta = $meta }
}

function Get-LocalStatusBundle {
    param([string]$Path)
    $status = Read-OnPremSyncRunStatus -Path $Path
    $meta = @{
        ParentAlive   = ''
        DataImporter  = [bool](Get-Process -Name 'Visa2026.DataImporter' -ErrorAction SilentlyContinue)
    }
    $pidFile = Join-Path (Split-Path -Parent $Path) 'logs\manual-sync-pid.txt'
    if (-not (Test-Path -LiteralPath $pidFile)) {
        $alt = 'C:\visa2026-sync\logs\manual-sync-pid.txt'
        if (Test-Path -LiteralPath $alt) { $pidFile = $alt }
    }
    if (Test-Path -LiteralPath $pidFile) {
        $lines = Get-Content -LiteralPath $pidFile
        $meta['PidFile'] = ($lines -join '|')
        $parentId = 0
        if ([int]::TryParse(($lines | Select-Object -First 1), [ref]$parentId)) {
            $meta['ParentAlive'] = [bool](Get-Process -Id $parentId -ErrorAction SilentlyContinue)
        }
    }
    return @{ Status = $status; Meta = $meta }
}

function Get-RemoteWaveTail {
    param([string]$LogFile, [int]$Lines)
    if ([string]::IsNullOrWhiteSpace($LogFile) -or $Lines -le 0) { return @() }
    $escaped = $LogFile.Replace("'", "''")
    $remotePs = @"
`$p = '$escaped'
if (Test-Path -LiteralPath `$p) { Get-Content -LiteralPath `$p -Tail $Lines } else { ' (log not found)' }
"@
    $b64 = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($remotePs))
    & ssh -o BatchMode=yes $SshHost "powershell -NoProfile -EncodedCommand $b64" 2>$null
}

function Format-WaveRows {
    param($Status)
    if (-not $Status -or -not $Status.Waves) { return @() }

    $now = Get-Date
    foreach ($w in @($Status.Waves)) {
        $elapsed = ''
        if ($w.StartedUtc) {
            try {
                $start = [datetime]::Parse($w.StartedUtc).ToUniversalTime()
                $end = if ($w.CompletedUtc) {
                    [datetime]::Parse($w.CompletedUtc).ToUniversalTime()
                } else {
                    $now.ToUniversalTime()
                }
                $span = $end - $start
                if ($span.TotalHours -ge 1) {
                    $elapsed = '{0:h\:mm\:ss}' -f $span
                } else {
                    $elapsed = '{0:mm\:ss}' -f $span
                }
            } catch { $elapsed = '' }
        }

        [pscustomobject]@{
            Wave        = $w.Name
            Status      = $w.Status
            Exit        = $w.ExitCode
            Ins         = $w.Inserted
            Upd         = $w.Updated
            SoftDel     = $w.SoftDeleted
            Fail        = $w.Failed
            Legacy      = $w.LegacyRows
            Elapsed     = $elapsed
            Log         = $(if ($w.LogFile) { Split-Path -Leaf $w.LogFile } else { '' })
        }
    }
}

function Write-RunBanner {
    param($Status, $Meta, [datetime]$SampleTime, [int]$Index)

    Write-Host ("=== Sync run watch #{0} @ {1} ===" -f $Index, $SampleTime.ToString('yyyy-MM-dd HH:mm:ss')) -ForegroundColor Cyan
    if (-not $Status) {
        Write-Host 'No sync-run-status.json (or unreadable).' -ForegroundColor Yellow
        return
    }

    $summary = Get-OnPremSyncWaveSummary -RunStatus $Status
    Write-Host ("RunId: {0}  Overall: {1}  Current: {2}" -f $Status.RunId, $Status.OverallStatus, $Status.CurrentWave) -ForegroundColor White
    Write-Host ("Mode: Import  Profile: {0}" -f $Status.Profile) -ForegroundColor DarkGray
    Write-Host ("Started: {0}  Updated: {1}" -f $Status.StartedUtc, $Status.UpdatedUtc) -ForegroundColor DarkGray
    Write-Host ("Waves  Completed={0} Running={1} Failed={2} Pending={3}" -f `
        $summary.Completed, $summary.Running, $summary.Failed, $summary.Pending) -ForegroundColor DarkGray

    $di = $Meta['DataImporter']
    $pa = $Meta['ParentAlive']
    Write-Host ("Process  ParentAlive={0}  DataImporter={1}" -f $pa, $di) -ForegroundColor DarkGray
    if ($Meta['DataImporterCpu'] -or $Meta['DataImporterWsMb']) {
        Write-Host ("         CPU={0}s  WS={1} MB" -f $Meta['DataImporterCpu'], $Meta['DataImporterWsMb']) -ForegroundColor DarkGray
    }

    $progressLine = $null
    if ($Meta['ProgressJson']) {
        try {
            $p = $Meta['ProgressJson'] | ConvertFrom-Json
            $progressLine = ("LIVE {0}: {1}/{2} ({3}%)  upd={4} ins={5} skip={6} fail={7}  @{8}" -f `
                $p.entity, $p.processed, $p.total, $p.percent, $p.updated, $p.inserted, $p.skippedUnchanged, $p.failed, $p.utc)
        } catch {
            $progressLine = "LIVE progress: $($Meta['ProgressJson'])"
        }
    }
    elseif ($Status -and $Status.CurrentWave -and ($di -eq 'True' -or $di -eq $true)) {
        $hint = "LIVE $($Status.CurrentWave): working (log may stay quiet - stdout buffered until wave ends)"
        if ($Meta['DbRowCount']) {
            $hint += " | DB rows=$($Meta['DbRowCount'])"
        }
        $progressLine = $hint
    }
    if ($progressLine) {
        Write-Host $progressLine -ForegroundColor Yellow
    }

    Write-Host ("Interval: {0}s  |  Ctrl+C to stop" -f $IntervalSeconds) -ForegroundColor DarkGray
}

$localPath = $null
if (-not $ViaSsh) {
    $localPath = Resolve-LocalStatusPath
    Write-Host "INF Watching $localPath" -ForegroundColor Green
} else {
    Write-Host "INF Watching via SSH $SshHost -> $RemoteStatusPath" -ForegroundColor Green
}

$startedAt = Get-Date
$deadline = if ($DurationMinutes -gt 0) { $startedAt.AddMinutes($DurationMinutes) } else { $null }
$sampleIndex = 0

try {
    while ($true) {
        if ($deadline -and (Get-Date) -gt $deadline) {
            Write-Host "INF Duration limit reached ($DurationMinutes min)." -ForegroundColor Yellow
            break
        }

        $sampleIndex++
        $sampleTime = Get-Date
        $bundle = if ($ViaSsh) { Get-RemoteStatusJson } else { Get-LocalStatusBundle -Path $localPath }
        $status = $bundle.Status
        $meta = $bundle.Meta

        if ($ClearScreen) { Clear-Host }
        Write-RunBanner -Status $status -Meta $meta -SampleTime $sampleTime -Index $sampleIndex
        Write-Host ''

        $rows = Format-WaveRows -Status $status
        if ($rows.Count -gt 0) {
            $rows | Format-Table -AutoSize Wave, Status, Exit, Ins, Upd, SoftDel, Fail, Legacy, Elapsed, Log
        } else {
            Write-Host '(no waves)' -ForegroundColor Yellow
        }

        if (-not $NoTail -and $status -and $status.CurrentWave) {
            $current = @($status.Waves) | Where-Object { $_.Name -eq $status.CurrentWave } | Select-Object -First 1
            if ($current -and $current.LogFile) {
                Write-Host ("--- tail {0} ---" -f (Split-Path -Leaf $current.LogFile)) -ForegroundColor Cyan
                if ($ViaSsh) {
                    Get-RemoteWaveTail -LogFile $current.LogFile -Lines $TailLogLines | ForEach-Object { Write-Host $_ }
                } elseif (Test-Path -LiteralPath $current.LogFile) {
                    Get-Content -LiteralPath $current.LogFile -Tail $TailLogLines | ForEach-Object { Write-Host $_ }
                } else {
                    Write-Host ' (log path not reachable from this machine)' -ForegroundColor DarkYellow
                }
            }
        }

        $overall = if ($status) { [string]$status.OverallStatus } else { '' }
        if ($overall -in @('Completed', 'CompletedWithErrors', 'Failed') -and $meta['DataImporter'] -ne 'True' -and $meta['DataImporter'] -ne $true) {
            Write-Host ''
            Write-Host "INF Run finished ($overall). Exiting watch." -ForegroundColor Green
            break
        }

        Start-Sleep -Seconds $IntervalSeconds
    }
}
catch {
    Write-Host "ERR $($_.Exception.Message)" -ForegroundColor Red
    throw
}
finally {
    Write-Host 'INF Watch stopped.' -ForegroundColor Green
}
