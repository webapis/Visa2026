# Generates tenant project-contract.calik-energi.json from VISA2015 (Çalik Energi).
# Requires VISA2014_SQL_PASSWORD env and ReadOnlyUser on localhost\SQLEXPRESS / VISA2015.
#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$password = $env:VISA2014_SQL_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) {
    throw 'Set VISA2014_SQL_PASSWORD before running this script.'
}

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$outFile = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant\project-contract.calik-energi.json'

$query = @'
WITH person_codes AS (
  SELECT LTRIM(RTRIM(c.NumberOfContract)) AS code, COUNT(*) AS person_cnt
  FROM dbo.Person p INNER JOIN dbo.Contract c ON p.Contract = c.Oid
  WHERE p.GCRecord IS NULL AND c.GCRecord IS NULL
  GROUP BY LTRIM(RTRIM(c.NumberOfContract))
),
app_codes AS (
  SELECT LTRIM(RTRIM(c.NumberOfContract)) AS code, COUNT(*) AS app_cnt
  FROM dbo.Application a INNER JOIN dbo.Contract c ON a.Contract = c.Oid
  WHERE a.GCRecord IS NULL AND c.GCRecord IS NULL
  GROUP BY LTRIM(RTRIM(c.NumberOfContract))
),
union_codes AS (
  SELECT COALESCE(p.code, a.code) AS code,
         ISNULL(p.person_cnt, 0) AS person_cnt,
         ISNULL(a.app_cnt, 0) AS app_cnt
  FROM person_codes p
  FULL OUTER JOIN app_codes a ON p.code = a.code
),
contract_best AS (
  SELECT
    LTRIM(RTRIM(c.NumberOfContract)) AS code,
    c.ContentOfContract,
    m.TitleOfMinistery,
    m.TitleOfMinisteryL,
    ROW_NUMBER() OVER (
      PARTITION BY LTRIM(RTRIM(c.NumberOfContract))
      ORDER BY CASE WHEN c.AppliedMinistery IS NOT NULL THEN 0 ELSE 1 END, c.Oid
    ) AS rn
  FROM dbo.Contract c
  LEFT JOIN dbo.AppliedMinistery m ON c.AppliedMinistery = m.Oid
  WHERE c.GCRecord IS NULL
)
SELECT u.code, u.person_cnt, u.app_cnt,
       REPLACE(REPLACE(ISNULL(cb.ContentOfContract, ''), CHAR(13), ' '), CHAR(10), ' ') AS ContentOfContract,
       REPLACE(REPLACE(ISNULL(cb.TitleOfMinistery, ''), CHAR(13), ' '), CHAR(10), ' ') AS TitleOfMinistery,
       REPLACE(REPLACE(ISNULL(cb.TitleOfMinisteryL, ''), CHAR(13), ' '), CHAR(10), ' ') AS TitleOfMinisteryL
FROM union_codes u
LEFT JOIN contract_best cb ON u.code = cb.code AND cb.rn = 1
ORDER BY u.person_cnt + u.app_cnt DESC, u.code;
'@

$tempCsv = [System.IO.Path]::GetTempFileName()
try {
    & sqlcmd -S 'localhost\SQLEXPRESS' -U ReadOnlyUser -P $password -d VISA2015 -C `
        -y 0 -s "`t" -Q $query -o $tempCsv -f o:65001 | Out-Null

    $lines = Get-Content -LiteralPath $tempCsv -Encoding UTF8 |
        Where-Object { $_ -and $_ -notmatch '^\(\d+ rows affected\)$' -and $_ -notmatch '^\s*$' }

    function Split-SqlRow([string]$line) {
        $tab = "`t"
        $parts = $line -split $tab, 6
        if ($parts.Count -ge 6) { return $parts }
        return $null
    }

    $MinistryTe = 'T' + [char]0x00FC + 'rkmenenergo'
    $MinistryEn = 'Energetika'
    $MinistryGu = 'Gurlu' + [char]0x015F + 'yk'
    $MinistryTg = 'T' + [char]0x00FC + 'rkmengaz'

    function Fold-Turkmen([string]$s) {
        if ([string]::IsNullOrWhiteSpace($s)) { return '' }
        $map = @{
            ([char]0x00FD) = 'y'; ([char]0x00DD) = 'y'
            ([char]0x00E4) = 'a'; ([char]0x00C4) = 'a'
            ([char]0x00F6) = 'o'; ([char]0x00D6) = 'o'
            ([char]0x00FC) = 'u'; ([char]0x00DC) = 'u'
            ([char]0x00E7) = 'c'; ([char]0x00C7) = 'c'
            ([char]0x015F) = 's'; ([char]0x0160) = 's'
            ([char]0x0148) = 'n'; ([char]0x0147) = 'n'
            ([char]0x017E) = 'z'; ([char]0x017D) = 'z'
            ([char]0x00EE) = 'i'; ([char]0x00CE) = 'i'
        }
        $sb = New-Object System.Text.StringBuilder
        foreach ($ch in $s.ToCharArray()) {
            if ($map.ContainsKey($ch)) { [void]$sb.Append($map[$ch]) }
            else { [void]$sb.Append($ch) }
        }
        return $sb.ToString()
    }

    function To-LocalizationKey([string]$value) {
        $folded = (Fold-Turkmen $value).ToLowerInvariant()
        $folded = ($folded -replace '[^a-z0-9]+', '-').Trim('-')
        if ($folded.Length -le 64) { return $folded }
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($folded)
        $hash = [System.BitConverter]::ToString(
            [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
        ).Replace('-', '').Substring(0, 8).ToLowerInvariant()
        $prefixLen = 64 - 1 - $hash.Length
        return $folded.Substring(0, $prefixLen) + '_' + $hash
    }

    function Normalize-Content([string]$content, [int]$max = 2000) {
        if ([string]::IsNullOrWhiteSpace($content)) { return $null }
        $oneLine = ($content -replace '\s+', ' ').Trim()
        if ($oneLine.Length -le $max) { return $oneLine }
        return $oneLine.Substring(0, $max).TrimEnd()
    }

    function Normalize-MatchText([string]$s) {
        if ([string]::IsNullOrWhiteSpace($s)) { return '' }
        return (Fold-Turkmen $s).ToLowerInvariant()
    }

    function Map-MinistryLegs([string]$title, [string]$titleL) {
        $combined = Normalize-MatchText "$title $titleL"

        if ($combined -match 'turkmengaz' -and $combined -notmatch 'energetika') {
            return @{ Legs = @(@{ Sequence = 1; ApprovingMinistryShortNameTm = $MinistryTg; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }); Fallback = $false }
        }
        if ($combined -match 'gurlusyk') {
            return @{ Legs = @(@{ Sequence = 1; ApprovingMinistryShortNameTm = $MinistryGu; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }); Fallback = $false }
        }
        if ($combined -match 'energetika' -and $combined -match 'turkmenenergo') {
            return @{
                Legs = @(
                    @{ Sequence = 1; ApprovingMinistryShortNameTm = $MinistryTe; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }
                    @{ Sequence = 2; ApprovingMinistryShortNameTm = $MinistryEn; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }
                )
                Fallback = $false
            }
        }
        if ($combined -match 'energetika') {
            return @{ Legs = @(@{ Sequence = 1; ApprovingMinistryShortNameTm = $MinistryEn; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }); Fallback = $false }
        }
        if ($combined -match 'turkmenenergo') {
            return @{ Legs = @(@{ Sequence = 1; ApprovingMinistryShortNameTm = $MinistryTe; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }); Fallback = $false }
        }

        return @{
            Legs = @(@{
                Sequence = 1
                ApprovingMinistryShortNameTm = $MinistryEn
                MaxDaysInReview = 10
                WarningDaysBeforeMax = 8
            })
            Fallback = $true
        }
    }

    function Leg-Suffix([array]$legs) {
        $parts = foreach ($leg in $legs) {
            $n = Normalize-MatchText $leg.ApprovingMinistryShortNameTm
            switch -Wildcard ($n) {
                'turkmenenergo' { 'TE' }
                'energetika' { 'EN' }
                'gurlusyk' { 'GU' }
                'turkmengaz' { 'TG' }
                default { 'DF' }
            }
        }
        return "YL$($legs.Count)-$($parts -join '-')"
    }

    $rows = New-Object System.Collections.Generic.List[object]
    $usedKeys = @{}
    $fallbackCount = 0
    $gapMinistries = [System.Collections.Generic.HashSet[string]]::new()

    foreach ($line in $lines) {
        $parts = Split-SqlRow $line
        if (-not $parts) { continue }

        $code = $parts[0].Trim()
        if ($code.Length -gt 56) { $code = $code.Substring(0, 56) }

        $content = $parts[3]
        $title = $parts[4]
        $titleL = $parts[5]

        if ($title -eq 'NULL') { $title = $null }
        if ($titleL -eq 'NULL') { $titleL = $null }

        $mapped = Map-MinistryLegs $title $titleL
        $legs = $mapped.Legs
        if ($mapped.Fallback) {
            $fallbackCount++
            if ($title) { [void]$gapMinistries.Add($title.Trim()) }
        }
        $cleanLegs = $legs

        $legSummary = ($cleanLegs | ForEach-Object { $_.ApprovingMinistryShortNameTm.ToLowerInvariant() }) -join ' > '
        $yl = [char]0x015F  # ş
        $ylala = "ylala${yl}yk"
        $description = Normalize-Content $content
        if (-not $description) {
            $description = "(1 $ylala`: $($legSummary))"
        }

        $baseKey = To-LocalizationKey "$code-$(Leg-Suffix $cleanLegs)"
        $locKey = $baseKey
        $n = 2
        while ($usedKeys.ContainsKey($locKey)) {
            $locKey = To-LocalizationKey "$baseKey-$n"
            $n++
        }
        $usedKeys[$locKey] = $true

        $rows.Add([ordered]@{
            NameTm = $code
            Description = $description
            LocalizationKey = $locKey
            Code = $code
            IsActive = $true
            MinistryLegs = $cleanLegs
        })
    }

    if ($rows.Count -ne 73) {
        throw "Expected 73 rows, got $($rows.Count)."
    }

    $catalog = [ordered]@{
        rows = $rows
    }

    $json = $catalog | ConvertTo-Json -Depth 6
    # ConvertTo-Json escapes Unicode; re-emit UTF-8 without BOM for git consistency.
    [System.IO.File]::WriteAllText($outFile, $json, (New-Object System.Text.UTF8Encoding $false))

    Write-Host "Wrote $($rows.Count) rows to $outFile"
    Write-Host "Fallback ministry (Energetika) rows: $fallbackCount"
    if ($gapMinistries.Count -gt 0) {
        Write-Host 'Unmapped legacy ministry titles (used Energetika fallback):'
        $gapMinistries | Sort-Object | ForEach-Object { Write-Host "  - $_" }
    }
}
finally {
    if (Test-Path $tempCsv) { Remove-Item -LiteralPath $tempCsv -Force }
}
