# Generates tenant subcontractor.calik-energi.json from VISA2015.
# UNION: dbo.Tasaron names (when table exists) + dbo.Person.IDNumber text + seed subcontractor.json
#Requires -Version 5.1
param(
    [string]$SqlServer,
    [string]$Database = 'VISA2015'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-LegacySqlEndpoint {
    param([string]$Server, [string]$Db)
    $conn = $env:VISA2014_SQL_CONNECTION
    if ($conn -match '(?i)Server\s*=\s*([^;]+)') { $Server = $matches[1].Trim() }
    if ($conn -match '(?i)(?:Database|Initial Catalog)\s*=\s*([^;]+)') { $Db = $matches[1].Trim() }
    if ([string]::IsNullOrWhiteSpace($Server)) { $Server = 'localhost\SQLEXPRESS' }
    return @{ Server = $Server; Database = $Db }
}

$password = $env:VISA2014_SQL_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) { throw 'Set VISA2014_SQL_PASSWORD before running this script.' }

$resolved = Resolve-LegacySqlEndpoint -Server $SqlServer -Db $Database
$SqlServer = $resolved.Server
$Database = $resolved.Database

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$outFile = Join-Path $tenantDir 'subcontractor.calik-energi.json'
$seedFile = Join-Path $tenantDir 'subcontractor.json'

function Get-DistinctLabels([string]$query) {
    $tempCsv = [System.IO.Path]::GetTempFileName()
    try {
        & sqlcmd -S $SqlServer -U ReadOnlyUser -P $password -d $Database -C `
            -y 0 -s "`t" -Q $query -o $tempCsv -f o:65001 | Out-Null
        $labels = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        Get-Content -LiteralPath $tempCsv -Encoding UTF8 |
            Where-Object { $_ -and $_ -notmatch '^\(\d+ rows affected\)$' -and $_ -notmatch '^\s*$' } |
            ForEach-Object {
                $label = ($_ -split "`t", 2)[0].Trim()
                if ($label -and $label -ne 'NULL') { [void]$labels.Add($label) }
            }
        return $labels
    }
    finally {
        if (Test-Path $tempCsv) { Remove-Item -LiteralPath $tempCsv -Force }
    }
}

$labels = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)

$idQuery = @"
SELECT DISTINCT LTRIM(RTRIM(p.IDNumber)) AS label
FROM dbo.Person p
WHERE p.GCRecord IS NULL AND NULLIF(LTRIM(RTRIM(p.IDNumber)), '') IS NOT NULL
"@
foreach ($l in (Get-DistinctLabels $idQuery)) { [void]$labels.Add($l) }

$tasaronQuery = @"
IF OBJECT_ID('dbo.Tasaron','U') IS NOT NULL
BEGIN
    DECLARE @nameCol sysname;
    SELECT TOP 1 @nameCol = c.name FROM sys.columns c JOIN sys.tables t ON c.object_id=t.object_id
    WHERE t.name='Tasaron' AND c.name LIKE 'Name%';
    IF @nameCol IS NOT NULL
    BEGIN
        DECLARE @sql nvarchar(max) = N'SELECT DISTINCT LTRIM(RTRIM(' + QUOTENAME(@nameCol) + N')) FROM dbo.Tasaron WHERE GCRecord IS NULL AND NULLIF(LTRIM(RTRIM(' + QUOTENAME(@nameCol) + N')), '''''''') IS NOT NULL';
        EXEC sp_executesql @sql;
    END
END
"@
foreach ($l in (Get-DistinctLabels $tasaronQuery)) { [void]$labels.Add($l) }

$defaultName = 'Çalyk Enerji'
if (Test-Path $seedFile) {
    $seed = Get-Content -LiteralPath $seedFile -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($row in $seed.rows) {
        if (-not [string]::IsNullOrWhiteSpace([string]$row.NameTm)) { [void]$labels.Add([string]$row.NameTm.Trim()) }
    }
    $seedDefault = ($seed.rows | Where-Object { $_.IsDefault -eq $true } | Select-Object -First 1).NameTm
    if ($seedDefault) { $defaultName = [string]$seedDefault }
}

$rows = $labels | Sort-Object { $_.ToLowerInvariant() } | ForEach-Object {
    $isDefault = ($_.Trim() -eq $defaultName.Trim())
    [ordered]@{ NameTm = $_; IsDefault = $isDefault }
}
$json = (@{ rows = @($rows) } | ConvertTo-Json -Depth 4)
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outFile, $json, $utf8)
Write-Host "Wrote $($labels.Count) subcontractor rows to $outFile"
