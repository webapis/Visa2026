-- Report Dashboard: Registration category.
-- One row per not-expired visa: latest registration Application linked via ApplicationItem.CurrentVisa.
-- Sub-report filter = ApplicationTypeName; chart Status = ProgressStateLabel (latest ApplicationState).
CREATE OR ALTER VIEW [dbo].[vw_rd_registration] AS
WITH ranked AS (
    SELECT
        ai.ID,
        p.ID AS PersonOid,
        CONCAT_WS(N' ',
            NULLIF(LTRIM(RTRIM(p.FirstName)), N''),
            NULLIF(LTRIM(RTRIM(p.MiddleName)), N''),
            NULLIF(LTRIM(RTRIM(p.LastName)), N'')
        ) AS PersonName,
        COALESCE(
            NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
            NULLIF(LTRIM(RTRIM(spc.NameTm)), N''),
            N''
        ) AS ProjectName,
        COALESCE(pc.NameTm, spc.NameTm, N'') AS ProjectNameRaw,
        COALESCE(pc.NameTm, spc.NameTm, N'') AS ProjectNameTm,
        p.PersonRole AS PersonRoleCode,
        COALESCE(NULLIF(LTRIM(RTRIM(v.VisaNumber)), N''), N'') AS VisaNumber,
        v.ExpirationDate AS VisaExpirationDate,
        COALESCE(
            NULLIF(LTRIM(RTRIM(a.FullApplicationNumber)), N''),
            NULLIF(LTRIM(RTRIM(a.ApplicationNumber)), N''),
            N''
        ) AS ApplicationNumber,
        a.ApplicationDate AS ApplicationDate,
        at.Name AS ApplicationTypeName,
        COALESCE(
            NULLIF(LTRIM(RTRIM(at.NameTm)), N''),
            NULLIF(LTRIM(RTRIM(at.Name)), N''),
            N'Unknown'
        ) AS ApplicationTypeLabel,
        COALESCE(
            NULLIF(LTRIM(RTRIM(ast.NameTm)), N''),
            NULLIF(LTRIM(RTRIM(ast.Name)), N''),
            N'OFISDE'
        ) AS ProgressStateLabel,
        CASE
            WHEN ast.Code IN (N'PROCESS_ISSUED') THEN N'st-approved'
            WHEN ast.Code IN (N'PROCESS_REJECTED', N'PROCESS_CANCELLED') THEN N'st-expiring'
            WHEN ast.Code IS NULL THEN N'st-pending'
            ELSE N'st-pending'
        END AS ProgressStateCssClass,
        COALESCE(ast.Code, N'AT_OFFICE') AS ProgressStateCode,
        DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) AS DaysRemaining,
        CASE
            WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 7   THEN N'< 7 days'
            WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 14  THEN N'< 14 days'
            WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 30  THEN N'< 1 month'
            WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 90  THEN N'< 3 months'
            WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 180 THEN N'< 6 months'
            ELSE N'â‰¥ 6 months'
        END AS ExpiryBucketLabel,
        CASE
            WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 14  THEN N'st-expiring'
            WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 90  THEN N'st-pending'
            ELSE N'st-approved'
        END AS ExpiryBucketCssClass,
        CAST(ISNULL(p.IsArchived, 0) AS bit) AS IsArchived,
        COALESCE(
            NULLIF(LTRIM(RTRIM(city.NameTm)), N''),
            NULLIF(LTRIM(RTRIM(city.Name)), N''),
            N'Unknown city'
        ) AS CityLabel,
        ROW_NUMBER() OVER (
            PARTITION BY v.ID
            ORDER BY a.ApplicationDate DESC, a.ID DESC, ai.ID DESC
        ) AS rn
    FROM Visas v
    INNER JOIN Passports pp
        ON pp.ID = v.PassportID AND ISNULL(pp.GCRecord, 0) = 0
    INNER JOIN People p
        ON p.ID = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
    INNER JOIN ApplicationItems ai
        ON ai.CurrentVisaId = v.ID AND ISNULL(ai.GCRecord, 0) = 0
    INNER JOIN Applications a
        ON a.ID = ai.ApplicationID AND ISNULL(a.GCRecord, 0) = 0
    INNER JOIN ApplicationTypes at
        ON at.ID = a.ApplicationTypeID AND ISNULL(at.GCRecord, 0) = 0
    LEFT JOIN AddressesOfResidence addr
        ON addr.ID = ai.CurrentAddressOfResidenceID AND ISNULL(addr.GCRecord, 0) = 0
    LEFT JOIN Cities city
        ON city.ID = addr.CityID AND ISNULL(city.GCRecord, 0) = 0
    LEFT JOIN ProjectContracts pc
        ON pc.ID = COALESCE(a.ProjectContractID, p.ProjectContractID)
       AND ISNULL(pc.GCRecord, 0) = 0
    LEFT JOIN People sp
        ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
    LEFT JOIN ProjectContracts spc
        ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
    OUTER APPLY (
        SELECT TOP 1 ap.StateID
        FROM ApplicationProgresses ap
        WHERE ap.ApplicationID = a.ID
          AND ISNULL(ap.GCRecord, 0) = 0
        ORDER BY ap.[Date] DESC, ap.ID DESC
    ) latest_ap
    LEFT JOIN ApplicationStates ast
        ON ast.ID = latest_ap.StateID AND ISNULL(ast.GCRecord, 0) = 0
    WHERE ISNULL(v.GCRecord, 0) = 0
      AND ISNULL(v.IsCancelled, 0) = 0
      AND CAST(v.ExpirationDate AS date) >= CAST(GETDATE() AS date)
      AND at.Name IN (
            N'App_Reg_Check_In',
            N'App_Reg_Check_In_Internal',
            N'App_Reg_Check_Out',
            N'App_Reg_Check_Out_Internal',
            N'App_Reg_ext',
            N'App_Reg_Info_Change_Address',
            N'App_Reg_Info_Change_Passport',
            N'App_Reg_Info_Change_Visa'
        )
)
SELECT
    ID,
    PersonOid,
    PersonName,
    ProjectName,
    ProjectNameRaw,
    ProjectNameTm,
    PersonRoleCode,
    VisaNumber,
    VisaExpirationDate,
    ApplicationNumber,
    ApplicationDate,
    ApplicationTypeName,
    ApplicationTypeLabel,
    ProgressStateLabel,
    ProgressStateCssClass,
    ProgressStateCode,
    DaysRemaining,
    ExpiryBucketLabel,
    ExpiryBucketCssClass,
    IsArchived,
    CityLabel
FROM ranked
WHERE rn = 1;
