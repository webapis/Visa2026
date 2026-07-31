-- Report Dashboard: Education by-country (education country only) for PostgreSQL.
-- Dedicated view for Education "By Country" sub-report.
DROP VIEW IF EXISTS vw_rd_education_by_country;
CREATE VIEW vw_rd_education_by_country AS
SELECT
    e."ID"                                                                  AS "ID",
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
    COALESCE(
        NULLIF(BTRIM(ei."NameTm"), ''),
        NULLIF(BTRIM(ei."Name"), ''),
        ''
    )                                                                       AS "InstitutionName",
    COALESCE(NULLIF(BTRIM(e."GraduationYear"), ''), '')                     AS "GraduationYear",
    COALESCE(
        NULLIF(BTRIM(c."NameTm"), ''),
        NULLIF(BTRIM(c."Name"), ''),
        'Unknown'
    )                                                                       AS "CountryLabel",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "Educations" e
INNER JOIN "People" p
    ON p."ID" = e."PersonID"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sponsor
    ON sponsor."ID" = p."SponsoringEmployeeID" AND COALESCE(sponsor."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sponsor."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
LEFT JOIN "EducationInstitutions" ei
    ON ei."ID" = e."EducationInstitutionID" AND COALESCE(ei."GCRecord", 0) = 0
LEFT JOIN "Countries" c
    ON c."ID" = e."EducationCountryID" AND COALESCE(c."GCRecord", 0) = 0
WHERE COALESCE(e."GCRecord", 0) = 0;
