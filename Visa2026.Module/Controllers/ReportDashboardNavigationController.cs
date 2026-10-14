using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects.ReportDashboard;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the Report Dashboard on the folder (Overview) or a category child.
/// A second click while the dashboard is already showing switches the report in place.
/// </summary>
public class ReportDashboardNavigationController : WindowController
{
    private ShowNavigationItemController? _navigationController;

    public ReportDashboardNavigationController()
    {
        TargetWindowType = WindowType.Main;
    }

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
        if (!ReportDashboardNavigationItems.TryRead(
                e.ActionArguments.SelectedChoiceActionItem,
                out var showOverview,
                out var category))
        {
            return;
        }

        Application.ServiceProvider?.GetService<ReportDashboardNavigationRequest>()
            ?.Apply(showOverview, category);

        if (TryApplyToOpenDashboard(showOverview, category))
        {
            e.Handled = true;
            return;
        }

        var objectSpace = Application.CreateObjectSpace(typeof(ReportDashboardHost));
        var host = objectSpace.CreateObject<ReportDashboardHost>();
        e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateDetailView(objectSpace, host);
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }

    private bool TryApplyToOpenDashboard(bool showOverview, ReportDashboardCategory category)
    {
        if (Application.MainWindow?.View is not DetailView detail)
            return false;

        if (detail.ObjectTypeInfo?.Type != typeof(ReportDashboardHost))
            return false;

        foreach (var item in detail.Items)
        {
            if (item is not IReportDashboardNavigationTarget target)
                continue;

            target.ApplyNavigation(showOverview, category);
            return true;
        }

        return false;
    }
}
