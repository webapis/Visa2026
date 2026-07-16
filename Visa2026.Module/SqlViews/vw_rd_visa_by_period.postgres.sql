-- Report Dashboard: valid visas by nearest granted period (StartDate → ExpirationDate).
-- Chart labels: 1 month / 3 months / 6 months / 1 year. Valid visas only. No start/end columns in UI.
DROP VIEW IF EXISTS vw_rd_visa_by_period;
CREATE VIEW vw_rd_visa_by_period AS
SELECT
    x."ID",
    x."PersonOid",
    x."PersonName",
    x."ProjectName",
    x."ProjectNameRaw",
    x."ProjectNameTm",
    x."PersonRoleCode",
    x."VisaNumber",
    x."ExpirationDate",
    x."PeriodDays",
    x."PeriodLabel",
    x."PeriodLabel"                                                     AS "StatusLabel",
    CASE x."PeriodLabel"
        WHEN '1 month'   THEN 'st-cat-1'
        WHEN '3 months'  THEN 'st-cat-2'
        WHEN '6 months'  THEN 'st-cat-3'
        ELSE                  'st-cat-4'
    END                                                                 AS "StatusCssClass",
    x."IsArchived"
FROM (
    SELECT
        v."ID"                                                          AS "ID",
        p."ID"                                                          AS "PersonOid",
        CONCAT_WS(' ',
            NULLIF(BTRIM(p."FirstName"), ''),
            NULLIF(BTRIM(p."MiddleName"), ''),
            NULLIF(BTRIM(p."LastName"), '')
        )                                                               AS "PersonName",
        COALESCE(
            NULLIF(BTRIM(pc."NameTm"), ''),
            NULLIF(BTRIM(spc."NameTm"), ''),
            ''
        )                                                               AS "ProjectName",
        COALESCE(pc."NameTm", spc."NameTm", '')                         AS "ProjectNameRaw",
        COALESCE(pc."NameTm", spc."NameTm", '')                         AS "ProjectNameTm",
        p."PersonRole"                                                  AS "PersonRoleCode",
        COALESCE(NULLIF(BTRIM(v."VisaNumber"), ''), '')                 AS "VisaNumber",
        CASE WHEN (v."ExpirationDate")::date > DATE '1900-01-01' THEN v."ExpirationDate" ELSE NULL END AS "ExpirationDate",
        GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) AS "PeriodDays",
        CASE
            WHEN ABS(GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) - 30)
               <= LEAST(
                    ABS(GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) - 90),
                    ABS(GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) - 180),
                    ABS(GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) - 365)
                  )
                THEN '1 month'
            WHEN ABS(GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) - 90)
               <= LEAST(
                    ABS(GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) - 180),
                    ABS(GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) - 365)
                  )
                THEN '3 months'
            WHEN ABS(GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) - 180)
               <= ABS(GREATEST(0, (v."ExpirationDate")::date - (v."StartDate")::date) - 365)
                THEN '6 months'
            ELSE '1 year'
        END                                                             AS "PeriodLabel",
        COALESCE(p."IsArchived", FALSE)                                 AS "IsArchived"
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
      AND (v."ExpirationDate")::date >= CURRENT_DATE
      AND v."StartDate" IS NOT NULL
      AND (v."StartDate")::date > DATE '1900-01-01'
) x;