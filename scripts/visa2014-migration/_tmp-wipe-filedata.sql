BEGIN;
DELETE FROM "FileData" fd
WHERE NOT EXISTS (
  SELECT 1 FROM "UserReportTemplates" u WHERE u."TemplateFileID" = fd."ID"
)
AND NOT EXISTS (
  SELECT 1 FROM "ProjectContractDocuments" p WHERE p."FileID" = fd."ID"
);
COMMIT;