using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Blazor.Server.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;
using Visa2026.Module.Services.ApplicationWorkspace;
namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Shows each linked person by name. Document marks and Seretmezlik cards open in the state preview.
/// </summary>
public sealed class ApplicationListViewLinkedPeopleController : ViewController<ListView>
{
    private CancellationTokenSource? deferredApplyCts;

    public ApplicationListViewLinkedPeopleController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        ApplicationListLinkedPeopleOpenBridge.Attach(this);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplicationListLinkedPeopleOpenBridge.Attach(this);
        ApplyPeopleTemplate();
        ScheduleDeferredApply();
    }

    private void ScheduleDeferredApply()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = new CancellationTokenSource();
        var token = deferredApplyCts.Token;
        _ = ApplyDeferredAsync(token);
    }

    private async Task ApplyDeferredAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(150, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (View is { IsDisposed: false })
            ApplyPeopleTemplate();
    }

    internal void OpenApplication(Guid applicationId)
    {
        var workspaceView = ApplicationWorkspaceOpenHelper.CreateWorkspaceView(Application, applicationId);
        if (workspaceView == null)
            return;

        Application.ShowViewStrategy.ShowView(
            new ShowViewParameters(workspaceView) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(Frame, null));
    }

    protected override void OnDeactivated()
    {
        ApplicationListLinkedPeopleOpenBridge.Detach(this);
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = null;
        base.OnDeactivated();
    }

    internal void ApplyPeopleTemplate()
    {
        if (View?.Editor is not DxGridListEditor gridListEditor)
            return;
        if (gridListEditor.GridModel == null)
            return;

        foreach (var columnModel in gridListEditor.GridDataColumnModels)
        {
            if (!string.Equals(
                    columnModel.FieldName,
                    nameof(ApplicationProfileInstance.LinkedPeopleDisplay),
                    StringComparison.Ordinal))
                continue;

            columnModel.MinWidth = 220;
            if (string.IsNullOrEmpty(columnModel.Name))
                columnModel.Name = ApplicationProfileInstanceLinkedPeopleColumnUpdater.ColumnId;

            columnModel.CellDisplayTemplate = context => builder =>
            {
                if (context.DataItem is not ApplicationProfileInstance application)
                {
                    builder.AddContent(0, "\u2014");
                    return;
                }

                var people = application.ListViewLinkedPeople;
                if (people.Count == 0)
                {
                    people = Frame.GetController<ApplicationListViewPreloadController>()
                        ?.LinkedPeopleFor(application.ID) ?? people;
                }

                if (people.Count == 0)
                {
                    builder.AddContent(0, string.IsNullOrWhiteSpace(application.LinkedPeopleDisplay)
                        ? "\u2014"
                        : application.LinkedPeopleDisplay);
                    return;
                }

                builder.OpenComponent<ApplicationListPeopleCell>(0);
                builder.AddAttribute(1, "ApplicationId", application.ID);
                builder.AddAttribute(2, "People", people);
                builder.CloseComponent();
            };
        }
    }
}
