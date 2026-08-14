using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Starts a numbered case from selected staged Application Profile Instances (replaces officer-shell Start process).
/// </summary>
public sealed class ApplicationStagedStartProcessController : ViewController<ListView>
{
    private readonly SimpleAction _startProcessAction;

    public ApplicationStagedStartProcessController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
        TargetViewId = ApplicationProfileInstanceProgressRouteNavigation.ListViewStaged;

        _startProcessAction = new SimpleAction(this, "StartStagedProcess", PredefinedCategory.View)
        {
            Caption = "Start process",
            ToolTip = "Assign a process number to the selected staged profiles and open the case workspace.",
            ImageName = "Action_Grant",
            SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
        };
        _startProcessAction.Execute += StartProcess_Execute;
    }

    private void StartProcess_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var ids = View.SelectedObjects
            .OfType<ApplicationProfileInstance>()
            .Select(a => a.ID)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var service = Application.ServiceProvider?.GetService<IOfficerShellStartProcessService>()
            ?? new OfficerShellStartProcessService();

        var result = service.Start(ObjectSpace, ids);
        if (!result.Success)
        {
            Application.ShowViewStrategy.ShowMessage(
                result.ErrorMessage ?? "Could not start process.",
                InformationType.Warning);
            return;
        }

        ObjectSpace.CommitChanges();

        var message = result.MergedCount > 1
            ? $"Started process {result.ProcessNumber} — merged {result.MergedCount} profiles."
            : $"Started process {result.ProcessNumber}.";
        Application.ShowViewStrategy.ShowMessage(message, InformationType.Success);

        var workspaceView = ApplicationWorkspaceOpenHelper.CreateWorkspaceView(
            Application,
            result.ApplicationProfileInstanceId);
        if (workspaceView == null)
            return;

        e.ShowViewParameters.CreatedView = workspaceView;
        e.ShowViewParameters.TargetWindow = TargetWindow.Current;
    }
}