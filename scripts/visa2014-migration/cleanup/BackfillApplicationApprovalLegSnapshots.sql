-- Backfill ApplicationApprovalLegSnapshots from ApprovalLegProfile ministry legs.
-- Fills Ministrlik / status suffix without touching ApplicationProgress rows.
-- Preview: DECLARE @Apply bit = 0;
-- Apply:   DECLARE @Apply bit = 1;
SET NOCOUNT ON;
DECLARE @Apply bit = 0;

DECLARE @MaxDays int = 10;
DECLARE @WarnDays int = 8;
IF OBJECT_ID(N'dbo.MinistryReviewSlaSettings', N'U') IS NOT NULL
BEGIN
    SELECT TOP (1)
        @MaxDays = ISNULL(MaxDaysInReview, @MaxDays),
        @WarnDays = ISNULL(WarningDaysBeforeMax, @WarnDays)
    FROM dbo.MinistryReviewSlaSettings
    WHERE GCRecord IS NULL OR GCRecord = 0;
END;

;WITH ProfileLegs AS (
    SELECT ml.ApprovalLegProfileId, COUNT(*) AS LegCount
    FROM dbo.ApprovalLegProfileMinistryLegs ml
    WHERE ml.ApprovingMinistryId IS NOT NULL
      AND (ml.GCRecord IS NULL OR ml.GCRecord = 0)
    GROUP BY ml.ApprovalLegProfileId
),
Snap AS (
    SELECT s.ApplicationId, COUNT(*) AS SnapshotLegCount
    FROM dbo.ApplicationApprovalLegSnapshots s
    WHERE (s.GCRecord IS NULL OR s.GCRecord = 0)
      AND s.MinistryShortName IS NOT NULL
      AND LEN(LTRIM(RTRIM(s.MinistryShortName))) > 0
    GROUP BY s.ApplicationId
),
Need AS (
    SELECT a.ID AS ApplicationId, a.ApprovalLegProfileID, pl.LegCount, ISNULL(snap.SnapshotLegCount, 0) AS SnapshotLegCount
    FROM dbo.Applications a
    INNER JOIN dbo.ApplicationTypes t ON t.ID = a.ApplicationTypeID
    INNER JOIN ProfileLegs pl ON pl.ApprovalLegProfileId = a.ApprovalLegProfileID
    LEFT JOIN Snap snap ON snap.ApplicationId = a.ID
    WHERE (a.GCRecord IS NULL OR a.GCRecord = 0)
      AND t.ApplicationProgressRoute = 0 -- ViaMinistries
      AND a.ApprovalLegProfileID IS NOT NULL
      AND ISNULL(snap.SnapshotLegCount, 0) <> pl.LegCount
)
SELECT
    AppsNeedingBackfill = COUNT(*),
    ExpectedSnapshotRows = SUM(LegCount),
    CurrentSnapshotRowsOnThoseApps = SUM(SnapshotLegCount)
FROM Need;

IF @Apply = 0
BEGIN
    PRINT 'PREVIEW only — set @Apply = 1 to write.';
    RETURN;
END;

BEGIN TRAN;

;WITH ProfileLegs AS (
    SELECT ml.ApprovalLegProfileId, COUNT(*) AS LegCount
    FROM dbo.ApprovalLegProfileMinistryLegs ml
    WHERE ml.ApprovingMinistryId IS NOT NULL
      AND (ml.GCRecord IS NULL OR ml.GCRecord = 0)
    GROUP BY ml.ApprovalLegProfileId
),
Snap AS (
    SELECT s.ApplicationId, COUNT(*) AS SnapshotLegCount
    FROM dbo.ApplicationApprovalLegSnapshots s
    WHERE (s.GCRecord IS NULL OR s.GCRecord = 0)
      AND s.MinistryShortName IS NOT NULL
      AND LEN(LTRIM(RTRIM(s.MinistryShortName))) > 0
    GROUP BY s.ApplicationId
),
Need AS (
    SELECT a.ID AS ApplicationId
    FROM dbo.Applications a
    INNER JOIN dbo.ApplicationTypes t ON t.ID = a.ApplicationTypeID
    INNER JOIN ProfileLegs pl ON pl.ApprovalLegProfileId = a.ApprovalLegProfileID
    LEFT JOIN Snap snap ON snap.ApplicationId = a.ID
    WHERE (a.GCRecord IS NULL OR a.GCRecord = 0)
      AND t.ApplicationProgressRoute = 0
      AND a.ApprovalLegProfileID IS NOT NULL
      AND ISNULL(snap.SnapshotLegCount, 0) <> pl.LegCount
)
UPDATE s
SET GCRecord = 1
FROM dbo.ApplicationApprovalLegSnapshots s
INNER JOIN Need n ON n.ApplicationId = s.ApplicationId
WHERE s.GCRecord IS NULL OR s.GCRecord = 0;

DECLARE @Deleted int = @@ROWCOUNT;

;WITH ProfileLegs AS (
    SELECT ml.ApprovalLegProfileId, COUNT(*) AS LegCount
    FROM dbo.ApprovalLegProfileMinistryLegs ml
    WHERE ml.ApprovingMinistryId IS NOT NULL
      AND (ml.GCRecord IS NULL OR ml.GCRecord = 0)
    GROUP BY ml.ApprovalLegProfileId
),
Snap AS (
    SELECT s.ApplicationId, COUNT(*) AS SnapshotLegCount
    FROM dbo.ApplicationApprovalLegSnapshots s
    WHERE (s.GCRecord IS NULL OR s.GCRecord = 0)
      AND s.MinistryShortName IS NOT NULL
      AND LEN(LTRIM(RTRIM(s.MinistryShortName))) > 0
    GROUP BY s.ApplicationId
),
Need AS (
    SELECT a.ID AS ApplicationId, a.ApprovalLegProfileID
    FROM dbo.Applications a
    INNER JOIN dbo.ApplicationTypes t ON t.ID = a.ApplicationTypeID
    INNER JOIN ProfileLegs pl ON pl.ApprovalLegProfileId = a.ApprovalLegProfileID
    LEFT JOIN Snap snap ON snap.ApplicationId = a.ID
    WHERE (a.GCRecord IS NULL OR a.GCRecord = 0)
      AND t.ApplicationProgressRoute = 0
      AND a.ApprovalLegProfileID IS NOT NULL
      AND ISNULL(snap.SnapshotLegCount, 0) <> pl.LegCount
)
INSERT INTO dbo.ApplicationApprovalLegSnapshots (
    ID, ApplicationId, Sequence, ApprovingMinistryId,
    MinistryShortName, MinistryNameTm, MaxDaysInReview, WarningDaysBeforeMax,
    GCRecord, OptimisticLockField
)
SELECT
    NEWID(),
    n.ApplicationId,
    ml.Sequence,
    ml.ApprovingMinistryId,
    LEFT(COALESCE(NULLIF(LTRIM(RTRIM(m.ShortNameTm)), ''), NULLIF(LTRIM(RTRIM(m.NameTm)), ''), ''), 40),
    LEFT(COALESCE(NULLIF(LTRIM(RTRIM(m.NameTm)), ''), ''), 200),
    @MaxDays,
    @WarnDays,
    0,
    0
FROM Need n
INNER JOIN dbo.ApprovalLegProfileMinistryLegs ml
    ON ml.ApprovalLegProfileId = n.ApprovalLegProfileID
   AND ml.ApprovingMinistryId IS NOT NULL
   AND (ml.GCRecord IS NULL OR ml.GCRecord = 0)
INNER JOIN dbo.ApprovingMinistries m ON m.ID = ml.ApprovingMinistryId
WHERE m.GCRecord IS NULL OR m.GCRecord = 0;

DECLARE @Inserted int = @@ROWCOUNT;

COMMIT TRAN;

SELECT SoftDeletedOldSnapshots = @Deleted, InsertedSnapshots = @Inserted;

SELECT ActiveSnapshotRows = COUNT(*)
FROM dbo.ApplicationApprovalLegSnapshots
WHERE GCRecord IS NULL OR GCRecord = 0;