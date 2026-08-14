-- Report Dashboard: Extension Required (P)/(V) (PostgreSQL).
DROP VIEW IF EXISTS vw_rd_visa_extension_required;
CREATE VIEW vw_rd_visa_extension_required AS
WITH valid_visas AS (
    SELECT
        v."ID" AS "ID",
        p."ID" AS "PersonOid",
        v."PassportID" AS "PassportID",
        COALESCE(NULLIF(BTRIM(pp."PassportNumber"), ''), '') AS "PassportNumber",
        CONCAT_WS(' ',
            NULLIF(BTRIM(p."FirstName"), ''),
            NULLIF(BTRIM(p."MiddleName"), ''),
            NULLIF(BTRIM(p."LastName"), '')
        ) AS "PersonName",
        COALESCE(NULLIF(BTRIM(pc."NameTm"), ''), NULLIF(BTRIM(spc."NameTm"), ''), '') AS "ProjectName",
        COALESCE(pc."NameTm", spc."NameTm", '') AS "ProjectNameRaw",
        COALESCE(pc."NameTm", spc."NameTm", '') AS "ProjectNameTm",
        p."PersonRole" AS "PersonRoleCode",
        COALESCE(NULLIF(BTRIM(v."VisaNumber"), ''), '') AS "VisaNumber",
        CASE WHEN (v."ExpirationDate")::date > DATE '1900-01-01' THEN v."ExpirationDate" ELSE NULL END AS "ExpirationDate",
        CASE
            WHEN ((v."ExpirationDate")::date - (v."StartDate")::date) < 0 THEN 0
            ELSE ((v."ExpirationDate")::date - (v."StartDate")::date)
        END AS "PeriodDays",
        CASE
            WHEN ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 30)
                 <= ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 90)
             AND ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 30)
                 <= ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 180)
             AND ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 30)
                 <= ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 365) THEN '1 month'
            WHEN ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 90)
                 <= ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 180)
             AND ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 90)
                 <= ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 365) THEN '3 months'
            WHEN ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 180)
                 <= ABS(((v."ExpirationDate")::date - (v."StartDate")::date) - 365) THEN '6 months'
            ELSE '1 year'
        END AS "PeriodLabel",
        COALESCE(NULLIF(BTRIM(vc."NameTm"), ''), NULLIF(BTRIM(vc."Name"), ''), '(No category)') AS "CategoryLabel",
        COALESCE(NULLIF(BTRIM(vt."NameTm"), ''), NULLIF(BTRIM(vt."Name"), ''), '(No type)') AS "TypeLabel",
        COALESCE(p."IsArchived", FALSE) AS "IsArchived",
        ROW_NUMBER() OVER (
            PARTITION BY p."ID"
            ORDER BY v."ExpirationDate" DESC, v."ID" DESC
        ) AS rn
    FROM "Visas" v
    INNER JOIN "Passports" pp ON pp."ID" = v."PassportID" AND COALESCE(pp."GCRecord", 0) = 0
    INNER JOIN "People" p ON p."ID" = pp."PersonID" AND COALESCE(p."GCRecord", 0) = 0
    LEFT JOIN "VisaCategories" vc ON vc."ID" = v."VisaCategoryID" AND COALESCE(vc."GCRecord", 0) = 0
    LEFT JOIN "VisaTypes" vt ON vt."ID" = v."VisaTypeID" AND COALESCE(vt."GCRecord", 0) = 0
    LEFT JOIN "ProjectContracts" pc ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
    LEFT JOIN "People" sp ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
    LEFT JOIN "ProjectContracts" spc ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
    WHERE COALESCE(v."GCRecord", 0) = 0
      AND COALESCE(v."IsCancelled", FALSE) = FALSE
      AND v."ExpirationDate" IS NOT NULL
      AND (v."ExpirationDate")::date >= CURRENT_DATE
      AND v."StartDate" IS NOT NULL
      AND (v."StartDate")::date > DATE '1900-01-01'
),
visa_ext_roster AS (
    SELECT
        md5(concat(ap."ApplicationProfileInstanceId"::text, ap."PersonId"::text))::uuid AS "LineId",
        a."ID" AS "ApplicationProfileInstanceID",
        ap."PersonId" AS "PersonID",
        rl_visa."LinkedObjectId" AS "ExpiringVisaID"
    FROM "ApplicationProfileInstancePeople" ap
    INNER JOIN "ApplicationProfileInstances" a
        ON a."ID" = ap."ApplicationProfileInstanceId" AND COALESCE(a."GCRecord", 0) = 0
    INNER JOIN "ApplicationProfiles" apf
        ON apf."ID" = a."ApplicationProfileID" AND COALESCE(apf."GCRecord", 0) = 0
    INNER JOIN "ApplicationProfileInstancePersonResolvedLinks" rl_visa
        ON rl_visa."ApplicationProfileInstanceId" = ap."ApplicationProfileInstanceId" AND rl_visa."PersonId" = ap."PersonId"
       AND rl_visa."LinkKind" = 1
       AND rl_visa."LinkedObjectId" IS NOT NULL
       AND COALESCE(rl_visa."GCRecord", 0) = 0
    WHERE COALESCE(apf."ProduceVisa", FALSE) = TRUE
      AND COALESCE(apf."RequirePersonVisa", FALSE) = TRUE
      AND COALESCE(apf."ProduceInvitation", FALSE) = FALSE
      AND COALESCE(apf."ActionFamily", 0) = 0
),
unfinished_extension_people AS (
    SELECT DISTINCT roster."PersonID"
    FROM visa_ext_roster roster
    INNER JOIN "ApplicationProfileInstances" a
        ON a."ID" = roster."ApplicationProfileInstanceID" AND COALESCE(a."GCRecord", 0) = 0
    WHERE roster."ExpiringVisaID" IS NOT NULL
      AND roster."PersonID" IS NOT NULL
      AND (
          a."LatestPrimaryStateCode" IS NULL
          OR BTRIM(a."LatestPrimaryStateCode") = ''
          OR (
               a."LatestPrimaryStateCode" NOT IN ('PROCESS_ISSUED', 'PROCESS_CANCELLED', 'PROCESS_REJECTED')
               AND RIGHT(BTRIM(a."LatestPrimaryStateCode"), 16) <> '_REVIEW_REJECTED'
             )
      )
)
SELECT
    v."ID",
    v."PersonOid",
    v."PassportID",
    v."PassportNumber",
    v."PersonName",
    v."ProjectName",
    v."ProjectNameRaw",
    v."ProjectNameTm",
    v."PersonRoleCode",
    v."VisaNumber",
    v."ExpirationDate",
    v."PeriodDays",
    v."PeriodLabel",
    v."CategoryLabel",
    v."TypeLabel",
    GREATEST(0, (v."ExpirationDate")::date - CURRENT_DATE) AS "DaysRemaining",
    COALESCE(NULLIF(BTRIM(v."ProjectName"), ''), '(No project)') AS "StatusLabel",
    'st-cat-1' AS "StatusCssClass",
    v."IsArchived"
FROM valid_visas v
WHERE v.rn = 1
  AND NOT EXISTS (
        SELECT 1
        FROM unfinished_extension_people u
        WHERE u."PersonID" = v."PersonOid"
    );