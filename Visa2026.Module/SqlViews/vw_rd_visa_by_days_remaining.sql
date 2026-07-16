-- Report Dashboard: valid visas by days remaining until expiry (By Days Remaining).
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_by_days_remaining] AS
SELECT
    v.ID AS ID,
    p.ID AS PersonOid,
    CONCAT_WS(N' ',
        NULLIF(LTRIM(RTRIM(p.FirstName)), N''),
        NULLIF(LTRIM(RTRIM(p.MiddleName)), N''),
        NULLIF(LTRIM(RTRIM(p.LastName)), N'')
    ) AS PersonName,
    COALESCE(NULLIF(LTRIM(RTRIM(pc.NameTm)), N''), NULLIF(LTRIM(RTRIM(spc.NameTm)), N''), N'') AS ProjectName,
    COALESCE(pc.NameTm, spc.NameTm, N'') AS ProjectNameRaw,
    COALESCE(pc.NameTm, spc.NameTm, N'') AS ProjectNameTm,
    p.PersonRole AS PersonRoleCode,
    COALESCE(NULLIF(LTRIM(RTRIM(v.VisaNumber)), N''), N'') AS VisaNumber,
    CASE WHEN CAST(v.ExpirationDate AS date) > '1900-01-01' THEN v.ExpirationDate ELSE NULL END AS ExpirationDate,
    DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) AS DaysRemaining,
    CASE
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 10  THEN N'< 10 days'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 30  THEN N'< 1 month'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 90  THEN N'< 3 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 120 THEN N'< 4 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 150 THEN N'< 5 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 180 THEN N'< 6 months'
        ELSE N'≥ 6 months'
    END AS RemainingLabel,
    CASE
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 10  THEN N'< 10 days'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 30  THEN N'< 1 month'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 90  THEN N'< 3 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 120 THEN N'< 4 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 150 THEN N'< 5 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 180 THEN N'< 6 months'
        ELSE N'≥ 6 months'
    END AS StatusLabel,
    CASE
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 30  THEN N'st-expiring'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 90  THEN N'st-pending'
        ELSE N'st-approved'
    END AS StatusCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit) AS IsArchived
FROM Visas v
INNER JOIN Passports pp ON pp.ID = v.PassportID AND ISNULL(pp.GCRecord, 0) = 0
INNER JOIN People p ON p.ID = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(v.GCRecord, 0) = 0
  AND ISNULL(v.IsCancelled, 0) = 0
  AND v.ExpirationDate IS NOT NULL
  AND CAST(v.ExpirationDate AS date) >= CAST(GETDATE() AS date);