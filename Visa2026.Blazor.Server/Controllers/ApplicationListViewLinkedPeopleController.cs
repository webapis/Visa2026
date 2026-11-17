using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.AspNetCore.Components.Rendering;
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

                RenderPeople(builder, people);
            };
        }
    }

    private static void RenderPeople(
        RenderTreeBuilder builder,
        IReadOnlyList<ApplicationListLinkedPeople.Row> people)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "lv-people");
        var seq = 2;
        foreach (var person in people)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "lv-people__row");
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "lv-people__name");
            builder.AddContent(seq++, person.Name);
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
