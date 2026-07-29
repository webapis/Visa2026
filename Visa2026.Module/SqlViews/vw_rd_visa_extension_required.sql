-- Report Dashboard: Extension Required (P)/(V).
-- Last valid visa per person (ExpirationDate DESC, ID DESC), not cancelled / not expired.
-- Excludes people with an unfinished Visa Extension app (types like vw_rd_visa_app_progress;
-- unfinished = latest progress is not a terminal outcome (Issued/Cancelled/Rejected/*_REVIEW_REJECTED)).
-- (P) Status = Project; (V) Status = Period · Category · Type (resolved in C#).
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_extension_required] AS
WITH valid_visas AS (
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
        CASE
            WHEN DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) < 0 THEN 0
            ELSE DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date))
        END AS PeriodDays,
        CASE
            WHEN ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 30)
                 <= ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 90)
             AND ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 30)
                 <= ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 180)
             AND ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 30)
                 <= ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 365) THEN N'1 month'
            WHEN ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 90)
                 <= ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 180)
             AND ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 90)
                 <= ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 365) THEN N'3 months'
            WHEN ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 180)
                 <= ABS(DATEDIFF(day, CAST(v.StartDate AS date), CAST(v.ExpirationDate AS date)) - 365) THEN N'6 months'
            ELSE N'1 year'
        END AS PeriodLabel,
        COALESCE(NULLIF(LTRIM(RTRIM(vc.NameTm)), N''), NULLIF(LTRIM(RTRIM(vc.Name)), N''), N'(No category)') AS CategoryLabel,
        COALESCE(NULLIF(LTRIM(RTRIM(vt.NameTm)), N''), NULLIF(LTRIM(RTRIM(vt.Name)), N''), N'(No type)') AS TypeLabel,
        CAST(ISNULL(p.IsArchived, 0) AS bit) AS IsArchived,
        ROW_NUMBER() OVER (
            PARTITION BY p.ID
            ORDER BY v.ExpirationDate DESC, v.ID DESC
        ) AS rn
    FROM Visas v
    INNER JOIN Passports pp ON pp.ID = v.PassportID AND ISNULL(pp.GCRecord, 0) = 0
    INNER JOIN People p ON p.ID = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
    LEFT JOIN VisaCategories vc ON vc.ID = v.VisaCategoryID AND ISNULL(vc.GCRecord, 0) = 0
    LEFT JOIN VisaTypes vt ON vt.ID = v.VisaTypeID AND ISNULL(vt.GCRecord, 0) = 0
    LEFT JOIN ProjectContracts pc ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
    LEFT JOIN People sp ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
    LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
    WHERE ISNULL(v.GCRecord, 0) = 0
      AND ISNULL(v.IsCancelled, 0) = 0
      AND v.ExpirationDate IS NOT NULL
      AND CAST(v.ExpirationDate AS date) >= CAST(GETDATE() AS date)
      AND v.StartDate IS NOT NULL
      AND CAST(v.StartDate AS date) > '1900-01-01'
),
unfinished_extension_people AS (
    SELECT DISTINCT ai.PersonID
    FROM dbo.ApplicationItems ai
    INNER JOIN dbo.Applications a
        ON a.ID = ai.ApplicationID AND ISNULL(a.GCRecord, 0) = 0
    INNER JOIN dbo.ApplicationTypes at
        ON at.ID = a.ApplicationTypeID AND ISNULL(at.GCRecord, 0) = 0
    WHERE ISNULL(ai.GCRecord, 0) = 0
      AND ai.CurrentVisaID IS NOT NULL
      AND ai.PersonID IS NOT NULL
      AND at.Name IN (
            N'App_Visa_Ext',
            N'App_Visa_Ext_According_to_WP',
            N'App_Visa_Ext_FM',
            N'App_Visa_and_WP_Ext'
        )
      AND (
          a.LatestPrimaryStateCode IS NULL
          OR LTRIM(RTRIM(a.LatestPrimaryStateCode)) = N''
          OR (
               a.LatestPrimaryStateCode NOT IN (N'PROCESS_ISSUED', N'PROCESS_CANCELLED', N'PROCESS_REJECTED')
               AND RIGHT(LTRIM(RTRIM(a.LatestPrimaryStateCode)), 16) <> N'_REVIEW_REJECTED'
             )
      )
)
SELECT
    v.ID,
    v.PersonOid,
    v.PassportID,
    v.PassportNumber,
    v.PersonName,
    v.ProjectName,
    v.ProjectNameRaw,
    v.ProjectNameTm,
    v.PersonRoleCode,
    v.VisaNumber,
    v.ExpirationDate,
    v.PeriodDays,
    v.PeriodLabel,
    v.CategoryLabel,
    v.TypeLabel,
    CASE
        WHEN v.ExpirationDate IS NULL THEN 0
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date)) < 0 THEN 0
        ELSE DATEDIFF(day, CAST(GETDATE() AS date), CAST(v.ExpirationDate AS date))
    END AS DaysRemaining,
    COALESCE(NULLIF(LTRIM(RTRIM(v.ProjectName)), N''), N'(No project)') AS StatusLabel,
    N'st-cat-1' AS StatusCssClass,
    v.IsArchived
FROM valid_visas v
WHERE v.rn = 1
  AND NOT EXISTS (
        SELECT 1
        FROM unfinished_extension_people u
        WHERE u.PersonID = v.PersonOid
    );