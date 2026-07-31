#Requires -Version 5.1
# Immutable Import-run archive under <SyncHostRoot>\history\runs\<RunId>\
# Used after hard-delete reimports to compare DbCount / wave outcomes across runs.
# Import-only (not delta Sync).

function Get-OnPremImportRunHistoryRoot {
    param([Parameter(Mandatory)][string]$SyncHostRoot)
    Join-Path $SyncHostRoot 'history'
}

function Get-OnPremImportRunArchiveDir {
    param(
        [Parameter(Mandatory)][string]$SyncHostRoot,
        [Parameter(Mandatory)][string]$RunId
    )
    Join-Path (Get-OnPremImportRunHistoryRoot -SyncHostRoot $SyncHostRoot) "runs\$RunId"
}

function Get-OnPremImportDbCountMap {
    [ordered]@{
        Person                  = 'People'
        Passport                = 'Passports'
        Visa                    = 'Visas'
        Education               = 'Educations'
        EmployeePositionHistory = 'EmployeePositionHistories'
        AddressOfResidence      = 'AddressesOfResidence'
        EmployeeSalary          = 'EmployeeSalaries'
        MedicalRecord           = 'MedicalRecords'
        Application             = 'Applications'
        WorkPermit              = 'WorkPermits'
        WorkPermitItem          = 'WorkPermitItems'
        Invitation              = 'Invitations'
        InvitationItem          = 'InvitationItems'
        ApplicationItem         = 'ApplicationItems'
        ApplicationProgress     = 'ApplicationProgresses'
    }
}

function Get-OnPremImportProfileDbName {
    param(
        [ValidateSet('Production', 'Staging', 'Demo')]
        [string]$Profile = 'Demo'
    )
    switch ($Profile) {
        'Staging' { 'Visa2026DbStaging' }
        'Demo' { 'Visa2026DbDemo' }
        default { 'Visa2026DbProd' }
    }
}

function Get-OnPremImportTargetDbCounts {
    param(
        [ValidateSet('Production', 'Staging', 'Demo')]
        [string]$Profile = 'Demo',
        [string]$DatabaseName = ''
    )

    if ([string]::IsNullOrWhiteSpace($DatabaseName)) {
        $DatabaseName = Get-OnPremImportProfileDbName -Profile $Profile
    }

    $map = Get-OnPremImportDbCountMap
    $parts = @()
    foreach ($bo in $map.Keys) {
        $t = $map[$bo]
        $parts += "SELECT '$bo' AS BO, '$t' AS Tbl, COUNT(*) AS C FROM [$t] WHERE GCRecord IS NULL OR GCRecord = 0"
    }
    $sql = "SET NOCOUNT ON; USE [$DatabaseName]; " + ($parts -join ' UNION ALL ') + ';'
    $rows = @(sqlcmd -S 'localhost\SQLEXPRESS' -E -C -Q $sql -W -s '|' -h -1 2>$null)

    $counts = @()
    foreach ($r in $rows) {
        if (-not $r -or -not $r.Trim() -or $r -match 'rows affected') { continue }
        $bits = $r.Trim() -split '\|'
        if ($bits.Count -lt 3) { continue }
        $c = 0
        [void][int]::TryParse($bits[2].Trim(), [ref]$c)
        $counts += [ordered]@{
            BO     = $bits[0].Trim()
            Table  = $bits[1].Trim()
            Count  = $c
        }
    }
    return , $counts
}

function Get-OnPremImportFilePresence {
    param(
        [ValidateSet('Production', 'Staging', 'Demo')]
        [string]$Profile = 'Demo'
    )

    $databaseName = Get-OnPremImportProfileDbName -Profile $Profile
    $specs = @(
        @{ Metric='Person.Photo'; Parent='People'; Present='People'; PresentPredicate='[Photo] IS NOT NULL'; Notes='Active people with a photo' },
        @{ Metric='PassportDocument'; Parent='Passports'; Present='PassportDocuments'; Notes='Document rows compared with active passports' },
        @{ Metric='VisaDocument'; Parent='Visas'; Present='VisaDocument'; Notes='Document rows compared with active visas' },
        @{ Metric='EducationDocument'; Parent='Educations'; Present='EducationDocument'; Notes='Document rows compared with active educations' },
        @{ Metric='WorkPermitDocument'; Parent='WorkPermits'; Present='WorkPermitDocuments'; Notes='Document rows compared with active work permits' },
        @{ Metric='InvitationDocument'; Parent='Invitations'; Present='InvitationDocuments'; Notes='Document rows compared with active invitations' },
        @{ Metric='PersonDocument'; Parent='People'; Present='PersonDocuments'; Notes='Family-proof/person document rows compared with active people' },
        @{ Metric='MedicalRecordDocument'; Parent='MedicalRecords'; Present='MedicalRecordDocuments'; Notes='Document rows compared with active medical records; missing table soft-fails to null' }
    )

    $sqlParts = @("SET NOCOUNT ON; USE [$databaseName];", 'DECLARE @results TABLE (Metric nvarchar(100), ParentCount int NULL, PresentCount int NULL, Notes nvarchar(300));')
    foreach ($spec in $specs) {
        $metric = ([string]$spec.Metric).Replace("'", "''")
        $parent = [string]$spec.Parent
        $present = [string]$spec.Present
        $notes = ([string]$spec.Notes).Replace("'", "''")
        $presentPredicate = if ($spec.PresentPredicate) { " AND $($spec.PresentPredicate)" } else { '' }
        $sqlParts += @"
DECLARE @parent_$($sqlParts.Count) int = NULL, @present_$($sqlParts.Count) int = NULL, @q_$($sqlParts.Count) nvarchar(max);
IF OBJECT_ID(N'dbo.[$parent]', N'U') IS NOT NULL
BEGIN
  SET @q_$($sqlParts.Count) = N'SELECT @out=COUNT(*) FROM dbo.[$parent]' + CASE WHEN COL_LENGTH(N'dbo.$parent', N'GCRecord') IS NOT NULL THEN N' WHERE (GCRecord IS NULL OR GCRecord = 0)' ELSE N'' END;
  EXEC sp_executesql @q_$($sqlParts.Count), N'@out int OUTPUT', @out=@parent_$($sqlParts.Count) OUTPUT;
END;
IF OBJECT_ID(N'dbo.[$present]', N'U') IS NOT NULL
BEGIN
  SET @q_$($sqlParts.Count) = N'SELECT @out=COUNT(*) FROM dbo.[$present]' + CASE WHEN COL_LENGTH(N'dbo.$present', N'GCRecord') IS NOT NULL THEN N' WHERE (GCRecord IS NULL OR GCRecord = 0)$presentPredicate' ELSE N'$(if ($presentPredicate) { ' WHERE ' + $presentPredicate.Substring(5) } else { '' })' END;
  EXEC sp_executesql @q_$($sqlParts.Count), N'@out int OUTPUT', @out=@present_$($sqlParts.Count) OUTPUT;
END;
INSERT @results VALUES (N'$metric', @parent_$($sqlParts.Count), @present_$($sqlParts.Count), N'$notes');
"@
    }
    $sqlParts += "SELECT Metric, COALESCE(CONVERT(varchar(20), ParentCount), 'NULL'), COALESCE(CONVERT(varchar(20), PresentCount), 'NULL'), Notes FROM @results ORDER BY Metric;"
    $rows = @(sqlcmd -S 'localhost\SQLEXPRESS' -E -C -Q ($sqlParts -join ' ') -W -s '|' -h -1 2>$null)
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "File presence query failed for $databaseName."
        return , @()
    }

    $metrics = @()
    foreach ($row in $rows) {
        if (-not $row -or -not $row.Trim() -or $row -match 'rows affected') { continue }
        $bits = $row.Trim() -split '\|', 4
        if ($bits.Count -lt 4) { continue }
        $parentCount = $null
        $presentCount = $null
        $parsed = 0
        if ($bits[1].Trim() -ne 'NULL' -and [int]::TryParse($bits[1].Trim(), [ref]$parsed)) { $parentCount = $parsed }
        $parsed = 0
        if ($bits[2].Trim() -ne 'NULL' -and [int]::TryParse($bits[2].Trim(), [ref]$parsed)) { $presentCount = $parsed }
        $metrics += [ordered]@{ Metric=$bits[0].Trim(); ParentCount=$parentCount; PresentCount=$presentCount; Notes=$bits[3].Trim() }
    }
    return , $metrics
}
function Write-OnPremImportJsonFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        $Object
    )
    $parent = Split-Path -Parent $Path
    if ($parent -and -not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    $json = ConvertTo-Json -InputObject $Object -Depth 10 -Compress:$false
    [System.IO.File]::WriteAllText($Path, $json, (New-Object System.Text.UTF8Encoding $false))
}

function Save-OnPremImportRunArchive {
    <#
    .SYNOPSIS
      Snapshot the current Import run into history/runs/<RunId>/ (immutable).
    #>
    param(
        [Parameter(Mandatory)][string]$SyncHostRoot,
        [ValidateSet('Production', 'Staging', 'Demo')]
        [string]$Profile = 'Demo',
        [string]$RunId = '',
        [string]$StartAt = '',
        [string[]]$Flags = @(),
        [switch]$SkipDbCounts,
        [switch]$Force
    )

    $statusPath = Join-Path $SyncHostRoot 'sync-run-status.json'
    if (-not (Test-Path -LiteralPath $statusPath)) {
        Write-Warning "No sync-run-status.json at $statusPath ? skip archive."
        return $null
    }

    $status = Get-Content -LiteralPath $statusPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($RunId)) {
        $RunId = [string]$status.RunId
    }
    if ([string]::IsNullOrWhiteSpace($RunId)) {
        Write-Warning 'RunId missing ? skip archive.'
        return $null
    }

    $dir = Get-OnPremImportRunArchiveDir -SyncHostRoot $SyncHostRoot -RunId $RunId
    if ((Test-Path -LiteralPath $dir) -and -not $Force) {
        Write-Host "INF Import archive already exists (immutable): $dir" -ForegroundColor DarkGray
        Update-OnPremImportRunHistoryIndex -SyncHostRoot $SyncHostRoot
        return $dir
    }

    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Copy-Item -LiteralPath $statusPath -Destination (Join-Path $dir 'run-status.json') -Force

    $dbCounts = @()
    if (-not $SkipDbCounts) {
        try {
            $dbCounts = @(Get-OnPremImportTargetDbCounts -Profile $Profile)
        }
        catch {
            Write-Warning "DbCounts failed: $($_.Exception.Message)"
        }
    }
    Write-OnPremImportJsonFile -Path (Join-Path $dir 'db-counts.json') -Object ([ordered]@{
            Profile      = $Profile
            DatabaseName = Get-OnPremImportProfileDbName -Profile $Profile
            CapturedUtc  = (Get-Date).ToUniversalTime().ToString('o')
            Counts       = @($dbCounts)
        })


    $filePresence = @()
    if (-not $SkipDbCounts) {
        try {
            $filePresence = @(Get-OnPremImportFilePresence -Profile $Profile)
        }
        catch {
            Write-Warning "File presence capture failed: $($_.Exception.Message)"
        }
        Write-OnPremImportJsonFile -Path (Join-Path $dir 'file-presence.json') -Object ([ordered]@{
                Profile      = $Profile
                DatabaseName = Get-OnPremImportProfileDbName -Profile $Profile
                CapturedUtc  = (Get-Date).ToUniversalTime().ToString('o')
                Metrics      = @($filePresence)
            })
    }

    $includeFileWavesFlag = @($Flags) -contains 'IncludeFileWaves'
    $fileWavesStatusPath = Join-Path $SyncHostRoot 'file-waves-status.json'
    $fileWaves = $null
    if (Test-Path -LiteralPath $fileWavesStatusPath) {
        try {
            $fileWaves = Get-Content -LiteralPath $fileWavesStatusPath -Raw -Encoding UTF8 | ConvertFrom-Json
        }
        catch {
            Write-Warning "File wave status read failed: $($_.Exception.Message)"
        }
    }
    if ($null -eq $fileWaves) {
        $fileWaves = [ordered]@{
            Included = [bool]$includeFileWavesFlag
            Steps    = @()
            Note     = if ($includeFileWavesFlag) { 'IncludeFileWaves was requested but file-waves-status.json was missing' } else { 'File waves not run for this RunId' }
        }
    }
    Write-OnPremImportJsonFile -Path (Join-Path $dir 'file-waves.json') -Object $fileWaves
    $fileWavesIncluded = [bool]($includeFileWavesFlag -or ($fileWaves.PSObject.Properties.Name -contains 'Included' -and $fileWaves.Included))
    $waveSummary = @{ Pending = 0; Running = 0; Completed = 0; Failed = 0 }
    if (Get-Command Get-OnPremSyncWaveSummary -ErrorAction SilentlyContinue) {
        $waveSummary = Get-OnPremSyncWaveSummary -RunStatus $status
    }
    else {
        foreach ($w in @($status.Waves)) {
            switch ($w.Status) {
                'Pending' { $waveSummary.Pending++ }
                'Running' { $waveSummary.Running++ }
                'Completed' { $waveSummary.Completed++ }
                'Failed' { $waveSummary.Failed++ }
            }
        }
    }

    $elapsedSec = $null
    if ($status.StartedUtc -and $status.CompletedUtc) {
        try {
            $elapsedSec = [int]([datetime]$status.CompletedUtc - [datetime]$status.StartedUtc).TotalSeconds
        }
        catch {}
    }

    $meta = [ordered]@{
        Version        = 1
        Kind           = 'ImportReimportArchive'
        RunId          = $RunId
        Profile        = $Profile
        LegacySource   = $status.LegacySource
        OverallStatus  = $status.OverallStatus
        Mode           = $status.Mode
        StartedUtc     = $status.StartedUtc
        CompletedUtc   = $status.CompletedUtc
        ElapsedSeconds = $elapsedSec
        StartAt        = $StartAt
        Flags             = @($Flags)
        FileWavesIncluded = $fileWavesIncluded
        WaveSummary    = $waveSummary
        SyncHostRoot   = $SyncHostRoot
        ArchivedUtc    = (Get-Date).ToUniversalTime().ToString('o')
    }
    Write-OnPremImportJsonFile -Path (Join-Path $dir 'meta.json') -Object $meta

    Update-OnPremImportRunHistoryIndex -SyncHostRoot $SyncHostRoot
    Write-Host "INF Import run archived: $dir" -ForegroundColor Green
    Write-Host ("INF History dashboard: {0}" -f (Join-Path (Get-OnPremImportRunHistoryRoot -SyncHostRoot $SyncHostRoot) 'index.html')) -ForegroundColor DarkGray
    return $dir
}

function Get-OnPremImportRunArchiveList {
    param([Parameter(Mandatory)][string]$SyncHostRoot)

    $root = Join-Path (Get-OnPremImportRunHistoryRoot -SyncHostRoot $SyncHostRoot) 'runs'
    if (-not (Test-Path -LiteralPath $root)) { return @() }

    $list = @()
    Get-ChildItem -LiteralPath $root -Directory | Sort-Object Name -Descending | ForEach-Object {
        $metaPath = Join-Path $_.FullName 'meta.json'
        $meta = $null
        if (Test-Path -LiteralPath $metaPath) {
            try { $meta = Get-Content -LiteralPath $metaPath -Raw -Encoding UTF8 | ConvertFrom-Json } catch {}
        }
        $list += [pscustomobject]@{
            RunId         = $_.Name
            Path          = $_.FullName
            OverallStatus = if ($meta) { $meta.OverallStatus } else { '' }
            Profile       = if ($meta) { $meta.Profile } else { '' }
            StartedUtc    = if ($meta) { $meta.StartedUtc } else { '' }
            CompletedUtc  = if ($meta) { $meta.CompletedUtc } else { '' }
            ElapsedSeconds = if ($meta) { $meta.ElapsedSeconds } else { $null }
            WavesCompleted = if ($meta -and $meta.WaveSummary) { $meta.WaveSummary.Completed } else { $null }
            WavesFailed    = if ($meta -and $meta.WaveSummary) { $meta.WaveSummary.Failed } else { $null }
        }
    }
    return , $list
}

function Read-OnPremImportRunArchive {
    param(
        [Parameter(Mandatory)][string]$SyncHostRoot,
        [Parameter(Mandatory)][string]$RunId
    )

    $dir = Get-OnPremImportRunArchiveDir -SyncHostRoot $SyncHostRoot -RunId $RunId
    if (-not (Test-Path -LiteralPath $dir)) {
        throw "Import archive not found: $dir"
    }

    $meta = $null
    $status = $null
    $db = $null
    $metaPath = Join-Path $dir 'meta.json'
    $statusPath = Join-Path $dir 'run-status.json'
    $dbPath = Join-Path $dir 'db-counts.json'
    if (Test-Path $metaPath) { $meta = Get-Content $metaPath -Raw -Encoding UTF8 | ConvertFrom-Json }
    if (Test-Path $statusPath) { $status = Get-Content $statusPath -Raw -Encoding UTF8 | ConvertFrom-Json }
    if (Test-Path $dbPath) { $db = Get-Content $dbPath -Raw -Encoding UTF8 | ConvertFrom-Json }

    return [pscustomobject]@{
        RunId  = $RunId
        Dir    = $dir
        Meta   = $meta
        Status = $status
        Db     = $db
    }
}

function Update-OnPremImportRunHistoryIndex {
    param([Parameter(Mandatory)][string]$SyncHostRoot)

    $historyRoot = Get-OnPremImportRunHistoryRoot -SyncHostRoot $SyncHostRoot
    New-Item -ItemType Directory -Force -Path $historyRoot | Out-Null
    $runs = @(Get-OnPremImportRunArchiveList -SyncHostRoot $SyncHostRoot)

    $rowsHtml = New-Object System.Text.StringBuilder
    foreach ($r in $runs) {
        $elapsedSecRaw = $r.ElapsedSeconds
        if ($elapsedSecRaw -is [System.Array]) { $elapsedSecRaw = $elapsedSecRaw | Select-Object -First 1 }
        $elapsed = if ($null -ne $elapsedSecRaw -and "$elapsedSecRaw" -ne '') {
            $elapsedSecInt = [int]$elapsedSecRaw
            '{0:00}:{1:00}' -f [int]([math]::Floor($elapsedSecInt / 60)), ($elapsedSecInt % 60)
        } else { '' }
        $statusClass = switch ($r.OverallStatus) {
            'Completed' { 'ok' }
            'Failed' { 'fail' }
            default { '' }
        }
        [void]$rowsHtml.AppendLine((@"
<tr class="$statusClass">
  <td><a href="runs/$($r.RunId)/run-status.json">$($r.RunId)</a></td>
  <td>$($r.Profile)</td>
  <td>$($r.OverallStatus)</td>
  <td>$($r.WavesCompleted)</td>
  <td>$($r.WavesFailed)</td>
  <td>$elapsed</td>
  <td>$($r.CompletedUtc)</td>
  <td><a href="runs/$($r.RunId)/db-counts.json">db-counts</a> | <a href="runs/$($r.RunId)/file-waves.json">file-waves</a> | <a href="runs/$($r.RunId)/file-presence.json">file-presence</a> | <a href="runs/$($r.RunId)/meta.json">meta</a></td>
</tr>
"@).Trim())
    }

    $latest = if ($runs.Count -ge 1) { $runs[0].RunId } else { '' }
    $prev = if ($runs.Count -ge 2) { $runs[1].RunId } else { '' }
    $compareHint = if ($latest -and $prev) {
        "Compare latest two: <code>.\scripts\visa2014-migration\Compare-OnPremImportRuns.ps1 -Profile Demo -Left $prev -Right $latest</code>"
    } else {
        'Need at least two archived runs to compare.'
    }

    $html = @"
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<title>Visa2026 Import reimport history</title>
<style>
  body { font-family: Segoe UI, sans-serif; margin: 1.5rem; color: #1a1a1a; background: #f7f7f5; }
  h1 { font-size: 1.35rem; margin: 0 0 .25rem; }
  .sub { color: #555; margin-bottom: 1rem; }
  table { border-collapse: collapse; width: 100%; background: #fff; }
  th, td { border: 1px solid #ddd; padding: .4rem .55rem; text-align: left; font-size: .9rem; }
  th { background: #eee; }
  tr.ok td:nth-child(3) { color: #0a7a2f; font-weight: 600; }
  tr.fail td:nth-child(3) { color: #b00020; font-weight: 600; }
  code { background: #eee; padding: .1rem .3rem; }
  a { color: #0b57d0; }
</style>
</head>
<body>
  <h1>Visa2026 Import reimport history</h1>
  <p class="sub">Immutable snapshots after each on-prem Import (hard-delete reimport friendly). Host: $SyncHostRoot</p>
  <p>$compareHint</p>
  <table>
    <thead>
      <tr>
        <th>RunId</th><th>Profile</th><th>Overall</th><th>Waves OK</th><th>Waves Fail</th><th>Elapsed</th><th>CompletedUtc</th><th>Artifacts</th>
      </tr>
    </thead>
    <tbody>
$($rowsHtml.ToString())
    </tbody>
  </table>
</body>
</html>
"@
    $indexPath = Join-Path $historyRoot 'index.html'
    [System.IO.File]::WriteAllText($indexPath, $html, (New-Object System.Text.UTF8Encoding $false))
    return $indexPath
}

function Compare-OnPremImportRunArchives {
    param(
        [Parameter(Mandatory)]$Left,
        [Parameter(Mandatory)]$Right,
        [int]$AbsoluteCountThreshold = 20,
        [double]$RelativePercentThreshold = 1.0
    )

    function Get-OnPremImportFlatDbCountRows([object]$Counts) {
        $out = @()
        foreach ($c in @($Counts)) {
            if ($null -eq $c) { continue }
            if ($c -is [System.Array] -or ($c.PSObject.TypeNames -contains "System.Object[]")) {
                $out += @(Get-OnPremImportFlatDbCountRows $c)
                continue
            }
            if ($c.BO) { $out += $c }
        }
        return $out
    }

    $leftMap = @{}
    foreach ($c in @(Get-OnPremImportFlatDbCountRows $Left.Db.Counts)) { $leftMap[[string]$c.BO] = [int]$c.Count }
    $rightMap = @{}
    foreach ($c in @(Get-OnPremImportFlatDbCountRows $Right.Db.Counts)) { $rightMap[[string]$c.BO] = [int]$c.Count }

    $allBos = @($leftMap.Keys + $rightMap.Keys | Select-Object -Unique)
    $boRows = @()
    $anomalies = @()
    foreach ($bo in ($allBos | Sort-Object)) {
        $l = if ($leftMap.ContainsKey($bo)) { $leftMap[$bo] } else { $null }
        $r = if ($rightMap.ContainsKey($bo)) { $rightMap[$bo] } else { $null }
        $delta = if ($null -ne $l -and $null -ne $r) { $r - $l } else { $null }
        $pct = $null
        if ($null -ne $delta -and $l -gt 0) {
            $pct = [math]::Round(100.0 * [math]::Abs($delta) / $l, 2)
        }
        elseif ($null -ne $delta -and $l -eq 0 -and $r -gt 0) {
            $pct = 100.0
        }
        $isAnomaly = $false
        if ($null -ne $delta) {
            $absHit = [math]::Abs($delta) -ge $AbsoluteCountThreshold
            $pctHit = ($null -ne $pct) -and ($pct -ge $RelativePercentThreshold)
            # Flag when both absolute and relative thresholds are met (or left was 0 and right grew)
            $isAnomaly = ($absHit -and $pctHit) -or ($l -eq 0 -and $r -ge $AbsoluteCountThreshold) -or ($r -eq 0 -and $l -ge $AbsoluteCountThreshold)
        }
        $row = [ordered]@{
            BO        = $bo
            Left      = $l
            Right     = $r
            Delta     = $delta
            AbsPct    = $pct
            Anomaly   = $isAnomaly
        }
        $boRows += [pscustomobject]$row
        if ($isAnomaly) { $anomalies += "DbCount $bo delta=$delta ($pct%)" }
    }

    $waveRows = @()
    $leftWaves = @{}
    foreach ($w in @($Left.Status.Waves)) { $leftWaves[[string]$w.Name] = $w }
    $rightWaves = @{}
    foreach ($w in @($Right.Status.Waves)) { $rightWaves[[string]$w.Name] = $w }
    $waveNames = @($leftWaves.Keys + $rightWaves.Keys | Select-Object -Unique)
    foreach ($name in $waveNames) {
        $lw = $leftWaves[$name]
        $rw = $rightWaves[$name]
        $regressed = $false
        if ($lw -and $rw) {
            if ($lw.Status -eq 'Completed' -and $rw.Status -eq 'Failed') { $regressed = $true }
            $lf = if ($null -ne $lw.Failed) { [int]$lw.Failed } else { 0 }
            $rf = if ($null -ne $rw.Failed) { [int]$rw.Failed } else { 0 }
            if ($rf -gt $lf) { $regressed = $true }
        }
        if ($regressed) { $anomalies += "Wave $name regressed ($($lw.Status)/fail=$($lw.Failed) -> $($rw.Status)/fail=$($rw.Failed))" }
        $waveRows += [pscustomobject]@{
            Wave          = $name
            LeftStatus    = if ($lw) { $lw.Status } else { '' }
            RightStatus   = if ($rw) { $rw.Status } else { '' }
            LeftFailed    = if ($lw) { $lw.Failed } else { $null }
            RightFailed   = if ($rw) { $rw.Failed } else { $null }
            LeftExit      = if ($lw) { $lw.ExitCode } else { $null }
            RightExit     = if ($rw) { $rw.ExitCode } else { $null }
            Regressed     = $regressed
        }
    }

    return [pscustomobject]@{
        LeftRunId                  = $Left.RunId
        RightRunId                 = $Right.RunId
        AbsoluteCountThreshold     = $AbsoluteCountThreshold
        RelativePercentThreshold   = $RelativePercentThreshold
        BoRows                     = $boRows
        WaveRows                   = $waveRows
        Anomalies                  = $anomalies
        AnomalyCount               = $anomalies.Count
    }
}

function Write-OnPremImportCompareHtml {
    param(
        [Parameter(Mandatory)][string]$SyncHostRoot,
        [Parameter(Mandatory)]$CompareResult
    )

    $historyRoot = Get-OnPremImportRunHistoryRoot -SyncHostRoot $SyncHostRoot
    New-Item -ItemType Directory -Force -Path $historyRoot | Out-Null
    $fileName = "compare-$($CompareResult.LeftRunId)-vs-$($CompareResult.RightRunId).html"
    $path = Join-Path $historyRoot $fileName

    $boSb = New-Object System.Text.StringBuilder
    foreach ($r in $CompareResult.BoRows) {
        $cls = if ($r.Anomaly) { 'anomaly' } else { '' }
        $delta = if ($null -ne $r.Delta) { $r.Delta } else { '' }
        $pct = if ($null -ne $r.AbsPct) { "$($r.AbsPct)%" } else { '' }
        [void]$boSb.AppendLine("<tr class=`"$cls`"><td>$($r.BO)</td><td>$($r.Left)</td><td>$($r.Right)</td><td>$delta</td><td>$pct</td><td>$(if($r.Anomaly){'ANOMALY'}else{''})</td></tr>")
    }

    $wSb = New-Object System.Text.StringBuilder
    foreach ($r in $CompareResult.WaveRows) {
        $cls = if ($r.Regressed) { 'anomaly' } else { '' }
        [void]$wSb.AppendLine("<tr class=`"$cls`"><td>$($r.Wave)</td><td>$($r.LeftStatus)</td><td>$($r.RightStatus)</td><td>$($r.LeftFailed)</td><td>$($r.RightFailed)</td><td>$($r.LeftExit)</td><td>$($r.RightExit)</td><td>$(if($r.Regressed){'REGRESSED'}else{''})</td></tr>")
    }

    $anomList = if ($CompareResult.AnomalyCount -gt 0) {
        '<ul>' + (($CompareResult.Anomalies | ForEach-Object { "<li>$_</li>" }) -join '') + '</ul>'
    } else { '<p>No anomalies under current thresholds.</p>' }

    $html = @"
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<title>Import compare $($CompareResult.LeftRunId) vs $($CompareResult.RightRunId)</title>
<style>
  body { font-family: Segoe UI, sans-serif; margin: 1.5rem; background: #f7f7f5; color: #1a1a1a; }
  h1 { font-size: 1.25rem; }
  table { border-collapse: collapse; width: 100%; background: #fff; margin-bottom: 1.25rem; }
  th, td { border: 1px solid #ddd; padding: .35rem .5rem; font-size: .88rem; text-align: left; }
  th { background: #eee; }
  tr.anomaly { background: #ffe8e8; }
  a { color: #0b57d0; }
</style>
</head>
<body>
  <h1>Import reimport compare</h1>
  <p><a href="index.html">History index</a> | Left <code>$($CompareResult.LeftRunId)</code> -> Right <code>$($CompareResult.RightRunId)</code></p>
  <p>Thresholds: |delta| >= $($CompareResult.AbsoluteCountThreshold) and |delta%| >= $($CompareResult.RelativePercentThreshold)% (or zero-side wipe/spike).</p>
  <h2>Anomalies ($($CompareResult.AnomalyCount))</h2>
  $anomList
  <h2>Target DB counts</h2>
  <table>
    <thead><tr><th>BO</th><th>Left</th><th>Right</th><th>Delta</th><th>|Delta|%</th><th>Flag</th></tr></thead>
    <tbody>$($boSb.ToString())</tbody>
  </table>
  <h2>Waves</h2>
  <table>
    <thead><tr><th>Wave</th><th>Left</th><th>Right</th><th>Fail L</th><th>Fail R</th><th>Exit L</th><th>Exit R</th><th>Flag</th></tr></thead>
    <tbody>$($wSb.ToString())</tbody>
  </table>
</body>
</html>
"@
    [System.IO.File]::WriteAllText($path, $html, (New-Object System.Text.UTF8Encoding $false))
    return $path
}
