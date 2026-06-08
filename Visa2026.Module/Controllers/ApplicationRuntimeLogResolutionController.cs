using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Module.Controllers;

public sealed class ApplicationRuntimeLogResolutionController : ObjectViewController<ObjectView, ApplicationRuntimeLog>
{
    private SimpleAction markInProgressAction = null!;
    private SimpleAction markFixedAction = null!;
    private SimpleAction markIgnoredAction = null!;

    protected override void OnActivated()
    {
        base.OnActivated();

        if (!ApplicationRuntimeLogAdminHelper.IsCurrentUserAdministrator())
        {
            Active["ApplicationRuntimeLogAdminOnly"] = false;
            return;
        }
    }

    public ApplicationRuntimeLogResolutionController()
    {
        TargetObjectType = typeof(ApplicationRuntimeLog);

        markInProgressAction = new SimpleAction(this, "ApplicationRuntimeLogMarkInProgress", PredefinedCategory.Edit)
        {
            Caption = "Mark in progress",
            ImageName = "Action_Grant",
            SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
            ConfirmationMessage = "Mark selected runtime error(s) as in progress?"
        };
        markInProgressAction.Execute += MarkInProgressActionOnExecute;

        markFixedAction = new SimpleAction(this, "ApplicationRuntimeLogMarkFixed", PredefinedCategory.Edit)
        {
            Caption = "Mark fixed",
            ImageName = "Action_Validation_Validate",
            SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
            ConfirmationMessage = "Mark selected runtime error(s) as fixed?"
        };
        markFixedAction.Execute += MarkFixedActionOnExecute;

        markIgnoredAction = new SimpleAction(this, "ApplicationRuntimeLogMarkIgnored", PredefinedCategory.Edit)
        {
            Caption = "Mark ignored",
            ImageName = "Action_Deny",
            SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
            ConfirmationMessage = "Mark selected runtime error(s) as ignored?"
        };
        markIgnoredAction.Execute += MarkIgnoredActionOnExecute;
    }

    private void MarkInProgressActionOnExecute(object sender, SimpleActionExecuteEventArgs e) =>
        ApplyResolutionAsync(e, ApplicationRuntimeLogResolutionStatus.InProgress, null);

    private void MarkFixedActionOnExecute(object sender, SimpleActionExecuteEventArgs e) =>
        ApplyResolutionAsync(
            e,
            ApplicationRuntimeLogResolutionStatus.Fixed,
            "Marked fixed from Operations → Runtime errors.");

    private void MarkIgnoredActionOnExecute(object sender, SimpleActionExecuteEventArgs e) =>
        ApplyResolutionAsync(
            e,
            ApplicationRuntimeLogResolutionStatus.Ignored,
            "Marked ignored from Operations → Runtime errors.");

    private void ApplyResolutionAsync(
        SimpleActionExecuteEventArgs e,
        ApplicationRuntimeLogResolutionStatus status,
        string? notes)
    {
        var resolution = Application.ServiceProvider?.GetService<IApplicationRuntimeLogResolution>();
        if (resolution == null)
            throw new UserFriendlyException("Runtime log resolution service is not available.");

        var resolvedBy = SecuritySystem.CurrentUserName ?? "administrator";
        foreach (ApplicationRuntimeLog row in e.SelectedObjects.Cast<ApplicationRuntimeLog>())
        {
            var applied = resolution.TryApplyAsync(new RuntimeLogResolutionUpdate
            {
                Id = row.ID,
                Status = status,
                ResolvedBy = resolvedBy,
                ResolutionNotes = notes
            }).GetAwaiter().GetResult();

            if (!applied)
                continue;

            ObjectSpace.ReloadObject(row);
        }

        ObjectSpace.Refresh();
    }
}
