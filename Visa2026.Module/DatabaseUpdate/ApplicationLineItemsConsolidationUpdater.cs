using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Registration and BusinessTrip lines are consolidated into <see cref="ApplicationItem"/>.
/// Ensures application types that used separate collections now use <see cref="ApplicationType.ShowApplicationItems"/>.
/// Remaps legacy <see cref="UserReportBoType"/> values on user templates.
/// </summary>
public class ApplicationLineItemsConsolidationUpdater : ModuleUpdater
{
    public ApplicationLineItemsConsolidationUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        foreach (var applicationType in ObjectSpace.GetObjectsQuery<ApplicationType>())
        {
            if (applicationType.ShowRegistrations || applicationType.ShowBusinessTrips)
                applicationType.ShowApplicationItems = true;
            // Keep ShowRegistrations / ShowBusinessTrips: they gate ApplicationItem line fields and app-level headers, not removed collections.
        }

        const int legacyRegistration = 2;
        const int legacyBusinessTrip = 3;
        const int legacyPerson = 4;

        foreach (var template in ObjectSpace.GetObjectsQuery<UserReportTemplate>())
        {
            var root = (int)template.RootBoType;
            if (root == legacyRegistration || root == legacyBusinessTrip)
                template.RootBoType = UserReportBoType.ApplicationItem;
            else if (root == legacyPerson)
                template.RootBoType = UserReportBoType.Person;
        }

        RewriteLegacyPdfFormMappingPaths();

        ObjectSpace.CommitChanges();
    }

    private void RewriteLegacyPdfFormMappingPaths()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.PdfFormMapping', N'U') IS NULL
    RETURN;

UPDATE dbo.PdfFormMapping
SET PropertyPath = STUFF(PropertyPath, 1, LEN(N'CurrentRegistration.'), N'')
WHERE PropertyPath LIKE N'CurrentRegistration.%';

UPDATE dbo.PdfFormMapping
SET PropertyPath = STUFF(PropertyPath, 1, LEN(N'CurrentBusinessTrip.'), N'')
WHERE PropertyPath LIKE N'CurrentBusinessTrip.%';

UPDATE dbo.PdfFormMapping
SET Expression = REPLACE(Expression, N'CurrentRegistration.', N'')
WHERE Expression LIKE N'%CurrentRegistration.%';

UPDATE dbo.PdfFormMapping
SET Expression = REPLACE(Expression, N'CurrentBusinessTrip.', N'')
WHERE Expression LIKE N'%CurrentBusinessTrip.%';", false);
    }
}
