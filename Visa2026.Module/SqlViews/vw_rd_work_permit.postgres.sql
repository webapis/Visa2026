-- Report Dashboard: valid WorkPermitItems by days remaining (By Days Remaining).
-- One row per valid (non-cancelled, not expired) item; persons may appear more than once.
-- Buckets: < 10 days / < 1 month / < 3..6 months / ≥ 6 months.
DROP VIEW IF EXISTS vw_rd_work_permit;
CREATE VIEW vw_rd_work_permit AS
SELECT
    wpi."ID"                                                                AS "ID",
    p."ID"                                                                  AS "PersonOid",
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
    COALESCE(NULLIF(BTRIM(wpi."WorkPermitNumber"), ''), NULLIF(BTRIM(wpi."ASNumber"), ''), '') AS "WorkPermitNumber",
    CASE WHEN (wpi."ExpirationDate")::date > DATE '1900-01-01' THEN wpi."ExpirationDate" ELSE NULL END AS "ExpirationDate",
    (wpi."ExpirationDate")::date - CURRENT_DATE                             AS "DaysRemaining",
    CASE
        WHEN (wpi."ExpirationDate")::date - CURRENT_DATE < 10  THEN '< 10 days'
        WHEN (wpi."ExpirationDate")::date - CURRENT_DATE < 30  THEN '< 1 month'
        WHEN (wpi."ExpirationDate")::date - CURRENT_DATE < 90  THEN '< 3 months'
        WHEN (wpi."ExpirationDate")::date - CURRENT_DATE < 120 THEN '< 4 months'
        WHEN (wpi."ExpirationDate")::date - CURRENT_DATE < 150 THEN '< 5 months'
        WHEN (wpi."ExpirationDate")::date - CURRENT_DATE < 180 THEN '< 6 months'
        ELSE '≥ 6 months'
    END                                                                     AS "ValidityLabel",
    CASE
        WHEN (wpi."ExpirationDate")::date - CURRENT_DATE < 30  THEN 'st-expiring'
        WHEN (wpi."ExpirationDate")::date - CURRENT_DATE < 90  THEN 'st-pending'
        ELSE 'st-approved'
    END                                                                     AS "ValidityCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "WorkPermitItems" wpi
INNER JOIN "People" p
    ON p."ID" = wpi."PersonID"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
WHERE COALESCE(wpi."GCRecord", 0) = 0
  AND COALESCE(wpi."IsCancelled", FALSE) = FALSE
  AND wpi."PersonID" IS NOT NULL
  AND wpi."ExpirationDate" IS NOT NULL
  AND (wpi."ExpirationDate")::date >= CURRENT_DATE;
