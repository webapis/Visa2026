-- Report Dashboard: Passport (PostgreSQL).
-- One row per ApplicationItem that references a CurrentPassport.
-- C# loader keeps one last passport per person (latest IssueDate).
-- Date filter applies to Applications.ApplicationDate in the C# loader.
-- Soft-delete: COALESCE("GCRecord", 0) = 0. IsArchived is exposed for app-side toggle.
DROP VIEW IF EXISTS vw_rd_passport;
CREATE VIEW vw_rd_passport AS
SELECT
    ai."ID"                                                                 AS "ID",
    pp."ID"                                                                 AS "PassportOid",
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
    COALESCE(pp."PassportNumber", '')                                       AS "PassportNumber",
    pp."IssueDate"                                                          AS "IssueDate",
    pp."ExpirationDate"                                                     AS "ExpirationDate",
    a."ApplicationDate"                                                     AS "ApplicationDate",
    COALESCE(NULLIF(BTRIM(pt."NameTm"), ''), pt."Name", 'Unknown')          AS "TypeLabel",
    COALESCE(NULLIF(BTRIM(nat."NameTm"), ''), nat."Name", 'Unknown')         AS "CitizenshipLabel",
    CASE
      WHEN pp."ExpirationDate" IS NULL                                      THEN 'Pending'
      WHEN (pp."ExpirationDate")::date < CURRENT_DATE                        THEN 'Expired'
      WHEN (pp."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '30 days')::date
                                                                             THEN 'Expiring (<30 days)'
      WHEN (pp."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '90 days')::date
                                                                             THEN 'Valid (31-90 days)'
      ELSE                                                                   'Valid (>90 days)'
    END                                                                     AS "ValidityLabel",
    CASE
      WHEN pp."ExpirationDate" IS NULL                                      THEN 'st-pending'
      WHEN (pp."ExpirationDate")::date < CURRENT_DATE                        THEN 'st-expiring'
      WHEN (pp."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '30 days')::date
                                                                             THEN 'st-expiring'
      WHEN (pp."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '90 days')::date
                                                                             THEN 'st-pending'
      ELSE                                                                   'st-approved'
    END                                                                     AS "ValidityCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "ApplicationItems" ai
INNER JOIN "Applications" a
    ON a."ID" = ai."ApplicationID"
   AND COALESCE(a."GCRecord", 0) = 0
INNER JOIN "Passports" pp
    ON pp."ID" = ai."CurrentPassportID"
   AND COALESCE(pp."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = ai."PersonID"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = COALESCE(a."ProjectContractID", p."ProjectContractID")
   AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
LEFT JOIN "PassportTypes" pt
    ON pt."ID" = pp."PassportTypeID" AND COALESCE(pt."GCRecord", 0) = 0
LEFT JOIN "Countries" nat
    ON nat."ID" = p."NationalityID" AND COALESCE(nat."GCRecord", 0) = 0
WHERE COALESCE(ai."GCRecord", 0) = 0
  AND ai."CurrentPassportID" IS NOT NULL;