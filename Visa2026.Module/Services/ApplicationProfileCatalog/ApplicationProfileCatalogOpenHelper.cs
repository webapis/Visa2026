using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects.ApplicationProfileCatalog;

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

public static class ApplicationProfileCatalogOpenHelper
{
    public static DetailView? CreateCatalogView(XafApplication application)
    {
        if (application == null)
            return null;

        var objectSpace = application.CreateObjectSpace(typeof(ApplicationProfileCatalogHost));
        var host = objectSpace.CreateObject<ApplicationProfileCatalogHost>();
        var detailView = application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.View;
        return detailView;
    }
}