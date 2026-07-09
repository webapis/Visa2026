-- Backfill Lodgings.CityID from linked AddressesOfResidence (plurality vote).
-- Optional: soft-delete active duplicate addresses with zero AOR when a sibling has AOR refs.
-- @MinVoteShare: top CityID must have at least this fraction of AOR rows with city (default 0.5).
-- Run PREVIEW first (@Apply = 0).

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;
DECLARE @MinVoteShare decimal(5,4) = 0.5000;
DECLARE @SoftDeleteOrphanDupes bit = 1;

IF OBJECT_ID('tempdb..#Votes') IS NOT NULL DROP TABLE #Votes;
IF OBJECT_ID('tempdb..#Backfill') IS NOT NULL DROP TABLE #Backfill;
IF OBJECT_ID('tempdb..#OrphanDupes') IS NOT NULL DROP TABLE #OrphanDupes;

;WITH activeAor AS (
    SELECT a.LodgingID, a.CityID
    FROM dbo.AddressesOfResidence a
    WHERE (a.GCRecord IS NULL OR a.GCRecord = 0)
      AND a.LodgingID IS NOT NULL
      AND a.CityID IS NOT NULL
),
totals AS (
    SELECT LodgingID, COUNT(*) AS TotalAorWithCity
    FROM activeAor
    GROUP BY LodgingID
),
ranked AS (
    SELECT
        v.LodgingID,
        v.CityID,
        COUNT(*) AS VoteCnt,
        t.TotalAorWithCity,
        CAST(COUNT(*) AS decimal(18,4)) / NULLIF(t.TotalAorWithCity, 0) AS VoteShare,
        ROW_NUMBER() OVER (PARTITION BY v.LodgingID ORDER BY COUNT(*) DESC, v.CityID) AS rn
    FROM activeAor v
    INNER JOIN totals t ON t.LodgingID = v.LodgingID
    GROUP BY v.LodgingID, v.CityID, t.TotalAorWithCity
)
SELECT
    l.ID AS LodgingId,
    LEFT(l.FullAddress, 90) AS Addr90,
    r.CityID AS ProposedCityId,
    c.NameTm AS ProposedCity,
    r.VoteCnt,
    r.TotalAorWithCity,
    r.VoteShare,
    (SELECT COUNT(DISTINCT aa.CityID) FROM activeAor aa WHERE aa.LodgingID = l.ID) AS DistinctCities
INTO #Backfill
FROM dbo.Lodgings l
INNER JOIN ranked r ON r.LodgingID = l.ID AND r.rn = 1
LEFT JOIN dbo.Cities c ON c.ID = r.CityID
WHERE (l.GCRecord IS NULL OR l.GCRecord = 0)
  AND l.CityID IS NULL
  AND r.VoteShare >= @MinVoteShare;

SELECT
    l.ID AS OrphanId,
    LEFT(l.FullAddress, 90) AS Addr90,
    keeper.ID AS KeeperId,
    (SELECT COUNT(*) FROM dbo.AddressesOfResidence a WHERE a.LodgingID = keeper.ID AND (a.GCRecord IS NULL OR a.GCRecord = 0)) AS KeeperAorCnt
INTO #OrphanDupes
FROM dbo.Lodgings l
INNER JOIN dbo.Lodgings keeper ON keeper.ID <> l.ID
    AND LTRIM(RTRIM(keeper.FullAddress)) = LTRIM(RTRIM(l.FullAddress))
    AND (keeper.GCRecord IS NULL OR keeper.GCRecord = 0)
WHERE @SoftDeleteOrphanDupes = 1
  AND (l.GCRecord IS NULL OR l.GCRecord = 0)
  AND l.CityID IS NULL
  AND NOT EXISTS (
        SELECT 1 FROM dbo.AddressesOfResidence a
        WHERE a.LodgingID = l.ID AND (a.GCRecord IS NULL OR a.GCRecord = 0)
      )
  AND (
        EXISTS (
            SELECT 1 FROM dbo.AddressesOfResidence a
            WHERE a.LodgingID = keeper.ID AND (a.GCRecord IS NULL OR a.GCRecord = 0)
        )
        OR keeper.ID = (
            SELECT MIN(l3.ID)
            FROM dbo.Lodgings l3
            WHERE LTRIM(RTRIM(l3.FullAddress)) = LTRIM(RTRIM(l.FullAddress))
              AND (l3.GCRecord IS NULL OR l3.GCRecord = 0)
        )
      )
  AND l.ID <> (
        SELECT MIN(l2.ID)
        FROM dbo.Lodgings l2
        WHERE LTRIM(RTRIM(l2.FullAddress)) = LTRIM(RTRIM(l.FullAddress))
          AND (l2.GCRecord IS NULL OR l2.GCRecord = 0)
      );

DECLARE @BackfillCount int = (SELECT COUNT(*) FROM #Backfill);
DECLARE @OrphanCount int = (SELECT COUNT(*) FROM #OrphanDupes);
DECLARE @NullCityNow int = (
    SELECT COUNT(*) FROM dbo.Lodgings WHERE (GCRecord IS NULL OR GCRecord = 0) AND CityID IS NULL
);
PRINT CONCAT('Active Lodgings with null CityID now: ', @NullCityNow);
PRINT CONCAT('Backfill candidates (vote share >= ', @MinVoteShare, '): ', @BackfillCount);
PRINT CONCAT('Orphan duplicate rows to soft-delete: ', @OrphanCount);

SELECT LodgingId, ProposedCity, VoteCnt, TotalAorWithCity, VoteShare, DistinctCities, Addr90
FROM #Backfill
ORDER BY VoteShare, Addr90;

SELECT OrphanId, KeeperId, KeeperAorCnt, Addr90
FROM #OrphanDupes
ORDER BY Addr90, OrphanId;

SELECT COUNT(*) AS StillNullAfterBackfill
FROM dbo.Lodgings l
WHERE (l.GCRecord IS NULL OR l.GCRecord = 0) AND l.CityID IS NULL
  AND l.ID NOT IN (SELECT LodgingId FROM #Backfill)
  AND l.ID NOT IN (SELECT OrphanId FROM #OrphanDupes);

IF @Apply = 0
BEGIN
    PRINT 'PREVIEW ONLY — no changes applied.';
    RETURN;
END

BEGIN TRANSACTION;

UPDATE l
SET l.CityID = b.ProposedCityId
FROM dbo.Lodgings l
INNER JOIN #Backfill b ON b.LodgingId = l.ID
WHERE l.CityID IS NULL;

IF @SoftDeleteOrphanDupes = 1
BEGIN
    UPDATE l
    SET l.GCRecord = 1
    FROM dbo.Lodgings l
    INNER JOIN #OrphanDupes o ON o.OrphanId = l.ID
    WHERE (l.GCRecord IS NULL OR l.GCRecord = 0);
END

COMMIT TRANSACTION;

DECLARE @NullCityAfter int = (
    SELECT COUNT(*) FROM dbo.Lodgings WHERE (GCRecord IS NULL OR GCRecord = 0) AND CityID IS NULL
);
DECLARE @ActiveAfter int = (
    SELECT COUNT(*) FROM dbo.Lodgings WHERE (GCRecord IS NULL OR GCRecord = 0)
);
PRINT CONCAT('Active Lodgings after: ', @ActiveAfter);
PRINT CONCAT('Active Lodgings with null CityID after: ', @NullCityAfter);