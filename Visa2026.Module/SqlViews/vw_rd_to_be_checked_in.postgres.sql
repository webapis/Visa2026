-- Report Dashboard: To Be Checked In (Registration).
-- Valid visas with no ApplicationItem.CurrentVisa link to any App_Reg_* type.
-- Person must be in-country: latest TravelHistory is ExternalArrival.
-- Chart: days since that arrival TravelDate.
DROP VIEW IF EXISTS vw_rd_to_be_checked_in;
CREATE VIEW vw_rd_to_be_checked_in AS
WITH reg_linked AS (
    SELECT DISTINCT ai."CurrentVisaId" AS "VisaId"
    FROM "ApplicationItems" ai
    INNER JOIN "Applications" a
        ON a."ID" = ai."ApplicationID" AND COALESCE(a."GCRecord", 0) = 0
    INNER JOIN "ApplicationTypes" at
        ON at."ID" = a."ApplicationTypeID" AND COALESCE(at."GCRecord", 0) = 0
    WHERE COALESCE(ai."GCRecord", 0) = 0
      AND ai."CurrentVisaId" IS NOT NULL
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
),
latest_travel AS (
    SELECT DISTINCT ON (th."PersonID")
        th."PersonID",
        th."Discriminator",
        th."TravelDate" AS "EntryDate"
    FROM "TravelHistories" th
    WHERE COALESCE(th."GCRecord", 0) = 0
    ORDER BY th."PersonID", th."TravelDate" DESC NULLS LAST, th."ID" DESC
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
    lt."EntryDate" AS "EntryDate",
    (CURRENT_DATE - (lt."EntryDate")::date) AS "DaysSinceEntry",
    CASE
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 7  THEN '< 1 week'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 14 THEN '< 2 weeks'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 21 THEN '< 3 weeks'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 28 THEN '< 4 weeks'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 30 THEN '< 1 month'
        ELSE '≥ 1 month'
    END AS "EntryBucketLabel",
    CASE
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 14 THEN 'st-expiring'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 30 THEN 'st-pending'
        ELSE 'st-approved'
    END AS "EntryBucketCssClass",
    COALESCE(p."IsArchived", FALSE) AS "IsArchived"
FROM "Visas" v
INNER JOIN "Passports" pp
    ON pp."ID" = v."PassportID" AND COALESCE(pp."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = pp."PersonID" AND COALESCE(p."GCRecord", 0) = 0
INNER JOIN latest_travel lt
    ON lt."PersonID" = p."ID"
   AND lt."Discriminator" = 'ExternalArrival'
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
WHERE COALESCE(v."GCRecord", 0) = 0
  AND COALESCE(v."IsCancelled", FALSE) = FALSE
  AND (v."ExpirationDate")::date >= CURRENT_DATE
  AND NOT EXISTS (
        SELECT 1 FROM reg_linked rl WHERE rl."VisaId" = v."ID"
  );
