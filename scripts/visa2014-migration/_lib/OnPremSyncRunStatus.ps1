# Live Import-run status JSON for on-prem legacy Import (sync-run-status.json).

function Get-OnPremSyncRunStatusPath {
    param([string]$Root)
    Join-Path $Root 'sync-run-status.json'
}

function Resolve-OnPremSyncStatusRoot {
    param(
        [string]$SyncHostRoot = '',
        [string]$RepoRoot = ''
    )

    if (-not [string]::IsNullOrWhiteSpace($SyncHostRoot)) {
        return (Resolve-Path -LiteralPath $SyncHostRoot).Path
    }
    if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) {
        return Join-Path $RepoRoot 'Visa2026.DataImporter/legacy/visa2014'
    }
    throw 'SyncHostRoot or RepoRoot required for sync status paths.'
}

function Read-OnPremSyncRunStatus {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    try {
        return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Write-OnPremSyncRunStatus {
    param(
        [string]$Path,
        $Status
    )

    $parent = Split-Path -Parent $Path
    if ($parent -and -not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    $Status.UpdatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    $Status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Initialize-OnPremSyncRunStatus {
    param(
        [string]$Root,
        [string]$RunId,
        [string]$LegacySource,
        [string]$Profile,
        [string[]]$WaveNames,
        [string]$TaskLog = ''
    )

    $startedUtc = (Get-Date).ToUniversalTime().ToString('o')
    $waves = @($WaveNames | ForEach-Object {
        [ordered]@{
            Name         = $_
            Status       = 'Pending'
            StartedUtc   = $null
            CompletedUtc = $null
            ExitCode     = $null
            LogFile      = $null
            Inserted     = $null
            Updated      = $null
            SoftDeleted  = $null
            Failed       = $null
            LegacyRows   = $null
        }
    })

    $status = [ordered]@{
        Version       = 1
        RunId         = $RunId
        StartedUtc    = $startedUtc
        UpdatedUtc    = $startedUtc
        CompletedUtc  = $null
        Mode          = 'Import'
        LegacySource  = $LegacySource
        Profile       = $Profile
        OverallStatus = 'Running'
        CurrentWave   = $null
        TaskLog       = $TaskLog
        Waves         = $waves
    }

    $path = Get-OnPremSyncRunStatusPath -Root $Root
    Write-OnPremSyncRunStatus -Path $path -Status ([pscustomobject]$status)
    return $path
}

function Set-OnPremSyncRunWaveStarted {
    param(
        [string]$Root,
        [string]$WaveName,
        [string]$LogFile
    )

    $path = Get-OnPremSyncRunStatusPath -Root $Root
    $status = Read-OnPremSyncRunStatus -Path $path
    if (-not $status) { return }

    $status.CurrentWave = $WaveName
    $status.OverallStatus = 'Running'
    foreach ($wave in @($status.Waves)) {
        if ($wave.Name -eq $WaveName) {
            $wave | Add-Member -NotePropertyName Status -NotePropertyValue 'Running' -Force
            $wave | Add-Member -NotePropertyName StartedUtc -NotePropertyValue ((Get-Date).ToUniversalTime().ToString('o')) -Force
            $wave | Add-Member -NotePropertyName LogFile -NotePropertyValue $LogFile -Force
            break
        }
    }
    Write-OnPremSyncRunStatus -Path $path -Status $status
}

function Get-OnPremWaveLogStats {
    param([string]$LogFile)

    $stats = @{
        Inserted    = $null
        Updated     = $null
        SoftDeleted = $null
        Failed      = $null
        LegacyRows  = $null
    }
    if (-not $LogFile -or -not (Test-Path -LiteralPath $LogFile)) { return $stats }

    $text = Get-Content -LiteralPath $LogFile -Raw -ErrorAction SilentlyContinue
    if (-not $text) { return $stats }

    if ($text -match 'INF Inserted:\s*(\d+)\s+Updated:\s*(\d+)') {
        $stats.Inserted = [int]$Matches[1]
        $stats.Updated = [int]$Matches[2]
    }
    if ($text -match 'Soft-deleted:\s*(\d+)') {
        $stats.SoftDeleted = [int]$Matches[1]
    }
    if ($text -match 'Failed:\s*(\d+)') {
        $stats.Failed = [int]$Matches[1]
    }
    if ($text -match 'INF (\S+) legacy rows:\s*(\d+)') {
        $stats.LegacyRows = [int]$Matches[2]
    }
    return $stats
}

function Set-OnPremSyncRunWaveCompleted {
    param(
        [string]$Root,
        [string]$WaveName,
        [int]$ExitCode,
        [string]$LogFile = ''
    )

    $path = Get-OnPremSyncRunStatusPath -Root $Root
    $status = Read-OnPremSyncRunStatus -Path $path
    if (-not $status) { return }

    $logPath = if ($LogFile) { $LogFile } else { $null }
    $stats = Get-OnPremWaveLogStats -LogFile $logPath
    $waveStatus = if ($ExitCode -eq 0) { 'Completed' } else { 'Failed' }

    foreach ($wave in @($status.Waves)) {
        if ($wave.Name -eq $WaveName) {
            $wave | Add-Member -NotePropertyName Status -NotePropertyValue $waveStatus -Force
            $wave | Add-Member -NotePropertyName CompletedUtc -NotePropertyValue ((Get-Date).ToUniversalTime().ToString('o')) -Force
            $wave | Add-Member -NotePropertyName ExitCode -NotePropertyValue $ExitCode -Force
            if ($logPath) { $wave | Add-Member -NotePropertyName LogFile -NotePropertyValue $logPath -Force }
            foreach ($key in $stats.Keys) {
                if ($null -ne $stats[$key]) {
                    $wave | Add-Member -NotePropertyName $key -NotePropertyValue $stats[$key] -Force
                }
            }
            break
        }
    }

    if ($status.CurrentWave -eq $WaveName) {
        $status.CurrentWave = $null
    }
    Write-OnPremSyncRunStatus -Path $path -Status $status
}

function Complete-OnPremSyncRunStatus {
    param(
        [string]$Root,
        [ValidateSet('Completed', 'Failed')]
        [string]$OverallStatus
    )

    $path = Get-OnPremSyncRunStatusPath -Root $Root
    $status = Read-OnPremSyncRunStatus -Path $path
    if (-not $status) { return }

    $status | Add-Member -NotePropertyName OverallStatus -NotePropertyValue $OverallStatus -Force
    $status | Add-Member -NotePropertyName CurrentWave -NotePropertyValue $null -Force
    $status | Add-Member -NotePropertyName CompletedUtc -NotePropertyValue ((Get-Date).ToUniversalTime().ToString('o')) -Force
    Write-OnPremSyncRunStatus -Path $path -Status $status
}

function Get-OnPremSyncWaveSummary {
    param($RunStatus)

    $summary = @{
        Pending   = 0
        Running   = 0
        Completed = 0
        Failed    = 0
    }
    if (-not $RunStatus -or -not $RunStatus.Waves) { return $summary }

    foreach ($wave in $RunStatus.Waves) {
        switch ($wave.Status) {
            'Pending' { $summary.Pending++ }
            'Running' { $summary.Running++ }
            'Completed' { $summary.Completed++ }
            'Failed' { $summary.Failed++ }
        }
    }
    return $summary
}
