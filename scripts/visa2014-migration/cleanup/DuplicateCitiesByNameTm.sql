-- Review / merge duplicate City catalog rows (same NameTm, one without Region, one with Region).
-- Keeps the row with RegionID set; repoints all FK columns to Cities; soft-deletes null-Region extras (GCRecord = 1).
-- Scope NullVsSetRegion: exactly 2 rows per NameTm — one RegionID NULL + one RegionID set (prod calik-energi pattern).
-- Scope AllNameTm: every duplicate NameTm group — keeps MIN(ID) among rows with RegionID, else MIN(ID) overall.
-- Run PREVIEW first (@Apply = 0). Repair-DuplicateCities.ps1 replaces @Apply / @Scope.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;
DECLARE @Scope varchar(32) = N'NullVsSetRegion';

IF OBJECT_ID('tempdb..#ScopedEmp') IS NOT NULL DROP TABLE #ScopedEmp;
IF OBJECT_ID('tempdb..#IdentityGroups') IS NOT NULL DROP TABLE #IdentityGroups;
IF OBJECT_ID('tempdb..#Extras') IS NOT NULL DROP TABLE #Extras;

;WITH Active AS (
    SELECT
        c.ID,
        LTRIM(RTRIM(c.NameTm)) AS NameKey,
        c.RegionID,
        c.PdfForm_Code
    FROM dbo.Cities c
    WHERE (c.GCRecord IS NULL OR c.GCRecord = 0)
      AND NULLIF(LTRIM(RTRIM(c.NameTm)), N'') IS NOT NULL
),
DupKeys AS (
    SELECT NameKey
    FROM Active
    GROUP BY NameKey
    HAVING COUNT(*) > 1
),
Scoped AS (
    SELECT a.*
    FROM Active a
    INNER JOIN DupKeys d ON d.NameKey = a.NameKey
    WHERE @Scope = N'AllNameTm'
       OR (
            @Scope = N'NullVsSetRegion'
            AND (SELECT COUNT(*) FROM Active a2 WHERE a2.NameKey = a.NameKey) = 2
            AND (SELECT COUNT(*) FROM Active a2 WHERE a2.NameKey = a.NameKey AND a2.RegionID IS NULL) = 1
            AND (SELECT COUNT(*) FROM Active a2 WHERE a2.NameKey = a.NameKey AND a2.RegionID IS NOT NULL) = 1
          )
),
Keepers AS (
    SELECT
        s.NameKey,
        COALESCE(MIN(CASE WHEN s.RegionID IS NOT NULL THEN s.ID END), MIN(s.ID)) AS KeepId
    FROM Scoped s
    GROUP BY s.NameKey
)
SELECT
    k.NameKey,
    k.KeepId,
    s.ID AS RowId,
    s.RegionID,
    s.PdfForm_Code
INTO #ScopedEmp
FROM Scoped s
INNER JOIN Keepers k ON k.NameKey = s.NameKey;

SELECT
    se.NameKey,
    MIN(se.KeepId) AS KeepId,
    COUNT(*) AS DupRowCount
INTO #IdentityGroups
FROM #ScopedEmp se
GROUP BY se.NameKey;

SELECT
    se.RowId AS ExtraId,
    se.KeepId,
    se.NameKey,
    se.RegionID,
    se.PdfForm_Code,
    g.DupRowCount,
    (SELECT COUNT(*) FROM dbo.AddressesOfResidence a WHERE a.CityID = se.RowId AND (a.GCRecord IS NULL OR a.GCRecord = 0)) AS AorOnExtra,
    (SELECT COUNT(*) FROM dbo.Lodgings l WHERE l.CityID = se.RowId AND (l.GCRecord IS NULL OR l.GCRecord = 0)) AS LodgingsOnExtra,
    (SELECT COUNT(*) FROM dbo.Hotels h WHERE h.CityID = se.RowId AND (h.GCRecord IS NULL OR h.GCRecord = 0)) AS HotelsOnExtra
INTO #Extras
FROM #ScopedEmp se
INNER JOIN #IdentityGroups g ON g.NameKey = se.NameKey
WHERE se.RowId <> se.KeepId;

DECLARE @GroupCount int = (SELECT COUNT(*) FROM #IdentityGroups);
DECLARE @ExtraCount int = (SELECT COUNT(*) FROM #Extras);
DECLARE @AorRepoint int = (SELECT ISNULL(SUM(AorOnExtra), 0) FROM #Extras);
DECLARE @ActiveCities int = (SELECT COUNT(*) FROM dbo.Cities WHERE GCRecord IS NULL OR GCRecord = 0);
PRINT CONCAT('Scope: ', @Scope);
PRINT CONCAT('Duplicate identity groups: ', @GroupCount);
PRINT CONCAT('Extra City rows to soft-delete: ', @ExtraCount);
PRINT CONCAT('AddressesOfResidence rows to repoint: ', @AorRepoint);
PRINT CONCAT('Active Cities now: ', @ActiveCities);

SELECT TOP 50
    e.NameKey,
    CASE WHEN e.RegionID IS NULL THEN N'(null)' ELSE N'(has region)' END AS ExtraRegion,
    keepR.NameTm AS KeepRegion,
    e.AorOnExtra,
    e.LodgingsOnExtra,
    e.HotelsOnExtra,
    e.KeepId,
    e.ExtraId,
    e.DupRowCount
FROM #Extras e
INNER JOIN #ScopedEmp keepSe ON keepSe.RowId = e.KeepId
LEFT JOIN dbo.Regions keepR ON keepR.ID = keepSe.RegionID
ORDER BY e.AorOnExtra DESC, e.NameKey;

SELECT e.KeepId, e.ExtraId
FROM #Extras e
ORDER BY e.KeepId, e.ExtraId;

IF @Apply = 0
BEGIN
    PRINT 'PREVIEW ONLY — no changes applied.';
    RETURN;
END

BEGIN TRANSACTION;

DECLARE @fkSql nvarchar(max) = N'';
SELECT @fkSql = @fkSql + N'
UPDATE t SET t.' + QUOTENAME(c.name) + N' = e.KeepId
FROM dbo.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) + N' t
INNER JOIN #Extras e ON t.' + QUOTENAME(c.name) + N' = e.ExtraId;'
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE OBJECT_NAME(fk.referenced_object_id) = N'Cities';

EXEC sp_executesql @fkSql;

UPDATE c SET c.GCRecord = 1
FROM dbo.Cities c
INNER JOIN #Extras e ON e.ExtraId = c.ID
WHERE (c.GCRecord IS NULL OR c.GCRecord = 0);

COMMIT TRANSACTION;

DECLARE @ActiveAfter int = (SELECT COUNT(*) FROM dbo.Cities WHERE GCRecord IS NULL OR GCRecord = 0);
DECLARE @RemainingNullVsSet int = (
    SELECT COUNT(*) FROM (
        SELECT LTRIM(RTRIM(NameTm)) AS N
        FROM dbo.Cities
        WHERE (GCRecord IS NULL OR GCRecord = 0)
        GROUP BY LTRIM(RTRIM(NameTm))
        HAVING COUNT(*) = 2
           AND SUM(CASE WHEN RegionID IS NULL THEN 1 ELSE 0 END) = 1
           AND SUM(CASE WHEN RegionID IS NOT NULL THEN 1 ELSE 0 END) = 1
    ) x
);
DECLARE @RemainingAllNameTm int = (
    SELECT COUNT(*) FROM (
        SELECT LTRIM(RTRIM(NameTm)) AS N
        FROM dbo.Cities
        WHERE (GCRecord IS NULL OR GCRecord = 0)
        GROUP BY LTRIM(RTRIM(NameTm))
        HAVING COUNT(*) > 1
    ) y
);
PRINT CONCAT('Active Cities after: ', @ActiveAfter);
PRINT CONCAT('Remaining NullVsSet duplicate groups: ', @RemainingNullVsSet);
PRINT CONCAT('Remaining all NameTm duplicate groups: ', @RemainingAllNameTm);