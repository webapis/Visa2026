-- Verification after Apply-ApplicationProgressMinistryLegCorrections.ps1
-- Adjust server/database for staging/prod: sqlcmd -S <server> -d <db> -E -i this-file.sql
SET NOCOUNT ON;

PRINT '=== Via-ministry apps with snapshots: leg count vs ministry progress steps ===';
WITH Snap AS (
    SELECT a.ID AS ApplicationId, COUNT(s.ID) AS SnapshotLegs
    FROM dbo.Applications a
    INNER JOIN dbo.ApplicationTypes at ON at.ID = a.ApplicationTypeID
    LEFT JOIN dbo.ApplicationApprovalLegSnapshots s
        ON s.ApplicationId = a.ID AND s.GCRecord = 0
    WHERE a.GCRecord = 0
      AND at.ApplicationProgressRoute = 0
      AND a.ApprovalLegProfileID IS NOT NULL
    GROUP BY a.ID
    HAVING COUNT(s.ID) > 0
),
Prog AS (
    SELECT p.ApplicationID,
           SUM(CASE WHEN st.Code LIKE '%_REVIEW_STARTED' OR st.Code LIKE '%_REVIEW_APPROVED' THEN 1 ELSE 0 END) AS MinistrySteps
    FROM dbo.ApplicationProgresses p
    INNER JOIN dbo.ApplicationStates st ON st.ID = p.StateID
    WHERE p.GCRecord = 0
    GROUP BY p.ApplicationID
)
SELECT
    COUNT(*) AS AppsCompared,
    SUM(CASE WHEN s.SnapshotLegs * 2 = ISNULL(g.MinistrySteps, 0) THEN 1 ELSE 0 END) AS MatchingApps,
    SUM(CASE WHEN s.SnapshotLegs * 2 <> ISNULL(g.MinistrySteps, 0) THEN 1 ELSE 0 END) AS MismatchApps
FROM Snap s
LEFT JOIN Prog g ON g.ApplicationID = s.ApplicationId;

PRINT '=== Sample mismatches (top 20) ===';
WITH Snap AS (
    SELECT a.ID AS ApplicationId, a.FullApplicationNumber, COUNT(s.ID) AS SnapshotLegs
    FROM dbo.Applications a
    INNER JOIN dbo.ApplicationTypes at ON at.ID = a.ApplicationTypeID
    LEFT JOIN dbo.ApplicationApprovalLegSnapshots s
        ON s.ApplicationId = a.ID AND s.GCRecord = 0
    WHERE a.GCRecord = 0
      AND at.ApplicationProgressRoute = 0
      AND a.ApprovalLegProfileID IS NOT NULL
    GROUP BY a.ID, a.FullApplicationNumber
    HAVING COUNT(s.ID) > 0
),
Prog AS (
    SELECT p.ApplicationID,
           SUM(CASE WHEN st.Code LIKE '%_REVIEW_STARTED' OR st.Code LIKE '%_REVIEW_APPROVED' THEN 1 ELSE 0 END) AS MinistrySteps,
           COUNT(*) AS TotalSteps
    FROM dbo.ApplicationProgresses p
    INNER JOIN dbo.ApplicationStates st ON st.ID = p.StateID
    WHERE p.GCRecord = 0
    GROUP BY p.ApplicationID
)
SELECT TOP 20
    s.FullApplicationNumber,
    s.SnapshotLegs,
    ISNULL(g.MinistrySteps, 0) AS MinistrySteps,
    ISNULL(g.TotalSteps, 0) AS TotalSteps
FROM Snap s
LEFT JOIN Prog g ON g.ApplicationID = s.ApplicationId
WHERE s.SnapshotLegs * 2 <> ISNULL(g.MinistrySteps, 0)
ORDER BY s.FullApplicationNumber;