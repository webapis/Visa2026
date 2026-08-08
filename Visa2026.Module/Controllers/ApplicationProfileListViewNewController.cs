using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileCatalog;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Replaces standard New on Application Profile ListViews with create + wizard.
/// </summary>
public sealed class ApplicationProfileListViewNewController : ViewController<ListView>
{
    public ApplicationProfileListViewNewController()
    {
        TargetObjectType = typeof(ApplicationProfile);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController?.NewObjectAction != null)
            newObjectController.NewObjectAction.Executing += NewObjectAction_Executing;
    }

    protected override void OnDeactivated()
    {
        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController?.NewObjectAction != null)
            newObjectController.NewObjectAction.Executing -= NewObjectAction_Executing;
        base.OnDeactivated();
    }

    private void NewObjectAction_Executing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (MigrationImportContext.IsDataImport)
            return;

        if (View.Id != null && View.Id.Contains("Lookup", System.StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;

        var wizardView = ApplicationProfileCatalogCreateHelper.CreateNewProfileAndOpenWizard(Application);
        if (wizardView == null)
            return;

        Application.ShowViewStrategy.ShowView(
            new ShowViewParameters(wizardView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(Frame, null));
    }
}