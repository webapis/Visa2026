-- Report Dashboard: Ready Invitations (ready-by-project / ready-by-period-category).
-- One row per InvitationItem: valid (ExpirationDate >= today), not used/cancelled/changed.
-- StatusLabel = Project; Period + Category + Type (from Application.VisaType) for Ready By Period · Category · Type.
CREATE OR ALTER VIEW [dbo].[vw_rd_invitation_ready] AS
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
    COALESCE(
        NULLIF(LTRIM(RTRIM(vp.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(vp.Name)), N''),
        N'(No period)'
    )                                                                   AS VisaPeriodLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(vc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(vc.Name)), N''),
        N'(No category)'
    )                                                                   AS VisaCategoryLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(vt.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(vt.Name)), N''),
        N'(No type)'
    )                                                                   AS VisaTypeLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(apc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(spc.NameTm)), N''),
        N'(No project)'
    )                                                                   AS StatusLabel,
    N'st-cat-1'                                                         AS StatusCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM InvitationItems ii
INNER JOIN Invitations inv
    ON inv.ID = ii.InvitationID AND ISNULL(inv.GCRecord, 0) = 0
INNER JOIN People p
    ON p.ID = ii.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN VisaPeriods vp
    ON vp.ID = inv.VisaPeriodID AND ISNULL(vp.GCRecord, 0) = 0
LEFT JOIN VisaCategories vc
    ON vc.ID = inv.VisaCategoryID AND ISNULL(vc.GCRecord, 0) = 0
LEFT JOIN Applications a
    ON a.ID = inv.ApplicationID AND ISNULL(a.GCRecord, 0) = 0
LEFT JOIN VisaTypes vt
    ON vt.ID = a.VisaTypeID AND ISNULL(vt.GCRecord, 0) = 0
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
