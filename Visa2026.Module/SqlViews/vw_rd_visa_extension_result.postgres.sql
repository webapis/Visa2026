-- Extension Result (P): terminal (Issued/Cancelled/Rejected/*_REVIEW_REJECTED); StatusLabel = Project · State.
DROP VIEW IF EXISTS vw_rd_visa_extension_result;
CREATE VIEW vw_rd_visa_extension_result AS
SELECT
    b."ID",
    b."ApplicationOid",
    b."PersonOid",
    b."ExpiringVisaID",
    b."PassportID",
    b."PassportNumber",
    b."CurrentStateID",
    b."PersonName",
    b."ProjectName",
    b."ProjectNameRaw",
    b."ProjectNameTm",
    b."PersonRoleCode",
    b."ApplicationNumber",
    b."ApplicationDate",
    b."StatusDate",
    b."ProgressStateCode",
    b."ProgressStateLabel",
    b."ProgressStateCssClass",
    b."DaysRemainingOnVisa",
    CONCAT(
        COALESCE(NULLIF(BTRIM(b."ProjectName"), ''), '(No project)'),
        ' · ',
        COALESCE(NULLIF(BTRIM(b."ProgressStateLabel"), ''), 'Being Prepared')
    ) AS "StatusLabel",
    b."IsArchived"
FROM vw_rd_visa_app_progress b
WHERE b."ProgressStateCode" IN ('PROCESS_ISSUED', 'PROCESS_CANCELLED', 'PROCESS_REJECTED')
   OR RIGHT(BTRIM(b."ProgressStateCode"), 16) = '_REVIEW_REJECTED';
