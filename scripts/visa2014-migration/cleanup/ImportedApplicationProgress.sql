-- Delete VISA2014-imported ApplicationProgress rows (keep Application headers/items) for local reimport.
-- Run against Visa2026 target DB only. Never VISA2015.

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

DECLARE @progressCount int = (
    SELECT COUNT(*)
    FROM ApplicationProgresses ap
    INNER JOIN @appIds a ON ap.ApplicationID = a.ID
    WHERE ap.GCRecord IS NULL OR ap.GCRecord = 0);
PRINT CONCAT('ApplicationProgress rows to delete: ', @progressCount);

IF @progressCount > 0
BEGIN
    DELETE ap FROM ApplicationProgresses ap
    INNER JOIN @appIds a ON ap.ApplicationID = a.ID;
END

COMMIT TRANSACTION;

SELECT @progressCount AS ApplicationProgressDeleted;
SELECT COUNT(*) AS RemainingApplicationProgress
FROM ApplicationProgresses ap
INNER JOIN Applications app ON ap.ApplicationID = app.ID
WHERE app.IsManualEntry = 1 AND (app.GCRecord IS NULL OR app.GCRecord = 0)
  AND (ap.GCRecord IS NULL OR ap.GCRecord = 0);