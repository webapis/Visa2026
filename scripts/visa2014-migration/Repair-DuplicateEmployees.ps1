#Requires -Version 5.1
<#
.SYNOPSIS
  Preview or merge duplicate employee Persons (bootstrap + supplement twins) on Visa2026 prod.

.DESCRIPTION
  Finds duplicate employees with the same FirstName + LastName + DateOfBirth.
  Default scope BootstrapSupplement targets prod calik-energi bootstrap (.…d7f5) + supplement (.…aadd) pairs (~41 groups).
  Keeps MIN(Person.ID), repoints all FK columns to People, dedupes child rows, soft-deletes extras.
  Optionally repairs Person.json id-map (legacy OID -> canonical Person) when -Apply -UpdateIdMap.

  SQL: cleanup/DuplicateEmployeesByIdentity.sql

.EXAMPLE
  $env:VISA2026_PROD_SQL_CONNECTION = 'Server=10.100.128.25\SQLEXPRESS;Database=Visa2026DbProd;...'
  .\scripts\visa2014-migration\Repair-DuplicateEmployees.ps1

.EXAMPLE
  .\scripts\visa2014-migration\Repair-DuplicateEmployees.ps1 -Apply -PersonIdMapPath C:\visa2026-sync\data\id-maps\calik-energi-onprem-prod\Person.json

.EXAMPLE
  .\scripts\visa2014-migration\Repair-DuplicateEmployees.ps1 -Scope AllIdentity
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$TargetConnection = $env:VISA2026_PROD_SQL_CONNECTION,
    [string]$TargetServer = '10.100.128.25\SQLEXPRESS',
    [string]$TargetDatabase = 'Visa2026DbProd',
    [string]$TargetUser = 'sa',
    [string]$TargetPassword = '',
    [ValidateSet('BootstrapSupplement', 'AllIdentity')]
    [string]$Scope = 'BootstrapSupplement',
    [string]$PersonIdMapPath = '',
    [switch]$Apply,
    [switch]$UpdateIdMap
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_lib\Get-RepoRoot.ps1')

function Get-SqlConnectionParts {
    param([string]$ConnectionString)
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString
    return [ordered]@{
        Server   = if ($builder.ContainsKey('Data Source') -and $builder.'Data Source') { $builder.'Data Source' } else { $builder.Server }
        Database = if ($builder.ContainsKey('Initial Catalog') -and $builder.'Initial Catalog') { $builder.'Initial Catalog' } else { $builder.Database }
        User     = $builder.'User ID'
        Password = $builder.Password
    }
}

function Get-MergePairsFromProd {
    param(
        [string]$Server,
        [string]$Database,
        [string]$User,
        [string]$Password,
        [string]$ScopeName
    )

    $pairSql = @"
SET NOCOUNT ON;
DECLARE @Scope varchar(32) = N'$ScopeName';
;WITH Emp AS (
    SELECT p.ID, UPPER(LTRIM(RTRIM(p.FirstName))) AS Fn, UPPER(LTRIM(RTRIM(p.LastName))) AS Ln,
           CAST(p.DateOfBirth AS date) AS Dob, RIGHT(LOWER(CAST(p.ID AS varchar(36))), 4) AS IdSuffix
    FROM dbo.People p
    WHERE (p.GCRecord IS NULL OR p.GCRecord = 0) AND p.IsEmployee = 1
),
DupKeys AS (
    SELECT Fn, Ln, Dob FROM Emp GROUP BY Fn, Ln, Dob HAVING COUNT(*) > 1
),
Scoped AS (
    SELECT e.* FROM Emp e
    INNER JOIN DupKeys d ON d.Fn = e.Fn AND d.Ln = e.Ln AND d.Dob = e.Dob
    WHERE @Scope = N'AllIdentity'
       OR (
            @Scope = N'BootstrapSupplement'
            AND (SELECT COUNT(*) FROM Emp e2 WHERE e2.Fn = e.Fn AND e2.Ln = e.Ln AND e2.Dob = e.Dob) = 2
            AND (SELECT COUNT(*) FROM Emp e2 WHERE e2.Fn = e.Fn AND e2.Ln = e.Ln AND e2.Dob = e.Dob AND e2.IdSuffix = N'd7f5') = 1
            AND (SELECT COUNT(*) FROM Emp e2 WHERE e2.Fn = e.Fn AND e2.Ln = e.Ln AND e2.Dob = e.Dob AND e2.IdSuffix = N'aadd') = 1
          )
),
Groups AS (
    SELECT s.Fn, s.Ln, s.Dob, MIN(s.ID) AS KeepId
    FROM Scoped s
    GROUP BY s.Fn, s.Ln, s.Dob
)
SELECT CAST(g.KeepId AS varchar(36)) AS KeepId, CAST(e.ID AS varchar(36)) AS ExtraId
FROM Scoped e
INNER JOIN Groups g ON g.Fn = e.Fn AND g.Ln = e.Ln AND g.Dob = e.Dob
WHERE e.ID <> g.KeepId
ORDER BY KeepId, ExtraId;
"@

    $tmp = [System.IO.Path]::GetTempFileName() + '.sql'
    Set-Content -LiteralPath $tmp -Value $pairSql -Encoding UTF8
    try {
        $raw = & sqlcmd -S $Server -U $User -P $Password -d $Database -C -i $tmp -W -s '|' -h -1
        if ($LASTEXITCODE -ne 0) { throw "sqlcmd pair query failed with exit code $LASTEXITCODE" }
    }
    finally {
        Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
    }

    $pairs = @()
    foreach ($line in $raw) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -match '^(KeepId|ExtraId|-+)') { continue }
        $parts = $line -split '\|'
        if ($parts.Count -lt 2) { continue }
        $pairs += [pscustomobject]@{ KeepId = $parts[0].Trim(); ExtraId = $parts[1].Trim() }
    }
    return $pairs
}

function Update-PersonIdMapMerge {
    param(
        [string]$MapPath,
        [array]$Pairs
    )

    if (-not (Test-Path -LiteralPath $MapPath)) {
        throw "Person id-map not found: $MapPath"
    }

    $backupPath = "$MapPath.bak-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item -LiteralPath $MapPath -Destination $backupPath -Force

    $map = Get-Content -LiteralPath $MapPath -Raw | ConvertFrom-Json
    $changed = 0
    $extraIdSet = @{}
    foreach ($p in $Pairs) { $extraIdSet[$p.ExtraId.ToLowerInvariant()] = $p.KeepId.ToLowerInvariant() }

    foreach ($prop in @($map.PSObject.Properties)) {
        $value = [string]$prop.Value
        if ($extraIdSet.ContainsKey($value.ToLowerInvariant())) {
            $prop.Value = $extraIdSet[$value.ToLowerInvariant()]
            $changed++
        }
    }

    $json = $map | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText($MapPath, $json, (New-Object System.Text.UTF8Encoding $false))
    Write-Host "INF Person id-map updated: $changed value(s) remapped; backup $backupPath" -ForegroundColor Green
}

if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
    $TargetConnection = [Environment]::GetEnvironmentVariable('VISA2026_PROD_SQL_CONNECTION', 'User')
}
if (-not [string]::IsNullOrWhiteSpace($TargetConnection)) {
    $parts = Get-SqlConnectionParts $TargetConnection
    $TargetServer = $parts.Server
    $TargetDatabase = $parts.Database
    if ($parts.User) { $TargetUser = $parts.User }
    if ($parts.Password) { $TargetPassword = $parts.Password }
}
if ([string]::IsNullOrWhiteSpace($TargetPassword)) {
    throw 'Set VISA2026_PROD_SQL_CONNECTION or -TargetPassword for prod SQL.'
}

$sqlPath = Join-Path $PSScriptRoot 'cleanup\DuplicateEmployeesByIdentity.sql'
$sql = Get-Content -LiteralPath $sqlPath -Raw
$applyBit = if ($Apply) { 1 } else { 0 }
$sql = $sql -replace 'DECLARE @Apply bit = 0;', "DECLARE @Apply bit = $applyBit;"
$sql = $sql -replace "DECLARE @Scope varchar\(32\) = N'BootstrapSupplement';", "DECLARE @Scope varchar(32) = N'$Scope';"

$mode = if ($Apply) { 'APPLY (merge + soft-delete extras)' } else { 'PREVIEW' }
Write-Host "=== Duplicate Employees by identity ($mode) ===" -ForegroundColor Cyan
Write-Host "Target: $TargetServer / $TargetDatabase  Scope: $Scope" -ForegroundColor DarkGray

$pairs = Get-MergePairsFromProd -Server $TargetServer -Database $TargetDatabase -User $TargetUser -Password $TargetPassword -ScopeName $Scope
Write-Host "INF Merge pairs in scope: $($pairs.Count)" -ForegroundColor DarkGray

if ($Apply) {
    if (-not $PSCmdlet.ShouldProcess($TargetDatabase, "Merge $($pairs.Count) duplicate employee Person row(s)")) { return }
    Write-Host 'Applying SQL in 5 seconds... Ctrl+C to abort.' -ForegroundColor Yellow
    Start-Sleep -Seconds 5
}

$tmp = [System.IO.Path]::GetTempFileName() + '.sql'
Set-Content -LiteralPath $tmp -Value $sql -Encoding UTF8
try {
    & sqlcmd -S $TargetServer -U $TargetUser -P $TargetPassword -d $TargetDatabase -C -i $tmp
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE" }
}
finally {
    Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
}

if ($Apply -and $UpdateIdMap) {
    if ([string]::IsNullOrWhiteSpace($PersonIdMapPath)) {
        Write-Warning 'UpdateIdMap requested but -PersonIdMapPath not set — skipped id-map repair.'
    }
    elseif ($pairs.Count -gt 0) {
        Update-PersonIdMapMerge -MapPath $PersonIdMapPath -Pairs $pairs
    }
}

if (-not $Apply) {
    Write-Host ''
    Write-Host 'Review the sample rows above. To apply (default BootstrapSupplement scope):' -ForegroundColor Green
    Write-Host '  .\scripts\visa2014-migration\Repair-DuplicateEmployees.ps1 -Apply -UpdateIdMap -PersonIdMapPath <path\to\Person.json>' -ForegroundColor Green
}