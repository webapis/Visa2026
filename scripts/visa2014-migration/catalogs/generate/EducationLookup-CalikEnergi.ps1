# Generates tenant education-institution.calik-energi.json and specialty.calik-energi.json from VISA2015.
# DISTINCT labels on active Education rows + union with existing tenant seed rows.
#Requires -Version 5.1
. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$password = $env:VISA2014_SQL_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) {
    throw 'Set VISA2014_SQL_PASSWORD before running this script.'
}
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$instOut = Join-Path $tenantDir 'education-institution.calik-energi.json'
$specOut = Join-Path $tenantDir 'specialty.calik-energi.json'
$instSeed = Join-Path $tenantDir 'education-institution.json'
$specSeed = Join-Path $tenantDir 'specialty.json'

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

$instQuery = @'
SELECT LTRIM(RTRIM(ei.TitleOfIEducationInstitution)) AS label
FROM dbo.Education e
INNER JOIN dbo.EducationInstitution ei ON e.EducationInstitution = ei.Oid
WHERE e.GCRecord IS NULL AND ei.TitleOfIEducationInstitution IS NOT NULL
GROUP BY LTRIM(RTRIM(ei.TitleOfIEducationInstitution))
'@

$specQuery = @'
SELECT LTRIM(RTRIM(s.TitleOfSpeciality)) AS label
FROM dbo.Education e
INNER JOIN dbo.Speciality s ON e.Spcialty = s.Oid
WHERE e.GCRecord IS NULL AND s.TitleOfSpeciality IS NOT NULL
GROUP BY LTRIM(RTRIM(s.TitleOfSpeciality))
'@

$instLabels = Get-DistinctLabels $instQuery
$specLabels = Get-DistinctLabels $specQuery
$instFromEducation = $instLabels.Count
$specFromEducation = $specLabels.Count

Add-SeedLabels $instLabels $instSeed
Add-SeedLabels $specLabels $specSeed

function Write-Catalog([string]$path, [System.Collections.Generic.HashSet[string]]$labels) {
    $rows = $labels | Sort-Object { $_.ToLowerInvariant() } | ForEach-Object { [ordered]@{ NameTm = $_ } }
    $json = (@{ rows = @($rows) } | ConvertTo-Json -Depth 4)
    [System.IO.File]::WriteAllText($path, $json, (New-Object System.Text.UTF8Encoding $false))
}

Write-Catalog $instOut $instLabels
Write-Catalog $specOut $specLabels

Write-Host "Wrote $($instLabels.Count) institution rows to $instOut ($instFromEducation from Education + seed union)"
Write-Host "Wrote $($specLabels.Count) specialty rows to $specOut ($specFromEducation from Education + seed union)"
