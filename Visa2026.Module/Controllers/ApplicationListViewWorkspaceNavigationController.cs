using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the ApplicationProfileInstance workspace when an officer activates a row on ApplicationProfileInstance ListViews
/// (replaces legacy <c>Application_DetailView</c> as the default drill-in).
/// </summary>
public sealed class ApplicationListViewWorkspaceNavigationController : ViewController<ListView>
{
    private ListViewProcessCurrentObjectController? _processCurrentObjectController;

    public ApplicationListViewWorkspaceNavigationController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
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
        if (View.CurrentObject is not ApplicationProfileInstance application)
            return;

        if (View.ObjectSpace.IsNewObject(application))
            return;

        if (MigrationImportContext.IsDataImport)
            return;

        var workspaceView = ApplicationWorkspaceOpenHelper.CreateWorkspaceView(Application, View.ObjectSpace, application);
        if (workspaceView == null)
            return;

        Application.ShowViewStrategy.ShowView(
            new ShowViewParameters(workspaceView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(Frame, null));

        e.Handled = true;
    }
}
