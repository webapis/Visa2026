-- Other Process Completed (P).
-- One row per ApplicationItem.
CREATE OR ALTER VIEW [dbo].[vw_rd_application_via_ministry_other_completed] AS
SELECT
    ai.ID                                                               AS ID,
    a.ID                                                                AS ApplicationOid,
    ai.ID                                                               AS ApplicationItemOid,
    p.ID                                                                AS PersonOid,
    latest_ap.StateID                                                   AS CurrentStateID,
    COALESCE(
        NULLIF(CONCAT_WS(N' ',
            NULLIF(LTRIM(RTRIM(p.FirstName)), N''),
            NULLIF(LTRIM(RTRIM(p.MiddleName)), N''),
            NULLIF(LTRIM(RTRIM(p.LastName)), N'')
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
    COALESCE(p.PersonRole, 0)                                           AS PersonRoleCode,
    COALESCE(
        NULLIF(LTRIM(RTRIM(pos.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pos.Name)), N''),
        N''
    )                                                                   AS PositionLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(at.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(at.Name)), N''),
        N''
    )                                                                   AS ApplicationTypeLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(vp.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(vp.Name)), N''),
        N''
    )                                                                   AS VisaPeriodLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(vt.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(vt.Name)), N''),
        N''
    )                                                                   AS VisaTypeLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(a.FullApplicationNumber)), N''),
        NULLIF(LTRIM(RTRIM(a.ApplicationNumber)), N''),
        N''
    )                                                                   AS ApplicationNumber,
    a.ApplicationDate                                                   AS ApplicationDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(a.LatestPrimaryStateCode)), N''),
        NULLIF(LTRIM(RTRIM(ast.Code)), N''),
        N''
    )                                                                   AS ProgressStateCode,
    COALESCE(
        NULLIF(LTRIM(RTRIM(a.LatestProgressDisplay)), N''),
        NULLIF(LTRIM(RTRIM(ast.Name)), N''),
        NULLIF(LTRIM(RTRIM(ast.NameTm)), N''),
        N'At office'
    )                                                                   AS StatusLabel,
    CASE
      WHEN COALESCE(NULLIF(LTRIM(RTRIM(a.LatestPrimaryStateCode)), N''), NULLIF(LTRIM(RTRIM(ast.Code)), N''), N'')
           IN (N'PROCESS_ISSUED', N'1_REVIEW_APPROVED', N'2_REVIEW_APPROVED')
                                                                              THEN N'st-approved'
      WHEN COALESCE(NULLIF(LTRIM(RTRIM(a.LatestPrimaryStateCode)), N''), NULLIF(LTRIM(RTRIM(ast.Code)), N''), N'')
           IN (N'PROCESS_REJECTED', N'PROCESS_CANCELLED', N'1_REVIEW_REJECTED', N'2_REVIEW_REJECTED')
           OR RIGHT(COALESCE(NULLIF(LTRIM(RTRIM(a.LatestPrimaryStateCode)), N''), NULLIF(LTRIM(RTRIM(ast.Code)), N''), N''), 16)
              = N'_REVIEW_REJECTED'
                                                                              THEN N'st-expiring'
      ELSE                                                                          N'st-pending'
    END                                                                 AS StatusCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived,
    COALESCE(
        NULLIF(LTRIM(RTRIM(vp.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(vp.Name)), N''),
        N'(No period)'
    )                                                                   AS PeriodLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(vc.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(vc.Name)), N''),
        N'(No category)'
    )                                                                   AS CategoryLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(vt.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(vt.Name)), N''),
        N'(No type)'
    )                                                                   AS TypeLabel
FROM ApplicationItems ai
INNER JOIN Applications a
    ON a.ID = ai.ApplicationID
   AND ISNULL(a.GCRecord, 0) = 0
INNER JOIN ApplicationTypes at
    ON at.ID = a.ApplicationTypeID
   AND ISNULL(at.GCRecord, 0) = 0
   AND ISNULL(at.ApplicationProgressRoute, 0) = 0
   AND ISNULL(at.CanIssueInvitation, 0) = 0
   AND (at.Name IS NULL OR at.Name NOT IN (N'App_Visa_Ext', N'App_Visa_Ext_According_to_WP', N'App_Visa_Ext_FM', N'App_Visa_and_WP_Ext'))
LEFT JOIN ProjectContracts pc
    ON pc.ID = a.ProjectContractID
   AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People p
    ON p.ID = ai.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN EmployeePositionHistories eph
    ON eph.ID = ai.CurrentPositionHistoryID
   AND ISNULL(eph.GCRecord, 0) = 0
LEFT JOIN Positions pos
    ON pos.ID = eph.PositionID
   AND ISNULL(pos.GCRecord, 0) = 0
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
LEFT JOIN VisaPeriods vp
    ON vp.ID = a.VisaPeriodID AND ISNULL(vp.GCRecord, 0) = 0
LEFT JOIN VisaCategories vc
    ON vc.ID = a.VisaCategoryID AND ISNULL(vc.GCRecord, 0) = 0
LEFT JOIN VisaTypes vt
    ON vt.ID = a.VisaTypeID AND ISNULL(vt.GCRecord, 0) = 0
WHERE ISNULL(ai.GCRecord, 0) = 0

  AND (
        COALESCE(NULLIF(LTRIM(RTRIM(a.LatestPrimaryStateCode)), N''), NULLIF(LTRIM(RTRIM(ast.Code)), N''), N'')
            IN (
                N'PROCESS_ISSUED', N'PROCESS_REJECTED', N'PROCESS_CANCELLED',
                N'1_REVIEW_REJECTED', N'2_REVIEW_REJECTED', N'3_REVIEW_REJECTED',
                N'4_REVIEW_REJECTED', N'5_REVIEW_REJECTED')
        OR RIGHT(COALESCE(NULLIF(LTRIM(RTRIM(a.LatestPrimaryStateCode)), N''), NULLIF(LTRIM(RTRIM(ast.Code)), N''), N''), 16)
            = N'_REVIEW_REJECTED'
      )
;
