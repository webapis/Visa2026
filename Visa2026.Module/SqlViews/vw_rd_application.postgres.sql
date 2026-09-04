-- Report Dashboard: ApplicationProfileInstance category (PostgreSQL).
DROP VIEW IF EXISTS vw_rd_application;
CREATE VIEW vw_rd_application AS
SELECT
    a."ID"                                                                  AS "ID",
    first_p."ID"                                                            AS "PersonOid",
    COALESCE(
        NULLIF(CONCAT_WS(' ',
            NULLIF(BTRIM(first_p."FirstName"), ''),
            NULLIF(BTRIM(first_p."MiddleName"), ''),
            NULLIF(BTRIM(first_p."LastName"), '')
        ), ''),
        NULLIF(BTRIM(a."FullApplicationNumber"), ''),
        NULLIF(BTRIM(a."ApplicationNumber"), ''),
        ''
    )                                                                       AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        ''
    )                                                                       AS "ProjectName",
    COALESCE(pc."NameTm", '')                                               AS "ProjectNameRaw",
    COALESCE(pc."NameTm", '')                                               AS "ProjectNameTm",
    COALESCE(first_p."PersonRole", 0)                                       AS "PersonRoleCode",
    COALESCE(
        NULLIF(BTRIM(a."FullApplicationNumber"), ''),
        NULLIF(BTRIM(a."ApplicationNumber"), ''),
        ''
    )                                                                       AS "ApplicationNumber",
    a."ApplicationDate"                                                     AS "ApplicationDate",
    COALESCE(
        NULLIF(BTRIM(ast."NameTm"), ''),
        NULLIF(BTRIM(ast."Name"), ''),
        'Being Prepared'
    )                                                                       AS "ProgressStateLabel",
    CASE
      WHEN ast."Code" IN ('PROCESS_ISSUED', '1_REVIEW_APPROVED', '2_REVIEW_APPROVED')
                                                                             THEN 'st-approved'
      WHEN ast."Code" IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED', '1_REVIEW_REJECTED', '2_REVIEW_REJECTED')
                                                                             THEN 'st-expiring'
      ELSE                                                                   'st-pending'
    END                                                                     AS "ProgressStateCssClass",
    COALESCE(ast."Code", '')                                                AS "ProgressStateCode",
    COALESCE(
        NULLIF(BTRIM(apf."Name"), ''),
        NULLIF(BTRIM(apf."Code"), ''),
        'Unknown'
    )                                                                       AS "TypeLabel",
    COALESCE(first_p."IsArchived", FALSE)                                   AS "IsArchived"
FROM "ApplicationProfileInstances" a
LEFT JOIN "ApplicationProfiles" apf
    ON apf."ID" = a."ApplicationProfileID"
   AND COALESCE(apf."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = a."ProjectContractID"
   AND COALESCE(pc."GCRecord", 0) = 0
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
    SELECT ap_row."PersonId"
    FROM "ApplicationProfileInstancePeople" ap_row
    WHERE ap_row."ApplicationProfileInstanceId" = a."ID"
    ORDER BY ap_row."PersonId"
    LIMIT 1
) first_m2m ON TRUE
LEFT JOIN "People" first_p
    ON first_p."ID" = first_m2m."PersonId"
   AND COALESCE(first_p."GCRecord", 0) = 0
WHERE COALESCE(a."GCRecord", 0) = 0;
