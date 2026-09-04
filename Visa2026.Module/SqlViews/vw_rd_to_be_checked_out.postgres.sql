-- Report Dashboard: To Be Checked Out (Registration).
-- Valid visas expiring within 1 week (DaysRemaining < 7), no Check-Out / Check-Out Internal on CurrentVisa.
-- Chart: < 1 day · < 2 days · … · < 7 days.
DROP VIEW IF EXISTS vw_rd_to_be_checked_out;
CREATE VIEW vw_rd_to_be_checked_out AS
WITH checkout_linked AS (
    SELECT DISTINCT rl."LinkedObjectId" AS "VisaId"
    FROM "ApplicationProfileInstancePersonResolvedLinks" rl
    INNER JOIN "ApplicationProfileInstancePeople" ap
        ON ap."ApplicationProfileInstanceId" = rl."ApplicationProfileInstanceId" AND ap."PersonId" = rl."PersonId"
    INNER JOIN "ApplicationProfileInstances" a
        ON a."ID" = ap."ApplicationProfileInstanceId" AND COALESCE(a."GCRecord", 0) = 0
    INNER JOIN "ApplicationProfiles" apf
        ON apf."ID" = a."ApplicationProfileID" AND COALESCE(apf."GCRecord", 0) = 0
    WHERE COALESCE(rl."GCRecord", 0) = 0
      AND rl."LinkKind" = 1
      AND rl."LinkedObjectId" IS NOT NULL
      AND apf."Code" = 'check_out'
)
SELECT
    v."ID" AS "ID",
    p."ID" AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    ) AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        ''
    ) AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '') AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '') AS "ProjectNameTm",
    p."PersonRole" AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(v."VisaNumber"), ''), '') AS "VisaNumber",
    v."ExpirationDate" AS "VisaExpirationDate",
    ((v."ExpirationDate")::date - CURRENT_DATE) AS "DaysRemaining",
    CASE
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 0 THEN 'Expired'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 1 THEN '< 1 day'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 2 THEN '< 2 days'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 3 THEN '< 3 days'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 4 THEN '< 4 days'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 5 THEN '< 5 days'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 6 THEN '< 6 days'
        ELSE '< 7 days'
    END AS "ExpiryBucketLabel",
    CASE
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 0 THEN 'st-expiring'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 3 THEN 'st-expiring'
        WHEN (v."ExpirationDate")::date - CURRENT_DATE < 5 THEN 'st-pending'
        ELSE 'st-approved'
    END AS "ExpiryBucketCssClass",
    COALESCE(p."IsArchived", FALSE) AS "IsArchived"
FROM "Visas" v
INNER JOIN "Passports" pp
    ON pp."ID" = v."PassportID" AND COALESCE(pp."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = pp."PersonID" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
WHERE COALESCE(v."GCRecord", 0) = 0
  AND (v."ExpirationDate")::date - CURRENT_DATE < 7
  AND NOT EXISTS (
        SELECT 1 FROM checkout_linked cl WHERE cl."VisaId" = v."ID"
  );
