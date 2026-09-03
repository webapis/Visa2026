using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.OrganizationCatalogs;

namespace Visa2026.Module.Services.OrganizationCatalogs;

public static class OrganizationCatalogsOpenHelper
{
    public static DetailView? CreateCatalogView(XafApplication application)
    {
        if (application == null)
            return null;

        var objectSpace = application.CreateObjectSpace(typeof(OrganizationCatalogsHost));
        var host = objectSpace.CreateObject<OrganizationCatalogsHost>();
        var detailView = application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.View;
        return detailView;
    }

    public static bool TryShow(XafApplication application, Action? onClosed = null)
    {
        var catalogView = CreateCatalogView(application);
        if (catalogView == null)
            return false;

        if (onClosed != null)
            catalogView.Closed += (_, _) => onClosed();

        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(catalogView) { TargetWindow = TargetWindow.NewModalWindow },
            new ShowViewSource(application.MainWindow, null));
        return true;
    }

    public static bool TryOpenEditor(
        XafApplication application,
        string kind,
        Guid id,
        Action? onClosed = null,
        Action<Guid>? onSaved = null)
    {
        if (application == null)
            return false;

        var type = ResolveType(kind);
        if (type == null)
            return false;

        var objectSpace = application.CreateObjectSpace(type);
        object target;
        if (id == Guid.Empty)
        {
            target = objectSpace.CreateObject(type);
        }
        else
        {
            target = objectSpace.GetObjectByKey(type, id);
            if (target == null)
                return false;
        }

        var detailView = application.CreateDetailView(objectSpace, target, isRoot: true);
        detailView.ViewEditMode = ViewEditMode.Edit;

        if (onSaved != null)
        {
            objectSpace.Committed += (_, _) =>
            {
                if (target is BaseObject saved && saved.ID != Guid.Empty)
                    onSaved(saved.ID);
            };
        }

        if (onClosed != null)
            detailView.Closed += (_, _) => onClosed();

        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.NewModalWindow },
            new ShowViewSource(application.MainWindow, null));
        return true;
    }

    private static Type? ResolveType(string kind) =>
        kind switch
        {
            OrganizationCatalogHelper.Company => typeof(CompanyProfile),
            OrganizationCatalogHelper.Signatory => typeof(AuthorizedSignatory),
            OrganizationCatalogHelper.Representative => typeof(AuthorizedRepresentative),
            _ => null
        };
}
