using System.Globalization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Shows CompletionPercentage as N%. Shares FieldName with the progress stepper column
/// (one grid sort); only this narrow column gets the % cell template.
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

        foreach (var columnModel in gridListEditor.GridDataColumnModels)
        {
            if (!IsNarrowPercentColumn(columnModel))
                continue;

            columnModel.AllowSort = true;
            columnModel.SortMode = DevExpress.Blazor.GridColumnSortMode.Value;
            columnModel.CellDisplayTemplate = context => builder =>
            {
                if (context.DataItem is not ApplicationProfileInstance application)
                {
                    builder.AddContent(0, "\u2014");
                    return;
                }

                var text = application.CompletionPercentage.ToString(CultureInfo.InvariantCulture) + "%";
                builder.AddContent(0, text);
            };
        }
    }

    /// <summary>Narrow % label only — not the wide progress stepper remapped to the same FieldName.</summary>
    private static bool IsNarrowPercentColumn(DxGridDataColumnModel columnModel) =>
        string.Equals(
            columnModel.FieldName,
            nameof(ApplicationProfileInstance.CompletionPercentage),
            StringComparison.Ordinal)
        && columnModel.MinWidth < 560;
}