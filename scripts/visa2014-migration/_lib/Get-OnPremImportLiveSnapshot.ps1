#Requires -Version 5.1
# Runs ON the sync host (.25). Emits compact JSON (no StatusJson — watcher SCPs that file).
[CmdletBinding()]
param(
    [ValidateSet('Production', 'Staging', 'Demo')]
    [string]$Profile = 'Demo',
    [string]$SyncHostRoot = '',
    [switch]$NoDbCounts
)

$ErrorActionPreference = 'Continue'

if ([string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    $SyncHostRoot = switch ($Profile) {
        'Staging' { 'C:\visa2026-sync-staging' }
        'Demo' { 'C:\visa2026-sync-demo' }
        default { 'C:\visa2026-sync' }
    }
}

$dbName = switch ($Profile) {
    'Staging' { 'Visa2026DbStaging' }
    'Demo' { 'Visa2026DbDemo' }
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

$diList = @()
Get-Process -Name 'Visa2026.DataImporter' -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        $cim = Get-CimInstance Win32_Process -Filter ("ProcessId=" + $_.Id) -ErrorAction SilentlyContinue
        $path = if ($cim) { $cim.ExecutablePath } else { '' }
        $cmd = if ($cim) { $cim.CommandLine } else { '' }
        if ($path -like ("*\" + $rootLeaf + "\*") -or $cmd -like ("*" + $rootLeaf + "*")) {
            $entity = ''
            if ($cmd -match '--entity\s+(\w+)') { $entity = $Matches[1] }
            $diList += @{ Pid = $_.Id; Entity = $entity }
        }
    } catch {}
}

$taskName = switch ($Profile) {
    'Demo' { 'Visa2026-OnPrem-DemoImportOnce' }
    default { 'Visa2026-OnPrem-ManualSyncOnce' }
}
$taskState = ''
$taskLast = ''
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($task) {
    $ti = $task | Get-ScheduledTaskInfo
    $taskState = [string]$task.State
    $taskLast = [string]$ti.LastTaskResult
}

$dbLines = @()
if (-not $NoDbCounts) {
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

# Live wave progress sidecar (written by DataImporter every ~100 rows; survives stdout buffering)
$progressJson = ''
$idMapSubDir = switch ($Profile) {
    'Staging' { 'calik-energi-onprem-staging' }
    'Demo' { 'calik-energi-onprem-demo' }
    default { 'calik-energi-onprem-prod' }
}
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

$payload = @{
    StatusExists   = [bool](Test-Path -LiteralPath $statusPath)
    StatusPath     = $statusPath
    DataImporters  = @($diList)
    TaskState      = $taskState
    TaskLastResult = $taskLast
    DbCountLines   = @($dbLines)
    ProgressJson   = $progressJson
}
# Depth 3, compact — keep small for SSH
$payload | ConvertTo-Json -Compress -Depth 4