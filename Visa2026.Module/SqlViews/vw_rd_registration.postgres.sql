-- Report Dashboard: Registration category (PostgreSQL).
-- One row per not-expired visa: latest registration Application via ApplicationItem.CurrentVisa.
DROP VIEW IF EXISTS vw_rd_registration;
CREATE VIEW vw_rd_registration AS
WITH ranked AS (
    SELECT
        ai."ID" AS "ID",
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
        COALESCE(
            NULLIF(BTRIM(a."FullApplicationNumber"), ''),
            NULLIF(BTRIM(a."ApplicationNumber"), ''),
            ''
        ) AS "ApplicationNumber",
        a."ApplicationDate" AS "ApplicationDate",
        at."Name" AS "ApplicationTypeName",
        COALESCE(
            NULLIF(BTRIM(at."NameTm"), ''),
            NULLIF(BTRIM(at."Name"), ''),
            'Unknown'
        ) AS "ApplicationTypeLabel",
        COALESCE(
            NULLIF(BTRIM(ast."NameTm"), ''),
            NULLIF(BTRIM(ast."Name"), ''),
            'OFISDE'
        ) AS "ProgressStateLabel",
        CASE
            WHEN ast."Code" IN ('PROCESS_ISSUED') THEN 'st-approved'
            WHEN ast."Code" IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED') THEN 'st-expiring'
            WHEN ast."Code" IS NULL THEN 'st-pending'
            ELSE 'st-pending'
        END AS "ProgressStateCssClass",
        COALESCE(ast."Code", 'AT_OFFICE') AS "ProgressStateCode",
        ((v."ExpirationDate")::date - CURRENT_DATE) AS "DaysRemaining",
        CASE
            WHEN (v."ExpirationDate")::date - CURRENT_DATE < 7   THEN '< 7 days'
            WHEN (v."ExpirationDate")::date - CURRENT_DATE < 14  THEN '< 14 days'
            WHEN (v."ExpirationDate")::date - CURRENT_DATE < 30  THEN '< 1 month'
            WHEN (v."ExpirationDate")::date - CURRENT_DATE < 90  THEN '< 3 months'
            WHEN (v."ExpirationDate")::date - CURRENT_DATE < 180 THEN '< 6 months'
            ELSE 'â‰¥ 6 months'
        END AS "ExpiryBucketLabel",
        CASE
            WHEN (v."ExpirationDate")::date - CURRENT_DATE < 14  THEN 'st-expiring'
            WHEN (v."ExpirationDate")::date - CURRENT_DATE < 90  THEN 'st-pending'
            ELSE 'st-approved'
        END AS "ExpiryBucketCssClass",
        COALESCE(p."IsArchived", FALSE) AS "IsArchived",
        COALESCE(
            NULLIF(BTRIM(city."NameTm"), ''),
            NULLIF(BTRIM(city."Name"), ''),
            'Unknown city'
        ) AS "CityLabel",
        ROW_NUMBER() OVER (
            PARTITION BY v."ID"
            ORDER BY a."ApplicationDate" DESC NULLS LAST, a."ID" DESC, ai."ID" DESC
        ) AS rn
    FROM "Visas" v
    INNER JOIN "Passports" pp
        ON pp."ID" = v."PassportID" AND COALESCE(pp."GCRecord", 0) = 0
    INNER JOIN "People" p
        ON p."ID" = pp."PersonID" AND COALESCE(p."GCRecord", 0) = 0
    INNER JOIN "ApplicationItems" ai
        ON ai."CurrentVisaId" = v."ID" AND COALESCE(ai."GCRecord", 0) = 0
    INNER JOIN "Applications" a
        ON a."ID" = ai."ApplicationID" AND COALESCE(a."GCRecord", 0) = 0
    INNER JOIN "ApplicationTypes" at
        ON at."ID" = a."ApplicationTypeID" AND COALESCE(at."GCRecord", 0) = 0
    LEFT JOIN "AddressesOfResidence" addr
        ON addr."ID" = ai."CurrentAddressOfResidenceID" AND COALESCE(addr."GCRecord", 0) = 0
    LEFT JOIN "Cities" city
        ON city."ID" = addr."CityID" AND COALESCE(city."GCRecord", 0) = 0
    LEFT JOIN "ProjectContracts" pc
        ON pc."ID" = COALESCE(a."ProjectContractID", p."ProjectContractID")
       AND COALESCE(pc."GCRecord", 0) = 0
    LEFT JOIN "People" sp
        ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
    LEFT JOIN "ProjectContracts" spc
        ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
    LEFT JOIN LATERAL (
        SELECT ap."StateID"
        FROM "ApplicationProgresses" ap
        WHERE ap."ApplicationID" = a."ID"
          AND COALESCE(ap."GCRecord", 0) = 0
        ORDER BY ap."Date" DESC NULLS LAST, ap."ID" DESC
        LIMIT 1
    ) latest_ap ON TRUE
    LEFT JOIN "ApplicationStates" ast
        ON ast."ID" = latest_ap."StateID" AND COALESCE(ast."GCRecord", 0) = 0
    WHERE COALESCE(v."GCRecord", 0) = 0
      AND COALESCE(v."IsCancelled", FALSE) = FALSE
      AND (v."ExpirationDate")::date >= CURRENT_DATE
      AND at."Name" IN (
            'App_Reg_Check_In',
            'App_Reg_Check_In_Internal',
            'App_Reg_Check_Out',
            'App_Reg_Check_Out_Internal',
            'App_Reg_ext',
            'App_Reg_Info_Change_Address',
            'App_Reg_Info_Change_Passport',
            'App_Reg_Info_Change_Visa'
        )
)
SELECT
    "ID",
    "PersonOid",
    "PersonName",
    "ProjectName",
    "ProjectNameRaw",
    "ProjectNameTm",
    "PersonRoleCode",
    "VisaNumber",
    "VisaExpirationDate",
    "ApplicationNumber",
    "ApplicationDate",
    "ApplicationTypeName",
    "ApplicationTypeLabel",
    "ProgressStateLabel",
    "ProgressStateCssClass",
    "ProgressStateCode",
    "DaysRemaining",
    "ExpiryBucketLabel",
    "ExpiryBucketCssClass",
    "IsArchived",
    "CityLabel"
FROM ranked
WHERE rn = 1;
