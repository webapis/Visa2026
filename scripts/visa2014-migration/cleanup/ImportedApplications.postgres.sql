-- Hard-delete ApplicationProfileInstance rows on Visa2026 PostgreSQL.
-- Visa, WorkPermit, and Invitation instance FKs are nullable NO ACTION: clear them first
-- so the letters stay. Rejection and BorderZone instance FKs are NOT NULL, so those
-- case rows cannot survive without an instance and are deleted here (documents and
-- items cascade). Clear ApplicationProfileInstanceBorderZoneItems first; that join
-- RESTRICT-references BorderZoneItems. LatestProgressId is cleared so progress rows
-- can cascade with the instance.
-- Preserves: ApplicationProfiles catalog, Person and other master data, WP/Inv/Visa rows.
-- Run against Visa2026 PostgreSQL only — never VISA2015.

BEGIN;

DO $$
DECLARE
    inst_count integer;
    progress_count integer;
    people_count integer;
    wp_linked integer;
    inv_linked integer;
BEGIN
    SELECT COUNT(*) INTO inst_count FROM "ApplicationProfileInstances";
    SELECT COUNT(*) INTO progress_count FROM "ApplicationProfileInstanceProgresses";
    SELECT COUNT(*) INTO people_count FROM "ApplicationProfileInstancePeople";
    SELECT COUNT(*) INTO wp_linked FROM "WorkPermits" WHERE "ApplicationProfileInstanceID" IS NOT NULL;
    SELECT COUNT(*) INTO inv_linked FROM "Invitations" WHERE "ApplicationProfileInstanceID" IS NOT NULL;
    RAISE NOTICE 'Before: instances=% progress=% people=% wp_linked=% inv_linked=%',
        inst_count, progress_count, people_count, wp_linked, inv_linked;

    UPDATE "ApplicationProfileInstances"
    SET "LatestProgressId" = NULL
    WHERE "LatestProgressId" IS NOT NULL;

    UPDATE "WorkPermits"
    SET "ApplicationProfileInstanceID" = NULL
    WHERE "ApplicationProfileInstanceID" IS NOT NULL;

    UPDATE "Invitations"
    SET "ApplicationProfileInstanceID" = NULL
    WHERE "ApplicationProfileInstanceID" IS NOT NULL;

    UPDATE "Visas"
    SET "IssuingApplicationProfileInstanceID" = NULL
    WHERE "IssuingApplicationProfileInstanceID" IS NOT NULL;

    UPDATE "WordReportGenerationBatches"
    SET "ApplicationProfileInstanceID" = NULL
    WHERE "ApplicationProfileInstanceID" IS NOT NULL;

    DELETE FROM "ApplicationProfileInstanceBorderZoneItems";
    DELETE FROM "BorderZones";
    DELETE FROM "Rejections";

    DELETE FROM "ApplicationProfileInstances";

    SELECT COUNT(*) INTO inst_count FROM "ApplicationProfileInstances";
    SELECT COUNT(*) INTO progress_count FROM "ApplicationProfileInstanceProgresses";
    SELECT COUNT(*) INTO people_count FROM "ApplicationProfileInstancePeople";
    SELECT COUNT(*) INTO wp_linked FROM "WorkPermits";
    SELECT COUNT(*) INTO inv_linked FROM "Invitations";
    RAISE NOTICE 'After: instances=% progress=% people=% work_permits=% invitations=%',
        inst_count, progress_count, people_count, wp_linked, inv_linked;
END $$;

COMMIT;