using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationProfilePicker;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfilePickerOpenHelper
{
    public static DetailView? CreatePickerView(
        XafApplication application,
        ApplicationProfilePickerOpenContext context,
        Frame? sourceFrame = null)
    {
        if (application == null || context == null)
            return null;

        ApplicationProfilePickerContextGate.Set(application, context, sourceFrame);

        var objectSpace = application.CreateObjectSpace(typeof(ApplicationProfilePickerHost));
        var host = objectSpace.CreateObject<ApplicationProfilePickerHost>();

        var detailView = application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.Edit;
        return detailView;
    }

    /// <summary>
    /// Retired Person/Dossier start path. Officers create instances only from Application Profile Instances lists.
    /// </summary>
    public static DetailView? CreatePersonStartPickerView(
        XafApplication application,
        Guid personId,
        bool stayOnSourceAfterCreate,
        Frame? sourceFrame = null)
    {
        if (application == null || personId == Guid.Empty)
            return null;

        return CreatePickerView(application, new ApplicationProfilePickerOpenContext
        {
            SeedPersonId = personId,
            StayOnSourceAfterCreate = stayOnSourceAfterCreate,
        }, sourceFrame);
    }

    public static ApplicationProfileInstanceProgressRouteKind? ResolveRouteFromListView(string? listViewId) =>
        listViewId switch
        {
            ApplicationProfileInstanceProgressRouteNavigation.ListViewViaMinistries =>
                ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ApplicationProfileInstanceProgressRouteNavigation.ListViewDirectMigration =>
                ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            _ => null,
        };
}
