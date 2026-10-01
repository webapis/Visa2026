using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Opens the shared other-invitation-places catalog so an officer can add a missing address
/// while creating or editing a case.
/// </summary>
public static class AlternativeAddressCatalogOpenHelper
{
    public static bool TryOpenNew(XafApplication application, Action<Guid>? onSaved = null)
    {
        if (application == null)
            return false;

        var objectSpace = application.CreateObjectSpace(typeof(AlternativeAddressesForInvitation));
        var target = objectSpace.CreateObject<AlternativeAddressesForInvitation>();
        var detailView = application.CreateDetailView(objectSpace, target, isRoot: true);
        detailView.ViewEditMode = ViewEditMode.Edit;

        if (onSaved != null)
        {
            objectSpace.Committed += (_, _) =>
            {
                if (target.ID != Guid.Empty)
                    onSaved(target.ID);
            };
        }

        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.NewModalWindow },
            new ShowViewSource(application.MainWindow, null));
        return true;
    }
}
