# Seed EducationInstitution / Specialty NameTm gaps from live VISA2015 into local PostgreSQL
# and append the same labels to calik-energi tenant JSON (no full ConvertTo-Json rewrite).
# Calik Energi source is always 10.100.128.15 / VISA2015 — never Demo/Prod Visa2026 DBs.
#Requires -Version 5.1
param(
    [string]$LegacyServer = '10.100.128.15',
    [string]$PgHost = 'localhost',
    [int]$PgPort = 5432,
    [string]$PgDatabase = 'visa2026',
    [string]$PgUser = 'postgres',
    [string]$PgPassword = 'Visa2026Local',
    [switch]$DryRun
)

. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$password = $env:VISA2014_SQL_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) {
    throw 'Set VISA2014_SQL_PASSWORD before running this script.'
}

function Get-SqlCmdPath {
    $cmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $candidates = @(
        'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE',
        'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE',
        'C:\Program Files\Microsoft SQL Server\150\Tools\Binn\SQLCMD.EXE'
    )
    foreach ($path in $candidates) {
        if (Test-Path -LiteralPath $path) { return $path }
    }
    throw 'sqlcmd.exe not found.'
}

function Get-PsqlPath {
    $candidates = @(
        'C:\PostgreSQL\16\bin\psql.exe',
        'C:\Program Files\PostgreSQL\16\bin\psql.exe',
        'C:\Program Files\PostgreSQL\15\bin\psql.exe'
    )
    foreach ($path in $candidates) {
        if (Test-Path -LiteralPath $path) { return $path }
    }
    $cmd = Get-Command psql -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    throw 'psql.exe not found.'
}

function Get-DistinctLegacyLabels {
    param(
        [string]$SqlCmdPath,
        [string]$Query
    )
    $tempCsv = [System.IO.Path]::GetTempFileName()
    try {
        & $SqlCmdPath -S $LegacyServer -U ReadOnlyUser -P $password -d VISA2015 -C `
            -y 0 -s "`t" -Q $Query -o $tempCsv -f o:65001 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd failed against $LegacyServer / VISA2015 (exit $LASTEXITCODE)."
        }
        $labels = New-Object 'System.Collections.Generic.List[string]'
        $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        Get-Content -LiteralPath $tempCsv -Encoding UTF8 |
            Where-Object { $_ -and $_ -notmatch '^\(\d+ rows affected\)$' -and $_ -notmatch '^\s*$' -and $_ -notmatch '^label\s*$' } |
            ForEach-Object {
                $label = ($_ -split "`t", 2)[0].Trim()
                if ($label -and $label -ne 'NULL' -and $seen.Add($label)) {
                    $labels.Add($label)
                }
            }
        return $labels
    }
    finally {
        if (Test-Path -LiteralPath $tempCsv) { Remove-Item -LiteralPath $tempCsv -Force }
    }
}

function Get-PgNameTmSet {
    param(
        [string]$PsqlPath,
        [string]$TableName
    )
    $env:PGPASSWORD = $PgPassword
    $env:PGCLIENTENCODING = 'UTF8'
    $sql = "SELECT ""NameTm"" FROM ""$TableName"" WHERE ""GCRecord"" IS NULL OR ""GCRecord"" = 0;"
    $sqlPath = Join-Path $env:TEMP ("visa2026-select-" + $TableName + ".sql")
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($sqlPath, $sql, $utf8)
    $outPath = Join-Path $env:TEMP ("visa2026-select-" + $TableName + "-out.txt")
    & $PsqlPath -h $PgHost -p $PgPort -U $PgUser -d $PgDatabase -A -t -q -v ON_ERROR_STOP=1 -f $sqlPath -o $outPath
    if ($LASTEXITCODE -ne 0) {
        throw "psql SELECT from $TableName failed (exit $LASTEXITCODE)."
    }
    $set = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($line in [System.IO.File]::ReadAllLines($outPath, $utf8)) {
        $name = $line.Trim()
        if ($name) { [void]$set.Add($name) }
    }
    return $set
}

function Get-JsonNameTmSet {
    param([string]$JsonPath)
    $set = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    if (-not (Test-Path -LiteralPath $JsonPath)) { return $set }
    $doc = Get-Content -LiteralPath $JsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($row in @($doc.rows)) {
        $name = [string]$row.NameTm
        if (-not [string]::IsNullOrWhiteSpace($name)) { [void]$set.Add($name.Trim()) }
    }
    return $set
}

function Get-MissingLabels {
    param(
        [System.Collections.Generic.List[string]]$Legacy,
        [System.Collections.Generic.HashSet[string]]$Existing
    )
    $missing = New-Object 'System.Collections.Generic.List[string]'
    foreach ($label in $Legacy) {
        if (-not $Existing.Contains($label)) { $missing.Add($label) }
    }
    return $missing
}

function ConvertTo-PgLiteral {
    param([string]$Value)
    $tag = 'visa'
    while ($Value.Contains('$' + $tag + '$')) { $tag = $tag + 'x' }
    return ('$' + $tag + '$' + $Value + '$' + $tag + '$')
}

function ConvertTo-JsonNameTmBlock {
    param([string]$Value)
    $escaped = $Value.Replace('\', '\\').Replace('"', '\"')
    return "                 {`r`n                     `"NameTm`":  `"$escaped`"`r`n                 }"
}

function Add-PgLookupRows {
    param(
        [string]$PsqlPath,
        [string]$TableName,
        [System.Collections.Generic.List[string]]$Labels
    )
    if ($Labels.Count -eq 0) { return }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('BEGIN;')
    foreach ($label in $Labels) {
        if ($label.Length -gt 200) {
            throw "$TableName NameTm exceeds 200 chars: $label"
        }
        $lit = ConvertTo-PgLiteral $label
        $id = [guid]::NewGuid().ToString()
        [void]$sb.AppendLine(@"
INSERT INTO "$TableName" ("ID", "NameTm", "Name", "IsDefault")
SELECT '$id'::uuid, $lit, $lit, FALSE
WHERE NOT EXISTS (
    SELECT 1 FROM "$TableName" e
    WHERE e."NameTm" = $lit AND (e."GCRecord" IS NULL OR e."GCRecord" = 0)
);
"@)
    }
    [void]$sb.AppendLine('COMMIT;')
    $sqlPath = Join-Path $env:TEMP ("visa2026-seed-" + $TableName + ".sql")
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($sqlPath, $sb.ToString(), $utf8)
    $env:PGPASSWORD = $PgPassword
    $env:PGCLIENTENCODING = 'UTF8'
    & $PsqlPath -h $PgHost -p $PgPort -U $PgUser -d $PgDatabase -v ON_ERROR_STOP=1 -f $sqlPath | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "psql INSERT into $TableName failed (exit $LASTEXITCODE). SQL: $sqlPath"
    }
}

function Add-JsonNameTmRows {
    param(
        [string]$JsonPath,
        [System.Collections.Generic.List[string]]$Labels
    )
    if ($Labels.Count -eq 0) { return }
    $utf8 = New-Object System.Text.UTF8Encoding $false
    $text = [System.IO.File]::ReadAllText($JsonPath, $utf8)
    $insert = New-Object System.Collections.Generic.List[string]
    foreach ($label in $Labels) {
        $insert.Add((ConvertTo-JsonNameTmBlock $label))
    }
    $block = ",`r`n" + [string]::Join(",`r`n", $insert)
    $idx = $text.LastIndexOf(']')
    if ($idx -lt 0) { throw "No closing array in $JsonPath" }
    $before = $text.Substring(0, $idx)
    $before = $before.TrimEnd()
    if (-not $before.EndsWith('}')) {
        throw "Unexpected JSON tail in $JsonPath"
    }
    $after = $text.Substring($idx)
    $updated = $before + $block + "`r`n" + $after.TrimStart()
    [System.IO.File]::WriteAllText($JsonPath, $updated, $utf8)
}

function Copy-TenantOverlay {
    param(
        [string]$SrcInst,
        [string]$SrcSpec,
        [string]$DestDir
    )
    if (-not (Test-Path -LiteralPath $DestDir)) {
        New-Item -ItemType Directory -Force -Path $DestDir | Out-Null
    }
    Copy-Item -Force $SrcInst (Join-Path $DestDir 'education-institution.json')
    Copy-Item -Force $SrcSpec (Join-Path $DestDir 'specialty.json')
}

$sqlcmd = Get-SqlCmdPath
$psql = Get-PsqlPath
Write-Host "sqlcmd=$sqlcmd"
Write-Host "psql=$psql"
Write-Host "legacy=$LegacyServer / VISA2015"
Write-Host "target=$PgHost`:$PgPort/$PgDatabase"

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

$legacyInst = Get-DistinctLegacyLabels -SqlCmdPath $sqlcmd -Query $instQuery
$legacySpec = Get-DistinctLegacyLabels -SqlCmdPath $sqlcmd -Query $specQuery
Write-Host ("legacy DISTINCT institutions={0} specialties={1}" -f $legacyInst.Count, $legacySpec.Count)

$pgInst = Get-PgNameTmSet -PsqlPath $psql -TableName 'EducationInstitutions'
$pgSpec = Get-PgNameTmSet -PsqlPath $psql -TableName 'Specialties'
Write-Host ("PG NameTm institutions={0} specialties={1}" -f $pgInst.Count, $pgSpec.Count)

$missingInst = Get-MissingLabels -Legacy $legacyInst -Existing $pgInst
$missingSpec = Get-MissingLabels -Legacy $legacySpec -Existing $pgSpec
Write-Host ("PG gaps institutions={0} specialties={1}" -f $missingInst.Count, $missingSpec.Count)
foreach ($n in $missingInst) { Write-Host "  INST $n" }
foreach ($n in $missingSpec) { Write-Host "  SPEC $n" }

$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$instCalik = Join-Path $tenantDir 'education-institution.calik-energi.json'
$specCalik = Join-Path $tenantDir 'specialty.calik-energi.json'
$instEmbedded = Join-Path $tenantDir 'education-institution.json'
$specEmbedded = Join-Path $tenantDir 'specialty.json'

$jsonInst = Get-JsonNameTmSet -JsonPath $instCalik
$jsonSpec = Get-JsonNameTmSet -JsonPath $specCalik
$jsonMissingInst = Get-MissingLabels -Legacy $legacyInst -Existing $jsonInst
$jsonMissingSpec = Get-MissingLabels -Legacy $legacySpec -Existing $jsonSpec
Write-Host ("JSON gaps institutions={0} specialties={1}" -f $jsonMissingInst.Count, $jsonMissingSpec.Count)

if ($DryRun) {
    Write-Host 'DryRun: no INSERT / JSON write.'
    return
}

Add-PgLookupRows -PsqlPath $psql -TableName 'EducationInstitutions' -Labels $missingInst
Add-PgLookupRows -PsqlPath $psql -TableName 'Specialties' -Labels $missingSpec
Add-JsonNameTmRows -JsonPath $instCalik -Labels $jsonMissingInst
Add-JsonNameTmRows -JsonPath $specCalik -Labels $jsonMissingSpec
Copy-Item -Force $instCalik $instEmbedded
Copy-Item -Force $specCalik $specEmbedded

$overlayDirs = @(
    (Join-Path $repoRoot 'Visa2026.Blazor.Server\bin\Debug\net8.0\LookupCatalogs\tenant'),
    (Join-Path $repoRoot 'Visa2026.DataImporter\bin\Debug\net8.0\LookupCatalogs\tenant')
)
foreach ($dir in $overlayDirs) {
    Copy-TenantOverlay -SrcInst $instEmbedded -SrcSpec $specEmbedded -DestDir $dir
    Write-Host "overlay $dir"
}

Write-Host 'Seed complete.'
