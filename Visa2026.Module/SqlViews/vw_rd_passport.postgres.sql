DROP VIEW IF EXISTS vw_rd_passport;
CREATE VIEW vw_rd_passport AS
SELECT
    md5(concat(ap."ApplicationProfileInstanceId"::text, ap."PersonId"::text))::uuid AS "ID",
    pp."ID" AS "PassportOid",
    p."ID" AS "PersonOid",
    CONCAT_WS(' ', NULLIF(BTRIM(p."FirstName"), ''), NULLIF(BTRIM(p."MiddleName"), ''), NULLIF(BTRIM(p."LastName"), '')) AS "PersonName",
    COALESCE(NULLIF(BTRIM(pc."NameTm"), ''), NULLIF(BTRIM(spc."NameTm"), ''), '') AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '') AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '') AS "ProjectNameTm",
    p."PersonRole" AS "PersonRoleCode",
    COALESCE(pp."PassportNumber", '') AS "PassportNumber",
    pp."IssueDate" AS "IssueDate",
    pp."ExpirationDate" AS "ExpirationDate",
    a."ApplicationDate" AS "ApplicationDate",
    COALESCE(NULLIF(BTRIM(pt."NameTm"), ''), pt."Name", 'Unknown') AS "TypeLabel",
    COALESCE(NULLIF(BTRIM(nat."NameTm"), ''), nat."Name", 'Unknown') AS "CitizenshipLabel",
    CASE
      WHEN pp."ExpirationDate" IS NULL THEN 'Pending'
      WHEN (pp."ExpirationDate")::date < CURRENT_DATE THEN 'Expired'
      WHEN (pp."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '30 days')::date THEN 'Expiring (<30 days)'
      WHEN (pp."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '90 days')::date THEN 'Valid (31-90 days)'
      ELSE 'Valid (>90 days)'
    END AS "ValidityLabel",
    CASE
      WHEN pp."ExpirationDate" IS NULL THEN 'st-pending'
      WHEN (pp."ExpirationDate")::date < CURRENT_DATE THEN 'st-expiring'
      WHEN (pp."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '30 days')::date THEN 'st-expiring'
      WHEN (pp."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '90 days')::date THEN 'st-pending'
      ELSE 'st-approved'
    END AS "ValidityCssClass",
    COALESCE(p."IsArchived", FALSE) AS "IsArchived"
FROM "ApplicationProfileInstancePeople" ap
INNER JOIN "ApplicationProfileInstances" a ON a."ID" = ap."ApplicationProfileInstanceId" AND COALESCE(a."GCRecord", 0) = 0
INNER JOIN "ApplicationProfileInstancePersonResolvedLinks" rl_pass
    ON rl_pass."ApplicationProfileInstanceId" = ap."ApplicationProfileInstanceId" AND rl_pass."PersonId" = ap."PersonId" AND rl_pass."LinkKind" = 0 AND COALESCE(rl_pass."GCRecord", 0) = 0
INNER JOIN "Passports" pp ON pp."ID" = rl_pass."LinkedObjectId" AND COALESCE(pp."GCRecord", 0) = 0
INNER JOIN "People" p ON p."ID" = ap."PersonId" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc ON pc."ID" = COALESCE(a."ProjectContractID", p."ProjectContractID") AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
LEFT JOIN "PassportTypes" pt ON pt."ID" = pp."PassportTypeID" AND COALESCE(pt."GCRecord", 0) = 0
LEFT JOIN "Countries" nat ON nat."ID" = p."NationalityID" AND COALESCE(nat."GCRecord", 0) = 0
WHERE rl_pass."LinkedObjectId" IS NOT NULL;