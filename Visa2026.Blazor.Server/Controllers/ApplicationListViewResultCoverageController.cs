using System.Linq;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Renders Result-tab people-coverage chips in the instance ListView column.
/// </summary>
public sealed class ApplicationListViewResultCoverageController : ViewController<ListView>
{
    private CancellationTokenSource? deferredApplyCts;

    public ApplicationListViewResultCoverageController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyChipTemplate();
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
        {
            ApplyChipTemplate();
            AutoFitColumns();
        }
    }

    private void AutoFitColumns()
    {
        if (View?.Editor is DxGridListEditor { GridModel.ComponentInstance: { } grid })
            grid.AutoFitColumnWidths();
    }

    protected override void OnDeactivated()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = null;
        base.OnDeactivated();
    }

    private void ApplyChipTemplate()
    {
        if (View?.Editor is not DxGridListEditor gridListEditor)
            return;

        foreach (var columnModel in gridListEditor.GridDataColumnModels)
        {
            if (!string.Equals(
                    columnModel.FieldName,
                    nameof(ApplicationProfileInstance.ResultCoverageDisplay),
                    StringComparison.Ordinal))
                continue;

            columnModel.AllowSort = false;
            columnModel.MinWidth = 560;
            columnModel.Width = "560px";
            var lastVisible = gridListEditor.GridDataColumnModels
                .Where(c => !ReferenceEquals(c, columnModel) && c.VisibleIndex >= 0)
                .Select(c => c.VisibleIndex)
                .DefaultIfEmpty(-1)
                .Max();
            columnModel.VisibleIndex = lastVisible + 1;
            columnModel.CellDisplayTemplate = context => builder =>
            {
                if (context.DataItem is not ApplicationProfileInstance application)
                {
                    builder.AddContent(0, "—");
                    return;
                }

                var chips = application.ListViewResultCoverageChips;
                if (chips.Count == 0)
                {
                    builder.AddContent(0, application.ResultCoverageDisplay);
                    return;
                }

                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "lv-result-chips");
                var seq = 2;
                foreach (var chip in chips)
                {
                    builder.OpenElement(seq++, "span");
                    builder.AddAttribute(seq++, "class", "lv-result-chip lv-result-chip--" + ApplicationWorkspaceIssuedResultListCoverage.Tone(chip));
                    builder.AddContent(seq++, ApplicationWorkspaceIssuedResultListCoverage.Caption(chip));
                    builder.AddContent(seq++, " ");
                    builder.OpenElement(seq++, "strong");
                    builder.AddContent(seq++, ApplicationWorkspaceIssuedResultListCoverage.RatioText(chip));
                    builder.CloseElement();
                    builder.CloseElement();
                }

                builder.CloseElement();
            };
        }
    }
}
