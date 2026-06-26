# Generates tenant position.calik-energi.json and department.calik-energi.json from VISA2015.
# DISTINCT labels on active WorkHistoryOfEmployee rows + union with existing tenant seed rows.
#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$password = $env:VISA2014_SQL_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) {
    throw 'Set VISA2014_SQL_PASSWORD before running this script.'
}

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$posOut = Join-Path $tenantDir 'position.calik-energi.json'
$depOut = Join-Path $tenantDir 'department.calik-energi.json'
$posSeed = Join-Path $tenantDir 'position.json'
$depSeed = Join-Path $tenantDir 'department.json'

function Get-DistinctLabels([string]$query) {
    $tempCsv = [System.IO.Path]::GetTempFileName()
    try {
        & sqlcmd -S 'localhost\SQLEXPRESS' -U ReadOnlyUser -P $password -d VISA2015 -C `
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

function Add-SeedLabels([System.Collections.Generic.HashSet[string]]$set, [string]$jsonPath) {
    if (-not (Test-Path $jsonPath)) { return }
    $doc = Get-Content -LiteralPath $jsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($row in $doc.rows) {
        $name = [string]$row.NameTm
        if (-not [string]::IsNullOrWhiteSpace($name)) { [void]$set.Add($name.Trim()) }
    }
}

$posQuery = @'
SELECT LTRIM(RTRIM(pos.TitleOfPosition)) AS label
FROM dbo.WorkHistoryOfEmployee w
INNER JOIN dbo.Position pos ON w.Position = pos.Oid
WHERE w.GCRecord IS NULL AND pos.TitleOfPosition IS NOT NULL
GROUP BY LTRIM(RTRIM(pos.TitleOfPosition))
'@

$depQuery = @'
SELECT LTRIM(RTRIM(dep.TitleOfDepartment)) AS label
FROM dbo.WorkHistoryOfEmployee w
INNER JOIN dbo.Department dep ON w.Department = dep.Oid
WHERE w.GCRecord IS NULL AND dep.TitleOfDepartment IS NOT NULL
GROUP BY LTRIM(RTRIM(dep.TitleOfDepartment))
'@

$posLabels = Get-DistinctLabels $posQuery
$depLabels = Get-DistinctLabels $depQuery
$posFromWorkHistory = $posLabels.Count
$depFromWorkHistory = $depLabels.Count

Add-SeedLabels $posLabels $posSeed
Add-SeedLabels $depLabels $depSeed

function Write-Catalog([string]$path, [System.Collections.Generic.HashSet[string]]$labels) {
    $rows = $labels | Sort-Object { $_.ToLowerInvariant() } | ForEach-Object { [ordered]@{ NameTm = $_ } }
    $json = (@{ rows = @($rows) } | ConvertTo-Json -Depth 4)
    [System.IO.File]::WriteAllText($path, $json, (New-Object System.Text.UTF8Encoding $false))
}

Write-Catalog $posOut $posLabels
Write-Catalog $depOut $depLabels

Write-Host "Wrote $($posLabels.Count) position rows to $posOut ($posFromWorkHistory from WorkHistory + seed union)"
Write-Host "Wrote $($depLabels.Count) department rows to $depOut ($depFromWorkHistory from WorkHistory + seed union)"
