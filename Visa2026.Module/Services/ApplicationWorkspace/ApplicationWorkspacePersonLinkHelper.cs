using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Person link/unlink pickers for the ApplicationProfileInstance workspace (modal ListView + DialogController).
/// Callable from XAF actions and custom Blazor property editors.
/// </summary>
public static class ApplicationWorkspacePersonLinkHelper
{
    public static void ShowLinkPersonPicker(
        XafApplication application,
        Frame sourceFrame,
        Guid applicationId,
        Action? onPersonLinked = null)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (sourceFrame == null || applicationId == Guid.Empty)
            return;

        var objectSpace = application.CreateObjectSpace(typeof(Person));
        var listView = application.CreateListView(objectSpace, typeof(Person), true);
        listView.CollectionSource.Criteria["NotAlreadyLinked"] = CriteriaOperator.Parse(
            "Not [ApplicationProfileInstances][ID = ?]",
            applicationId);

        var dialogController = application.CreateController<DialogController>();
        dialogController.SaveOnAccept = false;
        dialogController.AcceptAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
        dialogController.Accepting += (_, e) =>
        {
            if (listView.CurrentObject is not Person selectedPerson)
            {
                e.Cancel = true;
                return;
            }

            using var linkObjectSpace = application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var applicationBo = linkObjectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
            if (applicationBo == null)
            {
                e.Cancel = true;
                return;
            }

            if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(applicationBo))
            {
                application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Get("ApplicationProfileInstancePerson.RosterLockedWhenWorkflowTerminal"),
                    InformationType.Warning);
                e.Cancel = true;
                return;
            }

            var person = linkObjectSpace.GetObject(selectedPerson);
            var linked = ApplicationProfileInstancePersonService.LinkPerson(linkObjectSpace, applicationBo, person);
            if (linked == null)
            {
                application.ShowViewStrategy.ShowMessage(
                    "Could not link the selected person.",
                    InformationType.Warning);
                e.Cancel = true;
                return;
            }

            linkObjectSpace.CommitChanges();
            application.ShowViewStrategy.ShowMessage(
                $"{person.FullName} linked.",
                InformationType.Success,
                2000);
            onPersonLinked?.Invoke();
        };

        var showViewParameters = new ShowViewParameters(listView)
        {
            TargetWindow = TargetWindow.NewModalWindow,
        };
        showViewParameters.Controllers.Add(dialogController);
        application.ShowViewStrategy.ShowView(
            showViewParameters,
            new ShowViewSource(sourceFrame, null));
    }

    public static void ShowUnlinkPersonPicker(
        XafApplication application,
        Frame sourceFrame,
        Guid applicationId,
        Action? onPersonUnlinked = null)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (sourceFrame == null || applicationId == Guid.Empty)
            return;

        var objectSpace = application.CreateObjectSpace(typeof(Person));
        var listView = application.CreateListView(objectSpace, typeof(Person), true);
        listView.CollectionSource.Criteria["LinkedToApplication"] = CriteriaOperator.Parse(
            "[ApplicationProfileInstances][ID = ?]",
            applicationId);

        var dialogController = application.CreateController<DialogController>();
        dialogController.SaveOnAccept = false;
        dialogController.AcceptAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
        dialogController.Accepting += (_, e) =>
        {
            if (listView.CurrentObject is not Person selectedPerson)
            {
                e.Cancel = true;
                return;
            }

            using var unlinkObjectSpace = application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var applicationBo = unlinkObjectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
            var person = unlinkObjectSpace.GetObject(selectedPerson);
            if (applicationBo == null || person == null)
            {
                e.Cancel = true;
                return;
            }

            if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(applicationBo))
            {
                application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Get("ApplicationProfileInstancePerson.RosterLockedWhenWorkflowTerminal"),
                    InformationType.Warning);
                e.Cancel = true;
                return;
            }

            var personName = person.FullName ?? "Person";
            ApplicationProfileInstancePersonService.UnlinkPerson(unlinkObjectSpace, applicationBo, person);
            unlinkObjectSpace.CommitChanges();

            application.ShowViewStrategy.ShowMessage(
                $"{personName} unlinked.",
                InformationType.Success,
                2000);
            onPersonUnlinked?.Invoke();
        };

        var showViewParameters = new ShowViewParameters(listView)
        {
            TargetWindow = TargetWindow.NewModalWindow,
        };
        showViewParameters.Controllers.Add(dialogController);
        application.ShowViewStrategy.ShowView(
            showViewParameters,
            new ShowViewSource(sourceFrame, null));
    }

    public static Frame? ResolveSourceFrame(XafApplication application, Frame? viewFrame)
    {
        if (viewFrame != null)
            return viewFrame;

        return application.MainWindow;
    }
}
