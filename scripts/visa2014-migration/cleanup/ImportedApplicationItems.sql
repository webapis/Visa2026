-- Delete VISA2014-imported ApplicationItem rows (keep Application headers) for local reimport.
-- Run against Visa2026 target DB only -- never VISA2015.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @appIds TABLE (ID uniqueidentifier PRIMARY KEY);
INSERT INTO @appIds (ID)
SELECT ID FROM Applications
WHERE IsManualEntry = 1 AND (GCRecord IS NULL OR GCRecord = 0);

DECLARE @appCount int = (SELECT COUNT(*) FROM @appIds);
PRINT CONCAT('Manual-entry applications in scope: ', @appCount);

DECLARE @itemCount int = (
    SELECT COUNT(*)
    FROM ApplicationItems ai
    INNER JOIN @appIds a ON ai.ApplicationID = a.ID
    WHERE ai.GCRecord IS NULL OR ai.GCRecord = 0);
PRINT CONCAT('ApplicationItems to delete: ', @itemCount);

IF @itemCount > 0
BEGIN
    IF OBJECT_ID('dbo.TravelHistories', 'U') IS NOT NULL
        DELETE th FROM TravelHistories th
        INNER JOIN ApplicationItems ai ON th.SourceApplicationItemID = ai.ID
        INNER JOIN @appIds a ON ai.ApplicationID = a.ID;

    DELETE ai FROM ApplicationItems ai
    INNER JOIN @appIds a ON ai.ApplicationID = a.ID;
END

COMMIT TRANSACTION;

SELECT @itemCount AS ApplicationItemsDeleted;
SELECT COUNT(*) AS RemainingApplicationItems
FROM ApplicationItems ai
INNER JOIN Applications app ON ai.ApplicationID = app.ID
WHERE app.IsManualEntry = 1 AND (app.GCRecord IS NULL OR app.GCRecord = 0)
  AND (ai.GCRecord IS NULL OR ai.GCRecord = 0);