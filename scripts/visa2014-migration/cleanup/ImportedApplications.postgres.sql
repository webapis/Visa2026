-- Hard-delete ApplicationProfileInstance rows on Visa2026 PostgreSQL (local reimport).
-- Unlink issued headers first: WorkPermit / Invitation / Rejection FKs are ON DELETE CASCADE
-- (a bare DELETE of instances would wipe those letters). BorderZone / Visa issuing /
-- WordReportGenerationBatch are NO ACTION. LatestProgressId is NO ACTION onto progress.
-- Preserves: ApplicationProfiles catalog, Person and other master data, WP/Inv/Rejection/Visa rows.
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

    UPDATE "Rejections"
    SET "ApplicationProfileInstanceID" = NULL
    WHERE "ApplicationProfileInstanceID" IS NOT NULL;

    UPDATE "BorderZones"
    SET "ApplicationProfileInstanceID" = NULL
    WHERE "ApplicationProfileInstanceID" IS NOT NULL;

    UPDATE "Visas"
    SET "IssuingApplicationProfileInstanceID" = NULL
    WHERE "IssuingApplicationProfileInstanceID" IS NOT NULL;

    UPDATE "WordReportGenerationBatches"
    SET "ApplicationProfileInstanceID" = NULL
    WHERE "ApplicationProfileInstanceID" IS NOT NULL;

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