using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate
{
    public class SqlViewsUpdater : ModuleUpdater
    {
        public SqlViewsUpdater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            CreateViewVisaExtensionTracking();
            CreateViewVisaExtensionStatus();
            CreateViewWorkPermitExtensionTracking();
            CreateViewWorkPermitExtensionStatus();
            CreateViewVisaTransferStatus();
            CreateViewVisaCancelExtStatus();
            CreateViewVisaCancellationStatus();
            CreateViewForeignWorkerMaglumat();
            CreateViewRdPassport();
            CreateFunctions();
            CreateFunctionRegistrationState();
        }

        /// <summary>
        /// One row per non-archived person with a current WorkPermitItem (latest StartDate, then ID).
        /// Mirrors PersonCurrentItems for passport/visa/education/address; MAGLUMAT Excel export source.
        /// </summary>
        private void CreateViewForeignWorkerMaglumat()
        {
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_ForeignWorkerMaglumat] AS
                SELECT
                    cur_wp.ID,
                    p.ID AS PersonID,
                    CONCAT_WS(N' ',
                        NULLIF(LTRIM(RTRIM(p.FirstName)), N''),
                        NULLIF(LTRIM(RTRIM(p.MiddleName)), N''),
                        NULLIF(LTRIM(RTRIM(p.LastName)), N'')
                    ) AS FullName,
                    p.DateOfBirth,
                    c.Code AS NationalityCode,
                    COALESCE(snap_pp.PassportNumber, cur_pp.PassportNumber) AS PassportNumber,
                    COALESCE(snap_pp.ExpirationDate, cur_pp.ExpirationDate) AS PassportExpirationDate,
                    cur_edu.EducationLevelTm,
                    pos.NameTm AS PositionNameTm,
                    cur_addr.ResidenceAddress,
                    cur_wp.WorkPermitNumber,
                    cur_wp.StartDate AS WorkPermitStartDate,
                    cur_wp.ExpirationDate AS WorkPermitExpirationDate,
                    CAST(ISNULL(cur_wp.IsCancelled, 0) AS bit) AS WorkPermitIsCancelled,
                    CAST(CASE
                        WHEN ISNULL(cur_wp.IsCancelled, 0) = 0
                         AND CAST(cur_wp.StartDate AS date) <= CAST(GETDATE() AS date)
                         AND CAST(cur_wp.ExpirationDate AS date) >= CAST(GETDATE() AS date)
                        THEN 1 ELSE 0
                    END AS bit) AS IsValid,
                    cur_visa.VisaNumber,
                    cur_visa.StartDate AS VisaStartDate,
                    cur_visa.ExpirationDate AS VisaExpirationDate,
                    CAST(NULL AS nvarchar(max)) AS Remarks,
                    -- Multiline blocks for Excel parity (dd.MM.yyyy)
                    NULLIF(LTRIM(RTRIM(CONCAT(
                        ISNULL(CONVERT(varchar(10), p.DateOfBirth, 104), N''),
                        CASE
                            WHEN p.DateOfBirth IS NOT NULL AND NULLIF(LTRIM(RTRIM(c.Code)), N'') IS NOT NULL
                                THEN CHAR(13) + CHAR(10)
                            ELSE N''
                        END,
                        ISNULL(c.Code, N'')
                    ))), N'') AS BirthAndNationality,
                    NULLIF(LTRIM(RTRIM(CONCAT(
                        ISNULL(COALESCE(snap_pp.PassportNumber, cur_pp.PassportNumber), N''),
                        CASE
                            WHEN COALESCE(snap_pp.PassportNumber, cur_pp.PassportNumber) IS NOT NULL
                             AND COALESCE(snap_pp.ExpirationDate, cur_pp.ExpirationDate) IS NOT NULL
                                THEN CHAR(13) + CHAR(10)
                            ELSE N''
                        END,
                        ISNULL(CONVERT(varchar(10), COALESCE(snap_pp.ExpirationDate, cur_pp.ExpirationDate), 104), N'')
                    ))), N'') AS PassportBlock,
                    NULLIF(LTRIM(RTRIM(CONCAT(
                        ISNULL(cur_wp.WorkPermitNumber, N''),
                        CASE
                            WHEN NULLIF(LTRIM(RTRIM(cur_wp.WorkPermitNumber)), N'') IS NOT NULL
                                THEN CHAR(13) + CHAR(10)
                            ELSE N''
                        END,
                        ISNULL(CONVERT(varchar(10), cur_wp.StartDate, 104), N''),
                        CASE
                            WHEN cur_wp.StartDate IS NOT NULL AND cur_wp.ExpirationDate IS NOT NULL
                                THEN CHAR(13) + CHAR(10)
                            WHEN cur_wp.ExpirationDate IS NOT NULL
                                THEN N''
                            ELSE N''
                        END,
                        ISNULL(CONVERT(varchar(10), cur_wp.ExpirationDate, 104), N'')
                    ))), N'') AS PermitBlock,
                    NULLIF(LTRIM(RTRIM(CONCAT(
                        ISNULL(cur_visa.VisaNumber, N''),
                        CASE
                            WHEN NULLIF(LTRIM(RTRIM(cur_visa.VisaNumber)), N'') IS NOT NULL
                             AND cur_visa.StartDate IS NOT NULL
                                THEN CHAR(13) + CHAR(10)
                            ELSE N''
                        END,
                        ISNULL(CONVERT(varchar(10), cur_visa.StartDate, 104), N''),
                        CASE
                            WHEN cur_visa.StartDate IS NOT NULL AND cur_visa.ExpirationDate IS NOT NULL
                                THEN CHAR(13) + CHAR(10)
                            ELSE N''
                        END,
                        ISNULL(CONVERT(varchar(10), cur_visa.ExpirationDate, 104), N'')
                    ))), N'') AS VisaBlock
                FROM People p
                -- Live rows in this DB use GCRecord = 0 (import/default); classic XAF soft-delete uses NULL.
                LEFT JOIN Countries c ON p.NationalityID = c.ID AND ISNULL(c.GCRecord, 0) = 0
                -- CROSS APPLY: only people with a current WorkPermitItem (guarantees non-null ID for EF key).
                CROSS APPLY (
                    SELECT TOP 1
                        wpi.ID,
                        wpi.PassportID,
                        wpi.CurrentPositionHistoryID,
                        wpi.WorkPermitNumber,
                        wpi.StartDate,
                        wpi.ExpirationDate,
                        wpi.IsCancelled
                    FROM WorkPermitItems wpi
                    WHERE wpi.PersonID = p.ID
                      AND ISNULL(wpi.GCRecord, 0) = 0
                      AND wpi.StartDate IS NOT NULL
                      AND CAST(wpi.StartDate AS date) > CAST('0001-01-01' AS date)
                    ORDER BY wpi.StartDate DESC, wpi.ID DESC
                ) cur_wp
                LEFT JOIN Passports snap_pp
                    ON snap_pp.ID = cur_wp.PassportID AND ISNULL(snap_pp.GCRecord, 0) = 0
                OUTER APPLY (
                    SELECT TOP 1 pp.PassportNumber, pp.ExpirationDate
                    FROM Passports pp
                    WHERE pp.PersonID = p.ID
                      AND ISNULL(pp.GCRecord, 0) = 0
                      AND pp.IssueDate IS NOT NULL
                    ORDER BY pp.IssueDate DESC, pp.ID DESC
                ) cur_pp
                LEFT JOIN EmployeePositionHistories eph
                    ON eph.ID = cur_wp.CurrentPositionHistoryID AND ISNULL(eph.GCRecord, 0) = 0
                LEFT JOIN Positions pos
                    ON pos.ID = eph.PositionID AND ISNULL(pos.GCRecord, 0) = 0
                OUTER APPLY (
                    SELECT TOP 1 el.NameTm AS EducationLevelTm
                    FROM Educations e
                    LEFT JOIN EducationLevels el ON el.ID = e.EducationLevelID AND ISNULL(el.GCRecord, 0) = 0
                    WHERE e.PersonID = p.ID
                      AND ISNULL(e.GCRecord, 0) = 0
                    ORDER BY TRY_CAST(NULLIF(LTRIM(RTRIM(e.GraduationYear)), N'') AS int) DESC, e.ID DESC
                ) cur_edu
                OUTER APPLY (
                    SELECT TOP 1
                        CONCAT_WS(N', ',
                            NULLIF(LTRIM(RTRIM(reg.NameTm)), N''),
                            NULLIF(LTRIM(RTRIM(cit.NameTm)), N''),
                            NULLIF(LTRIM(RTRIM(
                                CASE
                                    WHEN a.Type = 0 THEN COALESCE(NULLIF(LTRIM(RTRIM(l.FullAddress)), N''), a.FullAddress)
                                    WHEN a.Type = 1 THEN COALESCE(NULLIF(LTRIM(RTRIM(h.Name)), N''), a.FullAddress)
                                    WHEN a.Type = 3 THEN COALESCE(NULLIF(LTRIM(RTRIM(hosp.Name)), N''), a.FullAddress)
                                    WHEN a.Type = 4 THEN COALESCE(NULLIF(LTRIM(RTRIM(osite.FullAddress)), N''), a.FullAddress)
                                    ELSE a.FullAddress
                                END
                            )), N'')
                        ) AS ResidenceAddress
                    FROM AddressesOfResidence a
                    LEFT JOIN Regions reg ON reg.ID = a.RegionID AND ISNULL(reg.GCRecord, 0) = 0
                    LEFT JOIN Cities cit ON cit.ID = a.CityID AND ISNULL(cit.GCRecord, 0) = 0
                    LEFT JOIN Lodgings l ON l.ID = a.LodgingID AND ISNULL(l.GCRecord, 0) = 0
                    LEFT JOIN Hotels h ON h.ID = a.HotelID AND ISNULL(h.GCRecord, 0) = 0
                    LEFT JOIN Hospitals hosp ON hosp.ID = a.HospitalID AND ISNULL(hosp.GCRecord, 0) = 0
                    LEFT JOIN OtherSites osite ON osite.ID = a.OtherSiteID AND ISNULL(osite.GCRecord, 0) = 0
                    WHERE a.PersonID = p.ID
                      AND ISNULL(a.GCRecord, 0) = 0
                    ORDER BY
                        CASE
                            WHEN a.ExpirationDate IS NULL
                              OR CAST(a.ExpirationDate AS date) >= CAST(GETDATE() AS date) THEN 0
                            ELSE 1
                        END,
                        CASE
                            WHEN a.ExpirationDate IS NULL
                              OR CAST(a.ExpirationDate AS date) >= CAST(GETDATE() AS date)
                                THEN ISNULL(a.ExpirationDate, CAST('9999-12-31' AS datetime2))
                            ELSE ISNULL(a.ExpirationDate, CAST('0001-01-01' AS datetime2))
                        END DESC,
                        a.ID DESC
                ) cur_addr
                OUTER APPLY (
                    SELECT TOP 1 v.VisaNumber, v.StartDate, v.ExpirationDate
                    FROM Passports pp
                    INNER JOIN Visas v ON v.PassportID = pp.ID AND ISNULL(v.GCRecord, 0) = 0
                    WHERE pp.PersonID = p.ID
                      AND ISNULL(pp.GCRecord, 0) = 0
                      AND ISNULL(v.IsCancelled, 0) = 0
                      AND v.StartDate IS NOT NULL
                      AND CAST(v.StartDate AS date) > CAST('0001-01-01' AS date)
                      AND CAST(v.StartDate AS date) <= CAST(GETDATE() AS date)
                    ORDER BY v.StartDate DESC, v.IssueDate DESC, v.ID DESC
                ) cur_visa
                WHERE ISNULL(p.GCRecord, 0) = 0
                  AND ISNULL(p.IsArchived, 0) = 0
            ", false); // do not swallow errors — stale Maglumat view causes SqlNullValueException on null ID
        }

        private void CreateViewVisaExtensionTracking()
        {
            // Create or Update the SQL View for VisaExtensionTracking.
            // This ensures the view exists for the VisaExtensionTracking Business Object.
            // Note: CREATE OR ALTER VIEW requires SQL Server 2016 SP1 or later.
            // IMPORTANT: Verify these table names match your database (EF Core usually pluralizes them).
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_VisaExtensionTracking] AS
                SELECT 
                    -- Concatenated Unique ID for EF Core Key
                    CONCAT(CAST(ai.ID AS VARCHAR(36)), '-', CAST(ap.ID AS VARCHAR(36))) AS ID,

                    -- Composite Key Components for EF Core
                    ai.ID AS ApplicationItemID,
                    ap.ID AS ApplicationProgressID,

                    -- Relationships
                    ai.ApplicationID,
                    ai.CurrentVisaID AS ExpiringVisaID,
                    ai.PersonID,
                    ai.CurrentPassportID AS PassportID,
                    
                    -- Data
                    a.ApplicationNumber,
                    a.ApplicationDate,
                    ap.StateID AS CurrentStateID, -- The state for this specific history row
                    ap.Date AS StatusDate,
                    ap.Description AS StatusDescription,
                    CASE
                        WHEN v.IsCancelled = 1 THEN 0
                        WHEN v.ExpirationDate IS NULL THEN 0
                        WHEN DATEDIFF(day, GETDATE(), v.ExpirationDate) < 0 THEN 0
                        ELSE DATEDIFF(day, GETDATE(), v.ExpirationDate)
                    END AS DaysRemainingOnVisa
                FROM ApplicationItems ai
                JOIN Applications a ON ai.ApplicationID = a.ID
                JOIN Visas v ON ai.CurrentVisaID = v.ID
                JOIN ApplicationProgresses ap ON a.ID = ap.ApplicationID -- Join all progress history
                WHERE 1 = 1            ", true); // 'true' ignores exceptions (useful if tables don't exist yet during initial create)
        }

        private void CreateViewVisaExtensionStatus()
        {
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_VisaExtensionStatus] AS
                SELECT
                    ai.ID,
                    ai.ApplicationID,
                    ai.CurrentVisaID        AS ExpiringVisaID,
                    ai.PersonID,
                    ai.CurrentPassportID    AS PassportID,
                    a.ApplicationNumber,
                    a.ApplicationDate,
                    latest_ap.StateID       AS CurrentStateID,
                    latest_ap.[Date]        AS StatusDate,
                    latest_ap.Description   AS StatusDescription,
                    CASE
                        WHEN v.IsCancelled = 1 THEN 0
                        WHEN v.ExpirationDate IS NULL THEN 0
                        WHEN DATEDIFF(day, GETDATE(), v.ExpirationDate) < 0 THEN 0
                        ELSE DATEDIFF(day, GETDATE(), v.ExpirationDate)
                    END AS DaysRemainingOnVisa,
                    (SELECT TOP 1 iv.ID FROM Visas iv
                     WHERE iv.IssuingApplicationItemId = ai.ID) AS IssuedVisaID,
                    (SELECT TOP 1 ri.ID
                     FROM Rejections r
                     JOIN RejectionItems ri ON ri.RejectionID = r.ID
                     WHERE r.ApplicationID = a.ID AND ri.PersonID = ai.PersonID) AS RejectionItemID
                FROM ApplicationItems ai
                JOIN Applications     a  ON ai.ApplicationID   = a.ID
                JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                LEFT JOIN Visas        v  ON ai.CurrentVisaID   = v.ID
                OUTER APPLY (
                    SELECT TOP 1 ap.StateID, ap.[Date], ap.Description
                    FROM ApplicationProgresses ap
                    WHERE ap.ApplicationID = a.ID
                    ORDER BY ap.[Date] DESC, ap.ID DESC
                ) latest_ap
                WHERE at.Name IN (
                      'App_Visa_Ext',
                      'App_Visa_Ext_According_to_WP',
                      'App_Visa_Ext_FM',
                      'App_Visa_and_WP_Ext'
                  )
            ", true);
        }

        private void CreateViewWorkPermitExtensionTracking()
        {
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_WorkPermitExtensionTracking] AS
                SELECT 
                    -- Concatenated Unique ID for EF Core Key
                    CONCAT(CAST(ai.ID AS VARCHAR(36)), '-', CAST(ap.ID AS VARCHAR(36))) AS ID,

                    -- Composite Key Components for EF Core
                    ai.ID AS ApplicationItemID,
                    ap.ID AS ApplicationProgressID,

                    -- Relationships
                    ai.ApplicationID,
                    ai.CurrentWorkPermitItemID AS ExpiringWorkPermitItemID,
                    ai.PersonID,
                    ai.CurrentPassportID AS PassportID,
                    
                    -- Data
                    a.ApplicationNumber,
                    a.ApplicationDate,
                    ap.StateID AS CurrentStateID,
                    ap.Date AS StatusDate,
                    ap.Description AS StatusDescription,
                    CASE
                        WHEN wpi.IsCancelled = 1 THEN 0
                        WHEN wpi.ExpirationDate IS NULL THEN 0
                        WHEN DATEDIFF(day, GETDATE(), wpi.ExpirationDate) < 0 THEN 0
                        ELSE DATEDIFF(day, GETDATE(), wpi.ExpirationDate)
                    END AS DaysRemaining
                FROM ApplicationItems ai
                JOIN Applications a ON ai.ApplicationID = a.ID
                JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                JOIN WorkPermitItems wpi ON ai.CurrentWorkPermitItemID = wpi.ID
                JOIN ApplicationProgresses ap ON a.ID = ap.ApplicationID -- Join all progress history
                WHERE 1 = 1 AND at.Name IN ('App_Visa_and_WP_Ext', 'App_WP_Ext')
            ", true);
        }

        private void CreateViewWorkPermitExtensionStatus()
        {
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_WorkPermitExtensionStatus] AS
                SELECT 
                    ai.ID,
                    ai.ApplicationID,
                    ai.CurrentWorkPermitItemID AS ExpiringWorkPermitItemID,
                    ai.PersonID,
                    ai.CurrentPassportID AS PassportID,
                    a.ApplicationNumber,
                    a.ApplicationDate,
                    latest_ap.StateID AS CurrentStateID,
                    latest_ap.[Date] AS StatusDate,
                    latest_ap.Description AS StatusDescription,
                    CASE
                        WHEN wpi.IsCancelled = 1 THEN 0
                        WHEN wpi.ExpirationDate IS NULL THEN 0
                        WHEN DATEDIFF(day, GETDATE(), wpi.ExpirationDate) < 0 THEN 0
                        ELSE DATEDIFF(day, GETDATE(), wpi.ExpirationDate)
                    END AS DaysRemaining
                FROM ApplicationItems ai
                JOIN Applications a ON ai.ApplicationID = a.ID
                JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                JOIN WorkPermitItems wpi ON ai.CurrentWorkPermitItemID = wpi.ID
                OUTER APPLY (
                    SELECT TOP 1 ap.StateID, ap.[Date], ap.Description
                    FROM ApplicationProgresses ap
                    WHERE ap.ApplicationID = a.ID
                    ORDER BY ap.[Date] DESC, ap.ID DESC
                ) latest_ap
                WHERE 1 = 1 AND at.Name IN ('App_Visa_and_WP_Ext', 'App_WP_Ext')
            ", true);
        }

        private void CreateViewVisaTransferStatus()
        {
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_VisaTransferStatus] AS
                SELECT
                    ai.ID,
                    ai.ApplicationID,
                    ai.CurrentVisaID        AS TransferredVisaID,
                    ai.PersonID,
                    ai.CurrentPassportID    AS PassportID,
                    a.ApplicationNumber,
                    a.ApplicationDate,
                    latest_ap.StateID       AS CurrentStateID,
                    latest_ap.[Date]        AS StatusDate,
                    latest_ap.Description   AS StatusDescription,
                    (SELECT TOP 1 iv.ID FROM Visas iv
                     WHERE iv.IssuingApplicationItemId = ai.ID) AS IssuedVisaID
                FROM ApplicationItems ai
                JOIN Applications     a  ON ai.ApplicationID   = a.ID
                JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                OUTER APPLY (
                    SELECT TOP 1 ap.StateID, ap.[Date], ap.Description
                    FROM ApplicationProgresses ap
                    WHERE ap.ApplicationID = a.ID
                    ORDER BY ap.[Date] DESC, ap.ID DESC
                ) latest_ap
                WHERE at.Name IN ('App_Change_Passport')
            ", true);
        }

        private void CreateViewVisaCancelExtStatus()
        {
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_VisaCancelExtStatus] AS
                SELECT
                    ai.ID,
                    ai.ApplicationID,
                    ai.CurrentVisaID        AS VisaID,
                    ai.PersonID,
                    ai.CurrentPassportID    AS PassportID,
                    a.ApplicationNumber,
                    a.ApplicationDate,
                    at.Name                 AS ApplicationTypeName,
                    latest_ap.StateID       AS CurrentStateID,
                    latest_ap.[Date]        AS StatusDate,
                    latest_ap.Description   AS StatusDescription,
                    CASE
                        WHEN v.IsCancelled = 1 THEN 0
                        WHEN v.ExpirationDate IS NULL THEN 0
                        WHEN DATEDIFF(day, GETDATE(), v.ExpirationDate) < 0 THEN 0
                        ELSE DATEDIFF(day, GETDATE(), v.ExpirationDate)
                    END AS DaysRemainingOnVisa,
                    -- Extension application for the same visa (if any)
                    (SELECT TOP 1 ext_a.ApplicationNumber
                     FROM ApplicationItems ext_ai
                     JOIN Applications     ext_a  ON ext_ai.ApplicationID   = ext_a.ID
                     JOIN ApplicationTypes ext_at ON ext_a.ApplicationTypeID = ext_at.ID
                     WHERE ext_ai.CurrentVisaID = ai.CurrentVisaID
                       AND ext_at.Name IN ('App_Visa_Ext','App_Visa_Ext_According_to_WP','App_Visa_Ext_FM','App_Visa_and_WP_Ext')
                     ORDER BY ext_a.ApplicationDate DESC) AS ExtApplicationNumber,
                    -- Extension application's current state ID (via OUTER APPLY on latest progress)
                    (SELECT TOP 1 ext_ast.ID
                     FROM ApplicationItems ext_ai2
                     JOIN Applications     ext_a2  ON ext_ai2.ApplicationID   = ext_a2.ID
                     JOIN ApplicationTypes ext_at2 ON ext_a2.ApplicationTypeID = ext_at2.ID
                     OUTER APPLY (SELECT TOP 1 ap2.StateID FROM ApplicationProgresses ap2
                                  WHERE ap2.ApplicationID = ext_a2.ID
                                  ORDER BY ap2.[Date] DESC, ap2.ID DESC) latest2
                     LEFT JOIN ApplicationStates ext_ast ON latest2.StateID = ext_ast.ID
                     WHERE ext_ai2.CurrentVisaID = ai.CurrentVisaID
                       AND ext_at2.Name IN ('App_Visa_Ext','App_Visa_Ext_According_to_WP','App_Visa_Ext_FM','App_Visa_and_WP_Ext')
                     ORDER BY ext_a2.ApplicationDate DESC) AS ExtCurrentStateID
                FROM ApplicationItems ai
                JOIN Applications     a  ON ai.ApplicationID   = a.ID
                JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                LEFT JOIN Visas        v  ON ai.CurrentVisaID   = v.ID
                OUTER APPLY (
                    SELECT TOP 1 ap.StateID, ap.[Date], ap.Description
                    FROM ApplicationProgresses ap
                    WHERE ap.ApplicationID = a.ID
                    ORDER BY ap.[Date] DESC, ap.ID DESC
                ) latest_ap
                WHERE at.Name IN ('App_Cancel_Visa_Ext', 'App_Cancel_Visa_and_WP_Ext')
            ", true);
        }

        private void CreateViewVisaCancellationStatus()
        {
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_VisaCancellationStatus] AS
                SELECT
                    ai.ID,
                    ai.ApplicationID,
                    ai.CurrentVisaID        AS VisaID,
                    ai.PersonID,
                    ai.CurrentPassportID    AS PassportID,
                    a.ApplicationNumber,
                    a.ApplicationDate,
                    at.Name                 AS ApplicationTypeName,
                    latest_ap.StateID       AS CurrentStateID,
                    latest_ap.[Date]        AS StatusDate,
                    latest_ap.Description   AS StatusDescription,
                    checkout.ApplicationNumber AS CheckOutApplicationNumber,
                    checkout_ap.StateID        AS CheckOutStateID
                FROM ApplicationItems ai
                JOIN Applications     a  ON ai.ApplicationID   = a.ID
                JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                OUTER APPLY (
                    SELECT TOP 1 ap.StateID, ap.[Date], ap.Description
                    FROM ApplicationProgresses ap
                    WHERE ap.ApplicationID = a.ID
                    ORDER BY ap.[Date] DESC, ap.ID DESC
                ) latest_ap
                OUTER APPLY (
                    SELECT TOP 1 co_a.ID AS co_AppID, co_a.ApplicationNumber
                    FROM ApplicationItems r
                    JOIN Applications     co_a  ON r.ApplicationID  = co_a.ID
                    JOIN ApplicationTypes co_at ON co_a.ApplicationTypeID = co_at.ID
                    WHERE r.PersonID     = ai.PersonID
                      AND co_at.Name    = 'App_Reg_Check_Out'
                      AND co_a.ApplicationDate >= a.ApplicationDate
                    ORDER BY co_a.ApplicationDate DESC
                ) checkout
                OUTER APPLY (
                    SELECT TOP 1 ap2.StateID
                    FROM ApplicationProgresses ap2
                    WHERE ap2.ApplicationID = checkout.co_AppID
                    ORDER BY ap2.[Date] DESC, ap2.ID DESC
                ) checkout_ap
                WHERE at.Name IN ('App_Cancel_Visa', 'App_Cancel_Visa_and_WP')
            ", true);
        }


        /// <summary>
        /// Report Dashboard passport rows (by-validity / by-type / by-citizenship labels).
        /// Soft-deleted and cancelled passports excluded. See SqlViews/vw_rd_passport.sql.
        /// </summary>
        private void CreateViewRdPassport()
        {
            // Latest passport per person by IssueDate. See SqlViews/vw_rd_passport.sql.
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[vw_rd_passport] AS
                SELECT
                    pp.ID                                                               AS ID,
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
                    COALESCE(pp.PassportNumber, N'')                                    AS PassportNumber,
                    pp.ExpirationDate                                                   AS ExpirationDate,
                    COALESCE(NULLIF(LTRIM(RTRIM(pt.NameTm)), N''), pt.Name, N'Unknown') AS TypeLabel,
                    COALESCE(NULLIF(LTRIM(RTRIM(nat.NameTm)), N''), nat.Name, N'Unknown') AS CitizenshipLabel,
                    CASE
                      WHEN pp.ExpirationDate IS NULL                                          THEN N'Pending'
                      WHEN CAST(pp.ExpirationDate AS date) <  CAST(GETDATE() AS date)         THEN N'Expired'
                      WHEN CAST(pp.ExpirationDate AS date) <= DATEADD(day, 30, CAST(GETDATE() AS date))
                                                                                               THEN N'Expiring (<30 days)'
                      WHEN CAST(pp.ExpirationDate AS date) <= DATEADD(day, 90, CAST(GETDATE() AS date))
                                                                                               THEN N'Valid (31-90 days)'
                      ELSE                                                                         N'Valid (>90 days)'
                    END                                                                 AS ValidityLabel,
                    CASE
                      WHEN pp.ExpirationDate IS NULL                                          THEN N'st-pending'
                      WHEN CAST(pp.ExpirationDate AS date) <  CAST(GETDATE() AS date)         THEN N'st-expiring'
                      WHEN CAST(pp.ExpirationDate AS date) <= DATEADD(day, 30, CAST(GETDATE() AS date))
                                                                                               THEN N'st-expiring'
                      WHEN CAST(pp.ExpirationDate AS date) <= DATEADD(day, 90, CAST(GETDATE() AS date))
                                                                                               THEN N'st-pending'
                      ELSE                                                                         N'st-approved'
                    END                                                                 AS ValidityCssClass,
                    CAST(ISNULL(p.IsArchived, 0) AS bit)                               AS IsArchived
                FROM (
                    SELECT
                        pp0.*,
                        ROW_NUMBER() OVER (
                            PARTITION BY pp0.PersonID
                            ORDER BY
                                CASE WHEN pp0.IssueDate IS NULL THEN 1 ELSE 0 END,
                                pp0.IssueDate DESC,
                                pp0.ID DESC
                        ) AS rn
                    FROM Passports pp0
                    WHERE ISNULL(pp0.GCRecord, 0) = 0
                      AND ISNULL(pp0.IsCancelled, 0) = 0
                ) pp
                INNER JOIN People p
                    ON p.ID = pp.PersonID
                   AND ISNULL(p.GCRecord, 0) = 0
                LEFT JOIN ProjectContracts pc
                    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
                LEFT JOIN People sp
                    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
                LEFT JOIN ProjectContracts spc
                    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
                LEFT JOIN PassportTypes pt
                    ON pt.ID = pp.PassportTypeID AND ISNULL(pt.GCRecord, 0) = 0
                LEFT JOIN Countries nat
                    ON nat.ID = p.NationalityID AND ISNULL(nat.GCRecord, 0) = 0
                WHERE pp.rn = 1
            ", true);
        }
        private void CreateFunctions()
        {
            // Create a Scalar Function to calculate days remaining
            // This acts like a Stored Procedure but can be used in Computed Columns
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER FUNCTION [dbo].[fn_CalculateDaysRemaining] (@ExpirationDate DATE)
                RETURNS INT
                AS
                BEGIN
                    IF @ExpirationDate IS NULL RETURN 0;
                    DECLARE @Days INT = DATEDIFF(day, GETDATE(), @ExpirationDate);
                    IF @Days < 0 RETURN 0;
                    RETURN @Days;
                END
            ", true);
        }

        private void CreateFunctionRegistrationState()
        {
            // Function to retrieve the Registration State for a Visa
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER FUNCTION [dbo].[fn_GetVisaRegistrationState] (@VisaID UNIQUEIDENTIFIER)
                RETURNS NVARCHAR(255)
                AS
                BEGIN
                    DECLARE @Result NVARCHAR(255);

                    SELECT TOP 1 @Result = ast.Name
                    FROM ApplicationItems ai
                    JOIN Applications a ON ai.ApplicationID = a.ID
                    JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                    OUTER APPLY (
                        SELECT TOP 1 ap.StateID
                        FROM ApplicationProgresses ap
                        WHERE ap.ApplicationID = a.ID
                        ORDER BY ap.[Date] DESC, ap.ID DESC
                    ) latest_ap
                    LEFT JOIN ApplicationStates ast ON latest_ap.StateID = ast.ID
                    WHERE ai.CurrentVisaID = @VisaID
                      AND at.Name IN ('App_Reg_Check_In', 'App_Reg_Info_Change', 'App_Reg_Check_Out', 'App_Reg_ext')
                    ORDER BY a.ApplicationDate DESC;

                    IF @Result IS NULL SET @Result = 'Not Registered';

                    RETURN @Result;
                END
            ", true);

            // 2. Bind the Computed Column manually
            // Since we removed HasComputedColumnSql from EF to prevent startup errors, we must apply the schema change here.
            ExecuteNonQueryCommand(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Visas')
                BEGIN
                    -- If the column exists and is NOT computed (meaning EF Core created it as a regular string column), drop it.
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Visas') AND name = 'RegistrationState' AND is_computed = 0)
                    BEGIN
                        ALTER TABLE Visas DROP COLUMN RegistrationState;
                    END

                    -- If the column does not exist (was dropped or never created), create it as a computed column.
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Visas') AND name = 'RegistrationState')
                    BEGIN
                        ALTER TABLE Visas ADD RegistrationState AS [dbo].[fn_GetVisaRegistrationState]([ID]);
                    END
                END
            ", true);
        }
    }
}