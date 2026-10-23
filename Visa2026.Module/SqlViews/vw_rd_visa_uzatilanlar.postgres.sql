-- Report Dashboard: uzatılanlar — PostgreSQL.
-- Same people as Visa On Extension. Invitation Process columns, plus the current visa end.
-- Projesi is the department. Firması is the subcontractor. Görevi is the visa-report position.
DROP VIEW IF EXISTS vw_rd_visa_uzatilanlar;
CREATE VIEW vw_rd_visa_uzatilanlar AS
SELECT
    b."ID"                                                                  AS "ID",
    b."ApplicationProfileInstanceOid"                                       AS "ApplicationProfileInstanceOid",
    b."PersonOid"                                                           AS "PersonOid",
    b."ExpiringVisaID"                                                      AS "VisaOid",
    b."PassportNumber"                                                      AS "PassportNumber",
    b."PersonName"                                                          AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(visa_pos."NameTm"), ''),
        NULLIF(BTRIM(visa_pos."Name"), ''),
        ''
    )                                                                       AS "PositionLabel",
    COALESCE(dept."Name", '')                                               AS "DepartmentLabel",
    COALESCE(
        NULLIF(BTRIM(sub."NameTm"), ''),
        NULLIF(BTRIM(sub."Name"), ''),
        ''
    )                                                                       AS "SubcontractorLabel",
    CASE
        WHEN (v."ExpirationDate")::date > DATE '1900-01-01' THEN v."ExpirationDate"
        ELSE NULL
    END                                                                     AS "CurrentVisaEndDate",
    COALESCE(NULLIF(BTRIM(b."ApplicationNumber"), ''), '')                  AS "ApplicationNumber",
    CASE
        WHEN b."ApplicationDate" IS NOT NULL AND (b."ApplicationDate")::date > DATE '1900-01-01'
            THEN b."ApplicationDate"
        ELSE NULL
    END                                                                     AS "ApplicationDate",
    COALESCE(
        NULLIF(BTRIM(vp."NameTm"), ''),
        NULLIF(BTRIM(vp."Name"), ''),
        ''
    )                                                                       AS "VisaPeriodLabel",
    last_approval."Date"                                                    AS "LastApprovalDate",
    COALESCE(last_approval."Number", '')                                    AS "LastApprovalNumber",
    last_approval."SecondLastDate"                                           AS "SecondLastApprovalDate",
    COALESCE(last_approval."SecondLastNumber", '')                          AS "SecondLastApprovalNumber",
    last_approval."ThirdLastDate"                                            AS "ThirdLastApprovalDate",
    COALESCE(last_approval."ThirdLastNumber", '')                           AS "ThirdLastApprovalNumber",
    CASE
        WHEN mig_submit."Date" IS NOT NULL AND (mig_submit."Date")::date > DATE '1900-01-01'
            THEN mig_submit."Date"
        ELSE NULL
    END                                                                     AS "MigrationSubmittedDate",
    COALESCE(mig_issued."Date", mig_started."Date")                         AS "MigrationDate",
    COALESCE(
        mig_issued."ProcessNumber",
        mig_started."ProcessNumber",
        NULLIF(BTRIM(a."ProcessNumber"), ''),
        ''
    )                                                                       AS "MigrationResultNumber",
    COALESCE(b."ProgressStateCode", '')                                     AS "ProgressStateCode",
    COALESCE(NULLIF(BTRIM(held_ministry."Name"), ''), '')                   AS "MinistryShortName",
    b."ProjectName"                                                         AS "ProjectName",
    b."ProjectNameRaw"                                                      AS "ProjectNameRaw",
    b."ProjectNameTm"                                                       AS "ProjectNameTm",
    b."PersonRoleCode"                                                      AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(b."ProjectName"), ''), '(No project)')            AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    b."IsArchived"                                                          AS "IsArchived"
FROM vw_rd_visa_on_extension b
INNER JOIN "ApplicationProfileInstances" a
    ON a."ID" = b."ApplicationProfileInstanceOid"
   AND COALESCE(a."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = b."PersonOid"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "Visas" v
    ON v."ID" = b."ExpiringVisaID"
   AND COALESCE(v."GCRecord", 0) = 0
LEFT JOIN "Subcontractors" sub
    ON sub."ID" = p."SubcontractorID"
   AND COALESCE(sub."GCRecord", 0) = 0
LEFT JOIN "VisaPeriods" vp
    ON vp."ID" = a."VisaPeriodID"
   AND COALESCE(vp."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT eph."PositionID"
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
LEFT JOIN "Positions" visa_pos
    ON visa_pos."ID" = cur_pos."PositionID"
   AND COALESCE(visa_pos."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT COALESCE(NULLIF(BTRIM(dep."NameTm"), ''), NULLIF(BTRIM(dep."Name"), ''), '') AS "Name"
    FROM "People" dept_person
    LEFT JOIN "People" dept_sponsor
        ON dept_sponsor."ID" = dept_person."SponsoringEmployeeID"
       AND COALESCE(dept_sponsor."GCRecord", 0) = 0
    INNER JOIN "EmployeePositionHistories" dept_eph
        ON dept_eph."PersonID" = CASE
            WHEN dept_person."PersonRole" = 1 AND dept_sponsor."ID" IS NOT NULL THEN dept_sponsor."ID"
            ELSE dept_person."ID"
        END
       AND COALESCE(dept_eph."GCRecord", 0) = 0
    LEFT JOIN "Departments" dep
        ON dep."ID" = dept_eph."DepartmentID"
       AND COALESCE(dep."GCRecord", 0) = 0
    WHERE dept_person."ID" = p."ID"
    ORDER BY
        CASE
            WHEN dept_eph."EndDate" IS NULL OR (dept_eph."EndDate")::date >= CURRENT_DATE THEN 0
            ELSE 1
        END,
        dept_eph."StartDate" DESC NULLS LAST,
        dept_eph."ID" DESC
    LIMIT 1
) dept ON TRUE
LEFT JOIN LATERAL (
    SELECT BTRIM(legs.mname) AS "Name"
    FROM (
        SELECT snap."Sequence" AS seq, snap."MinistryShortName" AS mname
        FROM "ApplicationProfileInstanceApprovalLegSnapshots" snap
        WHERE snap."ApplicationProfileInstanceId" = a."ID"
          AND COALESCE(snap."GCRecord", 0) = 0
        UNION ALL
        SELECT leg."Sequence", m."ShortNameTm"
        FROM "ApprovalLegProfileMinistryLegs" leg
        INNER JOIN "ApprovingMinistries" m
            ON m."ID" = leg."ApprovingMinistryId"
           AND COALESCE(m."GCRecord", 0) = 0
        WHERE a."ApprovalLegProfileID" IS NOT NULL
          AND leg."ApprovalLegProfileId" = a."ApprovalLegProfileID"
          AND COALESCE(leg."GCRecord", 0) = 0
          AND NOT EXISTS (
              SELECT 1
              FROM "ApplicationProfileInstanceApprovalLegSnapshots" snap2
              WHERE snap2."ApplicationProfileInstanceId" = a."ID"
                AND COALESCE(snap2."GCRecord", 0) = 0)
    ) legs
    WHERE BTRIM(COALESCE(b."ProgressStateCode", '')) ~ '^[1-5]_REVIEW_STARTED$'
      AND legs.seq = (left(BTRIM(b."ProgressStateCode"), 1))::int
    LIMIT 1
) held_ministry ON TRUE
LEFT JOIN LATERAL (
    SELECT
        MAX(ranked.d) FILTER (WHERE ranked.rn = 1) AS "Date",
        COALESCE(MAX(ranked.n) FILTER (WHERE ranked.rn = 1), '') AS "Number",
        MAX(ranked.d) FILTER (WHERE ranked.rn = 2) AS "SecondLastDate",
        COALESCE(MAX(ranked.n) FILTER (WHERE ranked.rn = 2), '') AS "SecondLastNumber",
        MAX(ranked.d) FILTER (WHERE ranked.rn = 3) AS "ThirdLastDate",
        COALESCE(MAX(ranked.n) FILTER (WHERE ranked.rn = 3), '') AS "ThirdLastNumber"
    FROM (
        SELECT
            dec.d,
            dec.n,
            ROW_NUMBER() OVER (ORDER BY legs.seq DESC) AS rn
        FROM (
            SELECT snap."Sequence" AS seq
            FROM "ApplicationProfileInstanceApprovalLegSnapshots" snap
            WHERE snap."ApplicationProfileInstanceId" = a."ID"
              AND COALESCE(snap."GCRecord", 0) = 0
            UNION ALL
            SELECT leg."Sequence"
            FROM "ApprovalLegProfileMinistryLegs" leg
            WHERE a."ApprovalLegProfileID" IS NOT NULL
              AND leg."ApprovalLegProfileId" = a."ApprovalLegProfileID"
              AND COALESCE(leg."GCRecord", 0) = 0
              AND NOT EXISTS (
                  SELECT 1
                  FROM "ApplicationProfileInstanceApprovalLegSnapshots" snap2
                  WHERE snap2."ApplicationProfileInstanceId" = a."ID"
                    AND COALESCE(snap2."GCRecord", 0) = 0)
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
                ON st."ID" = pr."StateID"
               AND COALESCE(st."GCRecord", 0) = 0
            WHERE pr."ApplicationProfileInstanceID" = a."ID"
              AND COALESCE(pr."GCRecord", 0) = 0
              AND legs.seq BETWEEN 1 AND 5
              AND BTRIM(COALESCE(st."Code", '')) IN (
                  legs.seq::text || '_REVIEW_APPROVED',
                  legs.seq::text || '_REVIEW_REJECTED')
            ORDER BY pr."Date" DESC NULLS LAST, pr."ID" DESC
            LIMIT 1
        ) dec ON TRUE
        WHERE dec.d IS NOT NULL OR NULLIF(BTRIM(dec.n), '') IS NOT NULL
    ) ranked
    WHERE ranked.rn <= 3
) last_approval ON TRUE
LEFT JOIN LATERAL (
    SELECT pr."Date" AS "Date", NULLIF(BTRIM(pr."ProcessNumber"), '') AS "ProcessNumber"
    FROM "ApplicationProfileInstanceProgresses" pr
    INNER JOIN "ApplicationStates" st
        ON st."ID" = pr."StateID"
       AND COALESCE(st."GCRecord", 0) = 0
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
        ON st."ID" = pr."StateID"
       AND COALESCE(st."GCRecord", 0) = 0
    WHERE pr."ApplicationProfileInstanceID" = a."ID"
      AND COALESCE(pr."GCRecord", 0) = 0
      AND BTRIM(COALESCE(st."Code", '')) = 'PROCESS_STARTED'
    ORDER BY pr."Date" DESC NULLS LAST, pr."ID" DESC
    LIMIT 1
) mig_started ON TRUE
LEFT JOIN LATERAL (
    SELECT pr."Date" AS "Date"
    FROM "ApplicationProfileInstanceProgresses" pr
    INNER JOIN "ApplicationStates" st
        ON st."ID" = pr."StateID"
       AND COALESCE(st."GCRecord", 0) = 0
    WHERE pr."ApplicationProfileInstanceID" = a."ID"
      AND COALESCE(pr."GCRecord", 0) = 0
      AND BTRIM(COALESCE(st."Code", '')) IN ('SUBMITTED_MIGRATION', 'PROCESS_STARTED')
    ORDER BY
        CASE WHEN BTRIM(COALESCE(st."Code", '')) = 'SUBMITTED_MIGRATION' THEN 0 ELSE 1 END,
        pr."Date" ASC NULLS LAST,
        pr."ID" ASC
    LIMIT 1
) mig_submit ON TRUE;
