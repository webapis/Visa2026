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
                JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                JOIN Visas v ON ai.CurrentVisaID = v.ID
                JOIN ApplicationProgresses ap ON a.ID = ap.ApplicationID -- Join all progress history
                WHERE a.IsDeleted = 0 AND ai.IsDeleted = 0
                  AND at.Name IN ('App_Visa_Ext', 'App_Visa_Ext_FM', 'App_Visa_and_WP_Ext', 'App_Visa_Ext_According_to_WP')
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
                JOIN ApplicationTypes at ON a.ApplicationTypeID = at.ID
                JOIN Visas v ON ai.CurrentVisaID = v.ID
                LEFT JOIN ApplicationProgresses ap ON a.CurrentStateID = ap.ID
                WHERE a.IsDeleted = 0 AND ai.IsDeleted = 0
                  AND at.Name IN ('App_Visa_Ext', 'App_Visa_Ext_FM', 'App_Visa_and_WP_Ext', 'App_Visa_Ext_According_to_WP')
            ", true);
        }

        private void CreateViewWorkPermitExtensionTracking()
        {
            ExecuteNonQueryCommand(@"
                CREATE OR ALTER VIEW [dbo].[View_WorkPermitExtensionTracking] AS
                SELECT 
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
    }
}