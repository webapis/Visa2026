using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the multi-step configuration wizard for <see cref="ApplicationProfile"/>.
/// </summary>
public sealed class ApplicationProfileWizardController : ViewController
{
    private readonly SimpleAction openWizardAction;

    public ApplicationProfileWizardController()
    {
        TargetObjectType = typeof(ApplicationProfile);

        openWizardAction = new SimpleAction(this, "OpenApplicationProfileWizard", "View");
        openWizardAction.ImageName = "Action_Edit";
        openWizardAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
        openWizardAction.Execute += OpenWizardAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        openWizardAction.Caption = "Configure profile";
        openWizardAction.ToolTip =
            "Open the Application Profile configuration wizard (live FK model; read-only when config locked).";
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
        // Catalog / overview host Configure CTAs; hide toolbar action on native ListView.
        var isListView = View is ListView;
        openWizardAction.Active["ApplicationProfileCatalogHome"] = !isListView;

        var profile = ResolveProfile();
        openWizardAction.Enabled["ApplicationProfile"] = profile != null && !ObjectSpace.IsNewObject(profile);
    }

    private ApplicationProfile? ResolveProfile()
    {
        if (View is DetailView && View.CurrentObject is ApplicationProfile detailProfile)
            return detailProfile;

        if (View is ListView listView && listView.CurrentObject is ApplicationProfile listProfile)
            return listProfile;

        return null;
    }

    private void OpenWizardAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var profile = ResolveProfile();
        if (profile == null)
        {
            Application.ShowViewStrategy.ShowMessage(
                "Select a saved Application Profile first.",
                InformationType.Warning);
            return;
        }

        var wizardView = ApplicationProfileWizardOpenHelper.CreateWizardView(Application, ObjectSpace, profile);
        if (wizardView == null)
            return;

        e.ShowViewParameters.CreatedView = wizardView;
        e.ShowViewParameters.TargetWindow = TargetWindow.Current;
    }
}
