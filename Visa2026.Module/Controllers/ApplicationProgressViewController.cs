using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Validates <see cref="ApplicationProfileInstanceProgress"/> rows on commit (including nested saves from <see cref="Application"/>).
/// </summary>
public sealed class ApplicationProfileInstanceProgressCommitValidationController : ViewController
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.Committing += ObjectSpace_Committing;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.Committing -= ObjectSpace_Committing;
        base.OnDeactivated();
    }

    private void ObjectSpace_Committing(object sender, CancelEventArgs e)
    {
        if (!ApplicationProfileInstanceProgressProfileResolver.TryValidateApplicationEditableWhenWorkflowTerminal(
                ObjectSpace, out var terminalLockError))
        {
            e.Cancel = true;
            Application.ShowViewStrategy.ShowMessage(
                terminalLockError ?? VisaUiMessages.Get("Application.FieldsLockedWhenWorkflowTerminal"),
                InformationType.Error,
                5000,
                InformationPosition.Top);
            return;
        }

        if (!ApplicationProfileInstancePersonRosterLockHelper.TryValidateRosterEditableWhenWorkflowTerminal(
                ObjectSpace, out var rosterLockError))
        {
            e.Cancel = true;
            Application.ShowViewStrategy.ShowMessage(
                rosterLockError ?? VisaUiMessages.Get("ApplicationProfileInstancePerson.RosterLockedWhenWorkflowTerminal"),
                InformationType.Error,
                5000,
                InformationPosition.Top);
            return;
        }

        foreach (var application in ObjectSpace.GetObjectsToSave(false).OfType<BusinessObjects.ApplicationProfileInstance>())
        {
            if (!ApplicationProfileInstanceProgressProfileResolver.TryValidateApplicationUnchangedAfterProgress(
                    application, ObjectSpace, out var lockError))
            {
                e.Cancel = true;
                Application.ShowViewStrategy.ShowMessage(
                    lockError ?? VisaUiMessages.Get("Application.FieldsLockedAfterProgress"),
                    InformationType.Error,
                    5000,
                    InformationPosition.Top);
                return;
            }

            if (ApplicationProfileInstanceProgressProfileResolver.TryValidateProjectContractOnApplication(
                    application, ObjectSpace, out var applicationError))
                continue;

            e.Cancel = true;
            Application.ShowViewStrategy.ShowMessage(
                applicationError ?? VisaUiMessages.Get("ApplicationProfileInstanceProgress.ProjectContractRequired"),
                InformationType.Error,
                5000,
                InformationPosition.Top);
            return;
        }

        foreach (var progress in ObjectSpace.GetObjectsToSave(false).OfType<ApplicationProfileInstanceProgress>())
        {
            if (ApplicationProfileInstanceProgressTransitionHelper.TryValidateProgressStep(progress, ObjectSpace, out var errorMessage))
                continue;

            e.Cancel = true;
            Application.ShowViewStrategy.ShowMessage(
                errorMessage ?? VisaUiMessages.Get("ApplicationProfileInstanceProgress.InvalidForRoute"),
                InformationType.Error,
                5000,
                InformationPosition.Top);
            return;
        }
    }
}

/// <summary>
/// Suggests state defaults on the <see cref="ApplicationProfileInstanceProgress"/> detail view.
/// </summary>
public sealed class ApplicationProfileInstanceProgressDetailViewController : ObjectViewController<DetailView, ApplicationProfileInstanceProgress>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyDefaults(ViewCurrentObject);
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        base.OnDeactivated();
    }

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (e.Object is not ApplicationProfileInstanceProgress progress || !ReferenceEquals(progress, ViewCurrentObject))
            return;

        if (e.PropertyName is nameof(ApplicationProfileInstanceProgress.State) or nameof(ApplicationProfileInstanceProgress.ApplicationProfileInstance))
            ApplyDefaults(progress);
    }

    private void ApplyDefaults(ApplicationProfileInstanceProgress? progress)
    {
        if (progress == null)
            return;

        ApplicationProfileInstanceProgressTransitionHelper.TryApplySuggestedNextStep(progress);
        View.Refresh();
    }
}
