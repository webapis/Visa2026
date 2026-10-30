using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Hides the standalone ResultCoverageDisplay column; chips render in Application progress.
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
        if (PersonLinkedApplicationsColumnWidths.SkipAutoFit(View?.Id))
            return;

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

    internal void ApplyChipTemplate()
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

            // Chips merge into Application progress after At the migration service.
            columnModel.Visible = false;
            columnModel.AllowSort = false;
            columnModel.CellDisplayTemplate = null;
        }
    }
}
