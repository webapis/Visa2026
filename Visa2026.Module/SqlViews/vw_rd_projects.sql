-- Report Dashboard: project chips (people per ProjectContract, by PersonRole).
-- Effective project = Person.ProjectContract, else SponsoringEmployee.ProjectContract (family).
-- Soft-delete / archived people excluded. Count 0 projects omitted by GROUP BY.
-- ProjectContracts use NameTm only (Name column dropped).
CREATE OR ALTER VIEW [dbo].[vw_rd_projects] AS
SELECT
    pc.ID                                                               AS ProjectOid,
    p.PersonRole                                                        AS PersonRoleCode,
    COALESCE(NULLIF(LTRIM(RTRIM(pc.NameTm)), N''), N'')                 AS ProjectNameTm,
    COALESCE(NULLIF(LTRIM(RTRIM(pc.NameTm)), N''), N'')                 AS ProjectNameRaw,
    COUNT_BIG(*)                                                        AS PersonCount
FROM People p
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID
   AND ISNULL(sp.GCRecord, 0) = 0
INNER JOIN ProjectContracts pc
    ON pc.ID = COALESCE(p.ProjectContractID, sp.ProjectContractID)
   AND ISNULL(pc.GCRecord, 0) = 0
WHERE ISNULL(p.GCRecord, 0) = 0
  AND ISNULL(p.IsArchived, 0) = 0
  AND COALESCE(p.ProjectContractID, sp.ProjectContractID) IS NOT NULL
GROUP BY
    pc.ID,
    p.PersonRole,
    COALESCE(NULLIF(LTRIM(RTRIM(pc.NameTm)), N''), N'');
