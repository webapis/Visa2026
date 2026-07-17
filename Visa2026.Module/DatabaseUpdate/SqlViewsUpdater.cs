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
            CreateViewRdWorkPermit();
            CreateViewRdVisaAppProgress();
            CreateViewRdProjects();
            CreateViewRdPersonRoles();
            CreateViewRdVisaState();
            CreateViewRdVisaByCategory();
            CreateViewRdVisaByType();
            CreateViewRdVisaByPeriod();
            CreateViewRdVisaByDaysRemaining();
            CreateViewRdApplication();
            CreateViewRdEducation();
            CreateViewRdEducationByCountry();
            CreateViewRdPositionHistory();
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
        /// One row per ApplicationItem with CurrentPassport; ApplicationDate for date filter.
        /// See SqlViews/vw_rd_passport.sql.
        /// </summary>
        private void CreateViewRdPassport()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: Passport category.
-- One row per ApplicationItem that references a CurrentPassport.
-- Date filter (dashboard top-right) applies to Applications.ApplicationDate in the C# loader.
CREATE OR ALTER VIEW [dbo].[vw_rd_passport] AS
SELECT
    ai.ID                                                               AS ID,
    pp.ID                                                               AS PassportOid,
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
    pp.IssueDate                                                        AS IssueDate,
    pp.ExpirationDate                                                   AS ExpirationDate,
    a.ApplicationDate                                                   AS ApplicationDate,
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
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                    AS IsArchived
FROM ApplicationItems ai
INNER JOIN Applications a
    ON a.ID = ai.ApplicationID
   AND ISNULL(a.GCRecord, 0) = 0
INNER JOIN Passports pp
    ON pp.ID = ai.CurrentPassportID
   AND ISNULL(pp.GCRecord, 0) = 0
INNER JOIN People p
    ON p.ID = ai.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = COALESCE(a.ProjectContractID, p.ProjectContractID)
   AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
LEFT JOIN PassportTypes pt
    ON pt.ID = pp.PassportTypeID AND ISNULL(pt.GCRecord, 0) = 0
LEFT JOIN Countries nat
    ON nat.ID = p.NationalityID AND ISNULL(nat.GCRecord, 0) = 0
WHERE ISNULL(ai.GCRecord, 0) = 0
  AND ai.CurrentPassportID IS NOT NULL
            ", true);
        }
        /// <summary>
        /// Report Dashboard work-permit rows (by-days-remaining). See SqlViews/vw_rd_work_permit.sql.
        /// </summary>
        private void CreateViewRdWorkPermit()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: valid WorkPermitItems by days remaining (By Days Remaining).
-- One row per valid (non-cancelled, not expired) item; persons may appear more than once.
-- Buckets: < 10 days / < 1 month / < 3..6 months / ≥ 6 months.
CREATE OR ALTER VIEW [dbo].[vw_rd_work_permit] AS
SELECT
    wpi.ID                                                              AS ID,
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
    COALESCE(NULLIF(LTRIM(RTRIM(wpi.WorkPermitNumber)), N''), NULLIF(LTRIM(RTRIM(wpi.ASNumber)), N''), N'') AS WorkPermitNumber,
    CASE WHEN CAST(wpi.ExpirationDate AS date) > '1900-01-01' THEN wpi.ExpirationDate ELSE NULL END AS ExpirationDate,
    DATEDIFF(day, CAST(GETDATE() AS date), CAST(wpi.ExpirationDate AS date)) AS DaysRemaining,
    CASE
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(wpi.ExpirationDate AS date)) < 10  THEN N'< 10 days'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(wpi.ExpirationDate AS date)) < 30  THEN N'< 1 month'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(wpi.ExpirationDate AS date)) < 90  THEN N'< 3 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(wpi.ExpirationDate AS date)) < 120 THEN N'< 4 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(wpi.ExpirationDate AS date)) < 150 THEN N'< 5 months'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(wpi.ExpirationDate AS date)) < 180 THEN N'< 6 months'
        ELSE N'≥ 6 months'
    END                                                                 AS ValidityLabel,
    CASE
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(wpi.ExpirationDate AS date)) < 30  THEN N'st-expiring'
        WHEN DATEDIFF(day, CAST(GETDATE() AS date), CAST(wpi.ExpirationDate AS date)) < 90  THEN N'st-pending'
        ELSE N'st-approved'
    END                                                                 AS ValidityCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM WorkPermitItems wpi
INNER JOIN People p
    ON p.ID = wpi.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(wpi.GCRecord, 0) = 0
  AND ISNULL(wpi.IsCancelled, 0) = 0
  AND wpi.PersonID IS NOT NULL
  AND wpi.ExpirationDate IS NOT NULL
  AND CAST(wpi.ExpirationDate AS date) >= CAST(GETDATE() AS date);
", true);
        }

        /// <summary>
        /// Report Dashboard visa application progress. See SqlViews/vw_rd_visa_app_progress.sql.
        /// </summary>
        private void CreateViewRdVisaAppProgress()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: Visa — Application Progress (app-progress sub-report).
-- One row per ApplicationItem on visa-extension application types with CurrentVisa set.
-- Progress state = latest ApplicationProgress.State for the parent Application.
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_app_progress] AS
SELECT
    ai.ID                                                               AS ID,
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
    COALESCE(
        NULLIF(LTRIM(RTRIM(a.FullApplicationNumber)), N''),
        NULLIF(LTRIM(RTRIM(a.ApplicationNumber)), N''),
        N''
    )                                                                   AS ApplicationNumber,
    a.ApplicationDate                                                   AS ApplicationDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(ast.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(ast.Name)), N''),
        N'Being Prepared'
    )                                                                   AS ProgressStateLabel,
    CASE
      WHEN ast.Code IN (N'PROCESS_ISSUED', N'1_REVIEW_APPROVED', N'2_REVIEW_APPROVED')
                                                                              THEN N'st-approved'
      WHEN ast.Code IN (N'PROCESS_REJECTED', N'PROCESS_CANCELLED', N'1_REVIEW_REJECTED', N'2_REVIEW_REJECTED')
                                                                              THEN N'st-expiring'
      ELSE                                                                          N'st-pending'
    END                                                                 AS ProgressStateCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM ApplicationItems ai
INNER JOIN Applications a
    ON a.ID = ai.ApplicationID
   AND ISNULL(a.GCRecord, 0) = 0
INNER JOIN ApplicationTypes at
    ON at.ID = a.ApplicationTypeID
   AND ISNULL(at.GCRecord, 0) = 0
INNER JOIN People p
    ON p.ID = ai.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = COALESCE(a.ProjectContractID, p.ProjectContractID)
   AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID
   AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sp.ProjectContractID
   AND ISNULL(spc.GCRecord, 0) = 0
OUTER APPLY (
    SELECT TOP 1 ap.StateID
    FROM ApplicationProgresses ap
    WHERE ap.ApplicationID = a.ID
      AND ISNULL(ap.GCRecord, 0) = 0
    ORDER BY ap.[Date] DESC, ap.ID DESC
) latest_ap
LEFT JOIN ApplicationStates ast
    ON ast.ID = latest_ap.StateID
   AND ISNULL(ast.GCRecord, 0) = 0
WHERE ISNULL(ai.GCRecord, 0) = 0
  AND ai.CurrentVisaID IS NOT NULL
  AND at.Name IN (
        N'App_Visa_Ext',
        N'App_Visa_Ext_According_to_WP',
        N'App_Visa_Ext_FM',
        N'App_Visa_and_WP_Ext'
    );
", true);
        }

        /// <summary>
        /// Report Dashboard project chips. See SqlViews/vw_rd_projects.sql.
        /// </summary>
        private void CreateViewRdProjects()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: project chips (people per ProjectContract, by PersonRole).
-- Effective project = Person.ProjectContract, else SponsoringEmployee.ProjectContract (family).
-- Soft-delete / archived people excluded. Count 0 projects omitted by GROUP BY.
-- ProjectContracts use NameTm only (Name column dropped).
CREATE OR ALTER VIEW [dbo].[vw_rd_projects] AS
SELECT
    pc.ID                                                               AS ProjectOid,
    p.PersonRole                                                        AS PersonRoleCode,
    COALESCE(NULLIF(LTRIM(RTRIM(pc.NameTm)), N''), N'')                 AS ProjectNameTm,
    COALESCE(NULLIF(LTRIM(RTRIM(pc.NameTm)), N''), N'')                 AS ProjectNameRaw,
    COUNT_BIG(*)                                                        AS PersonCount
FROM People p
LEFT JOIN People sp
    ON sp.ID = p.SponsoringEmployeeID
   AND ISNULL(sp.GCRecord, 0) = 0
INNER JOIN ProjectContracts pc
    ON pc.ID = COALESCE(p.ProjectContractID, sp.ProjectContractID)
   AND ISNULL(pc.GCRecord, 0) = 0
WHERE ISNULL(p.GCRecord, 0) = 0
  AND ISNULL(p.IsArchived, 0) = 0
  AND COALESCE(p.ProjectContractID, sp.ProjectContractID) IS NOT NULL
GROUP BY
    pc.ID,
    p.PersonRole,
    COALESCE(NULLIF(LTRIM(RTRIM(pc.NameTm)), N''), N'');
", true);
        }

        /// <summary>
        /// Report Dashboard person-role counts. See SqlViews/vw_rd_person_roles.sql.
        /// </summary>
        private void CreateViewRdPersonRoles()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: person-type tab counts (Employees / Family / Temporary Visitors).
-- Non-archived people only; all people in role (project optional).
CREATE OR ALTER VIEW [dbo].[vw_rd_person_roles] AS
SELECT
    p.PersonRole                                                        AS PersonRoleCode,
    COUNT_BIG(*)                                                        AS PersonCount
FROM People p
WHERE ISNULL(p.GCRecord, 0) = 0
  AND ISNULL(p.IsArchived, 0) = 0
GROUP BY p.PersonRole;
", true);
        }

        /// <summary>
        /// Report Dashboard visa state. See SqlViews/vw_rd_visa_state.sql.
        /// </summary>
        private void CreateViewRdVisaState()
        {
            ExecuteNonQueryCommand(@"
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
", true);
        }

        /// <summary>
        /// Report Dashboard visas by category. See SqlViews/vw_rd_visa_by_category.sql.
        /// </summary>
        private void CreateViewRdVisaByCategory()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: valid visas by VisaCategory only (not Visa State).
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_by_category] AS
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
    COALESCE(NULLIF(LTRIM(RTRIM(vc.NameTm)), N''), NULLIF(LTRIM(RTRIM(vc.Name)), N''), N'Unknown') AS CategoryLabel,
    COALESCE(NULLIF(LTRIM(RTRIM(vc.NameTm)), N''), NULLIF(LTRIM(RTRIM(vc.Name)), N''), N'Unknown') AS StatusLabel,
    N'st-cat-1' AS StatusCssClass,
    CAST(ISNULL(p.IsArchived, 0) AS bit) AS IsArchived
FROM Visas v
INNER JOIN Passports pp ON pp.ID = v.PassportID AND ISNULL(pp.GCRecord, 0) = 0
INNER JOIN People p ON p.ID = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN VisaCategories vc ON vc.ID = v.VisaCategoryID AND ISNULL(vc.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sp ON sp.ID = p.SponsoringEmployeeID AND ISNULL(sp.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
WHERE ISNULL(v.GCRecord, 0) = 0
  AND ISNULL(v.IsCancelled, 0) = 0
  AND v.ExpirationDate IS NOT NULL
  AND CAST(v.ExpirationDate AS date) >= CAST(GETDATE() AS date);
", true);
        }

        /// <summary>
        /// Report Dashboard visas by type. See SqlViews/vw_rd_visa_by_type.sql.
        /// </summary>
        private void CreateViewRdVisaByType()
        {
            ExecuteNonQueryCommand(@"
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
", true);
        }

        /// <summary>
        /// Report Dashboard visas by period. See SqlViews/vw_rd_visa_by_period.sql.
        /// </summary>
        private void CreateViewRdVisaByPeriod()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: valid visas by nearest granted period (StartDate → ExpirationDate).
CREATE OR ALTER VIEW [dbo].[vw_rd_visa_by_period] AS
SELECT
    x.ID,
    x.PersonOid,
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
    x.IsArchived
FROM (
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
        CAST(ISNULL(p.IsArchived, 0) AS bit) AS IsArchived
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
", true);
        }

        /// <summary>
        /// Report Dashboard visas by days remaining. See SqlViews/vw_rd_visa_by_days_remaining.sql.
        /// </summary>
        private void CreateViewRdVisaByDaysRemaining()
        {
            ExecuteNonQueryCommand(@"
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
", true);
        }
        /// <summary>
        /// Report Dashboard Application category. See SqlViews/vw_rd_application.sql.
        /// </summary>
        private void CreateViewRdApplication()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: Application category (by-progress / by-type).
-- One row per header Application; progress = latest ApplicationProgress; type = ApplicationTypes.
CREATE OR ALTER VIEW [dbo].[vw_rd_application] AS
SELECT
    a.ID                                                                AS ID,
    first_p.ID                                                          AS PersonOid,
    COALESCE(
        NULLIF(CONCAT_WS(N' ',
            NULLIF(LTRIM(RTRIM(first_p.FirstName)), N''),
            NULLIF(LTRIM(RTRIM(first_p.MiddleName)), N''),
            NULLIF(LTRIM(RTRIM(first_p.LastName)), N'')
        ), N''),
        NULLIF(LTRIM(RTRIM(a.FullApplicationNumber)), N''),
        NULLIF(LTRIM(RTRIM(a.ApplicationNumber)), N''),
        N''
    )                                                                   AS PersonName,
    COALESCE(
        NULLIF(LTRIM(RTRIM(pc.NameTm)), N''),
        N''
    )                                                                   AS ProjectName,
    COALESCE(pc.NameTm, N'')                                            AS ProjectNameRaw,
    COALESCE(pc.NameTm, N'')                                            AS ProjectNameTm,
    COALESCE(first_p.PersonRole, 0)                                     AS PersonRoleCode,
    COALESCE(
        NULLIF(LTRIM(RTRIM(a.FullApplicationNumber)), N''),
        NULLIF(LTRIM(RTRIM(a.ApplicationNumber)), N''),
        N''
    )                                                                   AS ApplicationNumber,
    a.ApplicationDate                                                   AS ApplicationDate,
    COALESCE(
        NULLIF(LTRIM(RTRIM(ast.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(ast.Name)), N''),
        N'Being Prepared'
    )                                                                   AS ProgressStateLabel,
    CASE
      WHEN ast.Code IN (N'PROCESS_ISSUED', N'1_REVIEW_APPROVED', N'2_REVIEW_APPROVED')
                                                                              THEN N'st-approved'
      WHEN ast.Code IN (N'PROCESS_REJECTED', N'PROCESS_CANCELLED', N'1_REVIEW_REJECTED', N'2_REVIEW_REJECTED')
                                                                              THEN N'st-expiring'
      ELSE                                                                          N'st-pending'
    END                                                                 AS ProgressStateCssClass,
    COALESCE(ast.Code, N'')                                             AS ProgressStateCode,`r`n    COALESCE(
        NULLIF(LTRIM(RTRIM(at.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(at.Name)), N''),
        N'Unknown'
    )                                                                   AS TypeLabel,
    CAST(ISNULL(first_p.IsArchived, 0) AS bit)                          AS IsArchived
FROM Applications a
LEFT JOIN ApplicationTypes at
    ON at.ID = a.ApplicationTypeID
   AND ISNULL(at.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = a.ProjectContractID
   AND ISNULL(pc.GCRecord, 0) = 0
OUTER APPLY (
    SELECT TOP 1 ap.StateID
    FROM ApplicationProgresses ap
    WHERE ap.ApplicationID = a.ID
      AND ISNULL(ap.GCRecord, 0) = 0
    ORDER BY ap.[Date] DESC, ap.ID DESC
) latest_ap
LEFT JOIN ApplicationStates ast
    ON ast.ID = latest_ap.StateID
   AND ISNULL(ast.GCRecord, 0) = 0
OUTER APPLY (
    SELECT TOP 1 ai.PersonID
    FROM ApplicationItems ai
    WHERE ai.ApplicationID = a.ID
      AND ISNULL(ai.GCRecord, 0) = 0
    ORDER BY ai.ID
) first_ai
LEFT JOIN People first_p
    ON first_p.ID = first_ai.PersonID
   AND ISNULL(first_p.GCRecord, 0) = 0
WHERE ISNULL(a.GCRecord, 0) = 0;
", true);
        }

        /// <summary>
        /// Report Dashboard Education category. See SqlViews/vw_rd_education.sql.
        /// </summary>
        private void CreateViewRdEducation()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: Education category (by-level / by-country / by-specialty).
-- One row per Education; person may appear more than once.
CREATE OR ALTER VIEW [dbo].[vw_rd_education] AS
SELECT
    e.ID                                                                AS ID,
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
    COALESCE(
        NULLIF(LTRIM(RTRIM(ei.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(ei.Name)), N''),
        N''
    )                                                                   AS InstitutionName,
    COALESCE(NULLIF(LTRIM(RTRIM(e.GraduationYear)), N''), N'')          AS GraduationYear,
    COALESCE(
        NULLIF(LTRIM(RTRIM(el.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(el.Name)), N''),
        N'Unknown'
    )                                                                   AS LevelLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(c.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(c.Name)), N''),
        N'Unknown'
    )                                                                   AS CountryLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(sp.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(sp.Name)), N''),
        N'Unknown'
    )                                                                   AS SpecialtyLabel,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM Educations e
INNER JOIN People p
    ON p.ID = e.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sponsor
    ON sponsor.ID = p.SponsoringEmployeeID AND ISNULL(sponsor.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sponsor.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
LEFT JOIN EducationLevels el
    ON el.ID = e.EducationLevelID AND ISNULL(el.GCRecord, 0) = 0
LEFT JOIN EducationInstitutions ei
    ON ei.ID = e.EducationInstitutionID AND ISNULL(ei.GCRecord, 0) = 0
LEFT JOIN Countries c
    ON c.ID = e.EducationCountryID AND ISNULL(c.GCRecord, 0) = 0
LEFT JOIN Specialties sp
    ON sp.ID = e.SpecialtyID AND ISNULL(sp.GCRecord, 0) = 0
WHERE ISNULL(e.GCRecord, 0) = 0;
", true);
        }

        /// <summary>
        /// Report Dashboard Education by-country dedicated view.
        /// Education country label for By Country sub-report loading.
        /// </summary>
        private void CreateViewRdEducationByCountry()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: Education by-country (education country only).
-- Dedicated view for Education ""By Country"" sub-report to keep query path lean.
CREATE OR ALTER VIEW [dbo].[vw_rd_education_by_country] AS
SELECT
    e.ID                                                                AS ID,
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
    COALESCE(
        NULLIF(LTRIM(RTRIM(ei.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(ei.Name)), N''),
        N''
    )                                                                   AS InstitutionName,
    COALESCE(NULLIF(LTRIM(RTRIM(e.GraduationYear)), N''), N'')          AS GraduationYear,
    COALESCE(
        NULLIF(LTRIM(RTRIM(c.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(c.Name)), N''),
        N'Unknown'
    )                                                                   AS CountryLabel,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM Educations e
INNER JOIN People p
    ON p.ID = e.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sponsor
    ON sponsor.ID = p.SponsoringEmployeeID AND ISNULL(sponsor.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sponsor.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
LEFT JOIN EducationInstitutions ei
    ON ei.ID = e.EducationInstitutionID AND ISNULL(ei.GCRecord, 0) = 0
LEFT JOIN Countries c
    ON c.ID = e.EducationCountryID AND ISNULL(c.GCRecord, 0) = 0
WHERE ISNULL(e.GCRecord, 0) = 0;
", true);
        }

        /// <summary>
        /// Report Dashboard Position History category. See SqlViews/vw_rd_position_history.sql.
        /// </summary>
        private void CreateViewRdPositionHistory()
        {
            ExecuteNonQueryCommand(@"
-- Report Dashboard: Position History (by-status / by-position).
-- One row per EmployeePositionHistory.
CREATE OR ALTER VIEW [dbo].[vw_rd_position_history] AS
SELECT
    eph.ID                                                              AS ID,
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
    COALESCE(
        NULLIF(LTRIM(RTRIM(pos.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pos.Name)), N''),
        N'Unknown'
    )                                                                   AS PositionName,
    eph.StartDate                                                       AS StartDate,
    CASE
      WHEN eph.EndDate IS NULL
        OR CAST(eph.EndDate AS date) >= CAST(GETDATE() AS date)
                                                                              THEN N'Current'
      ELSE                                                                        N'Ended'
    END                                                                 AS StatusLabel,
    CASE
      WHEN eph.EndDate IS NULL
        OR CAST(eph.EndDate AS date) >= CAST(GETDATE() AS date)
                                                                              THEN N'st-approved'
      ELSE                                                                        N'st-pending'
    END                                                                 AS StatusCssClass,
    COALESCE(
        NULLIF(LTRIM(RTRIM(pos.NameTm)), N''),
        NULLIF(LTRIM(RTRIM(pos.Name)), N''),
        N'Unknown'
    )                                                                   AS PositionLabel,
    COALESCE(
        NULLIF(LTRIM(RTRIM(ap.Name)), N''),
        N'Unknown'
    )                                                                   AS ActualPositionLabel,
    CAST(ISNULL(p.IsArchived, 0) AS bit)                                AS IsArchived
FROM EmployeePositionHistories eph
INNER JOIN People p
    ON p.ID = eph.PersonID
   AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc
    ON pc.ID = p.ProjectContractID AND ISNULL(pc.GCRecord, 0) = 0
LEFT JOIN People sponsor
    ON sponsor.ID = p.SponsoringEmployeeID AND ISNULL(sponsor.GCRecord, 0) = 0
LEFT JOIN ProjectContracts spc
    ON spc.ID = sponsor.ProjectContractID AND ISNULL(spc.GCRecord, 0) = 0
LEFT JOIN Positions pos
    ON pos.ID = eph.PositionID AND ISNULL(pos.GCRecord, 0) = 0
LEFT JOIN ActualPositions ap
    ON ap.ID = eph.ActualPositionID AND ISNULL(ap.GCRecord, 0) = 0
WHERE ISNULL(eph.GCRecord, 0) = 0;
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