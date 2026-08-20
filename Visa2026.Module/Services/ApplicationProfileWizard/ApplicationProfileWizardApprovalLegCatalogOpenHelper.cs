using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>
/// Opens the shared Approval leg profile catalog (Configuration) from the Application Profile wizard.
/// </summary>
public static class ApplicationProfileWizardApprovalLegCatalogOpenHelper
{
    public static bool TryOpen(XafApplication application, Action? onClosed = null)
    {
        if (application == null)
            return false;

        var objectSpace = application.CreateObjectSpace(typeof(ApprovalLegProfile));
        var listView = application.CreateListView(objectSpace, typeof(ApprovalLegProfile), true);

        if (onClosed != null)
        {
            EventHandler? committed = null;
            committed = (_, _) =>
            {
                objectSpace.Committed -= committed;
                onClosed();
            };
            objectSpace.Committed += committed;
            listView.Closed += (_, _) => onClosed();
        }

        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(listView) { TargetWindow = TargetWindow.NewModalWindow },
            new ShowViewSource(application.MainWindow, null));
        return true;
    }
}