-- Report Dashboard: Active WorkPermit (P) — valid WorkPermitItems by project.
-- One row per valid (non-cancelled, not expired) item; persons may appear more than once.
-- StatusLabel = Project (Person.ProjectContract, else sponsor).
CREATE OR ALTER VIEW [dbo].[vw_rd_work_permit_active] AS
SELECT
    wpi.ID                                                              AS ID,
    p.ID                                                                AS PersonOid,
    CONCAT_WS(N' ',
        NULLIF(LTRIM(RTRIM(p.FirstName)), N''),
        NULLIF(LTRIM(RTRIM(p.MiddleName)), N''),
        NULLIF(LTRIM(RTRIM(p.LastName)), N'')
    )                                                                   AS PersonName,
    COALESCE(
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(spc.NameTm)), N''),
        N'(No project)'
    )                                                                   AS ProjectName,
    COALESCE(pc.NameTm, spc.NameTm, N'')                                AS ProjectNameRaw,
    COALESCE(pc.NameTm, spc.NameTm, N'')                                AS ProjectNameTm,
    p.PersonRole                                                        AS PersonRoleCode,
    COALESCE(NULLIF(LTRIM(RTRIM(wpi.WorkPermitNumber)), N''), NULLIF(LTRIM(RTRIM(wpi.ASNumber)), N''), N'') AS WorkPermitNumber,
    CASE WHEN CAST(wpi.ExpirationDate AS date) > '1900-01-01' THEN wpi.ExpirationDate ELSE NULL END AS ExpirationDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(spc.NameTm)), N''),
        N'(No project)'
    )                                                                   AS StatusLabel,
    N'st-cat-1'                                                         AS StatusCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM WorkPermitItems wpi
INNER JOIN People p
    ON p.ID = wpi.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(wpi.GCRecord, 0) = 0
  AND ISNULL(wpi.IsCancelled, 0) = 0
  AND wpi.PersonID IS NOT NULL
  AND wpi.ExpirationDate IS NOT NULL
  AND CAST(wpi.ExpirationDate AS date) >= CAST(GETDATE() AS date);