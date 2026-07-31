BEGIN;
UPDATE "Applications" SET "LatestProgressId" = NULL
WHERE "IsManualEntry" = true AND COALESCE("GCRecord", 0) = 0;
DELETE FROM "ApplicationProgresses" ap
USING "Applications" a
WHERE ap."ApplicationID" = a."ID"
  AND a."IsManualEntry" = true
  AND COALESCE(a."GCRecord", 0) = 0;
COMMIT;
SELECT COUNT(*) AS remaining_progress FROM "ApplicationProgresses";