-- Wipe VISA2014-imported file waves only (Postgres).
-- Keeps scalar BOs (People, Passports, Visas, …), lookups, UserReportTemplates,
-- ProjectContractDocuments, and ApplicationProgress.MinistryLetterFile.
--
-- After wipe: reset document id-maps to {} then re-run DocumentCopies.ps1
-- (+ MedicalRecord file wave via OnPrem-Sync -StartAt MedicalRecord without IncludeFileWaves,
--   or DocumentCopies after a one-off MedicalRecord CLI).

BEGIN;

UPDATE "People"
SET "Photo" = NULL
WHERE "Photo" IS NOT NULL;

TRUNCATE TABLE
  "PassportDocuments",
  "VisaDocument",
  "EducationDocument",
  "WorkPermitDocuments",
  "InvitationDocuments",
  "PersonDocuments",
  "PersonFamilyRelationDocuments",
  "MedicalRecordDocuments"
RESTART IDENTITY CASCADE;

COMMIT;

-- Fast path: keep template/contract/ministry-letter blobs, truncate the rest.
-- Plain DELETE on multi-GB FileData TOAST is too slow for on-prem reimport.
BEGIN;

CREATE TEMP TABLE filedata_keep AS
SELECT fd.*
FROM "FileData" fd
WHERE EXISTS (SELECT 1 FROM "UserReportTemplates" u WHERE u."TemplateFileID" = fd."ID")
   OR EXISTS (SELECT 1 FROM "ProjectContractDocuments" p WHERE p."FileID" = fd."ID")
   OR EXISTS (SELECT 1 FROM "ApplicationProgresses" a WHERE a."MinistryLetterFileID" = fd."ID");

CREATE TEMP TABLE fk_drop AS
SELECT con.conname, rel.relname AS table_name, pg_get_constraintdef(con.oid) AS def
FROM pg_constraint con
JOIN pg_class rel ON rel.oid = con.conrelid
JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
WHERE con.contype = 'f'
  AND nsp.nspname = 'public'
  AND con.confrelid = '"FileData"'::regclass;

DO $$
DECLARE r record;
BEGIN
  FOR r IN SELECT * FROM fk_drop LOOP
    EXECUTE format('ALTER TABLE %I DROP CONSTRAINT %I', r.table_name, r.conname);
  END LOOP;
END $$;

TRUNCATE TABLE "FileData";
INSERT INTO "FileData" SELECT * FROM filedata_keep;

DO $$
DECLARE r record;
BEGIN
  FOR r IN SELECT * FROM fk_drop LOOP
    EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I %s', r.table_name, r.conname, r.def);
  END LOOP;
END $$;

COMMIT;

SELECT 'People_with_Photo' AS metric, COUNT(*)::text AS value
FROM "People" WHERE "Photo" IS NOT NULL AND COALESCE("GCRecord", 0) = 0
UNION ALL SELECT 'PassportDocuments', COUNT(*)::text FROM "PassportDocuments" WHERE COALESCE("GCRecord", 0) = 0
UNION ALL SELECT 'VisaDocument', COUNT(*)::text FROM "VisaDocument" WHERE COALESCE("GCRecord", 0) = 0
UNION ALL SELECT 'EducationDocument', COUNT(*)::text FROM "EducationDocument" WHERE COALESCE("GCRecord", 0) = 0
UNION ALL SELECT 'WorkPermitDocuments', COUNT(*)::text FROM "WorkPermitDocuments" WHERE COALESCE("GCRecord", 0) = 0
UNION ALL SELECT 'InvitationDocuments', COUNT(*)::text FROM "InvitationDocuments" WHERE COALESCE("GCRecord", 0) = 0
UNION ALL SELECT 'PersonDocuments', COUNT(*)::text FROM "PersonDocuments" WHERE COALESCE("GCRecord", 0) = 0
UNION ALL SELECT 'PersonFamilyRelationDocuments', COUNT(*)::text FROM "PersonFamilyRelationDocuments" WHERE COALESCE("GCRecord", 0) = 0
UNION ALL SELECT 'MedicalRecordDocuments', COUNT(*)::text FROM "MedicalRecordDocuments" WHERE COALESCE("GCRecord", 0) = 0
UNION ALL SELECT 'FileData', COUNT(*)::text FROM "FileData" WHERE COALESCE("GCRecord", 0) = 0;
