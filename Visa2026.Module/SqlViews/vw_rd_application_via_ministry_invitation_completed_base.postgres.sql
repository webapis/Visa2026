-- Invitation Completed base.
-- One row per ApplicationItem — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_application_via_ministry_invitation_completed_base CASCADE;
CREATE VIEW vw_rd_application_via_ministry_invitation_completed_base AS
SELECT
    ai."ID"                                                                 AS "ID",
    a."ID"                                                                  AS "ApplicationOid",
    ai."ID"                                                                 AS "ApplicationItemOid",
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
    COALESCE(NULLIF(BTRIM(pc."NameTm"), ''), '(No project)')                AS "ProjectName",
    COALESCE(pc."NameTm", '')                                               AS "ProjectNameRaw",
    COALESCE(pc."NameTm", '')                                               AS "ProjectNameTm",
    COALESCE(p."PersonRole", 0)                                             AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(pos."NameTm"), ''), NULLIF(BTRIM(pos."Name"), ''), '') AS "PositionLabel",
    COALESCE(NULLIF(BTRIM(at."NameTm"), ''), NULLIF(BTRIM(at."Name"), ''), '') AS "ApplicationTypeLabel",
    COALESCE(
        NULLIF(BTRIM(vp."NameTm"), ''),
        NULLIF(BTRIM(vp."Name"), ''),
        ''
    )                                                                   AS "VisaPeriodLabel",
    COALESCE(
        NULLIF(BTRIM(vt."NameTm"), ''),
        NULLIF(BTRIM(vt."Name"), ''),
        ''
    )                                                                   AS "VisaTypeLabel",
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
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived",
    COALESCE(NULLIF(BTRIM(vp."NameTm"), ''), NULLIF(BTRIM(vp."Name"), ''), '(No period)') AS "PeriodLabel",
    COALESCE(NULLIF(BTRIM(vc."NameTm"), ''), NULLIF(BTRIM(vc."Name"), ''), '(No category)') AS "CategoryLabel",
    COALESCE(NULLIF(BTRIM(vt."NameTm"), ''), NULLIF(BTRIM(vt."Name"), ''), '(No type)') AS "TypeLabel"
FROM "ApplicationItems" ai
INNER JOIN "Applications" a
    ON a."ID" = ai."ApplicationID" AND COALESCE(a."GCRecord", 0) = 0
INNER JOIN "ApplicationTypes" at
    ON at."ID" = a."ApplicationTypeID" AND COALESCE(at."GCRecord", 0) = 0
   AND COALESCE(at."CanIssueInvitation", FALSE) = TRUE
   AND COALESCE(at."ApplicationProgressRoute", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = a."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" p
    ON p."ID" = ai."PersonID" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "EmployeePositionHistories" eph
    ON eph."ID" = ai."CurrentPositionHistoryID" AND COALESCE(eph."GCRecord", 0) = 0
LEFT JOIN "Positions" pos
    ON pos."ID" = eph."PositionID" AND COALESCE(pos."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap."StateID" FROM "ApplicationProgresses" ap
    WHERE ap."ApplicationID" = a."ID" AND COALESCE(ap."GCRecord", 0) = 0
    ORDER BY ap."Date" DESC NULLS LAST, ap."ID" DESC LIMIT 1
) latest_ap ON TRUE
LEFT JOIN "ApplicationStates" ast
    ON ast."ID" = latest_ap."StateID" AND COALESCE(ast."GCRecord", 0) = 0
LEFT JOIN "VisaPeriods" vp ON vp."ID" = a."VisaPeriodID" AND COALESCE(vp."GCRecord", 0) = 0
LEFT JOIN "VisaCategories" vc ON vc."ID" = a."VisaCategoryID" AND COALESCE(vc."GCRecord", 0) = 0
LEFT JOIN "VisaTypes" vt ON vt."ID" = a."VisaTypeID" AND COALESCE(vt."GCRecord", 0) = 0
WHERE COALESCE(ai."GCRecord", 0) = 0

  AND (
        COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), '')
            IN ('PROCESS_ISSUED','PROCESS_REJECTED','PROCESS_CANCELLED','1_REVIEW_REJECTED','2_REVIEW_REJECTED','3_REVIEW_REJECTED','4_REVIEW_REJECTED','5_REVIEW_REJECTED')
        OR RIGHT(COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), ''), 16) = '_REVIEW_REJECTED'
      )
;
