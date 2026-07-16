-- Report Dashboard: Visa State — Extension Started cohort (first state slice).
-- Definition (combined analytics queries):
--   1) Valid visa: not cancelled, soft-delete clean, ExpirationDate >= today
--   2) CurrentVisa on ApplicationItem of visa-extension ApplicationTypes
--   3) That visa is the person's current/last visa (StartDate DESC, IssueDate DESC; StartDate <= today)
--   4) Application ProgressHistory must not contain PROCESS_CANCELLED
-- Other Visa State labels (to be Started / Not Required / Rejected / Cancelled) will UNION later.
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_state] AS
WITH ranked_visas AS (
    SELECT
        v.ID AS VisaID,
        pp.PersonID,
        v.VisaNumber,
        v.ExpirationDate,
        v.StartDate,
        v.IssueDate,
        ROW_NUMBER() OVER (
            PARTITION BY pp.PersonID
            ORDER BY
                CASE WHEN v.StartDate IS NULL OR CAST(v.StartDate AS date) <= '1900-01-01' THEN 1 ELSE 0 END,
                v.StartDate DESC,
                v.IssueDate DESC,
                v.ID DESC
        ) AS rn
    FROM Visas v
    INNER JOIN Passports pp
        ON pp.ID = v.PassportID
       AND ISNULL(pp.GCRecord, 0) = 0
    WHERE ISNULL(v.GCRecord, 0) = 0
      AND ISNULL(v.IsCancelled, 0) = 0
      AND v.StartDate IS NOT NULL
      AND CAST(v.StartDate AS date) > '1900-01-01'
      AND CAST(v.StartDate AS date) <= CAST(GETDATE() AS date)
),
ext_items AS (
    SELECT
        ai.ID AS ApplicationItemID,
        ai.PersonID,
        ai.CurrentVisaID AS VisaID,
        a.ID AS ApplicationID,
        a.ApplicationNumber,
        a.FullApplicationNumber,
        a.ApplicationDate,
        a.ProjectContractID AS ApplicationProjectContractID
    FROM ApplicationItems ai
    INNER JOIN Applications a
        ON a.ID = ai.ApplicationID
       AND ISNULL(a.GCRecord, 0) = 0
    INNER JOIN ApplicationTypes at
        ON at.ID = a.ApplicationTypeID
       AND ISNULL(at.GCRecord, 0) = 0
    WHERE ISNULL(ai.GCRecord, 0) = 0
      AND ai.CurrentVisaID IS NOT NULL
      AND at.Name IN (
            N'App_Visa_Ext',
            N'App_Visa_Ext_According_to_WP',
            N'App_Visa_Ext_FM',
            N'App_Visa_and_WP_Ext'
        )
)
SELECT
    ei.ApplicationItemID                                                AS ID,
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
    COALESCE(NULLIF(LTRIM(RTRIM(rv.VisaNumber)), N''), N'')           AS VisaNumber,
    CASE WHEN CAST(rv.ExpirationDate AS date) > '1900-01-01' THEN rv.ExpirationDate ELSE NULL END AS ExpirationDate,
    N'Extension Started'                                                AS StateLabel,
    N'st-pending'                                                       AS StateCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM ext_items ei
INNER JOIN ranked_visas rv
    ON rv.VisaID = ei.VisaID
   AND rv.PersonID = ei.PersonID
   AND rv.rn = 1
INNER JOIN People p
    ON p.ID = ei.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = COALESCE(ei.ApplicationProjectContractID, p.ProjectContractID)
   AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID
   AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID
   AND ISNULL(spc.GCRecord, 0) = 0
WHERE rv.ExpirationDate IS NOT NULL
  AND CAST(rv.ExpirationDate AS date) >= CAST(GETDATE() AS date)
  AND NOT EXISTS (
        SELECT 1
        FROM ApplicationProgresses ap
        INNER JOIN ApplicationStates ast
            ON ast.ID = ap.StateID
           AND ISNULL(ast.GCRecord, 0) = 0
        WHERE ap.ApplicationID = ei.ApplicationID
          AND ISNULL(ap.GCRecord, 0) = 0
          AND ast.Code = N'PROCESS_CANCELLED'
      );
