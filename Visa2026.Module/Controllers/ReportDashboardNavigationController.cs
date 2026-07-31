using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.ReportDashboard;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the Report Dashboard detail view with a non-persistent host object.
/// </summary>
public class ReportDashboardNavigationController : WindowController
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
        if (e.ActionArguments.SelectedChoiceActionItem?.Id != "ReportDashboard")
            return;

        var objectSpace = Application.CreateObjectSpace(typeof(ReportDashboardHost));
        var host = objectSpace.CreateObject<ReportDashboardHost>();
        e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateDetailView(objectSpace, host);
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }
}
