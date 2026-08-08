using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the custom Application workspace from ListView or legacy DetailView.
/// </summary>
public sealed class ApplicationWorkspaceController : ViewController
{
    private readonly SimpleAction openWorkspaceAction;

    public ApplicationWorkspaceController()
    {
        TargetObjectType = typeof(Application);

        openWorkspaceAction = new SimpleAction(this, "OpenApplicationWorkspace", "View");
        openWorkspaceAction.ImageName = "BO_List";
        openWorkspaceAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
        openWorkspaceAction.Execute += OpenWorkspaceAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        openWorkspaceAction.Caption = "Open workspace";
        openWorkspaceAction.ToolTip =
            "Application workspace — roster, profile summary, and linked person data.";
        UpdateActionState();
        if (View != null)
            View.CurrentObjectChanged += View_CurrentObjectChanged;
        if (View is ListView listView)
            listView.SelectionChanged += ListView_SelectionChanged;
    }

    protected override void OnDeactivated()
    {
        if (View != null)
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
        if (View is ListView listView)
            listView.SelectionChanged -= ListView_SelectionChanged;
        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object? sender, EventArgs e) => UpdateActionState();

    private void ListView_SelectionChanged(object? sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        var app = ResolveApplication();
        openWorkspaceAction.Enabled["Application"] = app != null && !ObjectSpace.IsNewObject(app);
    }

    private Application? ResolveApplication()
    {
        if (View is DetailView && View.CurrentObject is Application detailApp)
            return detailApp;

        if (View is ListView listView && listView.CurrentObject is Application listApp)
            return listApp;

        return null;
    }

    private void OpenWorkspaceAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var app = ResolveApplication();
        if (app == null)
        {
            Application.ShowViewStrategy.ShowMessage(
                "Select a saved Application first.",
                InformationType.Warning);
            return;
        }

        var workspaceView = ApplicationWorkspaceOpenHelper.CreateWorkspaceView(Application, ObjectSpace, app);
        if (workspaceView == null)
            return;

        e.ShowViewParameters.CreatedView = workspaceView;
        e.ShowViewParameters.TargetWindow = TargetWindow.Current;
    }
}
