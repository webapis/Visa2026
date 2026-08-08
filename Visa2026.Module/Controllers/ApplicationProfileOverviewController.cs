using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileOverview;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the read-only Application Profile overview (mock / preview shell).
/// </summary>
public sealed class ApplicationProfileOverviewController : ViewController
{
    private readonly SimpleAction openOverviewAction;

    public ApplicationProfileOverviewController()
    {
        TargetObjectType = typeof(ApplicationProfile);

        openOverviewAction = new SimpleAction(this, "OpenApplicationProfileOverview", "View");
        openOverviewAction.ImageName = "BO_List";
        openOverviewAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
        openOverviewAction.Execute += OpenOverviewAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        openOverviewAction.Caption = "Open profile overview";
        openOverviewAction.ToolTip =
            "Prototype read-only overview of live configuration, defaults, and linked applications (mock rows where noted).";
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
        // Catalog is the home screen; hide overview toolbar on native ListView.
        // Keep available on native DetailView as a rare escape hatch.
        var isListView = View is ListView;
        openOverviewAction.Active["ApplicationProfileCatalogHome"] = !isListView;

        var profile = ResolveProfile();
        openOverviewAction.Enabled["ApplicationProfile"] = profile != null && !ObjectSpace.IsNewObject(profile);
    }

    private ApplicationProfile? ResolveProfile()
    {
        if (View is DetailView && View.CurrentObject is ApplicationProfile detailProfile)
            return detailProfile;

        if (View is ListView && View.CurrentObject is ApplicationProfile listProfile)
            return listProfile;

        return null;
    }

    private void OpenOverviewAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var profile = ResolveProfile();
        if (profile == null)
        {
            Application.ShowViewStrategy.ShowMessage(
                "Select a saved Application Profile first.",
                InformationType.Warning);
            return;
        }

        var overviewView = ApplicationProfileOverviewOpenHelper.CreateOverviewView(Application, ObjectSpace, profile);
        if (overviewView == null)
            return;

        e.ShowViewParameters.CreatedView = overviewView;
        e.ShowViewParameters.TargetWindow = TargetWindow.Current;
    }
}
