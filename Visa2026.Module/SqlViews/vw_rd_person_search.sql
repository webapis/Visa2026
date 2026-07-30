-- Report Dashboard: Person search (one row per Person).
-- Backs the Person search category: officers type a term, pick a result row, and open
-- the person dossier.
-- Status buckets follow the person's current visa (latest non-cancelled visa across all
-- of the person's passports).
-- SearchText is a lowercased haystack (name parts + personal number + every passport
-- number) so Preview loader and XAF ListView criteria can filter identically.
-- ProjectContracts uses NameTm (Name may be absent depending on schema).
CREATE OR ALTER VIEW [dbo].[vw_rd_person_search] AS
SELECT
    p.ID                                                                AS ID,
    p.ID                                                                AS PersonOid,
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
    CASE p.PersonRole
        WHEN 0 THEN N'Employee'
        WHEN 1 THEN N'Family Member'
        WHEN 2 THEN N'Temporary Visitor'
        ELSE N'Unknown'
    END                                                                 AS PersonTypeLabel,
    COALESCE(p.PersonalNumber, N'')                                     AS PersonalNumber,
    COALESCE(cp.PassportNumber, N'')                                    AS PassportNumber,
    COALESCE(cv.VisaNumber, N'')                                        AS VisaNumber,
    cv.ExpirationDate                                                   AS VisaExpirationDate,
    CASE
        WHEN cv.ExpirationDate IS NULL THEN N''
        ELSE FORMAT(cv.ExpirationDate, N'dd.MM.yyyy')
    END                                                                 AS VisaExpiryLabel,
    CASE
        WHEN cv.ExpirationDate IS NULL                                  THEN N'No visa'
        WHEN CAST(cv.ExpirationDate AS date) < CAST(GETDATE() AS date)  THEN N'Expired'
        WHEN CAST(cv.ExpirationDate AS date) <= DATEADD(day, 30, CAST(GETDATE() AS date))
                                                                        THEN N'Expiring (<30 days)'
        ELSE                                                            N'Valid'
    END                                                                 AS StatusLabel,
    CASE
        WHEN cv.ExpirationDate IS NULL                                  THEN N''
        WHEN CAST(cv.ExpirationDate AS date) < CAST(GETDATE() AS date)  THEN N'st-expiring'
        WHEN CAST(cv.ExpirationDate AS date) <= DATEADD(day, 30, CAST(GETDATE() AS date))
                                                                        THEN N'st-pending'
        ELSE                                                            N'st-approved'
    END                                                                 AS StatusCssClass,
    LOWER(CONCAT_WS(N' ',
        NULLIF(LTRIM(RTRIM(p.FirstName)), N''),
        NULLIF(LTRIM(RTRIM(p.MiddleName)), N''),
        NULLIF(LTRIM(RTRIM(p.LastName)), N''),
        NULLIF(LTRIM(RTRIM(p.PersonalNumber)), N''),
        allp.PassportNumbers
    ))                                                                  AS SearchText,
    COALESCE(p.IsArchived, 0)                                           AS IsArchived
FROM People p
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND COALESCE(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND COALESCE(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND COALESCE(spc.GCRecord, 0) = 0
OUTER APPLY (
    SELECT TOP 1 pp.PassportNumber, pp.ExpirationDate
    FROM Passports pp
    WHERE pp.PersonID = p.ID
      AND COALESCE(pp.GCRecord, 0) = 0
      AND COALESCE(pp.IsCancelled, 0) = 0
    ORDER BY CASE WHEN pp.ExpirationDate IS NULL THEN 1 ELSE 0 END, pp.ExpirationDate DESC,
             CASE WHEN pp.IssueDate IS NULL THEN 1 ELSE 0 END, pp.IssueDate DESC
) cp
OUTER APPLY (
    SELECT TOP 1 v.VisaNumber, v.ExpirationDate
    FROM Visas v
    INNER JOIN Passports vp
        ON vp.ID = v.PassportID AND COALESCE(vp.GCRecord, 0) = 0
    WHERE vp.PersonID = p.ID
      AND COALESCE(v.GCRecord, 0) = 0
      AND COALESCE(v.IsCancelled, 0) = 0
    ORDER BY CASE WHEN v.ExpirationDate IS NULL THEN 1 ELSE 0 END, v.ExpirationDate DESC,
             CASE WHEN v.IssueDate IS NULL THEN 1 ELSE 0 END, v.IssueDate DESC
) cv
OUTER APPLY (
    SELECT STRING_AGG(NULLIF(LTRIM(RTRIM(pp2.PassportNumber)), N''), N' ') AS PassportNumbers
    FROM Passports pp2
    WHERE pp2.PersonID = p.ID
      AND COALESCE(pp2.GCRecord, 0) = 0
) allp
WHERE COALESCE(p.GCRecord, 0) = 0;
