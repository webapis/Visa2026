# Generates tenant project-contract.calik-energi.json from VISA2015 (Çalik Energi).
# Requires VISA2014_SQL_PASSWORD env and ReadOnlyUser on VISA2015.
# SQL endpoint: param -SqlServer/-Database, or parse VISA2014_SQL_CONNECTION (set by --generate-visa2014-tenant-catalogs).
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
if ([string]::IsNullOrWhiteSpace($password)) {
    throw 'Set VISA2014_SQL_PASSWORD before running this script.'
}

$resolved = Resolve-LegacySqlEndpoint -Server $SqlServer -Db $Database
$SqlServer = $resolved.Server
$Database = $resolved.Database

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$outFile = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant\project-contract.calik-energi.json'

Write-Host "INF Legacy SQL: $SqlServer / $Database" -ForegroundColor DarkGray

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
    & sqlcmd -S $SqlServer -U ReadOnlyUser -P $password -d $Database -C `
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
    # Leg-1 ministries surfaced by Application.AppliedMinistery (see Visa2014ProjectContractMinistryLegPreviewExporter).
    $MinistryAsh = 'A' + [char]0x015F + 'gabat h' + [char]0x00E4 + 'kimlik'   # Aşgabat häkimlik
    $MinistryTngiz = 'TNGIZ'
    $MinistryThim = 'T' + [char]0x00FC + 'rkmenhimi' + [char]0x00FD + 'a'      # Türkmenhimiýa
    $MinistryTnebit = 'T' + [char]0x00FC + 'rkmennebit'                         # Türkmennebit

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

    # Maps a legacy AppliedMinistery.TitleOfMinistery to a canonical ApprovingMinistry.ShortNameTm.
    # Keep in sync with Visa2014ProjectContractMinistryLegPreviewExporter.MapMinistryShortName.
    function Map-MinistryShortName([string]$title) {
        $n = Normalize-MatchText $title
        if ([string]::IsNullOrWhiteSpace($n)) { return '' }
        if ($n -match 'energetika') { return $MinistryEn }
        if ($n -match 'gaz') { return $MinistryTg }
        if ($n -match 'gabat' -or $n -match 'hakim') { return $MinistryAsh }
        if ($n -match 'ngiz' -or $n -match 'nebiti gaytadan' -or $n -match 'turkmenbasydaky') { return $MinistryTngiz }
        if ($n -match 'himi') { return $MinistryThim }
        if ($n -match 'nebit') { return $MinistryTnebit }
        return ''
    }

    function Leg-Suffix([array]$legs) {
        $parts = foreach ($leg in $legs) {
            $n = Normalize-MatchText $leg.ApprovingMinistryShortNameTm
            switch -Wildcard ($n) {
                'turkmenenergo' { 'TE' }
                'energetika' { 'EN' }
                'gurlusyk' { 'GU' }
                'turkmengaz' { 'TG' }
                'asgabat hakimlik' { 'AH' }
                'tngiz' { 'NG' }
                'turkmenhimiya' { 'TH' }
                'turkmennebit' { 'TN' }
                default { 'DF' }
            }
        }
        return "YL$($legs.Count)-$($parts -join '-')"
    }

    # Application-level ministry legs: leg 1 = majority Application.AppliedMinistery per contract,
    # trailing "construction" leg (Gurluşyk) when any application was forwarded to the construction ministry.
    # Türkmenenergo flow: the Energetika ministry row whose signatory line names the "Türkmenenergo"
    # corporation (TitleOfMinisteryL LIKE '%energo%') is a two-step chain Türkmenenergo -> Energetika
    # (+ Gurluşyk when construction). The plain Energetika row (person signatory) stays a single leg.
    # contract.AppliedMinistery is only a fallback for codes with no application forwarding evidence.
    $legQuery = @'
WITH app_leg AS (
  SELECT LTRIM(RTRIM(c.NumberOfContract)) AS code,
         LTRIM(RTRIM(ISNULL(m.TitleOfMinistery, ''))) AS title,
         CASE WHEN m.TitleOfMinisteryL LIKE '%energo%' THEN 1 ELSE 0 END AS is_energo,
         SUM(CASE WHEN a.DateForwardedToMonistery >= '2000-01-01' THEN 1 ELSE 0 END) AS leg1fwd,
         SUM(CASE WHEN a.DateForwardedToMinConstruction >= '2000-01-01'
                    OR NULLIF(LTRIM(RTRIM(a.DocNumberForwardedToMinConstruction)), '') IS NOT NULL
                  THEN 1 ELSE 0 END) AS leg2
  FROM dbo.Application a
  INNER JOIN dbo.Contract c ON a.Contract = c.Oid
  LEFT JOIN dbo.AppliedMinistery m ON a.AppliedMinistery = m.Oid
  WHERE a.GCRecord IS NULL AND c.GCRecord IS NULL
  GROUP BY LTRIM(RTRIM(c.NumberOfContract)), LTRIM(RTRIM(ISNULL(m.TitleOfMinistery, ''))),
           CASE WHEN m.TitleOfMinisteryL LIKE '%energo%' THEN 1 ELSE 0 END
),
code_agg AS (
  SELECT code, SUM(leg1fwd) AS leg1_total, SUM(leg2) AS leg2_total
  FROM app_leg GROUP BY code
),
majority AS (
  SELECT code, title, is_energo, leg1fwd,
         ROW_NUMBER() OVER (PARTITION BY code ORDER BY leg1fwd DESC, title) AS rn
  FROM app_leg WHERE leg1fwd > 0 AND title <> ''
)
SELECT ca.code, ca.leg1_total, ca.leg2_total, ISNULL(mj.is_energo, 0) AS is_energo, ISNULL(mj.title, '') AS majority_title
FROM code_agg ca
LEFT JOIN majority mj ON mj.code = ca.code AND mj.rn = 1
ORDER BY ca.code;
'@

    $appLegs = @{}
    $tempCsv2 = [System.IO.Path]::GetTempFileName()
    try {
        & sqlcmd -S $SqlServer -U ReadOnlyUser -P $password -d $Database -C `
            -h -1 -y 1000 -s "`t" -Q $legQuery -o $tempCsv2 -f o:65001 | Out-Null

        $legLines = Get-Content -LiteralPath $tempCsv2 -Encoding UTF8 |
            Where-Object { $_ -and $_ -notmatch '^\(\d+ rows affected\)$' -and $_ -notmatch '^\s*$' }

        foreach ($legLine in $legLines) {
            $lp = $legLine -split "`t", 5
            if ($lp.Count -lt 5) { continue }
            $lc = $lp[0].Trim()
            if ($lc.Length -gt 56) { $lc = $lc.Substring(0, 56) }
            $l2 = 0; [void][int]::TryParse($lp[2].Trim(), [ref]$l2)
            $isEnergo = ($lp[3].Trim() -eq '1')
            $mt = $lp[4].Trim()
            if ($mt -eq 'NULL') { $mt = '' }
            $short = Map-MinistryShortName $mt
            $appLegs[$lc] = @{ Leg1Short = $short; HasLeg2 = ($l2 -gt 0); Leg1Title = $mt; IsTurkmenenergo = $isEnergo }
        }
    }
    finally {
        if (Test-Path $tempCsv2) { Remove-Item -LiteralPath $tempCsv2 -Force }
    }

    $rows = New-Object System.Collections.Generic.List[object]
    $usedKeys = @{}
    $fallbackCount = 0
    $appEvidenceCount = 0
    $gapMinistries = [System.Collections.Generic.HashSet[string]]::new()
    $unmappedAppTitles = [System.Collections.Generic.HashSet[string]]::new()

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

        $appLeg = if ($appLegs.ContainsKey($code)) { $appLegs[$code] } else { $null }

        $isEnergoFlow = $false
        $hasLeg2 = $false
        $leg1Short = ''

        if ($appLeg -and ($appLeg.IsTurkmenenergo -or -not [string]::IsNullOrWhiteSpace($appLeg.Leg1Short))) {
            # Authoritative: derived from the applications' own AppliedMinistery.
            $isEnergoFlow = [bool]$appLeg.IsTurkmenenergo
            $hasLeg2 = [bool]$appLeg.HasLeg2
            $leg1Short = $appLeg.Leg1Short
            $appEvidenceCount++
        }
        else {
            if ($appLeg -and $appLeg.Leg1Title) { [void]$unmappedAppTitles.Add($appLeg.Leg1Title) }
            # No application forwarding evidence (person-only / simple-process contract):
            # fall back to contract.AppliedMinistery. "Türkmenenergo" in the contract ministry text
            # means the contract is with Türkmenenergo -> Energetika chain.
            $combinedFold = Normalize-MatchText "$title $titleL"
            if ($combinedFold -match 'energo') {
                $isEnergoFlow = $true
            }
            else {
                $short = Map-MinistryShortName $title
                if ([string]::IsNullOrWhiteSpace($short)) { $short = Map-MinistryShortName $titleL }
                if ([string]::IsNullOrWhiteSpace($short)) {
                    $short = $MinistryEn
                    $fallbackCount++
                    if ($title) { [void]$gapMinistries.Add($title.Trim()) }
                }
                $leg1Short = $short
            }
        }

        # No leg-1 evidence at all defaults to Energetika (energy sector is the dominant flow).
        if ([string]::IsNullOrWhiteSpace($leg1Short)) { $leg1Short = $MinistryEn }

        if ($isEnergoFlow -or $leg1Short -eq $MinistryEn) {
            # Every Energetika approval is preceded by Türkmenenergo: all energy-sector contracts
            # require approval by "Türkmenenergo" (corporation) then "Energetika" (ministry),
            # then "Gurluşyk" (construction) when the application was forwarded to construction.
            $legs = @(
                @{ Sequence = 1; ApprovingMinistryShortNameTm = $MinistryTe; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }
                @{ Sequence = 2; ApprovingMinistryShortNameTm = $MinistryEn; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }
            )
            if ($hasLeg2) {
                $legs += @{ Sequence = 3; ApprovingMinistryShortNameTm = $MinistryGu; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }
            }
        }
        else {
            $legs = @(@{ Sequence = 1; ApprovingMinistryShortNameTm = $leg1Short; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 })
            if ($hasLeg2) {
                $legs += @{ Sequence = 2; ApprovingMinistryShortNameTm = $MinistryGu; MaxDaysInReview = 10; WarningDaysBeforeMax = 8 }
            }
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
    Write-Host "Application-derived leg rows (leg 1 = AppliedMinistery): $appEvidenceCount"
    Write-Host "Contract-fallback rows (no application forwarding evidence): $($rows.Count - $appEvidenceCount)"
    Write-Host "  of which Energetika default fallback: $fallbackCount"
    Write-Host 'Next: scripts/local/Generate-ApprovalLegProfileCatalog.ps1 (dedupe MinistryLegs into approval-leg-profile.json).'
    if ($unmappedAppTitles.Count -gt 0) {
        Write-Host 'Unmapped Application.AppliedMinistery titles (fell back to contract mapping):'
        $unmappedAppTitles | Sort-Object | ForEach-Object { Write-Host "  - $_" }
    }
    if ($gapMinistries.Count -gt 0) {
        Write-Host 'Unmapped legacy contract ministry titles (used Energetika fallback):'
        $gapMinistries | Sort-Object | ForEach-Object { Write-Host "  - $_" }
    }
}
finally {
    if (Test-Path $tempCsv) { Remove-Item -LiteralPath $tempCsv -Force }
}
