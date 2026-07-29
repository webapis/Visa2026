-- Report Dashboard: valid visas by days remaining until expiry (Visa Validity).
-- IsOneLastValidPerPerson: latest ExpirationDate per person (ties: highest ID) — ListView/Preview toggle parity.
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_by_days_remaining] AS
SELECT
    x.ID,
    x.PersonOid,
    x.PassportID,
    x.PassportNumber,
    x.PersonName,
    x.ProjectName,
    x.ProjectNameRaw,
    x.ProjectNameTm,
    x.PersonRoleCode,
    x.VisaNumber,
    x.ExpirationDate,
    x.DaysRemaining,
    x.RemainingLabel,
    x.StatusLabel,
    x.StatusCssClass,
    CAST(CASE WHEN x.Rn = 1 THEN 1 ELSE 0 END AS bit) AS IsOneLastValidPerPerson,
    x.IsArchived
FROM (
    SELECT
        v.ID AS ID,
        p.ID AS PersonOid,
        v.PassportID AS PassportID,
        COALESCE(NULLIF(LTRIM(RTRIM(pp.PassportNumber)), N''), N'') AS PassportNumber,
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
        CAST(ISNULL(p.IsArchived, 0) AS bit) AS IsArchived,
        ROW_NUMBER() OVER (
            PARTITION BY p.ID
            ORDER BY v.ExpirationDate DESC, v.ID DESC
        ) AS Rn
    FROM Visas v
    INNER JOIN Passports pp ON pp.ID = v.PassportID AND ISNULL(pp.GCRecord, 0) = 0
    INNER JOIN People p ON p.ID = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
    LEFT JOIN ProjectContracts pc ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
    LEFT JOIN People sp ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
    LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
    WHERE ISNULL(v.GCRecord, 0) = 0
      AND ISNULL(v.IsCancelled, 0) = 0
      AND v.ExpirationDate IS NOT NULL
      AND CAST(v.ExpirationDate AS date) >= CAST(GETDATE() AS date)
) x;