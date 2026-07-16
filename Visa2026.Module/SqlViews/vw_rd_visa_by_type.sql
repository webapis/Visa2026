-- Report Dashboard: valid visas by VisaType only (not Visa State).
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_by_type] AS
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
    COALESCE(NULLIF(LTRIM(RTRIM(vt.NameTm)), N''), NULLIF(LTRIM(RTRIM(vt.Name)), N''), N'Unknown') AS TypeLabel,
    COALESCE(NULLIF(LTRIM(RTRIM(vt.NameTm)), N''), NULLIF(LTRIM(RTRIM(vt.Name)), N''), N'Unknown') AS StatusLabel,
    N'st-cat-1' AS StatusCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit) AS IsArchived
FROM Visas v
INNER JOIN Passports pp ON pp.ID = v.PassportID AND ISNULL(pp.GCRecord, 0) = 0
INNER JOIN People p ON p.ID = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN VisaTypes vt ON vt.ID = v.VisaTypeID AND ISNULL(vt.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(v.GCRecord, 0) = 0
  AND ISNULL(v.IsCancelled, 0) = 0
  AND v.ExpirationDate IS NOT NULL
  AND CAST(v.ExpirationDate AS date) >= CAST(GETDATE() AS date);