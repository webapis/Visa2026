-- Report Dashboard: To Be Checked Out (Registration).
-- Visas with DaysRemaining < 7 (includes already expired), no Check-Out / Check-Out Internal on CurrentVisa.
-- Chart: Expired · < 1 day · < 2 days · … · < 7 days.
CREATE OR ALTER VIEW [dbo].[vw_rd_to_be_checked_out] AS
WITH checkout_linked AS (
    SELECT DISTINCT ai.CurrentVisaId AS VisaId
    FROM ApplicationItems ai
    INNER JOIN Applications a
        ON a.ID = ai.ApplicationID AND ISNULL(a.GCRecord, 0) = 0
    INNER JOIN ApplicationTypes at
        ON at.ID = a.ApplicationTypeID AND ISNULL(at.GCRecord, 0) = 0
    WHERE ISNULL(ai.GCRecord, 0) = 0
      AND ai.CurrentVisaId IS NOT NULL
      AND at.Name IN (N'App_Reg_Check_Out', N'App_Reg_Check_Out_Internal')
)
SELECT
    v.ID AS ID,
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
    DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) AS DaysRemaining,
    CASE
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 0 THEN N'Expired'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 1 THEN N'< 1 day'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 2 THEN N'< 2 days'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 3 THEN N'< 3 days'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 4 THEN N'< 4 days'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 5 THEN N'< 5 days'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 6 THEN N'< 6 days'
        ELSE N'< 7 days'
    END AS ExpiryBucketLabel,
    CASE
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 0 THEN N'st-expiring'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 3 THEN N'st-expiring'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 5 THEN N'st-pending'
        ELSE N'st-approved'
    END AS ExpiryBucketCssClass,
    CAST(COALESCE(p.IsArchived, 0) AS bit) AS IsArchived
FROM Visas v
INNER JOIN Passports pp
    ON pp.ID = v.PassportID AND ISNULL(pp.GCRecord, 0) = 0
INNER JOIN People p
    ON p.ID = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(v.GCRecord, 0) = 0
  AND DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 7
  AND NOT EXISTS (
        SELECT 1 FROM checkout_linked cl WHERE cl.VisaId = v.ID
  );
