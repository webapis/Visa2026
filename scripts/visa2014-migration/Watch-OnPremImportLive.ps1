#Requires -Version 5.1
<#
.SYNOPSIS
  Live incremental table of on-prem VISA2014 import/sync progress (waves + DB counts).

.DESCRIPTION
  Polls sync-run-status.json and target SQL row counts; prints a colored wave
  table (Completed=green, Running=cyan, Failed=red, Pending=gray) with
  per-sample deltas (DeltaIns / DeltaUpd / DeltaDb) and a live percent bar from
  {entity}.sync-progress.json (updated every ~100 rows; survives stdout buffering).

  Profiles map to sync-host root + database:
    Production  C:\visa2026-sync       Visa2026DbProd
    Staging     C:\visa2026-sync-staging Visa2026DbStaging
    Demo        C:\visa2026-sync-demo  Visa2026DbDemo

.EXAMPLE
  # Workstation — watch Demo import (current fresh migrate):
  .\scripts\visa2014-migration\Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh -ClearScreen

.EXAMPLE
  # On .25 while Demo import runs:
  C:\visa2026-sync-demo\tools\scripts\Watch-OnPremImportLive.ps1 -Profile Demo -ClearScreen

.EXAMPLE
  # Production catch-up waves only (skip SQL counts):
  .\scripts\visa2014-migration\Watch-OnPremImportLive.ps1 -Profile Production -ViaSsh -NoDbCounts -ClearScreen
#>
[CmdletBinding()]
param(
    [ValidateSet('Production', 'Staging', 'Demo')]
    [string]$Profile = 'Demo',

    [string]$SyncHostRoot = '',
    [switch]$ViaSsh,
    [string]$SshHost = 'visa2026-onprem',

    [int]$IntervalSeconds = 10,
    [int]$DurationMinutes = 0,
    [int]$TailLogLines = 6,

    [switch]$ClearScreen,
    [switch]$NoTail,
    [switch]$NoDbCounts,
    [switch]$OnlyRunningAndRecent,
    [switch]$Once
)

$ErrorActionPreference = 'Stop'
$scriptDir = $PSScriptRoot
. (Join-Path $scriptDir '_lib\Get-OnPremSyncHostRoot.ps1')
. (Join-Path $scriptDir '_lib\OnPremSyncRunStatus.ps1')

if ([string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    $SyncHostRoot = Get-DefaultOnPremSyncHostRoot -Profile $Profile
}

$dbName = switch ($Profile) {
    'Staging' { 'Visa2026DbStaging' }
    'Demo' { 'Visa2026DbDemo' }
    default { 'Visa2026DbProd' }
}

$statusPath = Join-Path $SyncHostRoot 'sync-run-status.json'
$importLogDir = Join-Path $SyncHostRoot 'data\import-logs'
$diPathHint = Join-Path $SyncHostRoot 'tools\DataImporter'

# BO display name -> SQL table (active rows: GCRecord IS NULL OR GCRecord = 0)
$script:DbCountMap = [ordered]@{
    Person                   = 'People'
    Passport                 = 'Passports'
    Visa                     = 'Visas'
    Education                = 'Educations'
    EmployeePositionHistory  = 'EmployeePositionHistories'
    AddressOfResidence       = 'AddressesOfResidence'
    EmployeeSalary           = 'EmployeeSalaries'
    MedicalRecord            = 'MedicalRecords'
    Application              = 'Applications'
    WorkPermit               = 'WorkPermits'
    WorkPermitItem           = 'WorkPermitItems'
    Invitation               = 'Invitations'
    InvitationItem           = 'InvitationItems'
    ApplicationItem          = 'ApplicationItems'
    ApplicationProgress      = 'ApplicationProgresses'
}

$script:PrevWave = @{}   # Name -> @{ Ins; Upd; Fail }
$script:PrevDb   = @{}   # BO -> count
$script:SampleIndex = 0

function Invoke-RemoteEncoded {
    param([string]$ScriptText)
    $b64 = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($ScriptText))
    $raw = & ssh -o BatchMode=yes -o ConnectTimeout=20 $SshHost "powershell -NoProfile -EncodedCommand $b64" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SSH failed ($LASTEXITCODE): $raw"
    }
    return ($raw | Out-String)
}

function Ensure-RemoteSnapshotHelper {
    $localHelper = Join-Path $scriptDir '_lib\Get-OnPremImportLiveSnapshot.ps1'
    if (-not (Test-Path -LiteralPath $localHelper)) {
        throw "Missing helper: $localHelper"
    }
    & scp -q $localHelper "${SshHost}:C:/visa2026-deploy/Get-OnPremImportLiveSnapshot.ps1"
    if ($LASTEXITCODE -ne 0) { throw "scp helper failed ($LASTEXITCODE)" }
    return 'C:\visa2026-deploy\Get-OnPremImportLiveSnapshot.ps1'
}

function Get-RemoteSnapshot {
    param([string]$RemoteHelper)

    $localStatus = Join-Path $env:TEMP ("visa2026-watch-{0}-status.json" -f $Profile.ToLowerInvariant())
    $remoteStatus = Join-Path $SyncHostRoot 'sync-run-status.json'
    # SCP status file (small); do not embed it in SSH JSON
    & scp -q "${SshHost}:$($remoteStatus.Replace('\','/'))" $localStatus 2>$null
    $statusJson = ''
    if ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $localStatus)) {
        $statusJson = Get-Content -LiteralPath $localStatus -Raw -Encoding UTF8
    }

    $noDbSwitch = if ($NoDbCounts) { '-NoDbCounts' } else { '' }
    $cmd = "powershell -NoProfile -ExecutionPolicy Bypass -File `"$RemoteHelper`" -Profile $Profile -SyncHostRoot `"$SyncHostRoot`" $noDbSwitch"
    $raw = & ssh -o BatchMode=yes -o ConnectTimeout=20 $SshHost $cmd 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SSH snapshot failed ($LASTEXITCODE): $raw"
    }
    $text = ($raw | Out-String)
    $jsonLine = ($text -split "`r?`n" | Where-Object { $_.Trim().StartsWith('{') } | Select-Object -Last 1)
    if (-not $jsonLine) { throw "Remote snapshot returned no JSON. Raw: $text" }
    $meta = $jsonLine | ConvertFrom-Json

    $dbCounts = ''
    if ($meta.DbCountLines) {
        $dbCounts = (@($meta.DbCountLines) -join "`n")
    }

    $diJson = '[]'
    if ($meta.DataImporters) {
        $diJson = ($meta.DataImporters | ConvertTo-Json -Compress -Depth 3)
        if (-not $diJson) { $diJson = '[]' }
        # single object -> not array
        if ($diJson.Trim().StartsWith('{')) { $diJson = "[$diJson]" }
    }

    return [pscustomobject]@{
        StatusJson     = $statusJson
        DataImporters  = $diJson
        TaskState      = [string]$meta.TaskState
        TaskLastResult = [string]$meta.TaskLastResult
        DbCounts       = $dbCounts
        ProgressJson   = [string]$meta.ProgressJson
    }
}

function Get-LocalSnapshot {
    $helper = Join-Path $scriptDir '_lib\Get-OnPremImportLiveSnapshot.ps1'
    $argList = @('-Profile', $Profile, '-SyncHostRoot', $SyncHostRoot)
    if ($NoDbCounts) { $argList += '-NoDbCounts' }
    $raw = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $helper @argList
    $text = ($raw | Out-String)
    $jsonLine = ($text -split "`r?`n" | Where-Object { $_.Trim().StartsWith('{') } | Select-Object -Last 1)
    if (-not $jsonLine) { throw "Local snapshot returned no JSON." }
    $meta = $jsonLine | ConvertFrom-Json

    $statusJson = ''
    $sp = Join-Path $SyncHostRoot 'sync-run-status.json'
    if (Test-Path -LiteralPath $sp) {
        $statusJson = Get-Content -LiteralPath $sp -Raw -Encoding UTF8
    }

    $dbCounts = ''
    if ($meta.DbCountLines) {
        $dbCounts = (@($meta.DbCountLines) -join "`n")
    }
    $diJson = '[]'
    if ($meta.DataImporters) {
        $diJson = ($meta.DataImporters | ConvertTo-Json -Compress -Depth 3)
        if (-not $diJson) { $diJson = '[]' }
        if ($diJson.Trim().StartsWith('{')) { $diJson = "[$diJson]" }
    }

    return [pscustomobject]@{
        StatusJson     = $statusJson
        DataImporters  = $diJson
        TaskState      = [string]$meta.TaskState
        TaskLastResult = [string]$meta.TaskLastResult
        DbCounts       = $dbCounts
        ProgressJson   = [string]$meta.ProgressJson
    }
}

function Parse-DbCounts {
    param([string]$Text)
    $map = @{}
    if ([string]::IsNullOrWhiteSpace($Text)) { return $map }
    foreach ($line in ($Text -split "`r?`n")) {
        $trim = $line.Trim()
        if ($trim -match '^(\w+)\|(\d+)$') {
            $map[$Matches[1]] = [int]$Matches[2]
        }
        elseif ($trim -match '^(\w+)\s+(\d+)$') {
            $map[$Matches[1]] = [int]$Matches[2]
        }
    }
    return $map
}

function Format-Elapsed([string]$StartedUtc, [string]$CompletedUtc) {
    if (-not $StartedUtc) { return '' }
    try {
        $start = [datetime]::Parse($StartedUtc).ToUniversalTime()
        $end = if ($CompletedUtc) {
            [datetime]::Parse($CompletedUtc).ToUniversalTime()
        } else {
            (Get-Date).ToUniversalTime()
        }
        $span = $end - $start
        if ($span.TotalHours -ge 1) { return ('{0:h\:mm\:ss}' -f $span) }
        return ('{0:mm\:ss}' -f $span)
    } catch { return '' }
}

function Get-ProgressHint {
    param([string]$LogFile, [int]$Lines)
    if ([string]::IsNullOrWhiteSpace($LogFile) -or $Lines -le 0) { return @() }

    if ($ViaSsh) {
        $escaped = $LogFile.Replace("'", "''")
        $remotePs = @"
`$p = '$escaped'
if (-not (Test-Path -LiteralPath `$p)) { ' (log not found)'; return }
Get-Content -LiteralPath `$p -Tail $Lines | Where-Object {
  `$_ -match 'INF Progress:' -or `$_ -match 'INF Inserted:' -or `$_ -match 'ERR ' -or `$_ -match 'legacy rows:' -or `$_ -match 'posted'
}
"@
        $raw = Invoke-RemoteEncoded -ScriptText $remotePs
        return @($raw -split "`r?`n" | Where-Object { $_.Trim() -and $_ -notmatch '^\s*$' } | Select-Object -Last $Lines)
    }

    if (-not (Test-Path -LiteralPath $LogFile)) { return @('(log not found)') }
    return @(Get-Content -LiteralPath $LogFile -Tail 40 |
        Where-Object { $_ -match 'INF Progress:|INF Inserted:|ERR |legacy rows:|posted' } |
        Select-Object -Last $Lines)
}

function Get-StatusColor {
    param([string]$Status)
    switch -Regex ($Status) {
        '^(Completed|ok)$' { return 'Green' }
        '^(Failed|Fail)$' { return 'Red' }
        '^(Running)$' { return 'Cyan' }
        '^(Skipped)$' { return 'DarkYellow' }
        '^(Pending)$' { return 'DarkGray' }
        'CompletedWithErrors' { return 'Yellow' }
        default { return 'Gray' }
    }
}

function Format-ProgressBar {
    param(
        [double]$Percent,
        [int]$Width = 24
    )
    if ($Percent -lt 0) { $Percent = 0 }
    if ($Percent -gt 100) { $Percent = 100 }
    $filled = [int][math]::Round($Width * $Percent / 100.0)
    if ($filled -gt $Width) { $filled = $Width }
    $empty = $Width - $filled
    return ('[{0}{1}]' -f ('#' * $filled), ('-' * $empty))
}

function Write-CurrentWaveProgress {
    param([string]$ProgressJson)

    if ([string]::IsNullOrWhiteSpace($ProgressJson)) {
        Write-Host 'Wave progress: (no sidecar yet — prepare phase or older DataImporter)' -ForegroundColor DarkYellow
        return
    }

    $p = $null
    try { $p = $ProgressJson | ConvertFrom-Json } catch {
        Write-Host ("Wave progress: (unreadable sidecar) {0}" -f $ProgressJson.Substring(0, [Math]::Min(80, $ProgressJson.Length))) -ForegroundColor DarkYellow
        return
    }

    $entity = [string]$p.entity
    $processed = [int]$p.processed
    $total = [int]$p.total
    $pct = if ($null -ne $p.percent) { [double]$p.percent } elseif ($total -gt 0) { 100.0 * $processed / $total } else { 0 }
    $ins = if ($null -ne $p.inserted) { [int]$p.inserted } else { 0 }
    $upd = if ($null -ne $p.updated) { [int]$p.updated } else { 0 }
    $fail = if ($null -ne $p.failed) { [int]$p.failed } else { 0 }
    $phase = if ($p.phase) { [string]$p.phase } else { '' }
    $bar = Format-ProgressBar -Percent $pct
    $color = if ($pct -ge 100) { 'Green' } elseif ($phase -in @('prepare', 'resolve-legs')) { 'Yellow' } else { 'Cyan' }

    Write-Host -NoNewline 'Wave progress: ' -ForegroundColor White
    Write-Host -NoNewline ("{0}  " -f $entity) -ForegroundColor Cyan
    if ($total -gt 0) {
        Write-Host -NoNewline ("{0} {1}/{2} ({3:0.#}%)" -f $bar, $processed, $total, $pct) -ForegroundColor $color
    }
    else {
        Write-Host -NoNewline ("phase={0}" -f $(if ($phase) { $phase } else { 'starting' })) -ForegroundColor $color
    }
    Write-Host -NoNewline ("  posted={0}" -f $ins) -ForegroundColor Green
    if ($upd -gt 0) { Write-Host -NoNewline (" upd={0}" -f $upd) -ForegroundColor Green }
    if ($fail -gt 0) { Write-Host -NoNewline (" fail={0}" -f $fail) -ForegroundColor Red }
    if ($phase -and $total -gt 0) { Write-Host -NoNewline ("  [{0}]" -f $phase) -ForegroundColor DarkGray }
    Write-Host ''
}

function Write-ColoredWaveTable {
    param([object[]]$Rows)

    # Fixed widths so Status can be colored mid-line (Format-Table cannot color cells)
    $fmt = '{0,-4} {1,-26} {2,-12} {3,7} {4,8} {5,7} {6,8} {7,5} {8,7} {9,8} {10,5}'
    Write-Host ($fmt -f 'Mark', 'Wave', 'Status', 'Ins', 'DeltaIns', 'Upd', 'DeltaUpd', 'Fail', 'Legacy', 'Elapsed', 'Exit') -ForegroundColor DarkGray
    Write-Host ($fmt -f '----', '----', '------', '---', '--------', '---', '--------', '----', '------', '-------', '----') -ForegroundColor DarkGray

    foreach ($r in $Rows) {
        $statusColor = Get-StatusColor -Status ([string]$r.Status)
        $rowColor = switch ($r.Mark) {
            '>' { 'Cyan' }
            '!' { 'Red' }
            'ok' { 'Green' }
            default { 'Gray' }
        }

        $mark = if ($null -eq $r.Mark) { '' } else { [string]$r.Mark }
        $wave = [string]$r.Wave
        $st = [string]$r.Status
        $ins = if ($null -eq $r.Ins) { '' } else { [string]$r.Ins }
        $dIns = if ($null -eq $r.DeltaIns) { '' } else { [string]$r.DeltaIns }
        $upd = if ($null -eq $r.Upd) { '' } else { [string]$r.Upd }
        $dUpd = if ($null -eq $r.DeltaUpd) { '' } else { [string]$r.DeltaUpd }
        $fail = if ($null -eq $r.Fail) { '' } else { [string]$r.Fail }
        $leg = if ($null -eq $r.Legacy) { '' } else { [string]$r.Legacy }
        $el = if ($null -eq $r.Elapsed) { '' } else { [string]$r.Elapsed }
        $ex = if ($null -eq $r.Exit) { '' } else { [string]$r.Exit }

        $dInsColor = if ($dIns -ne '' -and [int]$dIns -ne 0) { 'Green' } else { 'Gray' }
        $dUpdColor = if ($dUpd -ne '' -and [int]$dUpd -ne 0) { 'Green' } else { 'Gray' }
        $failColor = if ($fail -ne '' -and [int]$fail -gt 0) { 'Red' } else { 'Gray' }

        Write-Host -NoNewline (('{0,-4}' -f $mark)) -ForegroundColor $rowColor
        Write-Host -NoNewline ((' {0,-26}' -f $wave)) -ForegroundColor $rowColor
        Write-Host -NoNewline ((' {0,-12}' -f $st)) -ForegroundColor $statusColor
        Write-Host -NoNewline ((' {0,7}' -f $ins)) -ForegroundColor Gray
        Write-Host -NoNewline ((' {0,8}' -f $dIns)) -ForegroundColor $dInsColor
        Write-Host -NoNewline ((' {0,7}' -f $upd)) -ForegroundColor Gray
        Write-Host -NoNewline ((' {0,8}' -f $dUpd)) -ForegroundColor $dUpdColor
        Write-Host -NoNewline ((' {0,5}' -f $fail)) -ForegroundColor $failColor
        Write-Host -NoNewline ((' {0,7}' -f $leg)) -ForegroundColor Gray
        Write-Host -NoNewline ((' {0,8}' -f $el)) -ForegroundColor Gray
        Write-Host ((' {0,5}' -f $ex)) -ForegroundColor Gray
    }
}

function Write-LiveDashboard {
    param($Snap, [datetime]$SampleTime)

    $script:SampleIndex++
    $status = $null
    if ($Snap.StatusJson) {
        try { $status = $Snap.StatusJson | ConvertFrom-Json } catch { $status = $null }
    }

    $di = @()
    try {
        if ($Snap.DataImporters -and $Snap.DataImporters -ne '[]') {
            $parsed = $Snap.DataImporters | ConvertFrom-Json
            if ($parsed -is [System.Array]) { $di = @($parsed) }
            else { $di = @($parsed) }
        }
    } catch { $di = @() }

    $diEntity = if ($di.Count -gt 0) { ($di | ForEach-Object { $_.Entity } | Where-Object { $_ } | Select-Object -First 1) } else { '' }
    $diPids = if ($di.Count -gt 0) { ($di | ForEach-Object { $_.Pid }) -join ',' } else { '' }

    Write-Host ("=== Import live #{0} @ {1}  Profile={2} ===" -f $script:SampleIndex, $SampleTime.ToString('HH:mm:ss'), $Profile) -ForegroundColor Cyan
    Write-Host ("Host: {0}  DB: {1}  ViaSsh={2}" -f $SyncHostRoot, $dbName, [bool]$ViaSsh) -ForegroundColor DarkGray

    if (-not $status) {
        Write-Host "No sync-run-status.json yet at $statusPath" -ForegroundColor Yellow
    } else {
        $summary = Get-OnPremSyncWaveSummary -RunStatus $status
        $runElapsed = Format-Elapsed -StartedUtc $status.StartedUtc -CompletedUtc $status.CompletedUtc
        $overallColor = Get-StatusColor -Status ([string]$status.OverallStatus)
        Write-Host -NoNewline ("RunId={0}  Overall=" -f $status.RunId) -ForegroundColor White
        Write-Host -NoNewline ([string]$status.OverallStatus) -ForegroundColor $overallColor
        Write-Host ("  Mode={0}  CurrentWave={1}" -f $status.Mode, $status.CurrentWave) -ForegroundColor White
        Write-Host ("RunElapsed={0}  Waves Done={1} Run={2} Fail={3} Pend={4}  UpdatedUtc={5}" -f `
            $runElapsed, $summary.Completed, $summary.Running, $summary.Failed, $summary.Pending, $status.UpdatedUtc) -ForegroundColor DarkGray
    }

    $diColor = if ($di.Count -gt 0) { 'Green' } else { 'Yellow' }
    Write-Host ("DataImporter: alive={0}  entity={1}  pid={2}" -f ($di.Count -gt 0), $diEntity, $diPids) -ForegroundColor $diColor
    if ($Snap.TaskState) {
        $taskColor = if ($Snap.TaskState -eq 'Running') { 'Cyan' } else { 'DarkGray' }
        Write-Host ("Task: State={0}  LastResult={1}" -f $Snap.TaskState, $Snap.TaskLastResult) -ForegroundColor $taskColor
    }
    Write-Host ("Interval={0}s  |  Ctrl+C to stop" -f $IntervalSeconds) -ForegroundColor DarkGray
    Write-Host ''
    Write-CurrentWaveProgress -ProgressJson ([string]$Snap.ProgressJson)
    Write-Host ''

    # --- Wave table with deltas ---
    $waveRows = @()
    if ($status -and $status.Waves) {
        foreach ($w in @($status.Waves)) {
            if ($OnlyRunningAndRecent) {
                if ($w.Status -eq 'Pending') { continue }
            }
            $ins = if ($null -ne $w.Inserted) { [int]$w.Inserted } else { $null }
            $upd = if ($null -ne $w.Updated) { [int]$w.Updated } else { $null }
            $fail = if ($null -ne $w.Failed) { [int]$w.Failed } else { $null }

            $dIns = $null; $dUpd = $null
            if ($script:PrevWave.ContainsKey($w.Name)) {
                $prev = $script:PrevWave[$w.Name]
                if ($null -ne $ins -and $null -ne $prev.Ins) { $dIns = $ins - $prev.Ins }
                if ($null -ne $upd -and $null -ne $prev.Upd) { $dUpd = $upd - $prev.Upd }
            }
            $script:PrevWave[$w.Name] = @{ Ins = $ins; Upd = $upd; Fail = $fail }

            $marker = ''
            if ($w.Status -eq 'Running' -or $w.Name -eq $status.CurrentWave) { $marker = '>' }
            elseif ($w.Status -eq 'Failed') { $marker = '!' }
            elseif ($w.Status -eq 'Completed') { $marker = 'ok' }

            $waveRows += [pscustomobject]@{
                Mark     = $marker
                Wave     = $w.Name
                Status   = $w.Status
                Ins      = $ins
                Upd      = $upd
                Fail     = $fail
                Legacy   = $w.LegacyRows
                DeltaIns = $dIns
                DeltaUpd = $dUpd
                Elapsed  = (Format-Elapsed -StartedUtc $w.StartedUtc -CompletedUtc $w.CompletedUtc)
                Exit     = $w.ExitCode
            }
        }
    }

    if ($waveRows.Count -gt 0) {
        Write-Host '--- Waves (Delta* = change since last sample) ---' -ForegroundColor Cyan
        Write-ColoredWaveTable -Rows $waveRows
    } else {
        Write-Host '(no wave rows)' -ForegroundColor Yellow
    }

    # --- DB counts with deltas ---
    if (-not $NoDbCounts) {
        $dbMap = Parse-DbCounts -Text $Snap.DbCounts
        $dbRows = @()
        foreach ($bo in $script:DbCountMap.Keys) {
            $c = if ($dbMap.ContainsKey($bo)) { $dbMap[$bo] } else { $null }
            $delta = $null
            if ($null -ne $c -and $script:PrevDb.ContainsKey($bo)) {
                $delta = $c - $script:PrevDb[$bo]
            }
            if ($null -ne $c) { $script:PrevDb[$bo] = $c }

            $isCurrent = ($bo -eq $diEntity) -or ($status -and $bo -eq $status.CurrentWave)
            if ($OnlyRunningAndRecent -and -not $isCurrent -and ($null -eq $delta -or $delta -eq 0) -and ($null -eq $c -or $c -eq 0)) {
                continue
            }

            $dbRows += [pscustomobject]@{
                Mark     = $(if ($isCurrent) { '>' } else { '' })
                BO       = $bo
                DbCount  = $c
                Delta    = $delta
                Table    = $script:DbCountMap[$bo]
            }
        }
        Write-Host ("--- Target DB counts ({0}) ---" -f $dbName) -ForegroundColor Cyan
        if ($dbRows.Count -gt 0) {
            $dbRows | Format-Table -AutoSize Mark, BO, DbCount, Delta, Table | Out-Host
        } else {
            Write-Host '(no DB counts — check sqlcmd / TrustServerCertificate)' -ForegroundColor Yellow
            if ($Snap.DbCounts) { Write-Host $Snap.DbCounts -ForegroundColor DarkYellow }
        }
    }

    # --- Progress / log hints ---
    if (-not $NoTail -and $status -and $status.CurrentWave) {
        $current = @($status.Waves) | Where-Object { $_.Name -eq $status.CurrentWave } | Select-Object -First 1
        if ($current -and $current.LogFile) {
            Write-Host ("--- progress / {0} ---" -f (Split-Path -Leaf $current.LogFile)) -ForegroundColor Cyan
            Get-ProgressHint -LogFile $current.LogFile -Lines $TailLogLines | ForEach-Object {
                $line = $_
                if ($line -match 'ERR ') { Write-Host $line -ForegroundColor Red }
                elseif ($line -match 'Progress:') { Write-Host $line -ForegroundColor Green }
                else { Write-Host $line -ForegroundColor Gray }
            }
        }
    }

    # Explicit return so Format-Table does not become the function output
    return $status
}

# --- main loop ---
Write-Host ("INF Watch-OnPremImportLive Profile={0} Root={1}" -f $Profile, $SyncHostRoot) -ForegroundColor Green
$remoteHelper = $null
if ($ViaSsh) {
    Write-Host ("INF Via SSH {0}" -f $SshHost) -ForegroundColor Green
    $remoteHelper = Ensure-RemoteSnapshotHelper
    Write-Host "INF Remote snapshot helper ready" -ForegroundColor DarkGray
} else {
    Write-Host ("INF Local status {0}" -f $statusPath) -ForegroundColor Green
}

$startedAt = Get-Date
$deadline = if ($DurationMinutes -gt 0) { $startedAt.AddMinutes($DurationMinutes) } else { $null }

try {
    while ($true) {
        if ($deadline -and (Get-Date) -gt $deadline) {
            Write-Host "INF Duration limit reached ($DurationMinutes min)." -ForegroundColor Yellow
            break
        }

        $sampleTime = Get-Date
        $snap = if ($ViaSsh) {
            Get-RemoteSnapshot -RemoteHelper $remoteHelper
        } else {
            Get-LocalSnapshot
        }

        if ($ClearScreen) { Clear-Host }
        $status = Write-LiveDashboard -Snap $snap -SampleTime $sampleTime

        $overall = if ($status) { [string]$status.OverallStatus } else { '' }
        $diAlive = $false
        try {
            if ($snap.DataImporters -and $snap.DataImporters -ne '[]') {
                $diAlive = @($snap.DataImporters | ConvertFrom-Json).Count -gt 0
            }
        } catch { $diAlive = $false }

        if ($overall -in @('Completed', 'CompletedWithErrors', 'Failed') -and -not $diAlive) {
            Write-Host ''
            Write-Host ("INF Run finished ({0}). Exiting watch." -f $overall) -ForegroundColor Green
            break
        }

        if ($Once) {
            Write-Host 'INF -Once: single sample done.' -ForegroundColor Green
            break
        }

        Start-Sleep -Seconds $IntervalSeconds
    }
}
catch {
    Write-Host ("ERR {0}" -f $_.Exception.Message) -ForegroundColor Red
    throw
}
finally {
    Write-Host 'INF Watch stopped.' -ForegroundColor Green
}