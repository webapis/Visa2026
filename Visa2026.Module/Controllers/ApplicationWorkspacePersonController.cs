using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects.ApplicationWorkspace;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Link / unlink <see cref="Person"/> rows on the Application workspace host.
/// </summary>
public sealed class ApplicationWorkspacePersonController : ViewController<DetailView>
{
    private readonly SimpleAction _linkPersonAction;
    private readonly SimpleAction _unlinkPersonAction;

    public ApplicationWorkspacePersonController()
    {
        TargetViewId = ApplicationWorkspaceViewIds.DetailView;

        _linkPersonAction = new SimpleAction(this, "ApplicationWorkspaceLinkPerson", PredefinedCategory.Unspecified)
        {
            Caption = "Link person",
            ImageName = "Action_LinkUnlink_Link",
            ToolTip = "Link an existing person to this Application roster.",
            SelectionDependencyType = SelectionDependencyType.Independent,
        };
        _linkPersonAction.Execute += (_, _) => TriggerLinkPerson();

        _unlinkPersonAction = new SimpleAction(this, "ApplicationWorkspaceUnlinkPerson", PredefinedCategory.Unspecified)
        {
            Caption = "Unlink person",
            ImageName = "Action_LinkUnlink_Unlink",
            ToolTip = "Remove a person from this Application roster.",
            SelectionDependencyType = SelectionDependencyType.Independent,
        };
        _unlinkPersonAction.Execute += (_, _) => TriggerUnlinkPerson();
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        const string workspaceShell = "ApplicationWorkspaceShell";
        _linkPersonAction.Active[workspaceShell] = true;
        _unlinkPersonAction.Active[workspaceShell] = true;
        AttachUiActions();
        UpdateActionState();
        View.CurrentObjectChanged += View_CurrentObjectChanged;
    }

    protected override void OnDeactivated()
    {
        DetachUiActions();
        View.CurrentObjectChanged -= View_CurrentObjectChanged;
        base.OnDeactivated();
    }

    public void TriggerLinkPerson()
    {
        var applicationId = ResolveApplicationId();
        if (applicationId == Guid.Empty)
            return;

        ApplicationWorkspacePersonLinkHelper.ShowLinkPersonPicker(
            Application,
            ApplicationWorkspacePersonLinkHelper.ResolveSourceFrame(Application, Frame),
            applicationId,
            () =>
            {
                View.Refresh(true);
                NotifyWorkspaceChanged();
            });
    }

    public void TriggerUnlinkPerson()
    {
        var applicationId = ResolveApplicationId();
        if (applicationId == Guid.Empty)
            return;

        ApplicationWorkspacePersonLinkHelper.ShowUnlinkPersonPicker(
            Application,
            ApplicationWorkspacePersonLinkHelper.ResolveSourceFrame(Application, Frame),
            applicationId,
            () =>
            {
                View.Refresh(true);
                NotifyWorkspaceChanged();
            });
    }

    private void AttachUiActions()
    {
        if (Application.ServiceProvider.GetService(typeof(ApplicationWorkspacePersonUiActions))
            is ApplicationWorkspacePersonUiActions actions)
        {
            actions.Register(TriggerLinkPerson, TriggerUnlinkPerson);
            actions.NotifyWorkspaceChanged();
        }
    }

    private void DetachUiActions()
    {
        if (Application.ServiceProvider.GetService(typeof(ApplicationWorkspacePersonUiActions))
            is ApplicationWorkspacePersonUiActions actions)
        {
            actions.Clear();
        }
    }

    private void NotifyWorkspaceChanged()
    {
        if (Application.ServiceProvider.GetService(typeof(IApplicationWorkspacePersonUiActions))
            is IApplicationWorkspacePersonUiActions actions)
        {
            actions.NotifyWorkspaceChanged();
        }
    }

    private void View_CurrentObjectChanged(object? sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        var applicationId = ResolveApplicationId();
        var enabled = applicationId != Guid.Empty;
        _linkPersonAction.Enabled["Application"] = enabled;
        _unlinkPersonAction.Enabled["Application"] = enabled;
    }

    private Guid ResolveApplicationId()
    {
        if (View.CurrentObject is ApplicationWorkspaceHost host && host.ApplicationId != Guid.Empty)
            return host.ApplicationId;

        return ApplicationWorkspacePendingOpenGate.Get(Application);
    }
}
