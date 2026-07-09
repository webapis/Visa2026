-- Soft-delete duplicate Lodging catalog rows: same FullAddress (trim).
-- Keep preference: most AddressesOfResidence refs, then non-null CityID, then MIN(ID).
-- Repoint AddressesOfResidence.LodgingID (and LodgingDocuments / LodgingImages) before soft-delete.
-- Run PREVIEW first (@Apply = 0).

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;

IF OBJECT_ID('tempdb..#Keep') IS NOT NULL DROP TABLE #Keep;
IF OBJECT_ID('tempdb..#Extras') IS NOT NULL DROP TABLE #Extras;

;WITH ranked AS (
  SELECT
    l.ID,
    l.FullAddress,
    l.CityID,
    ROW_NUMBER() OVER (
      PARTITION BY LTRIM(RTRIM(l.FullAddress))
      ORDER BY
        (SELECT COUNT(*)
         FROM dbo.AddressesOfResidence a
         WHERE a.LodgingID = l.ID AND (a.GCRecord IS NULL OR a.GCRecord = 0)) DESC,
        CASE WHEN l.CityID IS NOT NULL THEN 0 ELSE 1 END,
        l.ID
    ) AS rn,
    (SELECT COUNT(*)
     FROM dbo.AddressesOfResidence a
     WHERE a.LodgingID = l.ID AND (a.GCRecord IS NULL OR a.GCRecord = 0)) AS AorCnt
  FROM dbo.Lodgings l
  WHERE (l.GCRecord IS NULL OR l.GCRecord = 0)
    AND l.FullAddress IS NOT NULL
    AND LTRIM(RTRIM(l.FullAddress)) <> ''
)
SELECT ID AS KeepId, FullAddress, CityID, AorCnt
INTO #Keep
FROM ranked
WHERE rn = 1
  AND EXISTS (
    SELECT 1 FROM ranked r2
    WHERE LTRIM(RTRIM(r2.FullAddress)) = LTRIM(RTRIM(ranked.FullAddress))
      AND r2.rn > 1
  );

;WITH ranked AS (
  SELECT
    l.ID,
    l.FullAddress,
    l.CityID,
    ROW_NUMBER() OVER (
      PARTITION BY LTRIM(RTRIM(l.FullAddress))
      ORDER BY
        (SELECT COUNT(*)
         FROM dbo.AddressesOfResidence a
         WHERE a.LodgingID = l.ID AND (a.GCRecord IS NULL OR a.GCRecord = 0)) DESC,
        CASE WHEN l.CityID IS NOT NULL THEN 0 ELSE 1 END,
        l.ID
    ) AS rn,
    (SELECT COUNT(*)
     FROM dbo.AddressesOfResidence a
     WHERE a.LodgingID = l.ID AND (a.GCRecord IS NULL OR a.GCRecord = 0)) AS AorCnt
  FROM dbo.Lodgings l
  WHERE (l.GCRecord IS NULL OR l.GCRecord = 0)
    AND l.FullAddress IS NOT NULL
    AND LTRIM(RTRIM(l.FullAddress)) <> ''
)
SELECT
  r.ID AS ExtraId,
  k.KeepId,
  r.FullAddress,
  r.CityID AS ExtraCityId,
  k.CityID AS KeepCityId,
  r.AorCnt AS ExtraAorCnt,
  k.AorCnt AS KeepAorCnt
INTO #Extras
FROM ranked r
INNER JOIN #Keep k ON LTRIM(RTRIM(k.FullAddress)) = LTRIM(RTRIM(r.FullAddress))
WHERE r.rn > 1;

DECLARE @GroupCount int = (SELECT COUNT(*) FROM #Keep);
DECLARE @ExtraCount int = (SELECT COUNT(*) FROM #Extras);
DECLARE @AorRepoint int = (SELECT ISNULL(SUM(ExtraAorCnt), 0) FROM #Extras);
DECLARE @ActiveNow int = (SELECT COUNT(*) FROM Lodgings WHERE GCRecord IS NULL OR GCRecord = 0);
DECLARE @DistinctNow int = (
  SELECT COUNT(DISTINCT LTRIM(RTRIM(FullAddress)))
  FROM Lodgings
  WHERE (GCRecord IS NULL OR GCRecord = 0) AND FullAddress IS NOT NULL AND LTRIM(RTRIM(FullAddress)) <> ''
);
PRINT CONCAT('Duplicate groups (by FullAddress): ', @GroupCount);
PRINT CONCAT('Extras to soft-delete: ', @ExtraCount);
PRINT CONCAT('AddressOfResidence rows to repoint: ', @AorRepoint);
PRINT CONCAT('Active Lodgings now: ', @ActiveNow);
PRINT CONCAT('Distinct FullAddress now: ', @DistinctNow);

SELECT TOP 40
  LEFT(e.FullAddress, 90) AS Addr90,
  e.KeepId,
  e.ExtraId,
  e.KeepAorCnt,
  e.ExtraAorCnt,
  CASE WHEN e.KeepCityId IS NULL THEN 'NULL' ELSE CONVERT(varchar(36), e.KeepCityId) END AS KeepCity,
  CASE WHEN e.ExtraCityId IS NULL THEN 'NULL' ELSE CONVERT(varchar(36), e.ExtraCityId) END AS ExtraCity
FROM #Extras e
ORDER BY e.FullAddress, e.ExtraAorCnt DESC;

IF @Apply = 0 BEGIN PRINT 'PREVIEW ONLY'; RETURN; END

BEGIN TRANSACTION;

UPDATE a
SET a.LodgingID = e.KeepId
FROM dbo.AddressesOfResidence a
INNER JOIN #Extras e ON e.ExtraId = a.LodgingID
WHERE a.LodgingID IS NOT NULL;

UPDATE d
SET d.LodgingID = e.KeepId
FROM dbo.LodgingDocuments d
INNER JOIN #Extras e ON e.ExtraId = d.LodgingID;

UPDATE i
SET i.LodgingID = e.KeepId
FROM dbo.LodgingImages i
INNER JOIN #Extras e ON e.ExtraId = i.LodgingID;

UPDATE k
SET k.CityID = e.ExtraCityId
FROM dbo.Lodgings k
INNER JOIN #Extras e ON e.KeepId = k.ID
WHERE k.CityID IS NULL AND e.ExtraCityId IS NOT NULL;

UPDATE l
SET l.GCRecord = 1
FROM dbo.Lodgings l
INNER JOIN #Extras e ON e.ExtraId = l.ID
WHERE l.GCRecord IS NULL OR l.GCRecord = 0;

COMMIT TRANSACTION;

DECLARE @ActiveAfter int = (SELECT COUNT(*) FROM Lodgings WHERE GCRecord IS NULL OR GCRecord = 0);
PRINT CONCAT('Active Lodgings after: ', @ActiveAfter);
SELECT COUNT(*) AS RemainingDuplicateGroups FROM (
  SELECT LTRIM(RTRIM(FullAddress)) AS Addr
  FROM Lodgings
  WHERE (GCRecord IS NULL OR GCRecord = 0)
    AND FullAddress IS NOT NULL AND LTRIM(RTRIM(FullAddress)) <> ''
  GROUP BY LTRIM(RTRIM(FullAddress))
  HAVING COUNT(*) > 1
) post;