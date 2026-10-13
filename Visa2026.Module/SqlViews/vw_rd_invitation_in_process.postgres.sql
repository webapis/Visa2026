-- Report Dashboard: Invitation Process (in-process) — PostgreSQL.
-- Population: invitation-producing applications that do not have an invitation yet,
-- and whose latest progress is not a completed or rejected state.
-- One row per person on the application roster.
DROP VIEW IF EXISTS vw_rd_invitation_in_process;
CREATE VIEW vw_rd_invitation_in_process AS
SELECT
    md5(a."ID"::text || p."ID"::text)::uuid                                 AS "ID",
    a."ID"                                                                  AS "ApplicationOid",
    p."ID"                                                                  AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    COALESCE(NULLIF(BTRIM(pc."NameTm"), ''), '(No project)')                AS "ProjectName",
    COALESCE(pc."NameTm", '')                                               AS "ProjectNameRaw",
    COALESCE(pc."NameTm", '')                                               AS "ProjectNameTm",
    COALESCE(p."PersonRole", 0)                                             AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(act."Name"), ''), '')                             AS "PositionLabel",
    COALESCE(
        NULLIF(BTRIM(sub."NameTm"), ''),
        NULLIF(BTRIM(sub."Name"), ''),
        ''
    )                                                                       AS "SubcontractorLabel",
    COALESCE(
        NULLIF(BTRIM(a."FullApplicationNumber"), ''),
        NULLIF(BTRIM(a."ApplicationNumber"), ''),
        ''
    )                                                                       AS "ApplicationNumber",
    CASE
        WHEN a."ApplicationDate" IS NOT NULL AND (a."ApplicationDate")::date > DATE '1900-01-01'
            THEN a."ApplicationDate"
        ELSE NULL
    END                                                                     AS "ApplicationDate",
    ministries."Approval1Date"                                              AS "Approval1Date",
    ministries."Approval1Number"                                            AS "Approval1Number",
    ministries."Approval1Ministry"                                          AS "Approval1Ministry",
    ministries."Approval2Date"                                              AS "Approval2Date",
    ministries."Approval2Number"                                            AS "Approval2Number",
    ministries."Approval2Ministry"                                          AS "Approval2Ministry",
    ministries."Approval3Date"                                              AS "Approval3Date",
    ministries."Approval3Number"                                            AS "Approval3Number",
    ministries."Approval3Ministry"                                          AS "Approval3Ministry",
    COALESCE(mig_issued."Date", mig_started."Date")                         AS "MigrationDate",
    COALESCE(
        mig_issued."ProcessNumber",
        mig_started."ProcessNumber",
        NULLIF(BTRIM(a."ProcessNumber"), ''),
        ''
    )                                                                       AS "MigrationResultNumber",
    CASE
        WHEN COALESCE(mig_issued."Date", mig_started."Date") IS NULL
             AND COALESCE(mig_issued."ProcessNumber", mig_started."ProcessNumber", NULLIF(BTRIM(a."ProcessNumber"), ''), '') = ''
            THEN ''
        ELSE
            CASE
                WHEN COALESCE(mig_issued."Date", mig_started."Date") IS NULL THEN ''
                ELSE to_char(COALESCE(mig_issued."Date", mig_started."Date"), 'DD.MM.YYYY')
            END
            || CASE
                WHEN COALESCE(mig_issued."ProcessNumber", mig_started."ProcessNumber", NULLIF(BTRIM(a."ProcessNumber"), ''), '') = '' THEN ''
                ELSE
                    CASE WHEN COALESCE(mig_issued."Date", mig_started."Date") IS NULL THEN '' ELSE chr(10) END
                    || '№ ' || COALESCE(mig_issued."ProcessNumber", mig_started."ProcessNumber", NULLIF(BTRIM(a."ProcessNumber"), ''))
            END
    END                                                                     AS "MigrationLabel",
    COALESCE(apf."ChangeInvitations", FALSE)                                AS "ValidityExtended",
    COALESCE(
        NULLIF(BTRIM(ast."NameTm"), ''),
        NULLIF(BTRIM(ast."Name"), ''),
        'Being Prepared'
    )                                                                       AS "StatusLabel",
    CASE
        WHEN ast."Code" IN ('1_REVIEW_APPROVED', '2_REVIEW_APPROVED', '3_REVIEW_APPROVED')
            THEN 'st-approved'
        ELSE 'st-pending'
    END                                                                     AS "StatusCssClass",
    COALESCE(ast."Code", '')                                                AS "ProgressStateCode",
    (
        COALESCE(NULLIF(BTRIM(pc."NameTm"), ''), '(No project)')
        || ' · ' ||
        COALESCE(NULLIF(BTRIM(ast."NameTm"), ''), NULLIF(BTRIM(ast."Name"), ''), 'Being Prepared')
    )                                                                       AS "ChartStatusLabel",
    CONCAT_WS(' · ',
        COALESCE(NULLIF(BTRIM(vp."NameTm"), ''), NULLIF(BTRIM(vp."Name"), ''), '(No period)'),
        COALESCE(NULLIF(BTRIM(vc."NameTm"), ''), NULLIF(BTRIM(vc."Name"), ''), '(No category)'),
        COALESCE(NULLIF(BTRIM(vt."NameTm"), ''), NULLIF(BTRIM(vt."Name"), ''), '(No type)'),
        COALESCE(NULLIF(BTRIM(ast."NameTm"), ''), NULLIF(BTRIM(ast."Name"), ''), 'Being Prepared')
    )                                                                       AS "PeriodCategoryTypeLabel",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "ApplicationProfileInstances" a
INNER JOIN "ApplicationProfiles" apf
    ON apf."ID" = a."ApplicationProfileID"
   AND COALESCE(apf."GCRecord", 0) = 0
   AND COALESCE(apf."ProduceInvitation", FALSE) = TRUE
INNER JOIN "ApplicationProfileInstancePeople" roster
    ON roster."ApplicationProfileInstanceId" = a."ID"
INNER JOIN "People" p
    ON p."ID" = roster."PersonId"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = a."ProjectContractID"
   AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "Subcontractors" sub
    ON sub."ID" = p."SubcontractorID"
   AND COALESCE(sub."GCRecord", 0) = 0
LEFT JOIN "VisaPeriods" vp
    ON vp."ID" = a."VisaPeriodID" AND COALESCE(vp."GCRecord", 0) = 0
LEFT JOIN "VisaCategories" vc
    ON vc."ID" = a."VisaCategoryID" AND COALESCE(vc."GCRecord", 0) = 0
LEFT JOIN "VisaTypes" vt
    ON vt."ID" = a."VisaTypeID" AND COALESCE(vt."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT eph."ActualPositionID"
    FROM "EmployeePositionHistories" eph
    WHERE eph."PersonID" = p."ID"
      AND COALESCE(eph."GCRecord", 0) = 0
    ORDER BY
        CASE
            WHEN eph."EndDate" IS NULL OR (eph."EndDate")::date >= CURRENT_DATE THEN 0
            ELSE 1
        END,
        eph."StartDate" DESC NULLS LAST,
        eph."ID" DESC
    LIMIT 1
) cur_pos ON TRUE
LEFT JOIN "ActualPositions" act
    ON act."ID" = cur_pos."ActualPositionID" AND COALESCE(act."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap."StateID"
    FROM "ApplicationProfileInstanceProgresses" ap
    WHERE ap."ApplicationProfileInstanceID" = a."ID"
      AND COALESCE(ap."GCRecord", 0) = 0
    ORDER BY ap."Date" DESC NULLS LAST, ap."ID" DESC
    LIMIT 1
) latest_ap ON TRUE
LEFT JOIN "ApplicationStates" ast
    ON ast."ID" = latest_ap."StateID"
   AND COALESCE(ast."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT
        MAX(dec.d) FILTER (WHERE legs.seq = 1) AS "Approval1Date",
        MAX(dec.n) FILTER (WHERE legs.seq = 1) AS "Approval1Number",
        MAX(legs.name) FILTER (WHERE legs.seq = 1) AS "Approval1Ministry",
        MAX(dec.d) FILTER (WHERE legs.seq = 2) AS "Approval2Date",
        MAX(dec.n) FILTER (WHERE legs.seq = 2) AS "Approval2Number",
        MAX(legs.name) FILTER (WHERE legs.seq = 2) AS "Approval2Ministry",
        MAX(dec.d) FILTER (WHERE legs.seq = 3) AS "Approval3Date",
        MAX(dec.n) FILTER (WHERE legs.seq = 3) AS "Approval3Number",
        MAX(legs.name) FILTER (WHERE legs.seq = 3) AS "Approval3Ministry"
    FROM (
        SELECT snap."Sequence" AS seq, BTRIM(snap."MinistryShortName") AS name
        FROM "ApplicationProfileInstanceApprovalLegSnapshots" snap
        WHERE snap."ApplicationProfileInstanceId" = a."ID"
          AND COALESCE(snap."GCRecord", 0) = 0
        UNION ALL
        SELECT leg."Sequence", BTRIM(m."ShortNameTm")
        FROM "ApprovalLegProfileMinistryLegs" leg
        INNER JOIN "ApprovingMinistries" m
            ON m."ID" = leg."ApprovingMinistryId" AND COALESCE(m."GCRecord", 0) = 0
        WHERE a."ApprovalLegProfileID" IS NOT NULL
          AND leg."ApprovalLegProfileId" = a."ApprovalLegProfileID"
          AND COALESCE(leg."GCRecord", 0) = 0
          AND NOT EXISTS (
              SELECT 1
              FROM "ApplicationProfileInstanceApprovalLegSnapshots" snap2
              WHERE snap2."ApplicationProfileInstanceId" = a."ID"
                AND COALESCE(snap2."GCRecord", 0) = 0
          )
    ) legs
    LEFT JOIN LATERAL (
        SELECT pr."Date" AS d,
               COALESCE(
                   NULLIF(BTRIM(pr."ResultNumber"), ''),
                   CASE
                       WHEN position(':' IN COALESCE(pr."Description", '')) > 0
                            AND BTRIM(substring(pr."Description" FROM position(':' IN pr."Description") + 1)) ~ '[0-9]'
                       THEN BTRIM(substring(pr."Description" FROM position(':' IN pr."Description") + 1))
                       ELSE NULL
                   END
               ) AS n
        FROM "ApplicationProfileInstanceProgresses" pr
        INNER JOIN "ApplicationStates" st
            ON st."ID" = pr."StateID" AND COALESCE(st."GCRecord", 0) = 0
        WHERE pr."ApplicationProfileInstanceID" = a."ID"
          AND COALESCE(pr."GCRecord", 0) = 0
          AND legs.seq BETWEEN 1 AND 5
          AND BTRIM(COALESCE(st."Code", '')) IN (
              legs.seq::text || '_REVIEW_APPROVED',
              legs.seq::text || '_REVIEW_REJECTED')
        ORDER BY pr."Date" DESC NULLS LAST, pr."ID" DESC
        LIMIT 1
    ) dec ON TRUE
) ministries ON TRUE
LEFT JOIN LATERAL (
    SELECT pr."Date" AS "Date", NULLIF(BTRIM(pr."ProcessNumber"), '') AS "ProcessNumber"
    FROM "ApplicationProfileInstanceProgresses" pr
    INNER JOIN "ApplicationStates" st
        ON st."ID" = pr."StateID" AND COALESCE(st."GCRecord", 0) = 0
    WHERE pr."ApplicationProfileInstanceID" = a."ID"
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
    WHERE pr."ApplicationProfileInstanceID" = a."ID"
      AND COALESCE(pr."GCRecord", 0) = 0
      AND BTRIM(COALESCE(st."Code", '')) = 'PROCESS_STARTED'
    ORDER BY pr."Date" DESC NULLS LAST, pr."ID" DESC
    LIMIT 1
) mig_started ON TRUE
WHERE COALESCE(a."GCRecord", 0) = 0
  AND NOT EXISTS (
        SELECT 1
        FROM "Invitations" inv
        WHERE inv."ApplicationProfileInstanceID" = a."ID"
          AND COALESCE(inv."GCRecord", 0) = 0)
  AND (
        ast."Code" IS NULL
        OR ast."Code" NOT IN (
            'PROCESS_ISSUED',
            'PROCESS_REJECTED',
            'PROCESS_CANCELLED',
            '1_REVIEW_REJECTED',
            '2_REVIEW_REJECTED',
            '3_REVIEW_REJECTED',
            '4_REVIEW_REJECTED',
            '5_REVIEW_REJECTED')
      )
  AND (
        BTRIM(COALESCE(a."LatestPrimaryStateCode", '')) = ''
        OR BTRIM(a."LatestPrimaryStateCode") NOT IN (
            'PROCESS_ISSUED',
            'PROCESS_REJECTED',
            'PROCESS_CANCELLED',
            '1_REVIEW_REJECTED',
            '2_REVIEW_REJECTED',
            '3_REVIEW_REJECTED',
            '4_REVIEW_REJECTED',
            '5_REVIEW_REJECTED')
      );
