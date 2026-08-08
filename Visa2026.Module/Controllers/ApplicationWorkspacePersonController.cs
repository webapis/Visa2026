using System;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationWorkspace;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Link / unlink <see cref="Person"/> rows on the Application workspace host.
/// </summary>
public sealed class ApplicationWorkspacePersonController : ViewController<DetailView>
{
    private readonly PopupWindowShowAction _linkPersonAction;
    private readonly PopupWindowShowAction _unlinkPersonAction;

    public ApplicationWorkspacePersonController()
    {
        TargetViewId = ApplicationWorkspaceViewIds.DetailView;

        _linkPersonAction = new PopupWindowShowAction(this, "ApplicationWorkspaceLinkPerson", PredefinedCategory.Unspecified)
        {
            Caption = "Link person",
            ImageName = "Action_LinkUnlink_Link",
            ToolTip = "Link an existing person to this Application roster.",
            SelectionDependencyType = SelectionDependencyType.Independent,
        };
        _linkPersonAction.CustomizePopupWindowParams += LinkPerson_CustomizePopupWindowParams;
        _linkPersonAction.Execute += LinkPerson_Execute;

        _unlinkPersonAction = new PopupWindowShowAction(this, "ApplicationWorkspaceUnlinkPerson", PredefinedCategory.Unspecified)
        {
            Caption = "Unlink person",
            ImageName = "Action_LinkUnlink_Unlink",
            ToolTip = "Remove a person from this Application roster.",
            SelectionDependencyType = SelectionDependencyType.Independent,
        };
        _unlinkPersonAction.CustomizePopupWindowParams += UnlinkPerson_CustomizePopupWindowParams;
        _unlinkPersonAction.Execute += UnlinkPerson_Execute;
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
        if (!_linkPersonAction.Active)
            return;

        _linkPersonAction.DoExecute(Application.MainWindow);
    }

    public void TriggerUnlinkPerson()
    {
        if (!_unlinkPersonAction.Active)
            return;

        _unlinkPersonAction.DoExecute(Application.MainWindow);
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

    private void LinkPerson_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
    {
        var applicationId = ResolveApplicationId();
        if (applicationId == Guid.Empty)
            return;

        var objectSpace = Application.CreateObjectSpace(typeof(Person));
        var listView = Application.CreateListView(objectSpace, typeof(Person), true);
        listView.CollectionSource.Criteria["NotAlreadyLinked"] = CriteriaOperator.Parse(
            "Not [ApplicationPeople][Application.ID = ?]",
            applicationId);

        e.View = listView;
        e.DialogController.SaveOnAccept = false;
    }

    private void UnlinkPerson_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
    {
        var applicationId = ResolveApplicationId();
        if (applicationId == Guid.Empty)
            return;

        var objectSpace = Application.CreateObjectSpace(typeof(ApplicationPerson));
        var listView = Application.CreateListView(objectSpace, typeof(ApplicationPerson), true);
        listView.CollectionSource.Criteria["Application"] = CriteriaOperator.Parse("Application.ID = ?", applicationId);

        e.View = listView;
        e.DialogController.SaveOnAccept = false;
    }

    private void LinkPerson_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
    {
        if (e.PopupWindowViewCurrentObject is not Person selectedPerson)
            return;

        var applicationId = ResolveApplicationId();
        if (applicationId == Guid.Empty)
            return;

        using var objectSpace = Application.CreateObjectSpace(typeof(Application));
        var application = objectSpace.GetObjectByKey<Application>(applicationId);
        if (application == null)
            return;

        var person = objectSpace.GetObject(selectedPerson);
        var linked = ApplicationPersonService.LinkPerson(objectSpace, application, person);
        if (linked == null)
        {
            Application.ShowViewStrategy.ShowMessage(
                "Could not link the selected person.",
                InformationType.Warning);
            return;
        }

        objectSpace.CommitChanges();
        Application.ShowViewStrategy.ShowMessage(
            $"{person.FullName} linked.",
            InformationType.Success,
            2000);
        View.Refresh(true);
        NotifyWorkspaceChanged();
    }

    private void UnlinkPerson_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
    {
        if (e.PopupWindowViewCurrentObject is not ApplicationPerson applicationPerson)
            return;

        using var objectSpace = Application.CreateObjectSpace(typeof(ApplicationPerson));
        var row = objectSpace.GetObject(applicationPerson);
        if (row == null)
            return;

        var personName = row.Person?.FullName ?? "Person";
        ApplicationPersonService.UnlinkPerson(objectSpace, row);
        objectSpace.CommitChanges();

        Application.ShowViewStrategy.ShowMessage(
            $"{personName} unlinked.",
            InformationType.Success,
            2000);
        View.Refresh(true);
        NotifyWorkspaceChanged();
    }

    private Guid ResolveApplicationId()
    {
        if (View.CurrentObject is ApplicationWorkspaceHost host && host.ApplicationId != Guid.Empty)
            return host.ApplicationId;

        return ApplicationWorkspacePendingOpenGate.Get(Application);
    }
}
