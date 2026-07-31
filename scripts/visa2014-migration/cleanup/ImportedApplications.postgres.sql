-- Delete VISA2014-imported Application scope (IsManualEntry = true) for local PG reimport.
-- Run against Visa2026 PostgreSQL target only — never VISA2015.

BEGIN;

CREATE TEMP TABLE app_ids (id uuid PRIMARY KEY) ON COMMIT DROP;
INSERT INTO app_ids (id)
SELECT a."ID"
FROM "Applications" a
WHERE a."IsManualEntry" = true
  AND COALESCE(a."GCRecord", 0) = 0;

DO $$
DECLARE
    app_count integer;
BEGIN
    SELECT COUNT(*) INTO app_count FROM app_ids;
    RAISE NOTICE 'Applications to delete: %', app_count;

    IF app_count = 0 THEN
        RETURN;
    END IF;

    UPDATE "ApplicationItems" ai
    SET
        "CurrentInvitationItemID" = NULL,
        "PreviousInvitationItemID" = NULL,
        "CurrentWorkPermitItemID" = NULL,
        "SecondWorkPermitItemId" = NULL
    FROM app_ids a
    WHERE ai."ApplicationID" = a.id;

    UPDATE "Visas" v
    SET "InvitationItemID" = NULL
    FROM "InvitationItems" ii
    INNER JOIN "Invitations" i ON i."ID" = ii."InvitationID"
    INNER JOIN app_ids a ON i."ApplicationID" = a.id
    WHERE v."InvitationItemID" = ii."ID";

    DELETE FROM "TravelHistories" th
    USING "ApplicationItems" ai, app_ids a
    WHERE th."SourceApplicationItemID" = ai."ID"
      AND ai."ApplicationID" = a.id;

    DELETE FROM "InvitationItems" ii
    USING "Invitations" i, app_ids a
    WHERE ii."InvitationID" = i."ID"
      AND i."ApplicationID" = a.id;

    DELETE FROM "WorkPermitItems" wi
    USING "WorkPermits" w, app_ids a
    WHERE wi."WorkPermitID" = w."ID"
      AND w."ApplicationID" = a.id;

    DELETE FROM "RejectionItems" ri
    USING "Rejections" r, app_ids a
    WHERE ri."RejectionID" = r."ID"
      AND r."ApplicationID" = a.id;

    DELETE FROM "ApplicationProgresses" ap
    USING app_ids a
    WHERE ap."ApplicationID" = a.id;

    DELETE FROM "ApplicationApprovalLegSnapshots" s
    USING app_ids a
    WHERE s."ApplicationId" = a.id;

    DELETE FROM "ApplicationItems" ai
    USING app_ids a
    WHERE ai."ApplicationID" = a.id;

    DELETE FROM "WordReportGenerationBatches" b
    USING app_ids a
    WHERE b."ApplicationID" = a.id;

    DELETE FROM "Invitations" i
    USING app_ids a
    WHERE i."ApplicationID" = a.id;

    DELETE FROM "WorkPermits" w
    USING app_ids a
    WHERE w."ApplicationID" = a.id;

    DELETE FROM "Rejections" r
    USING app_ids a
    WHERE r."ApplicationID" = a.id;

    DELETE FROM "BorderZones" bz
    USING app_ids a
    WHERE bz."ApplicationID" = a.id;

    DELETE FROM "Applications" app
    USING app_ids a
    WHERE app."ID" = a.id;

    RAISE NOTICE 'Remaining manual-entry apps after delete: %', (
        SELECT COUNT(*)
        FROM "Applications"
        WHERE "IsManualEntry" = true
          AND COALESCE("GCRecord", 0) = 0
    );
END $$;

COMMIT;
