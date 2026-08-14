-- ApplicationProfileInstance (direct migration) Process Complete.
-- One row per roster line (ApplicationProfileInstancePerson M2M + legacy ApplicationItem fallback); route = DirectToMigrationService (1).
-- Project from Person.ProjectContract (else sponsor) — not Application.ProjectContract. — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_application_direct_migration_process_complete;
CREATE VIEW vw_rd_application_direct_migration_process_complete AS
WITH {{MINISTRY_ROSTER_CTE}}
SELECT
    roster."LineId" AS "ID",
    a."ID"                                                                  AS "ApplicationProfileInstanceOid",
    roster."LineId" AS "ApplicationItemOid",
    p."ID"                                                                  AS "PersonOid",
    latest_ap."StateID"                                                     AS "CurrentStateID",
    COALESCE(
        NULLIF(CONCAT_WS(' ',
            NULLIF(BTRIM(p."FirstName"), ''),
            NULLIF(BTRIM(p."MiddleName"), ''),
            NULLIF(BTRIM(p."LastName"), '')
        ), ''),
        NULLIF(BTRIM(a."FullApplicationNumber"), ''),
        NULLIF(BTRIM(a."ApplicationNumber"), ''),
        ''
    )                                                                       AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameTm",
    COALESCE(p."PersonRole", 0)                                             AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(apf."Name"), ''), '') AS "ApplicationTypeLabel",
    COALESCE(NULLIF(BTRIM(a."FullApplicationNumber"), ''), NULLIF(BTRIM(a."ApplicationNumber"), ''), '') AS "ApplicationNumber",
    a."ApplicationDate"                                                     AS "ApplicationDate",
    COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), '') AS "ProgressStateCode",
    COALESCE(
        NULLIF(BTRIM(a."LatestProgressDisplay"), ''),
        NULLIF(BTRIM(ast."Name"), ''),
        NULLIF(BTRIM(ast."NameTm"), ''),
        'At office'
    )                                                                       AS "StatusLabel",
    CASE
      WHEN COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), '')
           IN ('PROCESS_ISSUED', '1_REVIEW_APPROVED', '2_REVIEW_APPROVED') THEN 'st-approved'
      WHEN COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), '')
           IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED', '1_REVIEW_REJECTED', '2_REVIEW_REJECTED')
           OR RIGHT(COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), ''), 16) = '_REVIEW_REJECTED'
           THEN 'st-expiring'
      ELSE 'st-pending'
    END                                                                     AS "StatusCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM ministry_roster_lines roster
INNER JOIN "ApplicationProfileInstances" a
    ON a."ID" = roster."ApplicationProfileInstanceID" AND COALESCE(a."GCRecord", 0) = 0
INNER JOIN "ApplicationProfiles" apf
    ON apf."ID" = a."ApplicationProfileID" AND COALESCE(apf."GCRecord", 0) = 0
   AND COALESCE(apf."ProgressRoute", 0) = 1
LEFT JOIN "People" p
    ON p."ID" = roster."PersonID" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap."StateID" FROM "ApplicationProfileInstanceProgresses" ap
    WHERE ap."ApplicationProfileInstanceID" = a."ID" AND COALESCE(ap."GCRecord", 0) = 0
    ORDER BY ap."Date" DESC NULLS LAST, ap."ID" DESC LIMIT 1
) latest_ap ON TRUE
LEFT JOIN "ApplicationStates" ast
    ON ast."ID" = latest_ap."StateID" AND COALESCE(ast."GCRecord", 0) = 0
WHERE COALESCE(a."GCRecord", 0) = 0
  AND (
        COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), '')
            IN ('PROCESS_ISSUED','PROCESS_REJECTED','PROCESS_CANCELLED','1_REVIEW_REJECTED','2_REVIEW_REJECTED','3_REVIEW_REJECTED','4_REVIEW_REJECTED','5_REVIEW_REJECTED')
        OR RIGHT(COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), ''), 16) = '_REVIEW_REJECTED'
      )
;