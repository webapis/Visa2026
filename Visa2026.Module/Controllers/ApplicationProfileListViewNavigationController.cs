using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileOverview;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the Application Profile overview when an officer activates a row on
/// <see cref="ApplicationProfile"/> ListViews (replaces native DetailView drill-in).
/// </summary>
public sealed class ApplicationProfileListViewNavigationController : ViewController<ListView>
{
    private ListViewProcessCurrentObjectController? _processCurrentObjectController;

    public ApplicationProfileListViewNavigationController()
    {
        TargetObjectType = typeof(ApplicationProfile);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _processCurrentObjectController = Frame.GetController<ListViewProcessCurrentObjectController>();
        if (_processCurrentObjectController != null)
            _processCurrentObjectController.CustomHandleProcessSelectedItem += OnCustomHandleProcessSelectedItem;
    }

    protected override void OnDeactivated()
    {
        if (_processCurrentObjectController != null)
        {
            _processCurrentObjectController.CustomHandleProcessSelectedItem -= OnCustomHandleProcessSelectedItem;
            _processCurrentObjectController = null;
        }

        base.OnDeactivated();
    }

    private void OnCustomHandleProcessSelectedItem(object? sender, HandledEventArgs e)
    {
        if (View.CurrentObject is not ApplicationProfile profile)
            return;

        if (View.ObjectSpace.IsNewObject(profile))
            return;

        if (MigrationImportContext.IsDataImport)
            return;

        // Lookup ListViews keep native selection behavior.
        if (View.Id != null && View.Id.Contains("Lookup", StringComparison.OrdinalIgnoreCase))
            return;

        var overviewView = ApplicationProfileOverviewOpenHelper.CreateOverviewView(Application, View.ObjectSpace, profile);
        if (overviewView == null)
            return;

        Application.ShowViewStrategy.ShowView(
            new ShowViewParameters(overviewView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(Frame, null));

        e.Handled = true;
    }
}