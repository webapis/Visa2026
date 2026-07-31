-- Report Dashboard: Invitation Valid Until (valid-until).
-- Unused / not cancelled / not changed InvitationItems with ExpirationDate >= today.
-- StatusLabel = remaining-time bucket (days / weeks / months).
CREATE OR ALTER VIEW [dbo].[vw_rd_invitation_valid_until] AS
SELECT
    ii.ID                                                               AS ID,
    p.ID                                                                AS PersonOid,
    CONCAT_WS(N' ',
        NULLIF(LTRIM(RTRIM(p.FirstName)), N''),
        NULLIF(LTRIM(RTRIM(p.MiddleName)), N''),
        NULLIF(LTRIM(RTRIM(p.LastName)), N'')
    )                                                                   AS PersonName,
    COALESCE(
        NULLIF(LTRIM(RTRIM(apc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(spc.NameTm)), N''),
        N'(No project)'
    )                                                                   AS ProjectName,
    COALESCE(apc.NameTm, pc.NameTm, spc.NameTm, N'')                    AS ProjectNameRaw,
    COALESCE(apc.NameTm, pc.NameTm, spc.NameTm, N'')                    AS ProjectNameTm,
    p.PersonRole                                                        AS PersonRoleCode,
    COALESCE(NULLIF(LTRIM(RTRIM(inv.InvitationNumber)), N''), N'')      AS InvitationNumber,
    CASE WHEN CAST(inv.ExpirationDate AS date) > '1900-01-01' THEN inv.ExpirationDate ELSE NULL END AS ExpirationDate,
    CASE WHEN CAST(inv.StartDate AS date) > '1900-01-01' THEN inv.StartDate ELSE NULL END AS IssuedDate,
    DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) AS DaysRemaining,
    CASE
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) < 1   THEN N'< 1 day'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) < 7   THEN N'< 1 week'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) < 14  THEN N'< 2 weeks'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) < 21  THEN N'< 3 weeks'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) < 30  THEN N'< 1 month'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) < 60  THEN N'< 2 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) < 90  THEN N'< 3 months'
        ELSE N'≥ 3 months'
    END                                                                 AS ValidityLabel,
    CASE
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) < 7   THEN N'st-expiring'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(inv.ExpirationDate AS date)) < 30  THEN N'st-pending'
        ELSE N'st-approved'
    END                                                                 AS ValidityCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM InvitationItems ii
INNER JOIN Invitations inv
    ON inv.ID = ii.InvitationID AND ISNULL(inv.GCRecord, 0) = 0
INNER JOIN People p
    ON p.ID = ii.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN Applications a
    ON a.ID = inv.ApplicationID AND ISNULL(a.GCRecord, 0) = 0
LEFT JOIN ProjectContracts apc
    ON apc.ID = a.ProjectContractID AND ISNULL(apc.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(ii.GCRecord, 0) = 0
  AND ISNULL(ii.IsUsed, 0) = 0
  AND ISNULL(ii.IsCancelled, 0) = 0
  AND ISNULL(ii.IsChanged, 0) = 0
  AND ii.PersonID IS NOT NULL
  AND inv.ExpirationDate IS NOT NULL
  AND CAST(inv.ExpirationDate AS date) >= CAST(GETDATE() AS date);