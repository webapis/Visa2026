using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the Import reimport history detail view with a non-persistent host object.
/// </summary>
public class ImportReimportHistoryNavigationController : WindowController
{
    private ShowNavigationItemController? _navigationController;

    protected override void OnActivated()
    {
        base.OnActivated();
        _navigationController = Frame.GetController<ShowNavigationItemController>();
        if (_navigationController != null)
            _navigationController.CustomShowNavigationItem += OnCustomShowNavigationItem;
    }

    protected override void OnDeactivated()
    {
        if (_navigationController != null)
            _navigationController.CustomShowNavigationItem -= OnCustomShowNavigationItem;
        _navigationController = null;
        base.OnDeactivated();
    }

    private void OnCustomShowNavigationItem(object sender, CustomShowNavigationItemEventArgs e)
    {
        if (e.ActionArguments.SelectedChoiceActionItem?.Id != "ImportReimportHistory")
            return;

        var objectSpace = Application.CreateObjectSpace(typeof(ImportReimportHistoryHost));
        var host = objectSpace.CreateObject<ImportReimportHistoryHost>();
        e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateDetailView(objectSpace, host);
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }
}
