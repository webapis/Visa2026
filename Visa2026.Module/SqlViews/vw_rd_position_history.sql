-- Report Dashboard: Position History (by-status / by-position).
-- One row per EmployeePositionHistory.
CREATE OR ALTER VIEW [dbo].[vw_rd_position_history] AS
SELECT
    eph.ID                                                              AS ID,
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
        NULLIF(LTRIM(RTRIM(pos.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pos.Name)), N''),
        N'Unknown'
    )                                                                   AS PositionName,
    eph.StartDate                                                       AS StartDate,
    CASE
      WHEN eph.EndDate IS NULL
        OR CAST(eph.EndDate AS date) >= CAST(GETDATE() AS date)
                                                                              THEN N'Current'
      ELSE                                                                        N'Ended'
    END                                                                 AS StatusLabel,
    CASE
      WHEN eph.EndDate IS NULL
        OR CAST(eph.EndDate AS date) >= CAST(GETDATE() AS date)
                                                                              THEN N'st-approved'
      ELSE                                                                        N'st-pending'
    END                                                                 AS StatusCssClass,
    COALESCE(
        NULLIF(LTRIM(RTRIM(pos.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pos.Name)), N''),
        N'Unknown'
    )                                                                   AS PositionLabel,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM EmployeePositionHistories eph
INNER JOIN People p
    ON p.ID = eph.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sponsor
    ON sponsor.ID = p.SponsoringEmployeeID AND ISNULL(sponsor.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sponsor.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
LEFT JOIN Positions pos
    ON pos.ID = eph.PositionID AND ISNULL(pos.GCRecord, 0) = 0
WHERE ISNULL(eph.GCRecord, 0) = 0;