-- Report Dashboard: Invitations Rejected (rejected-by-project).
-- UNION: RejectionItems on CanIssueInvitation apps
--      + PROCESS_REJECTED CanIssueInvitation apps with no Rejection header (no double-count).
-- StatusLabel = Project (Application → Person → sponsor → (No project)).
CREATE OR ALTER VIEW [dbo].[vw_rd_invitation_rejected] AS
SELECT
    ri.ID                                                               AS ID,
    N'rejection-item'                                                   AS SourceKind,
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
    COALESCE(NULLIF(LTRIM(RTRIM(r.RejectedDocNumber)), N''), N'')       AS DocumentNumber,
    CASE WHEN CAST(r.[Date] AS date) > '1900-01-01' THEN r.[Date] ELSE NULL END AS RecordDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(apc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(spc.NameTm)), N''),
        N'(No project)'
    )                                                                   AS StatusLabel,
    N'st-cat-1'                                                         AS StatusCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM RejectionItems ri
INNER JOIN Rejections r
    ON r.ID = ri.RejectionID AND ISNULL(r.GCRecord, 0) = 0
INNER JOIN Applications a
    ON a.ID = r.ApplicationID AND ISNULL(a.GCRecord, 0) = 0
INNER JOIN ApplicationTypes at
    ON at.ID = a.ApplicationTypeID
   AND ISNULL(at.GCRecord, 0) = 0
   AND ISNULL(at.CanIssueInvitation, 0) = 1
INNER JOIN People p
    ON p.ID = ri.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts apc
    ON apc.ID = a.ProjectContractID AND ISNULL(apc.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(ri.GCRecord, 0) = 0
  AND ri.PersonID IS NOT NULL

UNION ALL

SELECT
    a.ID                                                                AS ID,
    N'application'                                                      AS SourceKind,
    first_p.ID                                                          AS PersonOid,
    COALESCE(
        NULLIF(CONCAT_WS(N' ',
            NULLIF(LTRIM(RTRIM(first_p.FirstName)), N''),
            NULLIF(LTRIM(RTRIM(first_p.MiddleName)), N''),
            NULLIF(LTRIM(RTRIM(first_p.LastName)), N'')
        ), N''),
        NULLIF(LTRIM(RTRIM(a.FullApplicationNumber)), N''),
        NULLIF(LTRIM(RTRIM(a.ApplicationNumber)), N''),
        N''
    )                                                                   AS PersonName,
    COALESCE(
        NULLIF(LTRIM(RTRIM(apc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(spc.NameTm)), N''),
        N'(No project)'
    )                                                                   AS ProjectName,
    COALESCE(apc.NameTm, pc.NameTm, spc.NameTm, N'')                    AS ProjectNameRaw,
    COALESCE(apc.NameTm, pc.NameTm, spc.NameTm, N'')                    AS ProjectNameTm,
    COALESCE(first_p.PersonRole, 0)                                     AS PersonRoleCode,
    COALESCE(
        NULLIF(LTRIM(RTRIM(a.FullApplicationNumber)), N''),
        NULLIF(LTRIM(RTRIM(a.ApplicationNumber)), N''),
        N''
    )                                                                   AS DocumentNumber,
    a.ApplicationDate                                                   AS RecordDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(apc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(spc.NameTm)), N''),
        N'(No project)'
    )                                                                   AS StatusLabel,
    N'st-cat-1'                                                         AS StatusCssClass,
    CAST(ISNULL(first_p.IsArchived, 0) AS bit)                          AS IsArchived
FROM Applications a
INNER JOIN ApplicationTypes at
    ON at.ID = a.ApplicationTypeID
   AND ISNULL(at.GCRecord, 0) = 0
   AND ISNULL(at.CanIssueInvitation, 0) = 1
LEFT JOIN ProjectContracts apc
    ON apc.ID = a.ProjectContractID AND ISNULL(apc.GCRecord, 0) = 0
OUTER APPLY (
    SELECT TOP 1 ap.StateID
    FROM ApplicationProgresses ap
    WHERE ap.ApplicationID = a.ID
      AND ISNULL(ap.GCRecord, 0) = 0
    ORDER BY ap.[Date] DESC, ap.ID DESC
) latest_ap
INNER JOIN ApplicationStates ast
    ON ast.ID = latest_ap.StateID
   AND ISNULL(ast.GCRecord, 0) = 0
   AND ast.Code = N'PROCESS_REJECTED'
OUTER APPLY (
    SELECT TOP 1 ai.PersonID
    FROM ApplicationItems ai
    WHERE ai.ApplicationID = a.ID
      AND ISNULL(ai.GCRecord, 0) = 0
    ORDER BY ai.ID
) first_ai
LEFT JOIN People first_p
    ON first_p.ID = first_ai.PersonID AND ISNULL(first_p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = first_p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = first_p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(a.GCRecord, 0) = 0
  AND NOT EXISTS (
        SELECT 1
        FROM Rejections r
        WHERE r.ApplicationID = a.ID
          AND ISNULL(r.GCRecord, 0) = 0
    );