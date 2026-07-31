-- Report Dashboard: To Be Checked In (Registration).
-- Valid visas with no ApplicationItem.CurrentVisa link to any App_Reg_* type.
-- Person must be in-country: latest TravelHistory is ExternalArrival.
-- Chart: days since that arrival TravelDate.
CREATE OR ALTER VIEW [dbo].[vw_rd_to_be_checked_in] AS
WITH reg_linked AS (
    SELECT DISTINCT ai.CurrentVisaId AS VisaId
    FROM ApplicationItems ai
    INNER JOIN Applications a
        ON a.ID = ai.ApplicationID AND ISNULL(a.GCRecord, 0) = 0
    INNER JOIN ApplicationTypes at
        ON at.ID = a.ApplicationTypeID AND ISNULL(at.GCRecord, 0) = 0
    WHERE ISNULL(ai.GCRecord, 0) = 0
      AND ai.CurrentVisaId IS NOT NULL
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
),
latest_travel AS (
    SELECT
        th.PersonID,
        th.Discriminator,
        th.TravelDate AS EntryDate,
        ROW_NUMBER() OVER (
            PARTITION BY th.PersonID
            ORDER BY th.TravelDate DESC, th.ID DESC
        ) AS rn
    FROM TravelHistories th
    WHERE ISNULL(th.GCRecord, 0) = 0
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
    lt.EntryDate AS EntryDate,
    DATEDIFF(day, CAST(lt.EntryDate AS date), CAST(GETDATE() AS date)) AS DaysSinceEntry,
    CASE
        WHEN DATEDIFF(day, CAST(lt.EntryDate AS date), CAST(GETDATE() AS date)) < 7  THEN N'< 1 week'
        WHEN DATEDIFF(day, CAST(lt.EntryDate AS date), CAST(GETDATE() AS date)) < 14 THEN N'< 2 weeks'
        WHEN DATEDIFF(day, CAST(lt.EntryDate AS date), CAST(GETDATE() AS date)) < 21 THEN N'< 3 weeks'
        WHEN DATEDIFF(day, CAST(lt.EntryDate AS date), CAST(GETDATE() AS date)) < 28 THEN N'< 4 weeks'
        WHEN DATEDIFF(day, CAST(lt.EntryDate AS date), CAST(GETDATE() AS date)) < 30 THEN N'< 1 month'
        ELSE N'≥ 1 month'
    END AS EntryBucketLabel,
    CASE
        WHEN DATEDIFF(day, CAST(lt.EntryDate AS date), CAST(GETDATE() AS date)) < 14 THEN N'st-expiring'
        WHEN DATEDIFF(day, CAST(lt.EntryDate AS date), CAST(GETDATE() AS date)) < 30 THEN N'st-pending'
        ELSE N'st-approved'
    END AS EntryBucketCssClass,
    CAST(COALESCE(p.IsArchived, 0) AS bit) AS IsArchived
FROM Visas v
INNER JOIN Passports pp
    ON pp.ID = v.PassportID AND ISNULL(pp.GCRecord, 0) = 0
INNER JOIN People p
    ON p.ID = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
INNER JOIN latest_travel lt
    ON lt.PersonID = p.ID
   AND lt.rn = 1
   AND lt.Discriminator = N'ExternalArrival'
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(v.GCRecord, 0) = 0
  AND NOT EXISTS (
        SELECT 1 FROM reg_linked rl WHERE rl.VisaId = v.ID
  );
