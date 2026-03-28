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
                WHERE a.IsDeleted = 0 AND ai.IsDeleted = 0
            ", true); // 'true' ignores exceptions (useful if tables don't exist yet during initial create)
        }

        private void CreateViewVisaExtensionStatus()
        {
            // View for showing only the CURRENT status of extensions
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_VisaExtensionStatus] AS
                SELECT 
                    ai.ID,
                    ai.ApplicationID,
                    ai.CurrentVisaID AS ExpiringVisaID,
                    ai.PersonID,
                    ai.CurrentPassportID AS PassportID,
                    a.ApplicationNumber,
                    a.ApplicationDate,
                    ap.StateID AS CurrentStateID,
                    ap.Date AS StatusDate,
                    ap.Description AS StatusDescription,
                    DATEDIFF(day, GETDATE(), v.ExpirationDate) AS DaysRemainingOnVisa
                FROM ApplicationItems ai
                JOIN Applications a ON ai.ApplicationID = a.ID
                JOIN Visas v ON ai.CurrentVisaID = v.ID
                LEFT JOIN ApplicationProgresses ap ON a.CurrentStateID = ap.ID
                WHERE a.IsDeleted = 0 AND ai.IsDeleted = 0
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
                WHERE a.IsDeleted = 0 AND ai.IsDeleted = 0
                  AND at.Name IN ('App_Visa_and_WP_Ext', 'App_WP_Ext')
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
                    ap.StateID AS CurrentStateID,
                    ap.Date AS StatusDate,
                    ap.Description AS StatusDescription,
                    DATEDIFF(day, GETDATE(), wpi.ExpirationDate) AS DaysRemaining
                FROM ApplicationItems ai
                JOIN Applications a ON ai.ApplicationID = a.ID
                JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                JOIN WorkPermitItems wpi ON ai.CurrentWorkPermitItemID = wpi.ID
                LEFT JOIN ApplicationProgresses ap ON a.CurrentStateID = ap.ID
                WHERE a.IsDeleted = 0 AND ai.IsDeleted = 0
                  AND at.Name IN ('App_Visa_and_WP_Ext', 'App_WP_Ext')
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
                    LEFT JOIN ApplicationProgresses ap ON a.CurrentStateID = ap.ID
                    LEFT JOIN ApplicationStates ast ON ap.StateID = ast.ID
                    WHERE ai.CurrentVisaID = @VisaID
                      AND a.IsDeleted = 0
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