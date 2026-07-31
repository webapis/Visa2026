-- Report Dashboard: Incomplete persons (one row per Person with IsDataIncomplete = 1).
-- Chart grouping by missing-area flags is done in ReportDashboardQueryService (person counted per flag).
-- ProjectContracts uses NameTm (Name may be absent depending on schema).
CREATE OR ALTER VIEW [dbo].[vw_rd_incomplete_persons_by_missing_area] AS
SELECT
    p.ID                                                                AS ID,
    p.ID                                                                AS PersonOid,
    CONCAT_WS(N' ',
        NULLIF(LTRIM(RTRIM(p.FirstName)), N''),
        NULLIF(LTRIM(RTRIM(p.MiddleName)), N''),
        NULLIF(LTRIM(RTRIM(p.LastName)), N'')
    )                                                                   AS PersonName,
    COALESCE(NULLIF(LTRIM(RTRIM(pc.NameTm)), N''), N'')                 AS ProjectName,
    COALESCE(pc.NameTm, N'')                                            AS ProjectNameRaw,
    COALESCE(pc.NameTm, N'')                                            AS ProjectNameTm,
    p.PersonRole                                                        AS PersonRoleCode,
    CASE p.PersonRole
        WHEN 0 THEN N'Employee'
        WHEN 1 THEN N'Family Member'
        WHEN 2 THEN N'Temporary Visitor'
        ELSE N'Unknown'
    END                                                                 AS PersonTypeLabel,
    CONCAT_WS(N', ',
        CASE WHEN ISNULL(p.IncompleteMissingPersonalData, 0) = 1 THEN N'Personal data' END,
        CASE WHEN ISNULL(p.IncompleteMissingPassport, 0) = 1 THEN N'Passport' END,
        CASE WHEN ISNULL(p.IncompleteMissingCv, 0) = 1 THEN N'CV' END,
        CASE WHEN ISNULL(p.IncompleteMissingPhoto, 0) = 1 THEN N'Photo' END,
        CASE WHEN ISNULL(p.IncompleteMissingEducation, 0) = 1 THEN N'Education' END,
        CASE WHEN ISNULL(p.IncompleteMissingMedical, 0) = 1 THEN N'Medical' END,
        CASE WHEN ISNULL(p.IncompleteMissingAddress, 0) = 1 THEN N'Address' END,
        CASE WHEN ISNULL(p.IncompleteMissingFamilyDocs, 0) = 1 THEN N'Family docs' END,
        CASE WHEN ISNULL(p.IncompleteMissingOther, 0) = 1 THEN N'Other' END
    )                                                                   AS MissingAreasLabel,
    COALESCE(p.IncompleteNotes, N'')                                    AS IncompleteNotes,
    p.IncompleteMarkedOn                                                AS IncompleteMarkedOn,
    COALESCE(p.IncompleteMarkedBy, N'')                                 AS IncompleteMarkedBy,
    CASE
        WHEN p.IncompleteMarkedOn IS NULL THEN COALESCE(p.IncompleteMarkedBy, N'')
        WHEN NULLIF(LTRIM(RTRIM(p.IncompleteMarkedBy)), N'') IS NULL
            THEN CONVERT(nvarchar(10), p.IncompleteMarkedOn, 104)
        ELSE CONVERT(nvarchar(10), p.IncompleteMarkedOn, 104) + N' · ' + LTRIM(RTRIM(p.IncompleteMarkedBy))
    END                                                                 AS MarkedLabel,
    CAST(ISNULL(p.IncompleteMissingPersonalData, 0) AS bit)             AS MissingPersonalData,
    CAST(ISNULL(p.IncompleteMissingPassport, 0) AS bit)                 AS MissingPassport,
    CAST(ISNULL(p.IncompleteMissingCv, 0) AS bit)                       AS MissingCv,
    CAST(ISNULL(p.IncompleteMissingPhoto, 0) AS bit)                    AS MissingPhoto,
    CAST(ISNULL(p.IncompleteMissingEducation, 0) AS bit)                AS MissingEducation,
    CAST(ISNULL(p.IncompleteMissingMedical, 0) AS bit)                  AS MissingMedical,
    CAST(ISNULL(p.IncompleteMissingAddress, 0) AS bit)                  AS MissingAddress,
    CAST(ISNULL(p.IncompleteMissingFamilyDocs, 0) AS bit)               AS MissingFamilyDocs,
    CAST(ISNULL(p.IncompleteMissingOther, 0) AS bit)                    AS MissingOther,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM People p
LEFT JOIN ProjectContracts pc ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
WHERE ISNULL(p.GCRecord, 0) = 0
  AND ISNULL(p.IsDataIncomplete, 0) = 1;