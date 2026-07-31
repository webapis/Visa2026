-- Report Dashboard: Visa — On Extension (PostgreSQL).
-- Shared by dashboard preview and Open ListView (VwRdVisaAppProgress).
-- ProgressStateCode prefers Application.LatestPrimaryStateCode (authoritative terminal outcome; latest progress row can lag).
DROP VIEW IF EXISTS vw_rd_visa_app_progress CASCADE;
CREATE VIEW vw_rd_visa_app_progress AS
SELECT
    ai."ID"                                                                 AS "ID",
    a."ID"                                                                  AS "ApplicationOid",
    p."ID"                                                                  AS "PersonOid",
    ai."CurrentVisaId"                                                      AS "ExpiringVisaID",
    ai."CurrentPassportID"                                                  AS "PassportID",
    COALESCE(NULLIF(BTRIM(pp."PassportNumber"), ''), '')                     AS "PassportNumber",
    latest_ap."StateID"                                                     AS "CurrentStateID",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        ''
    )                                                                       AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameTm",
    p."PersonRole"                                                          AS "PersonRoleCode",
    COALESCE(
        NULLIF(BTRIM(a."FullApplicationNumber"), ''),
        NULLIF(BTRIM(a."ApplicationNumber"), ''),
        ''
    )                                                                       AS "ApplicationNumber",
    a."ApplicationDate"                                                     AS "ApplicationDate",
    latest_ap."Date"                                                        AS "StatusDate",
    COALESCE(
        NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''),
        NULLIF(BTRIM(ast."Code"), ''),
        ''
    )                                                                       AS "ProgressStateCode",
    COALESCE(
        NULLIF(BTRIM(a."LatestProgressDisplay"), ''),
        NULLIF(BTRIM(ast."Name"), ''),
        NULLIF(BTRIM(ast."NameTm"), ''),
        'Being Prepared'
    )                                                                       AS "ProgressStateLabel",
    CASE
      WHEN COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), '')
           IN ('PROCESS_ISSUED', '1_REVIEW_APPROVED', '2_REVIEW_APPROVED')
                                                                             THEN 'st-approved'
      WHEN COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), '')
           IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED', '1_REVIEW_REJECTED', '2_REVIEW_REJECTED')
           OR RIGHT(COALESCE(NULLIF(BTRIM(a."LatestPrimaryStateCode"), ''), NULLIF(BTRIM(ast."Code"), ''), ''), 16)
              = '_REVIEW_REJECTED'
                                                                             THEN 'st-expiring'
      ELSE                                                                   'st-pending'
    END                                                                     AS "ProgressStateCssClass",
    CASE
        WHEN COALESCE(v."IsCancelled", FALSE) THEN 0
        WHEN v."ExpirationDate" IS NULL THEN 0
        WHEN (v."ExpirationDate"::date - CURRENT_DATE) < 0 THEN 0
        ELSE (v."ExpirationDate"::date - CURRENT_DATE)
    END                                                                     AS "DaysRemainingOnVisa",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "ApplicationItems" ai
INNER JOIN "Applications" a
    ON a."ID" = ai."ApplicationID"
   AND COALESCE(a."GCRecord", 0) = 0
INNER JOIN "ApplicationTypes" at
    ON at."ID" = a."ApplicationTypeID"
   AND COALESCE(at."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = ai."PersonID"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = COALESCE(a."ProjectContractID", p."ProjectContractID")
   AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID"
   AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID"
   AND COALESCE(spc."GCRecord", 0) = 0
LEFT JOIN "Visas" v
    ON v."ID" = ai."CurrentVisaId"
   AND COALESCE(v."GCRecord", 0) = 0
LEFT JOIN "Passports" pp
    ON pp."ID" = ai."CurrentPassportID"
   AND COALESCE(pp."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap."StateID", ap."Date"
    FROM "ApplicationProgresses" ap
    WHERE ap."ApplicationID" = a."ID"
      AND COALESCE(ap."GCRecord", 0) = 0
    ORDER BY ap."Date" DESC NULLS LAST, ap."ID" DESC
    LIMIT 1
) latest_ap ON TRUE
LEFT JOIN "ApplicationStates" ast
    ON ast."ID" = latest_ap."StateID"
   AND COALESCE(ast."GCRecord", 0) = 0
WHERE COALESCE(ai."GCRecord", 0) = 0
  AND ai."CurrentVisaId" IS NOT NULL
  AND at."Name" IN (
        'App_Visa_Ext',
        'App_Visa_Ext_According_to_WP',
        'App_Visa_Ext_FM',
        'App_Visa_and_WP_Ext'
    );
