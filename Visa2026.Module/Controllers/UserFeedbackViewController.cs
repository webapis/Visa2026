using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.Feedback;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

public sealed class UserFeedbackViewController : ObjectViewController<ObjectView, UserFeedback>
{
    private readonly SimpleAction _markFixedAction;
    private readonly SimpleAction _markInProgressAction;

    public UserFeedbackViewController()
    {
        _markInProgressAction = new SimpleAction(this, "UserFeedbackMarkInProgress", PredefinedCategory.Edit)
        {
            Caption = "Mark in progress",
            ImageName = "Action_Grant",
            SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
            ConfirmationMessage = null
        };
        _markInProgressAction.Execute += MarkInProgressAction_Execute;

        _markFixedAction = new SimpleAction(this, "UserFeedbackMarkFixed", PredefinedCategory.Edit)
        {
            Caption = "Mark fixed",
            ImageName = "Action_Valid",
            SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
            ConfirmationMessage = null
        };
        _markFixedAction.Execute += MarkFixedAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        ApplyOfficerReadOnlyChrome();
        UpdateActionState();
        View.CurrentObjectChanged += View_CurrentObjectChanged;
        if (View is ListView listView)
            listView.SelectionChanged += ListView_SelectionChanged;
    }

    private void ApplyOfficerReadOnlyChrome()
    {
        if (IsAdmin())
            return;

        View.AllowEdit.SetItemValue("UserFeedbackOfficer", false);
        View.AllowNew.SetItemValue("UserFeedbackOfficer", false);
        View.AllowDelete.SetItemValue("UserFeedbackOfficer", false);

        var modifications = Frame.GetController<ModificationsController>();
        if (modifications != null)
        {
            modifications.SaveAction.Active.SetItemValue("UserFeedbackOfficer", false);
            modifications.SaveAndCloseAction.Active.SetItemValue("UserFeedbackOfficer", false);
            modifications.SaveAndNewAction.Active.SetItemValue("UserFeedbackOfficer", false);
        }

        Frame.GetController<DeleteObjectsViewController>()?.DeleteAction.Active.SetItemValue("UserFeedbackOfficer", false);
        Frame.GetController<NewObjectViewController>()?.NewObjectAction.Active.SetItemValue("UserFeedbackOfficer", false);
    }

    private static bool IsAdmin() =>
        SecuritySystem.CurrentUser is ApplicationUser user && user.Roles.Any(r => r.IsAdministrative);

    protected override void OnDeactivated()
    {
        View.CurrentObjectChanged -= View_CurrentObjectChanged;
        if (View is ListView listView)
            listView.SelectionChanged -= ListView_SelectionChanged;
        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object sender, EventArgs e) => UpdateActionState();

    private void ListView_SelectionChanged(object sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        bool isAdmin = IsAdmin();
        var selected = View.SelectedObjects.Cast<UserFeedback>().ToList();
        bool anyOpen = selected.Any(f => f.IsOpen);

        _markInProgressAction.Active["AdminOnly"] = isAdmin;
        _markFixedAction.Active["AdminOnly"] = isAdmin;
        _markInProgressAction.Enabled["Selection"] = anyOpen;
        _markFixedAction.Enabled["Selection"] = anyOpen;
    }

    private void MarkInProgressAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        ApplyStatus(UserFeedbackStatus.InProgress, setFixedMeta: false, e.SelectedObjects);
    }

    private void MarkFixedAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        ApplyStatus(UserFeedbackStatus.Fixed, setFixedMeta: true, e.SelectedObjects);
    }

    private void ApplyStatus(UserFeedbackStatus status, bool setFixedMeta, System.Collections.IList selectedObjects)
    {
        var currentUser = SecuritySystem.CurrentUser as ApplicationUser;
        foreach (UserFeedback feedback in selectedObjects.Cast<UserFeedback>())
        {
            feedback.Status = status;
            if (setFixedMeta)
            {
                feedback.FixedAt = DateTime.Now;
                feedback.FixedBy = ObjectSpace.GetObject(currentUser);
            }
        }

        ObjectSpace.CommitChanges();
        View.Refresh();
        Application.ShowViewStrategy.ShowMessage(
            VisaUiMessages.Get(setFixedMeta ? "UserFeedback.MarkFixed.Success" : "UserFeedback.MarkInProgress.Success"),
            InformationType.Success);
    }
}
