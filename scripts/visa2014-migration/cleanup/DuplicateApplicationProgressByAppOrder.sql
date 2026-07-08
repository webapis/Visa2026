-- Soft-delete duplicate ApplicationProgress rows: same Application + ProgressOrder.
-- Keeps MIN(ID). Soft-deletes extras (GCRecord = 1).
-- Run PREVIEW first (@Apply = 0).

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;

IF OBJECT_ID('tempdb..#DupGroups') IS NOT NULL DROP TABLE #DupGroups;
IF OBJECT_ID('tempdb..#Extras') IS NOT NULL DROP TABLE #Extras;

SELECT ap.ApplicationID, ap.ProgressOrder, MIN(ap.ID) AS KeepId, COUNT(*) AS DupRowCount
INTO #DupGroups
FROM dbo.ApplicationProgresses ap
WHERE (ap.GCRecord IS NULL OR ap.GCRecord = 0)
  AND ap.ApplicationID IS NOT NULL
GROUP BY ap.ApplicationID, ap.ProgressOrder
HAVING COUNT(*) > 1;

SELECT ap.ID AS ExtraId, g.KeepId, g.ApplicationID, g.ProgressOrder, g.DupRowCount AS DupCountInGroup
INTO #Extras
FROM dbo.ApplicationProgresses ap
INNER JOIN #DupGroups g
  ON g.ApplicationID = ap.ApplicationID AND g.ProgressOrder = ap.ProgressOrder
WHERE ap.ID <> g.KeepId AND (ap.GCRecord IS NULL OR ap.GCRecord = 0);

DECLARE @GroupCount int = (SELECT COUNT(*) FROM #DupGroups);
DECLARE @ExtraCount int = (SELECT COUNT(*) FROM #Extras);
PRINT CONCAT('Duplicate groups: ', @GroupCount);
PRINT CONCAT('Extras to soft-delete: ', @ExtraCount);

SELECT a.FullApplicationNumber, e.ProgressOrder, e.KeepId, e.ExtraId, e.DupCountInGroup,
       s.Code AS StateCode, l.Code AS LocationCode, CONVERT(varchar(10), ap.Date, 104) AS ProgressDate
FROM #Extras e
INNER JOIN dbo.Applications a ON a.ID = e.ApplicationID
INNER JOIN dbo.ApplicationProgresses ap ON ap.ID = e.ExtraId
LEFT JOIN dbo.ApplicationStates s ON s.ID = ap.StateID
LEFT JOIN dbo.ApplicationLocations l ON l.ID = ap.LocationID
ORDER BY a.FullApplicationNumber, e.ProgressOrder;

IF @Apply = 0 BEGIN PRINT 'PREVIEW ONLY'; RETURN; END

BEGIN TRANSACTION;
UPDATE ap SET ap.GCRecord = 1
FROM dbo.ApplicationProgresses ap
INNER JOIN #Extras e ON e.ExtraId = ap.ID
WHERE ap.GCRecord IS NULL OR ap.GCRecord = 0;
COMMIT TRANSACTION;

SELECT COUNT(*) AS RemainingDuplicateGroups FROM (
  SELECT ApplicationID, ProgressOrder
  FROM ApplicationProgresses
  WHERE (GCRecord IS NULL OR GCRecord = 0) AND ApplicationID IS NOT NULL
  GROUP BY ApplicationID, ProgressOrder
  HAVING COUNT(*) > 1
) post;