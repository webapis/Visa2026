-- Report Dashboard: valid visas by nearest granted period (StartDate → ExpirationDate).
-- Shared by Active Visa (P)/(V) preview and Open ListView (VwRdVisaByPeriod).
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_by_period] AS
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
    x.PeriodDays,
    x.PeriodLabel,
    x.PeriodLabel AS StatusLabel,
    CASE x.PeriodLabel
        WHEN N'1 month'  THEN N'st-cat-1'
        WHEN N'3 months' THEN N'st-cat-2'
        WHEN N'6 months' THEN N'st-cat-3'
        ELSE N'st-cat-4'
    END AS StatusCssClass,
    CASE
        WHEN x.ExpirationDate IS NULL THEN 0
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(x.ExpirationDate AS date)) < 0 THEN 0
        ELSE DATEDIFF(day, CAST(GETDATE() AS date), CAST(x.ExpirationDate AS date))
    END AS DaysRemaining,
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
        d.PeriodDays,
        CASE
            WHEN ABS(d.PeriodDays - 30) <= ABS(d.PeriodDays - 90)
             AND ABS(d.PeriodDays - 30) <= ABS(d.PeriodDays - 180)
             AND ABS(d.PeriodDays - 30) <= ABS(d.PeriodDays - 365) THEN N'1 month'
            WHEN ABS(d.PeriodDays - 90) <= ABS(d.PeriodDays - 180)
             AND ABS(d.PeriodDays - 90) <= ABS(d.PeriodDays - 365) THEN N'3 months'
            WHEN ABS(d.PeriodDays - 180) <= ABS(d.PeriodDays - 365) THEN N'6 months'
            ELSE N'1 year'
        END AS PeriodLabel,
        CAST(ISNULL(p.IsArchived, 0) AS bit) AS IsArchived,
        ROW_NUMBER() OVER (
            PARTITION BY p.ID
            ORDER BY v.ExpirationDate DESC, v.ID DESC
        ) AS Rn
    FROM Visas v
    CROSS APPLY (
        SELECT CASE
            WHEN DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) < 0 THEN 0
            ELSE DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date))
        END AS PeriodDays
    ) d
    INNER JOIN Passports pp ON pp.ID = v.PassportID AND ISNULL(pp.GCRecord, 0) = 0
    INNER JOIN People p ON p.ID = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
    LEFT JOIN ProjectContracts pc ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
    LEFT JOIN People sp ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
    LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
    WHERE ISNULL(v.GCRecord, 0) = 0
      AND ISNULL(v.IsCancelled, 0) = 0
      AND v.ExpirationDate IS NOT NULL
      AND CAST(v.ExpirationDate AS date) >= CAST(GETDATE() AS date)
      AND v.StartDate IS NOT NULL
      AND CAST(v.StartDate AS date) > '1900-01-01'
) x;
