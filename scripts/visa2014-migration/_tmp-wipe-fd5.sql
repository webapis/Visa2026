-- List and drop FKs referencing FileData, truncate, restore keepers, recreate FKs
BEGIN;

CREATE TEMP TABLE filedata_keep AS
SELECT fd.*
FROM "FileData" fd
WHERE EXISTS (SELECT 1 FROM "UserReportTemplates" u WHERE u."TemplateFileID" = fd."ID")
   OR EXISTS (SELECT 1 FROM "ProjectContractDocuments" p WHERE p."FileID" = fd."ID");

CREATE TEMP TABLE urt_map AS
SELECT "ID" AS tmpl_id, "TemplateFileID" AS fd_id
FROM "UserReportTemplates" WHERE "TemplateFileID" IS NOT NULL;

CREATE TEMP TABLE fk_drop AS
SELECT con.conname, rel.relname AS table_name,
       pg_get_constraintdef(con.oid) AS def
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

UPDATE "UserReportTemplates" u
SET "TemplateFileID" = m.fd_id
FROM urt_map m
WHERE u."ID" = m.tmpl_id AND u."TemplateFileID" IS DISTINCT FROM m.fd_id;

DO $$
DECLARE r record;
BEGIN
  FOR r IN SELECT * FROM fk_drop LOOP
    EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I %s', r.table_name, r.conname, r.def);
  END LOOP;
END $$;

COMMIT;

SELECT 'FileData' AS t, COUNT(*)::bigint AS c FROM "FileData"
UNION ALL SELECT 'People', COUNT(*) FROM "People"
UNION ALL SELECT 'Applications', COUNT(*) FROM "Applications"
UNION ALL SELECT 'UserReportTemplates', COUNT(*) FROM "UserReportTemplates" WHERE "GCRecord" = 0;