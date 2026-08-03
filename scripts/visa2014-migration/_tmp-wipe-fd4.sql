BEGIN;
CREATE TEMP TABLE keep_ids AS
SELECT fd."ID"
FROM "FileData" fd
WHERE EXISTS (SELECT 1 FROM "UserReportTemplates" u WHERE u."TemplateFileID" = fd."ID")
   OR EXISTS (SELECT 1 FROM "ProjectContractDocuments" p WHERE p."FileID" = fd."ID");

-- Drop toast payload first (fast), then delete rows
UPDATE "FileData" fd
SET "Content" = NULL
WHERE NOT EXISTS (SELECT 1 FROM keep_ids k WHERE k."ID" = fd."ID");

DELETE FROM "FileData" fd
WHERE NOT EXISTS (SELECT 1 FROM keep_ids k WHERE k."ID" = fd."ID");

COMMIT;