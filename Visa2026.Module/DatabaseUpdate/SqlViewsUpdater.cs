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
                    ai.ID,
                    ai.ID AS ApplicationItemID,
                    ai.ApplicationID,
                    ai.CurrentVisaID AS ExpiringVisaID,
                    ai.PersonID,
                    ai.CurrentPassportID AS PassportID,
                    a.ApplicationNumber,
                    a.ApplicationDate,
                    ap.StateID AS CurrentStateID,
                    ap.Date AS StatusDate,
                    DATEDIFF(day, GETDATE(), v.ExpirationDate) AS DaysRemainingOnVisa
                FROM ApplicationItems ai
                JOIN Applications a ON ai.ApplicationID = a.ID
                JOIN Visas v ON ai.CurrentVisaID = v.ID
                LEFT JOIN ApplicationProgresses ap ON a.CurrentStateID = ap.ID
                WHERE a.IsDeleted = 0 AND ai.IsDeleted = 0
            ", true); // 'true' ignores exceptions (useful if tables don't exist yet during initial create)
        }
    }
}