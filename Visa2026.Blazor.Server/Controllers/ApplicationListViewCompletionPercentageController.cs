using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Hides the completion % column. The percent stays on the state-preview sheet.
/// The link column still sorts by the same field.
/// </summary>
public sealed class ApplicationListViewCompletionPercentageController : ViewController<ListView>
{
    private CancellationTokenSource? deferredApplyCts;

    public ApplicationListViewCompletionPercentageController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyPercentTemplate();
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
            ApplyPercentTemplate();
    }

    protected override void OnDeactivated()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = null;
        base.OnDeactivated();
    }

    internal void ApplyPercentTemplate()
    {
        if (View?.Editor is not DxGridListEditor gridListEditor)
            return;
        if (gridListEditor.GridModel == null)
            return;

        foreach (var columnModel in gridListEditor.GridDataColumnModels)
        {
            if (!IsNarrowPercentColumn(columnModel))
                continue;

            // Completion % lives on the Ýagdaý syny sheet. Sort still uses this field on the link column.
            columnModel.Visible = false;
            columnModel.CellDisplayTemplate = null;
        }
    }

    /// <summary>Narrow % label only — not the wide progress stepper remapped to the same FieldName.</summary>
    private static bool IsNarrowPercentColumn(DxGridDataColumnModel columnModel) =>
        string.Equals(
            columnModel.FieldName,
            nameof(ApplicationProfileInstance.CompletionPercentage),
            StringComparison.Ordinal)
        && !string.Equals(
            columnModel.Name,
            ApplicationProfileInstanceProgressStepsColumnUpdater.ColumnId,
            StringComparison.Ordinal)
        && columnModel.MinWidth < ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth;
}