-- Report Dashboard: valid visas by VisaCategory only (not Visa State).
-- One row per valid visa (person may appear more than once).
DROP VIEW IF EXISTS vw_rd_visa_by_category;
CREATE VIEW vw_rd_visa_by_category AS
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
    COALESCE(NULLIF(BTRIM(vc."NameTm"), ''), NULLIF(BTRIM(vc."Name"), ''), 'Unknown') AS "CategoryLabel",
    COALESCE(NULLIF(BTRIM(vc."NameTm"), ''), NULLIF(BTRIM(vc."Name"), ''), 'Unknown') AS "StatusLabel",
    'st-cat-1'                                                          AS "StatusCssClass",
    COALESCE(p."IsArchived", FALSE)                                     AS "IsArchived"
FROM "Visas" v
INNER JOIN "Passports" pp
    ON pp."ID" = v."PassportID"
   AND COALESCE(pp."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = pp."PersonID"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "VisaCategories" vc
    ON vc."ID" = v."VisaCategoryID"
   AND COALESCE(vc."GCRecord", 0) = 0
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