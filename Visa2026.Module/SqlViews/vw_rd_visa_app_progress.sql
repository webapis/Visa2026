-- Report Dashboard: Visa — On Extension (on-extension sub-report).
-- One row per ApplicationItem on visa-extension application types with CurrentVisa set.
-- ProgressStateCode prefers Application.LatestPrimaryStateCode (authoritative; latest progress row can lag).
-- Shared by dashboard preview and Open ListView (VwRdVisaAppProgress).
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_app_progress] AS
SELECT
    ai.ID                                                               AS ID,
    a.ID                                                                AS ApplicationOid,
    p.ID                                                                AS PersonOid,
    ai.CurrentVisaID                                                    AS ExpiringVisaID,
    ai.CurrentPassportID AS PassportID,
    COALESCE(NULLIF(LTRIM(RTRIM(pp.PassportNumber)), N''), N'') AS PassportNumber,
    latest_ap.StateID                                                   AS CurrentStateID,
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
        NULLIF(LTRIM(RTRIM(a.FullApplicationNumber)), N''),
        NULLIF(LTRIM(RTRIM(a.ApplicationNumber)), N''),
        N''
    )                                                                   AS ApplicationNumber,
    a.ApplicationDate                                                   AS ApplicationDate,
    latest_ap.[Date]                                                    AS StatusDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(a.LatestPrimaryStateCode)), N''),
        NULLIF(LTRIM(RTRIM(ast.Code)), N''),
        N''
    )                                                                   AS ProgressStateCode,
    -- Fallback only; loader resolves ApplicationProgress StatusListLabel via Layer B.
    COALESCE(
        NULLIF(LTRIM(RTRIM(a.LatestProgressDisplay)), N''),
        NULLIF(LTRIM(RTRIM(ast.Name)), N''),
        NULLIF(LTRIM(RTRIM(ast.NameTm)), N''),
        N'Being Prepared'
    )                                                                   AS ProgressStateLabel,
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
    END                                                                 AS ProgressStateCssClass,
    CASE
        WHEN v.IsCancelled = 1 THEN 0
        WHEN v.ExpirationDate IS NULL THEN 0
        WHEN DATEDIFF(day, GETDATE(), v.ExpirationDate) < 0 THEN 0
        ELSE DATEDIFF(day, GETDATE(), v.ExpirationDate)
    END                                                                 AS DaysRemainingOnVisa,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM ApplicationItems ai
INNER JOIN Applications a
    ON a.ID = ai.ApplicationID
   AND ISNULL(a.GCRecord, 0) = 0
INNER JOIN ApplicationTypes at
    ON at.ID = a.ApplicationTypeID
   AND ISNULL(at.GCRecord, 0) = 0
INNER JOIN People p
    ON p.ID = ai.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = COALESCE(a.ProjectContractID, p.ProjectContractID)
   AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID
   AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID
   AND ISNULL(spc.GCRecord, 0) = 0
LEFT JOIN Visas v
    ON v.ID = ai.CurrentVisaID
   AND ISNULL(v.GCRecord, 0) = 0
LEFT JOIN Passports pp
    ON pp.ID = ai.CurrentPassportID
   AND ISNULL(pp.GCRecord, 0) = 0
OUTER APPLY (
    SELECT TOP 1 ap.StateID, ap.[Date]
    FROM ApplicationProgresses ap
    WHERE ap.ApplicationID = a.ID
      AND ISNULL(ap.GCRecord, 0) = 0
    ORDER BY ap.[Date] DESC, ap.ID DESC
) latest_ap
LEFT JOIN ApplicationStates ast
    ON ast.ID = latest_ap.StateID
   AND ISNULL(ast.GCRecord, 0) = 0
WHERE ISNULL(ai.GCRecord, 0) = 0
  AND ai.CurrentVisaID IS NOT NULL
  AND at.Name IN (
        N'App_Visa_Ext',
        N'App_Visa_Ext_According_to_WP',
        N'App_Visa_Ext_FM',
        N'App_Visa_and_WP_Ext'
    );
