#Requires -Version 5.1
<#
.SYNOPSIS
  Break down WorkPermitItem import gaps: legacy WorkPermit rows not in id-map vs missing parent FK id-maps.
#>
[CmdletBinding()]
param(
    [string]$LegacyServer = '10.100.128.15',
    [string]$LegacyDatabase = 'VISA2015',
    [string]$LegacyUser = 'ReadOnlyUser',
    [string]$LegacyPassword = '',
    [string]$LegacySource = 'calik-energi-onprem-prod',
    [string]$MapRoot = ''
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_lib\Get-RepoRoot.ps1')

function Read-IdMap([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return @{} }
    $raw = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $map = @{}
    foreach ($prop in $raw.PSObject.Properties) {
        $legacyKey = [guid]::Empty
        $targetId = [guid]::Empty
        $keyText = "$($prop.Name)"
        $valText = "$($prop.Value)"
        if ([guid]::TryParse($keyText, [ref]$legacyKey) -and [guid]::TryParse($valText, [ref]$targetId)) {
            $map[$legacyKey] = $targetId
        }
    }
    return $map
}

if ([string]::IsNullOrWhiteSpace($LegacyPassword)) {
    $LegacyPassword = [Environment]::GetEnvironmentVariable('SQL_SERVER_10.100.128.15', 'User')
    if (-not $LegacyPassword) {
        $LegacyPassword = [Environment]::GetEnvironmentVariable('VISA2014_SQL_PASSWORD', 'User')
    }
}
if ([string]::IsNullOrWhiteSpace($LegacyPassword)) {
    throw 'Set SQL_SERVER_10.100.128.15 or VISA2014_SQL_PASSWORD for legacy SQL.'
}

if ([string]::IsNullOrWhiteSpace($MapRoot)) {
    $repoRoot = Get-Visa2026RepoRoot
    $MapRoot = Join-Path $repoRoot "Visa2026.DataImporter\legacy\visa2014\id-maps\$LegacySource"
}

$personMap = Read-IdMap (Join-Path $MapRoot 'Person.json')
$passportMap = Read-IdMap (Join-Path $MapRoot 'Passport.json')
$ephMap = Read-IdMap (Join-Path $MapRoot 'EmployeePositionHistory.json')
$workPermitMap = Read-IdMap (Join-Path $MapRoot 'WorkPermit.json')
$wpItemMap = Read-IdMap (Join-Path $MapRoot 'WorkPermitItem.json')

$legacyCs = "Server=$LegacyServer;Database=$LegacyDatabase;User Id=$LegacyUser;Password=$LegacyPassword;TrustServerCertificate=True;Encrypt=False"
$sql = @"
SELECT
    CAST(wp.Oid AS uniqueidentifier) AS LegacyOid,
    CAST(wp.Employee AS uniqueidentifier) AS EmployeeOid,
    CAST(wp.Passport AS uniqueidentifier) AS PassportOid,
    CAST(wp.Position AS uniqueidentifier) AS PositionOid,
    CAST(wp.WorkPermitLetter AS uniqueidentifier) AS WorkPermitLetterOid
FROM dbo.WorkPermit wp
WHERE wp.GCRecord IS NULL
"@

$rows = Invoke-Sqlcmd -ConnectionString $legacyCs -Query $sql -ErrorAction Stop

$stats = [ordered]@{
    LegacyRows                = $rows.Count
    AlreadyInWorkPermitItem   = 0
    ReadyToImport             = 0
    MissingPerson             = 0
    MissingPassport           = 0
    MissingPositionOid        = 0
    MissingEphIdMap           = 0
    MissingWorkPermitHeader   = 0
}

foreach ($row in $rows) {
    $legacyOid = [guid]$row.LegacyOid
    if ($wpItemMap.ContainsKey($legacyOid)) {
        $stats.AlreadyInWorkPermitItem++
        continue
    }

    $employeeOid = [guid]::Empty
    $passportOid = [guid]::Empty
    $positionOid = [guid]::Empty
    $letterOid = [guid]::Empty
    if ($row.EmployeeOid) { [void][guid]::TryParse("$($row.EmployeeOid)", [ref]$employeeOid) }
    if ($row.PassportOid) { [void][guid]::TryParse("$($row.PassportOid)", [ref]$passportOid) }
    if ($row.PositionOid) { [void][guid]::TryParse("$($row.PositionOid)", [ref]$positionOid) }
    if ($row.WorkPermitLetterOid) { [void][guid]::TryParse("$($row.WorkPermitLetterOid)", [ref]$letterOid) }

    if ($employeeOid -eq [guid]::Empty -or -not $personMap.ContainsKey($employeeOid)) {
        $stats.MissingPerson++
        continue
    }
    if ($passportOid -eq [guid]::Empty -or -not $passportMap.ContainsKey($passportOid)) {
        $stats.MissingPassport++
        continue
    }
    if ($positionOid -eq [guid]::Empty) {
        $stats.MissingPositionOid++
        continue
    }
    if (-not $ephMap.ContainsKey($positionOid)) {
        $stats.MissingEphIdMap++
        continue
    }
    if ($letterOid -eq [guid]::Empty -or -not $workPermitMap.ContainsKey($letterOid)) {
        $stats.MissingWorkPermitHeader++
        continue
    }

    $stats.ReadyToImport++
}

Write-Host "=== WorkPermitItem gap triage ($LegacySource) ===" -ForegroundColor Cyan
Write-Host "Id-maps: $MapRoot" -ForegroundColor DarkGray
$stats.GetEnumerator() | ForEach-Object {
    [pscustomobject]@{ Metric = $_.Key; Count = $_.Value }
} | Format-Table -AutoSize

$pending = $stats.LegacyRows - $stats.AlreadyInWorkPermitItem
Write-Host "Pending legacy rows (not in WorkPermitItem id-map): $pending" -ForegroundColor Yellow
Write-Host "Note: EPH check is direct id-map only; importer may still resolve rows via position fallback." -ForegroundColor DarkGray