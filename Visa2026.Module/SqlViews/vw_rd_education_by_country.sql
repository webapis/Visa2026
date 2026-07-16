-- Report Dashboard: Education by-country (education country only).
-- Dedicated view for Education "By Country" sub-report to keep query path lean.
CREATE OR ALTER VIEW [dbo].[vw_rd_education_by_country] AS
SELECT
    e.ID                                                                AS ID,
    p.ID                                                                AS PersonOid,
    CONCAT_WS(N' ',
        NULLIF(LTRIM(RTRIM(p.FirstName)), N''),
        NULLIF(LTRIM(RTRIM(p.MiddleName)), N''),
        NULLIF(LTRIM(RTRIM(p.LastName)), N'')
    )                                                                   AS PersonName,
    COALESCE(
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(spc.NameTm)), N''),
        N''
    )                                                                   AS ProjectName,
    COALESCE(pc.NameTm, spc.NameTm, N'')                                AS ProjectNameRaw,
    COALESCE(pc.NameTm, spc.NameTm, N'')                                AS ProjectNameTm,
    p.PersonRole                                                        AS PersonRoleCode,
    COALESCE(
        NULLIF(LTRIM(RTRIM(ei.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(ei.Name)), N''),
        N''
    )                                                                   AS InstitutionName,
    COALESCE(NULLIF(LTRIM(RTRIM(e.GraduationYear)), N''), N'')          AS GraduationYear,
    COALESCE(
        NULLIF(LTRIM(RTRIM(c.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(c.Name)), N''),
        N'Unknown'
    )                                                                   AS CountryLabel,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM Educations e
INNER JOIN People p
    ON p.ID = e.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sponsor
    ON sponsor.ID = p.SponsoringEmployeeID AND ISNULL(sponsor.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sponsor.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
LEFT JOIN EducationInstitutions ei
    ON ei.ID = e.EducationInstitutionID AND ISNULL(ei.GCRecord, 0) = 0
LEFT JOIN Countries c
    ON c.ID = e.EducationCountryID AND ISNULL(c.GCRecord, 0) = 0
WHERE ISNULL(e.GCRecord, 0) = 0;
