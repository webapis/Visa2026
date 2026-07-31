-- Report Dashboard: Passport category.
-- One row per ApplicationItem that references a CurrentPassport.
-- C# loader keeps one last passport per person (latest IssueDate).
-- Date filter applies to Applications.ApplicationDate in the C# loader.
CREATE OR ALTER VIEW [dbo].[vw_rd_passport] AS
SELECT
    ai.ID                                                               AS ID,
    pp.ID                                                               AS PassportOid,
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
    COALESCE(pp.PassportNumber, N'')                                    AS PassportNumber,
    pp.IssueDate                                                        AS IssueDate,
    pp.ExpirationDate                                                   AS ExpirationDate,
    a.ApplicationDate                                                   AS ApplicationDate,
    COALESCE(NULLIF(LTRIM(RTRIM(pt.NameTm)), N''), pt.Name, N'Unknown') AS TypeLabel,
    COALESCE(NULLIF(LTRIM(RTRIM(nat.NameTm)), N''), nat.Name, N'Unknown') AS CitizenshipLabel,
    CASE
      WHEN pp.ExpirationDate IS NULL                                          THEN N'Pending'
      WHEN CAST(pp.ExpirationDate AS date) <  CAST(GETDATE() AS date)         THEN N'Expired'
      WHEN CAST(pp.ExpirationDate AS date) <= DATEADD(day, 30, CAST(GETDATE() AS date))
                                                                               THEN N'Expiring (<30 days)'
      WHEN CAST(pp.ExpirationDate AS date) <= DATEADD(day, 90, CAST(GETDATE() AS date))
                                                                               THEN N'Valid (31-90 days)'
      ELSE                                                                         N'Valid (>90 days)'
    END                                                                 AS ValidityLabel,
    CASE
      WHEN pp.ExpirationDate IS NULL                                          THEN N'st-pending'
      WHEN CAST(pp.ExpirationDate AS date) <  CAST(GETDATE() AS date)         THEN N'st-expiring'
      WHEN CAST(pp.ExpirationDate AS date) <= DATEADD(day, 30, CAST(GETDATE() AS date))
                                                                               THEN N'st-expiring'
      WHEN CAST(pp.ExpirationDate AS date) <= DATEADD(day, 90, CAST(GETDATE() AS date))
                                                                               THEN N'st-pending'
      ELSE                                                                         N'st-approved'
    END                                                                 AS ValidityCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                    AS IsArchived
FROM ApplicationItems ai
INNER JOIN Applications a
    ON a.ID = ai.ApplicationID
   AND ISNULL(a.GCRecord, 0) = 0
INNER JOIN Passports pp
    ON pp.ID = ai.CurrentPassportID
   AND ISNULL(pp.GCRecord, 0) = 0
INNER JOIN People p
    ON p.ID = ai.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = COALESCE(a.ProjectContractID, p.ProjectContractID)
   AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
LEFT JOIN PassportTypes pt
    ON pt.ID = pp.PassportTypeID AND ISNULL(pt.GCRecord, 0) = 0
LEFT JOIN Countries nat
    ON nat.ID = p.NationalityID AND ISNULL(nat.GCRecord, 0) = 0
WHERE ISNULL(ai.GCRecord, 0) = 0
  AND ai.CurrentPassportID IS NOT NULL;