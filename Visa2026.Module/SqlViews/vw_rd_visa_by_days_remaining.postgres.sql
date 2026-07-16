-- Report Dashboard: valid visas by days remaining until expiry (By Days Remaining).
-- Buckets: < 10 days / < 1 month / < 3..6 months / ≥ 6 months. Valid visas only.
DROP VIEW IF EXISTS vw_rd_visa_by_days_remaining;
CREATE VIEW vw_rd_visa_by_days_remaining AS
SELECT
    v."ID"                                                              AS "ID",
    p."ID"                                                              AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                   AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        ''
    )                                                                   AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '')                             AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '')                             AS "ProjectNameTm",
    p."PersonRole"                                                      AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(v."VisaNumber"), ''), '')                     AS "VisaNumber",
    CASE WHEN (v."ExpirationDate")::date > DATE '1900-01-01' THEN v."ExpirationDate" ELSE NULL END AS "ExpirationDate",
    (v."ExpirationDate")::date - CURRENT_DATE                           AS "DaysRemaining",
    CASE
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 10  THEN '< 10 days'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 30  THEN '< 1 month'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 90  THEN '< 3 months'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 120 THEN '< 4 months'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 150 THEN '< 5 months'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 180 THEN '< 6 months'
        ELSE '≥ 6 months'
    END                                                                 AS "RemainingLabel",
    CASE
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 10  THEN '< 10 days'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 30  THEN '< 1 month'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 90  THEN '< 3 months'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 120 THEN '< 4 months'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 150 THEN '< 5 months'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 180 THEN '< 6 months'
        ELSE '≥ 6 months'
    END                                                                 AS "StatusLabel",
    CASE
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 30  THEN 'st-expiring'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 90  THEN 'st-pending'
        ELSE 'st-approved'
    END                                                                 AS "StatusCssClass",
    COALESCE(p."IsArchived", FALSE)                                     AS "IsArchived"
FROM "Visas" v
INNER JOIN "Passports" pp
    ON pp."ID" = v."PassportID"
   AND COALESCE(pp."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = pp."PersonID"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID"
   AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID"
   AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID"
   AND COALESCE(spc."GCRecord", 0) = 0
WHERE COALESCE(v."GCRecord", 0) = 0
  AND COALESCE(v."IsCancelled", FALSE) = FALSE
  AND v."ExpirationDate" IS NOT NULL
  AND (v."ExpirationDate")::date >= CURRENT_DATE;