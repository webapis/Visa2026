-- Report Dashboard: Cancelled Invitation — PostgreSQL.
-- One invitation line cancelled by a completed cancellation application.
-- That application has no ministry approval. Migration only.
DROP VIEW IF EXISTS vw_rd_invitation_cancelled;
CREATE VIEW vw_rd_invitation_cancelled AS
SELECT
    ii."ID"                                                                 AS "ID",
    p."ID"                                                                  AS "PersonOid",
    inv."ID"                                                                AS "InvitationOid",
    cancel."ID"                                                             AS "CancelApplicationOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    COALESCE(NULLIF(BTRIM(inv."InvitationNumber"), ''), '')                 AS "InvitationNumber",
    CASE WHEN (inv."StartDate")::date > DATE '1900-01-01' THEN inv."StartDate" ELSE NULL END AS "IssuedDate",
    CASE WHEN (inv."ExpirationDate")::date > DATE '1900-01-01' THEN inv."ExpirationDate" ELSE NULL END AS "ExpirationDate",
    COALESCE(
        NULLIF(BTRIM(vp."NameTm"), ''),
        NULLIF(BTRIM(vp."Name"), ''),
        ''
    )                                                                       AS "VisaPeriodLabel",
    COALESCE(
        NULLIF(BTRIM(cancel."FullApplicationNumber"), ''),
        NULLIF(BTRIM(cancel."ApplicationNumber"), ''),
        ''
    )                                                                       AS "CancelApplicationNumber",
    CASE
        WHEN cancel."ApplicationDate" IS NOT NULL AND (cancel."ApplicationDate")::date > DATE '1900-01-01'
            THEN cancel."ApplicationDate"
        ELSE NULL
    END                                                                     AS "CancelApplicationDate",
    COALESCE(mig_issued."Date", mig_started."Date")                         AS "MigrationDate",
    COALESCE(
        mig_issued."ProcessNumber",
        mig_started."ProcessNumber",
        NULLIF(BTRIM(cancel."ProcessNumber"), ''),
        ''
    )                                                                       AS "MigrationResultNumber",
    CASE
        WHEN COALESCE(mig_issued."Date", mig_started."Date") IS NULL
             AND COALESCE(mig_issued."ProcessNumber", mig_started."ProcessNumber", NULLIF(BTRIM(cancel."ProcessNumber"), ''), '') = ''
            THEN ''
        ELSE COALESCE(to_char(COALESCE(mig_issued."Date", mig_started."Date"), 'DD.MM.YYYY'), '')
            || CASE
                WHEN COALESCE(mig_issued."ProcessNumber", mig_started."ProcessNumber", NULLIF(BTRIM(cancel."ProcessNumber"), ''), '') = '' THEN ''
                ELSE CASE
                    WHEN COALESCE(mig_issued."Date", mig_started."Date") IS NULL THEN ''
                    ELSE chr(10)
                END
                    || '№ ' || COALESCE(mig_issued."ProcessNumber", mig_started."ProcessNumber", NULLIF(BTRIM(cancel."ProcessNumber"), ''))
            END
    END                                                                     AS "MigrationLabel",
    COALESCE(
        NULLIF(BTRIM(apc."NameTm"), ''),
        NULLIF(BTRIM(pc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "ProjectName",
    COALESCE(apc."NameTm", pc."NameTm", '')                                 AS "ProjectNameRaw",
    COALESCE(apc."NameTm", pc."NameTm", '')                                 AS "ProjectNameTm",
    p."PersonRole"                                                          AS "PersonRoleCode",
    COALESCE(
        NULLIF(BTRIM(apc."NameTm"), ''),
        NULLIF(BTRIM(pc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "InvitationItems" ii
INNER JOIN "Invitations" inv
    ON inv."ID" = ii."InvitationID" AND COALESCE(inv."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = ii."PersonID" AND COALESCE(p."GCRecord", 0) = 0
INNER JOIN LATERAL (
    SELECT
        api."ID",
        api."FullApplicationNumber",
        api."ApplicationNumber",
        api."ApplicationDate",
        api."ProcessNumber",
        api."ProjectContractID"
    FROM "ApplicationProfileInstanceInvitationItems" j
    INNER JOIN "ApplicationProfileInstances" api
        ON api."ID" = j."ApplicationProfileInstanceId" AND COALESCE(api."GCRecord", 0) = 0
    INNER JOIN "ApplicationProfiles" ap
        ON ap."ID" = api."ApplicationProfileID" AND COALESCE(ap."GCRecord", 0) = 0
    WHERE j."InvitationItemId" = ii."ID"
      AND ap."ActionFamily" = 1
      AND BTRIM(COALESCE(api."LatestPrimaryStateCode", '')) = 'PROCESS_ISSUED'
    ORDER BY api."ApplicationDate" DESC NULLS LAST, api."ID" DESC
    LIMIT 1
) cancel ON TRUE
LEFT JOIN "VisaPeriods" vp
    ON vp."ID" = inv."VisaPeriodID" AND COALESCE(vp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" apc
    ON apc."ID" = cancel."ProjectContractID" AND COALESCE(apc."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT pr."Date" AS "Date", NULLIF(BTRIM(pr."ProcessNumber"), '') AS "ProcessNumber"
    FROM "ApplicationProfileInstanceProgresses" pr
    INNER JOIN "ApplicationStates" st
        ON st."ID" = pr."StateID" AND COALESCE(st."GCRecord", 0) = 0
    WHERE pr."ApplicationProfileInstanceID" = cancel."ID"
      AND COALESCE(pr."GCRecord", 0) = 0
      AND BTRIM(COALESCE(st."Code", '')) = 'PROCESS_ISSUED'
    ORDER BY pr."Date" DESC NULLS LAST, pr."ID" DESC
    LIMIT 1
) mig_issued ON TRUE
LEFT JOIN LATERAL (
    SELECT pr."Date" AS "Date", NULLIF(BTRIM(pr."ProcessNumber"), '') AS "ProcessNumber"
    FROM "ApplicationProfileInstanceProgresses" pr
    INNER JOIN "ApplicationStates" st
        ON st."ID" = pr."StateID" AND COALESCE(st."GCRecord", 0) = 0
    WHERE pr."ApplicationProfileInstanceID" = cancel."ID"
      AND COALESCE(pr."GCRecord", 0) = 0
      AND BTRIM(COALESCE(st."Code", '')) = 'PROCESS_STARTED'
    ORDER BY pr."Date" DESC NULLS LAST, pr."ID" DESC
    LIMIT 1
) mig_started ON TRUE
WHERE COALESCE(ii."GCRecord", 0) = 0
  AND ii."PersonID" IS NOT NULL;