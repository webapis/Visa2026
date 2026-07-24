-- Report Dashboard: Invitations In Process (in-process).
-- One row per Application: CanIssueInvitation, no linked Invitation, not issued/rejected/cancelled.
-- StatusLabel = simple Application Progress state (Being Prepared when none).
CREATE OR ALTER VIEW [dbo].[vw_rd_invitation_in_process] AS
SELECT
    a.ID                                                                AS ID,
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
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        N'(No project)'
    )                                                                   AS ProjectName,
    COALESCE(pc.NameTm, N'')                                            AS ProjectNameRaw,
    COALESCE(pc.NameTm, N'')                                            AS ProjectNameTm,
    COALESCE(first_p.PersonRole, 0)                                     AS PersonRoleCode,
    COALESCE(
        NULLIF(LTRIM(RTRIM(a.FullApplicationNumber)), N''),
        NULLIF(LTRIM(RTRIM(a.ApplicationNumber)), N''),
        N''
    )                                                                   AS ApplicationNumber,
    a.ApplicationDate                                                   AS ApplicationDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(ast.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(ast.Name)), N''),
        N'Being Prepared'
    )                                                                   AS StatusLabel,
    CASE
      WHEN ast.Code IN (N'PROCESS_ISSUED', N'1_REVIEW_APPROVED', N'2_REVIEW_APPROVED')
                                                                              THEN N'st-approved'
      WHEN ast.Code IN (N'PROCESS_REJECTED', N'PROCESS_CANCELLED', N'1_REVIEW_REJECTED', N'2_REVIEW_REJECTED')
                                                                              THEN N'st-expiring'
      ELSE                                                                          N'st-pending'
    END                                                                 AS StatusCssClass,
    COALESCE(ast.Code, N'')                                             AS ProgressStateCode,
    CAST(ISNULL(first_p.IsArchived, 0) AS bit)                          AS IsArchived
FROM Applications a
INNER JOIN ApplicationTypes at
    ON at.ID = a.ApplicationTypeID
   AND ISNULL(at.GCRecord, 0) = 0
   AND ISNULL(at.CanIssueInvitation, 0) = 1
LEFT JOIN ProjectContracts pc
    ON pc.ID = a.ProjectContractID
   AND ISNULL(pc.GCRecord, 0) = 0
OUTER APPLY (
    SELECT TOP 1 ap.StateID
    FROM ApplicationProgresses ap
    WHERE ap.ApplicationID = a.ID
      AND ISNULL(ap.GCRecord, 0) = 0
    ORDER BY ap.[Date] DESC, ap.ID DESC
) latest_ap
LEFT JOIN ApplicationStates ast
    ON ast.ID = latest_ap.StateID
   AND ISNULL(ast.GCRecord, 0) = 0
OUTER APPLY (
    SELECT TOP 1 ai.PersonID
    FROM ApplicationItems ai
    WHERE ai.ApplicationID = a.ID
      AND ISNULL(ai.GCRecord, 0) = 0
    ORDER BY ai.ID
) first_ai
LEFT JOIN People first_p
    ON first_p.ID = first_ai.PersonID
   AND ISNULL(first_p.GCRecord, 0) = 0
WHERE ISNULL(a.GCRecord, 0) = 0
  AND NOT EXISTS (
        SELECT 1
        FROM Invitations inv
        WHERE inv.ApplicationID = a.ID
          AND ISNULL(inv.GCRecord, 0) = 0
    )
  AND (
        ast.Code IS NULL
        OR ast.Code NOT IN (N'PROCESS_ISSUED', N'PROCESS_REJECTED', N'PROCESS_CANCELLED')
      );