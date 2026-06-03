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
            CreateFunctions();
            CreateFunctionRegistrationState();
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
                    DATEDIFF(day, GETDATE(), v.ExpirationDate) AS DaysRemainingOnVisa
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
                    DATEDIFF(day, GETDATE(), v.ExpirationDate) AS DaysRemainingOnVisa,
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
                  AND at.Name IN (
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
                    DATEDIFF(day, GETDATE(), wpi.ExpirationDate) AS DaysRemaining
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
                    DATEDIFF(day, GETDATE(), wpi.ExpirationDate) AS DaysRemaining
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
                  AND at.Name IN ('App_Change_Passport')
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
                    DATEDIFF(day, GETDATE(), v.ExpirationDate) AS DaysRemainingOnVisa,
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
                  AND at.Name IN ('App_Cancel_Visa_Ext', 'App_Cancel_Visa_and_WP_Ext')
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
                  AND at.Name IN ('App_Cancel_Visa', 'App_Cancel_Visa_and_WP')
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
                    RETURN DATEDIFF(day, GETDATE(), @ExpirationDate);
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