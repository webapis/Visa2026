-- Visa On Extension (V): unfinished; StatusLabel = Period · Category · Type · State.
DROP VIEW IF EXISTS vw_rd_visa_on_extension_by_period_category_type;
CREATE VIEW vw_rd_visa_on_extension_by_period_category_type AS
SELECT
    b."ID",
    b."ApplicationProfileInstanceOid",
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
    CONCAT_WS(' · ',
        COALESCE(NULLIF(BTRIM(vp."NameTm"), ''), NULLIF(BTRIM(vp."Name"), ''), '(No period)'),
        COALESCE(NULLIF(BTRIM(vc."NameTm"), ''), NULLIF(BTRIM(vc."Name"), ''), '(No category)'),
        COALESCE(NULLIF(BTRIM(vt."NameTm"), ''), NULLIF(BTRIM(vt."Name"), ''), '(No type)'),
        COALESCE(NULLIF(BTRIM(b."ProgressStateLabel"), ''), 'Being Prepared')
    ) AS "StatusLabel",
    b."IsArchived"
FROM vw_rd_visa_app_progress b
LEFT JOIN "ApplicationProfileInstances" a
    ON a."ID" = b."ApplicationProfileInstanceOid" AND COALESCE(a."GCRecord", 0) = 0
LEFT JOIN "VisaPeriods" vp
    ON vp."ID" = a."VisaPeriodID" AND COALESCE(vp."GCRecord", 0) = 0
LEFT JOIN "VisaCategories" vc
    ON vc."ID" = a."VisaCategoryID" AND COALESCE(vc."GCRecord", 0) = 0
LEFT JOIN "VisaTypes" vt
    ON vt."ID" = a."VisaTypeID" AND COALESCE(vt."GCRecord", 0) = 0
WHERE b."ProgressStateCode" IS NULL
   OR BTRIM(b."ProgressStateCode") = ''
   OR (
        b."ProgressStateCode" NOT IN ('PROCESS_ISSUED', 'PROCESS_CANCELLED', 'PROCESS_REJECTED')
        AND RIGHT(BTRIM(b."ProgressStateCode"), 16) <> '_REVIEW_REJECTED'
      );
