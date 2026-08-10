-- Visa Extension Completed base.
-- One row per roster line (ApplicationPerson M2M + legacy ApplicationItem fallback) — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_application_via_ministry_visa_extension_completed_base CASCADE;
CREATE VIEW vw_rd_application_via_ministry_visa_extension_completed_base AS
WITH {{MINISTRY_ROSTER_CTE}}
SELECT
    roster."LineId" AS "ID",
    a."ID"                                                                  AS "ApplicationOid",
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
    v_on_ext."ID"                                                           AS "VisaOnExtensionOid",
    COALESCE(NULLIF(BTRIM(v_on_ext."VisaNumber"), ''), '')                  AS "VisaOnExtensionNumber",
    issued."ID"                                                             AS "IssuedVisaOid",
    COALESCE(NULLIF(BTRIM(issued."VisaNumber"), ''), '')                    AS "IssuedVisaNumber",
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
FROM ministry_roster_lines roster
INNER JOIN "Applications" a
    ON a."ID" = roster."ApplicationID" AND COALESCE(a."GCRecord", 0) = 0
INNER JOIN "ApplicationTypes" at
    ON at."ID" = a."ApplicationTypeID" AND COALESCE(at."GCRecord", 0) = 0
   AND COALESCE(at."ApplicationProgressRoute", 0) = 0
   AND at."Name" IN ('App_Visa_Ext','App_Visa_Ext_According_to_WP','App_Visa_Ext_FM','App_Visa_and_WP_Ext')
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = a."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" p
    ON p."ID" = roster."PersonID" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "EmployeePositionHistories" eph
    ON eph."ID" = roster."PositionHistoryID" AND COALESCE(eph."GCRecord", 0) = 0
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
LEFT JOIN "Visas" v_on_ext
    ON v_on_ext."ID" = roster."ExpiringVisaID" AND COALESCE(v_on_ext."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT v."ID", v."VisaNumber"
    FROM "Visas" v
    WHERE (v."IssuingApplicationItemID" = roster."LineId" OR (v."IssuingApplicationID" = a."ID" AND roster."PassportID" IS NOT NULL AND v."PassportID" = roster."PassportID")) AND COALESCE(v."GCRecord", 0) = 0
    ORDER BY v."IssueDate" DESC NULLS LAST, v."ID" DESC
    LIMIT 1
) issued ON TRUE

WHERE COALESCE(a."GCRecord", 0) = 0
  AND (
        COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), '')
            IN ('PROCESS_ISSUED','PROCESS_REJECTED','PROCESS_CANCELLED','1_REVIEW_REJECTED','2_REVIEW_REJECTED','3_REVIEW_REJECTED','4_REVIEW_REJECTED','5_REVIEW_REJECTED')
        OR RIGHT(COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), ''), 16) = '_REVIEW_REJECTED'
      )
;
