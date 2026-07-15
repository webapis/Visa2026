-- Report Dashboard: Passport category.
-- One row per Person: latest non-cancelled passport by IssueDate (then ID).
CREATE OR ALTER VIEW [dbo].[vw_rd_passport] AS
SELECT
    pp.ID                                                               AS ID,
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
    pp.ExpirationDate                                                   AS ExpirationDate,
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
FROM (
    SELECT
        pp0.*,
        ROW_NUMBER() OVER (
            PARTITION BY pp0.PersonID
            ORDER BY
                CASE WHEN pp0.IssueDate IS NULL THEN 1 ELSE 0 END,
                pp0.IssueDate DESC,
                pp0.ID DESC
        ) AS rn
    FROM Passports pp0
    WHERE ISNULL(pp0.GCRecord, 0) = 0
      AND ISNULL(pp0.IsCancelled, 0) = 0
) pp
INNER JOIN People p
    ON p.ID = pp.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
LEFT JOIN PassportTypes pt
    ON pt.ID = pp.PassportTypeID AND ISNULL(pt.GCRecord, 0) = 0
LEFT JOIN Countries nat
    ON nat.ID = p.NationalityID AND ISNULL(nat.GCRecord, 0) = 0
WHERE pp.rn = 1;