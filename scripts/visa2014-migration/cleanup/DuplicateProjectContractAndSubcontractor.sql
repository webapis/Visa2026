-- Preview duplicate ProjectContract / Subcontractor lookup rows on Visa2026 (SQL Server).
-- PREVIEW ONLY — no writes. Repair-DuplicateProjectContractSubcontractor.ps1 runs this.
-- Match families:
--   ProjectContract: SameNameTm | SameLocalizationKey | SameCode | PrefixCandidate (short title prefixes longer NameTm)
--   Subcontractor:   SameNameTm | NormalizedNameTm (trim + collapse spaces + lower)

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

PRINT '=== ProjectContract — active row counts ===';
SELECT
    COUNT(*) AS ActiveRows,
    COUNT(DISTINCT NULLIF(LTRIM(RTRIM(NameTm)), N'')) AS DistinctNameTm,
    COUNT(DISTINCT NULLIF(LTRIM(RTRIM(Code)), N'')) AS DistinctCode,
    COUNT(DISTINCT NULLIF(LTRIM(RTRIM(LocalizationKey)), N'')) AS DistinctLocalizationKey
FROM dbo.ProjectContracts
WHERE (GCRecord IS NULL OR GCRecord = 0);

PRINT '';
PRINT '=== ProjectContract — SameNameTm groups (exact trim) ===';
;WITH Active AS (
    SELECT
        ID,
        LTRIM(RTRIM(NameTm)) AS NameTm,
        NULLIF(LTRIM(RTRIM(Code)), N'') AS Code,
        NULLIF(LTRIM(RTRIM(LocalizationKey)), N'') AS LocalizationKey,
        LEFT(LTRIM(RTRIM(Description)), 80) AS DescriptionPreview
    FROM dbo.ProjectContracts
    WHERE (GCRecord IS NULL OR GCRecord = 0)
      AND NULLIF(LTRIM(RTRIM(NameTm)), N'') IS NOT NULL
),
Dup AS (
    SELECT NameTm AS DupKey
    FROM Active
    GROUP BY NameTm
    HAVING COUNT(*) > 1
)
SELECT
    a.NameTm AS DupKey,
    a.ID,
    a.Code,
    a.LocalizationKey,
    a.DescriptionPreview,
    (SELECT COUNT(*) FROM dbo.People p WHERE p.ProjectContractID = a.ID AND (p.GCRecord IS NULL OR p.GCRecord = 0)) AS PersonRefs,
    (SELECT COUNT(*) FROM dbo.Applications app WHERE app.ProjectContractID = a.ID AND (app.GCRecord IS NULL OR app.GCRecord = 0)) AS ApplicationRefs
FROM Active a
INNER JOIN Dup d ON d.DupKey = a.NameTm
ORDER BY a.NameTm, a.ID;

PRINT '';
PRINT '=== ProjectContract — SameLocalizationKey groups ===';
;WITH Active AS (
    SELECT
        ID,
        LTRIM(RTRIM(NameTm)) AS NameTm,
        NULLIF(LTRIM(RTRIM(Code)), N'') AS Code,
        LOWER(LTRIM(RTRIM(LocalizationKey))) AS LocKey,
        LocalizationKey AS LocalizationKeyRaw
    FROM dbo.ProjectContracts
    WHERE (GCRecord IS NULL OR GCRecord = 0)
      AND NULLIF(LTRIM(RTRIM(LocalizationKey)), N'') IS NOT NULL
),
Dup AS (
    SELECT LocKey AS DupKey
    FROM Active
    GROUP BY LocKey
    HAVING COUNT(*) > 1
)
SELECT
    a.LocKey AS DupKey,
    a.ID,
    a.NameTm,
    a.Code,
    a.LocalizationKeyRaw AS LocalizationKey,
    (SELECT COUNT(*) FROM dbo.People p WHERE p.ProjectContractID = a.ID AND (p.GCRecord IS NULL OR p.GCRecord = 0)) AS PersonRefs,
    (SELECT COUNT(*) FROM dbo.Applications app WHERE app.ProjectContractID = a.ID AND (app.GCRecord IS NULL OR app.GCRecord = 0)) AS ApplicationRefs
FROM Active a
INNER JOIN Dup d ON d.DupKey = a.LocKey
ORDER BY a.LocKey, a.ID;

PRINT '';
PRINT '=== ProjectContract — SameCode groups (non-empty Code) ===';
;WITH Active AS (
    SELECT
        ID,
        LTRIM(RTRIM(NameTm)) AS NameTm,
        LTRIM(RTRIM(Code)) AS Code,
        NULLIF(LTRIM(RTRIM(LocalizationKey)), N'') AS LocalizationKey
    FROM dbo.ProjectContracts
    WHERE (GCRecord IS NULL OR GCRecord = 0)
      AND NULLIF(LTRIM(RTRIM(Code)), N'') IS NOT NULL
),
Dup AS (
    SELECT Code AS DupKey
    FROM Active
    GROUP BY Code
    HAVING COUNT(*) > 1
)
SELECT
    a.Code AS DupKey,
    a.ID,
    a.NameTm,
    a.LocalizationKey,
    (SELECT COUNT(*) FROM dbo.People p WHERE p.ProjectContractID = a.ID AND (p.GCRecord IS NULL OR p.GCRecord = 0)) AS PersonRefs,
    (SELECT COUNT(*) FROM dbo.Applications app WHERE app.ProjectContractID = a.ID AND (app.GCRecord IS NULL OR app.GCRecord = 0)) AS ApplicationRefs
FROM Active a
INNER JOIN Dup d ON d.DupKey = a.Code
ORDER BY a.Code, a.ID;

PRINT '';
PRINT '=== ProjectContract — PrefixCandidate pairs (short NameTm/Code prefixes longer NameTm) ===';
PRINT 'Review carefully — Satlik/Shatlik-style distinct codes may appear; do not merge without approval.';
;WITH Active AS (
    SELECT
        ID,
        LTRIM(RTRIM(NameTm)) AS NameTm,
        NULLIF(LTRIM(RTRIM(Code)), N'') AS Code
    FROM dbo.ProjectContracts
    WHERE (GCRecord IS NULL OR GCRecord = 0)
      AND NULLIF(LTRIM(RTRIM(NameTm)), N'') IS NOT NULL
)
SELECT TOP 200
    short.ID AS ShortId,
    short.NameTm AS ShortNameTm,
    short.Code AS ShortCode,
    long.ID AS LongId,
    long.NameTm AS LongNameTm,
    long.Code AS LongCode,
    (SELECT COUNT(*) FROM dbo.People p WHERE p.ProjectContractID = short.ID AND (p.GCRecord IS NULL OR p.GCRecord = 0))
      + (SELECT COUNT(*) FROM dbo.Applications app WHERE app.ProjectContractID = short.ID AND (app.GCRecord IS NULL OR app.GCRecord = 0)) AS ShortRefs,
    (SELECT COUNT(*) FROM dbo.People p WHERE p.ProjectContractID = long.ID AND (p.GCRecord IS NULL OR p.GCRecord = 0))
      + (SELECT COUNT(*) FROM dbo.Applications app WHERE app.ProjectContractID = long.ID AND (app.GCRecord IS NULL OR app.GCRecord = 0)) AS LongRefs
FROM Active short
INNER JOIN Active long ON short.ID <> long.ID
WHERE LEN(long.NameTm) > LEN(COALESCE(short.Code, short.NameTm))
  AND (
        (short.Code IS NOT NULL
         AND long.NameTm LIKE short.Code + N'[ -—]%' COLLATE Latin1_General_CI_AI)
     OR (long.NameTm LIKE short.NameTm + N'[ -—]%' COLLATE Latin1_General_CI_AI)
  )
ORDER BY ShortRefs + LongRefs DESC, short.NameTm, long.NameTm;

PRINT '';
PRINT '=== Subcontractor — active row counts ===';
SELECT
    COUNT(*) AS ActiveRows,
    COUNT(DISTINCT NULLIF(LTRIM(RTRIM(NameTm)), N'')) AS DistinctNameTm
FROM dbo.Subcontractors
WHERE (GCRecord IS NULL OR GCRecord = 0);

PRINT '';
PRINT '=== Subcontractor — SameNameTm groups (exact trim) ===';
;WITH Active AS (
    SELECT
        ID,
        LTRIM(RTRIM(NameTm)) AS NameTm,
        IsDefault
    FROM dbo.Subcontractors
    WHERE (GCRecord IS NULL OR GCRecord = 0)
      AND NULLIF(LTRIM(RTRIM(NameTm)), N'') IS NOT NULL
),
Dup AS (
    SELECT NameTm AS DupKey
    FROM Active
    GROUP BY NameTm
    HAVING COUNT(*) > 1
)
SELECT
    a.NameTm AS DupKey,
    a.ID,
    a.IsDefault,
    (SELECT COUNT(*) FROM dbo.People p WHERE p.SubcontractorID = a.ID AND (p.GCRecord IS NULL OR p.GCRecord = 0)) AS PersonRefs
FROM Active a
INNER JOIN Dup d ON d.DupKey = a.NameTm
ORDER BY a.NameTm, a.ID;

PRINT '';
PRINT '=== Subcontractor — NormalizedNameTm groups (case/space insensitive) ===';
;WITH Active AS (
    SELECT
        ID,
        LTRIM(RTRIM(NameTm)) AS NameTm,
        LOWER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(NameTm)), N'  ', N' '), N'  ', N' '), N'  ', N' ')) AS NormKey,
        IsDefault
    FROM dbo.Subcontractors
    WHERE (GCRecord IS NULL OR GCRecord = 0)
      AND NULLIF(LTRIM(RTRIM(NameTm)), N'') IS NOT NULL
),
Dup AS (
    SELECT NormKey AS DupKey
    FROM Active
    GROUP BY NormKey
    HAVING COUNT(*) > 1
)
SELECT
    a.NormKey AS DupKey,
    a.ID,
    a.NameTm,
    a.IsDefault,
    (SELECT COUNT(*) FROM dbo.People p WHERE p.SubcontractorID = a.ID AND (p.GCRecord IS NULL OR p.GCRecord = 0)) AS PersonRefs
FROM Active a
INNER JOIN Dup d ON d.DupKey = a.NormKey
ORDER BY a.NormKey, a.ID;

PRINT '';
PRINT '=== Summary group counts ===';
SELECT N'ProjectContract.SameNameTm' AS Family, COUNT(*) AS GroupCount
FROM (
    SELECT LTRIM(RTRIM(NameTm)) AS k
    FROM dbo.ProjectContracts
    WHERE (GCRecord IS NULL OR GCRecord = 0) AND NULLIF(LTRIM(RTRIM(NameTm)), N'') IS NOT NULL
    GROUP BY LTRIM(RTRIM(NameTm))
    HAVING COUNT(*) > 1
) x
UNION ALL
SELECT N'ProjectContract.SameLocalizationKey', COUNT(*)
FROM (
    SELECT LOWER(LTRIM(RTRIM(LocalizationKey))) AS k
    FROM dbo.ProjectContracts
    WHERE (GCRecord IS NULL OR GCRecord = 0) AND NULLIF(LTRIM(RTRIM(LocalizationKey)), N'') IS NOT NULL
    GROUP BY LOWER(LTRIM(RTRIM(LocalizationKey)))
    HAVING COUNT(*) > 1
) x
UNION ALL
SELECT N'ProjectContract.SameCode', COUNT(*)
FROM (
    SELECT LTRIM(RTRIM(Code)) AS k
    FROM dbo.ProjectContracts
    WHERE (GCRecord IS NULL OR GCRecord = 0) AND NULLIF(LTRIM(RTRIM(Code)), N'') IS NOT NULL
    GROUP BY LTRIM(RTRIM(Code))
    HAVING COUNT(*) > 1
) x
UNION ALL
SELECT N'Subcontractor.SameNameTm', COUNT(*)
FROM (
    SELECT LTRIM(RTRIM(NameTm)) AS k
    FROM dbo.Subcontractors
    WHERE (GCRecord IS NULL OR GCRecord = 0) AND NULLIF(LTRIM(RTRIM(NameTm)), N'') IS NOT NULL
    GROUP BY LTRIM(RTRIM(NameTm))
    HAVING COUNT(*) > 1
) x
UNION ALL
SELECT N'Subcontractor.NormalizedNameTm', COUNT(*)
FROM (
    SELECT LOWER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(NameTm)), N'  ', N' '), N'  ', N' '), N'  ', N' ')) AS k
    FROM dbo.Subcontractors
    WHERE (GCRecord IS NULL OR GCRecord = 0) AND NULLIF(LTRIM(RTRIM(NameTm)), N'') IS NOT NULL
    GROUP BY LOWER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(NameTm)), N'  ', N' '), N'  ', N' '), N'  ', N' '))
    HAVING COUNT(*) > 1
) x;

PRINT '';
PRINT 'Preview complete — no rows were modified.';