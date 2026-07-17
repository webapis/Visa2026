#Requires -Version 5.1
# Runs ON the sync host (.25). Emits compact JSON (no StatusJson — watcher SCPs that file).
[CmdletBinding()]
param(
    [ValidateSet('Production', 'Staging', 'Demo', 'Local')]
    [string]$Profile = 'Demo',
    [string]$SyncHostRoot = '',
    [switch]$NoDbCounts
)

$ErrorActionPreference = 'Continue'

if ([string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    $SyncHostRoot = switch ($Profile) {
        'Staging' { 'C:\visa2026-sync-staging' }
        'Demo' { 'C:\visa2026-sync-demo' }
        'Local' {
            $repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
            Join-Path $repo 'artifacts\local-pg-import'
        }
        default { 'C:\visa2026-sync' }
    }
}

$dbName = switch ($Profile) {
    'Staging' { 'Visa2026DbStaging' }
    'Demo' { 'Visa2026DbDemo' }
    'Local' { 'visa2026' }
    default { 'Visa2026DbProd' }
}

$rootLeaf = Split-Path -Leaf $SyncHostRoot
$statusPath = Join-Path $SyncHostRoot 'sync-run-status.json'

$dbCountMap = [ordered]@{
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

$localPgEnvPath = Join-Path $SyncHostRoot 'local-pg.env'
$isLocalPgRoot = Test-Path -LiteralPath $localPgEnvPath

$diList = @()
# DataImporter may run as "dotnet.exe … Visa2026.DataImporter" (local) or published Visa2026.DataImporter.exe (on-prem).
$diProcessNames = @('Visa2026.DataImporter', 'dotnet')
foreach ($procName in $diProcessNames) {
    Get-Process -Name $procName -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $cim = Get-CimInstance Win32_Process -Filter ("ProcessId=" + $_.Id) -ErrorAction SilentlyContinue
            $path = if ($cim) { $cim.ExecutablePath } else { '' }
            $cmd = if ($cim) { $cim.CommandLine } else { '' }
            if (-not $cmd) { return }
            $isImporter = ($procName -eq 'Visa2026.DataImporter') -or ($cmd -match 'Visa2026\.DataImporter')
            if (-not $isImporter) { return }
            $matchesRoot = $path -like ("*\" + $rootLeaf + "\*") -or $cmd -like ("*" + $rootLeaf + "*")
            $matchesLocal = $isLocalPgRoot -and ($cmd -match 'import-visa2014|calik-energi-local-pg')
            if (-not ($matchesRoot -or $matchesLocal)) { return }

            $entity = ''
            if ($cmd -match '--entity\s+(\w+)') { $entity = $Matches[1] }
            if ($cmd -match '--property\s+(\w+)') {
                $prop = $Matches[1]
                $entity = if ($entity) { "$entity.$prop" } else { $prop }
            }
            elseif ($cmd -match '--import-visa2014-files') {
                if ($entity) { $entity = "$entity (files)" } else { $entity = 'files' }
            }
            $diList += @{ Pid = $_.Id; Entity = $entity }
        } catch {}
    }
}

$taskNames = switch ($Profile) {
    'Demo' { @('Visa2026-OnPrem-DemoFileWavesOnly', 'Visa2026-OnPrem-DemoImportOnce', 'Visa2026-OnPrem-DemoImportFileWaves') }
    'Local' { @() }
    default { @('Visa2026-OnPrem-ProdFileWavesOnce', 'Visa2026-OnPrem-ManualSyncOnce') }
}
$taskState = ''
$taskLast = ''
$taskName = ''
foreach ($candidate in $taskNames) {
    $task = Get-ScheduledTask -TaskName $candidate -ErrorAction SilentlyContinue
    if (-not $task) { continue }
    $ti = $task | Get-ScheduledTaskInfo
    # Prefer a Running task; otherwise keep the first hit.
    if (-not $taskName -or [string]$task.State -eq 'Running') {
        $taskName = $candidate
        $taskState = [string]$task.State
        $taskLast = [string]$ti.LastTaskResult
        if ($taskState -eq 'Running') { break }
    }
}

$dbLines = @()
$localPgUser = 'postgres'
$localIdMapSubDir = ''
if ($isLocalPgRoot) {
    Get-Content -LiteralPath $localPgEnvPath | ForEach-Object {
        if ($_ -match '^PG_PASSWORD=(.*)$') { $script:localPgPassFromEnv = $Matches[1].Trim() }
        if ($_ -match '^DB_NAME=(.*)$') { $script:localPgDbFromEnv = $Matches[1].Trim() }
        if ($_ -match '^PG_USER=(.*)$') { $localPgUser = $Matches[1].Trim() }
        if ($_ -match '^ID_MAP_SUBDIR=(.*)$') { $localIdMapSubDir = $Matches[1].Trim() }
    }
}

if (-not $NoDbCounts) {
    $usePostgres = $false
    $pgDatabase = 'visa2026_demo'
    $pgPass = $null
    $pgUser = 'postgres'

    # Dev PC: SyncHostRoot with local-pg.env (artifacts/local-pg-import)
    if ($isLocalPgRoot) {
        $usePostgres = $true
        $pgDatabase = if ($script:localPgDbFromEnv) { $script:localPgDbFromEnv } else { 'visa2026' }
        $pgPass = $script:localPgPassFromEnv
        $pgUser = $localPgUser
    }
    else {
        # Slot IIS appsettings + env — Demo/Staging/Production may all be PostgreSQL.
        $slotMeta = switch ($Profile) {
            'Staging' {
                @{
                    AppSettings = 'C:\inetpub\visa2026-staging\appsettings.Production.json'
                    EnvFile     = 'C:\visa2026\env\staging.env'
                    SyncEnvKey  = 'VISA2026_STAGING_SQL_CONNECTION'
                    DefaultDb   = 'visa2026_staging'
                }
            }
            'Demo' {
                @{
                    AppSettings = 'C:\inetpub\visa2026-demo\appsettings.Production.json'
                    EnvFile     = 'C:\visa2026\env\demo.env'
                    SyncEnvKey  = 'VISA2026_DEMO_SQL_CONNECTION'
                    DefaultDb   = 'visa2026_demo'
                }
            }
            default {
                @{
                    # Multi-slot path (not legacy C:\inetpub\visa2026, which may still point at SQLEXPRESS).
                    AppSettings = 'C:\inetpub\visa2026-prod\appsettings.Production.json'
                    EnvFile     = 'C:\visa2026\env\prod.env'
                    SyncEnvKey  = 'VISA2026_PROD_SQL_CONNECTION'
                    DefaultDb   = 'visa2026_prod'
                }
            }
        }

        $pgDatabase = $slotMeta.DefaultDb
        $cs = $null
        if (Test-Path -LiteralPath $slotMeta.AppSettings) {
            try {
                $cs = (Get-Content -LiteralPath $slotMeta.AppSettings -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection
            } catch {}
        }
        $syncEnvPath = Join-Path $SyncHostRoot 'config\sync.env'
        if (-not $cs -and (Test-Path -LiteralPath $syncEnvPath)) {
            $keyPrefix = $slotMeta.SyncEnvKey + '='
            $line = Get-Content -LiteralPath $syncEnvPath | Where-Object { $_ -like ($keyPrefix + '*') } | Select-Object -First 1
            if ($line) { $cs = $line.Substring($keyPrefix.Length) }
        }
        if ($cs -and ($cs -match '(?i)EFCoreProvider\s*=\s*(Postgres|PostgreSQL)' -or $cs -match '(?i)(^|;)\s*Host\s*=')) {
            $usePostgres = $true
            if ($cs -match '(?i)Database\s*=\s*([^;]+)') { $pgDatabase = $Matches[1].Trim() }
            if ($cs -match '(?i)Password\s*=\s*([^;]+)') { $pgPass = $Matches[1].Trim() }
            if ($cs -match '(?i)Username\s*=\s*([^;]+)') { $pgUser = $Matches[1].Trim() }
            elseif ($cs -match '(?i)User Id\s*=\s*([^;]+)') { $pgUser = $Matches[1].Trim() }
        }
        if ($usePostgres -and (Test-Path -LiteralPath $slotMeta.EnvFile)) {
            Get-Content -LiteralPath $slotMeta.EnvFile | ForEach-Object {
                if ($_ -match '^PG_PASSWORD=(.*)$') { $pgPass = $Matches[1].Trim() }
                if ($_ -match '^DB_NAME=(.*)$') { $pgDatabase = $Matches[1].Trim() }
                if ($_ -match '^PG_USER=(.*)$') { $pgUser = $Matches[1].Trim() }
            }
        }
        if ($usePostgres -and -not $pgPass -and (Test-Path -LiteralPath $syncEnvPath)) {
            Get-Content -LiteralPath $syncEnvPath | ForEach-Object {
                if ($_ -match ('^' + [regex]::Escape($slotMeta.SyncEnvKey) + '=(.*)$')) {
                    $syncCs = $Matches[1]
                    if ($syncCs -match '(?i)Password\s*=\s*([^;]+)') { $pgPass = $Matches[1].Trim() }
                }
            }
        }
    }

    if ($usePostgres) {
        $psql = 'C:\PostgreSQL\16\bin\psql.exe'
        if (-not (Test-Path -LiteralPath $psql)) {
            $psql = Get-Command psql -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
        }
        if ($psql -and $pgPass) {
            $env:PGPASSWORD = $pgPass
            $unionParts = @()
            foreach ($bo in $dbCountMap.Keys) {
                $t = $dbCountMap[$bo]
                $unionParts += "SELECT '$bo' AS bo, COUNT(*)::int AS c FROM public.`"$t`" WHERE `"GCRecord`" IS NULL OR `"GCRecord`" = 0"
            }
            $sqlFile = Join-Path $env:TEMP ("visa2026-dbcounts-{0}.sql" -f [guid]::NewGuid().ToString('N'))
            ($unionParts -join "`nUNION ALL`n") + ';' | Set-Content -LiteralPath $sqlFile -Encoding UTF8
            $rows = & $psql -h localhost -U $pgUser -d $pgDatabase -t -A -F '|' -f $sqlFile 2>$null
            Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue
            foreach ($r in @($rows)) {
                if ($r -and $r.Trim() -and $r -match '\|') { $dbLines += $r.Trim() }
            }
        }
    }
    else {
        $parts = @()
        foreach ($bo in $dbCountMap.Keys) {
            $t = $dbCountMap[$bo]
            $parts += "SELECT '$bo' AS BO, COUNT(*) AS C FROM [$t] WHERE GCRecord IS NULL OR GCRecord = 0"
        }
        $sql = "SET NOCOUNT ON; USE [$dbName]; " + ($parts -join ' UNION ALL ') + ';'
        $rows = sqlcmd -S 'localhost\SQLEXPRESS' -E -C -Q $sql -W -s '|' -h -1 2>$null
        foreach ($r in @($rows)) {
            if ($r -and $r.Trim() -and $r -notmatch 'rows affected' -and $r -match '\|') {
                $dbLines += $r.Trim()
            }
        }
    }
}

# Live wave progress sidecar (written by DataImporter every ~100 rows; survives stdout buffering)
$progressJson = ''
$idMapSubDir = switch ($Profile) {
    'Staging' { 'calik-energi-onprem-staging' }
    'Demo' { 'calik-energi-onprem-demo' }
    default { 'calik-energi-onprem-prod' }
}
if ($localIdMapSubDir) { $idMapSubDir = $localIdMapSubDir }
$statusObj = $null
try {
    if (Test-Path -LiteralPath $statusPath) {
        $statusObj = Get-Content -LiteralPath $statusPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
} catch {}
$currentWave = if ($statusObj) { [string]$statusObj.CurrentWave } else { '' }
if (-not $currentWave -and $diList.Count -gt 0) {
    $currentWave = [string]$diList[0].Entity
}
if ($currentWave) {
    $progPath = Join-Path $SyncHostRoot ("data\id-maps\{0}\{1}.sync-progress.json" -f $idMapSubDir, $currentWave)
    if (Test-Path -LiteralPath $progPath) {
        try {
            $progressJson = (Get-Content -LiteralPath $progPath -Raw -Encoding UTF8).Trim()
        } catch {}
    }
}

$fileWavesJson = ''
$fileWavesPath = Join-Path $SyncHostRoot 'file-waves-status.json'
if (Test-Path -LiteralPath $fileWavesPath) {
    try {
        $fileWavesJson = (Get-Content -LiteralPath $fileWavesPath -Raw -Encoding UTF8).Trim()
    } catch {}
}

$payload = @{
    StatusExists   = [bool](Test-Path -LiteralPath $statusPath)
    StatusPath     = $statusPath
    DataImporters  = @($diList)
    TaskState      = $taskState
    TaskLastResult = $taskLast
    TaskName       = $taskName
    DbCountLines   = @($dbLines)
    ProgressJson   = $progressJson
    FileWavesJson  = $fileWavesJson
}
# Depth 3, compact — keep small for SSH
$payload | ConvertTo-Json -Compress -Depth 6