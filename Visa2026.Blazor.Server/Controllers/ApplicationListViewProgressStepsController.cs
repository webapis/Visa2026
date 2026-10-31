using System.Linq;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Renders the compact workspace progress stepper in the instance ListView column.
/// </summary>
public sealed class ApplicationListViewProgressStepsController : ViewController<ListView>
{
    private CancellationTokenSource? deferredApplyCts;
    private Action<GridCustomSortEventArgs>? customSortHandler;
    private Action<GridCustomSortEventArgs>? previousCustomSort;

    public ApplicationListViewProgressStepsController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyStepperTemplate();
        WireCustomSort();
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
            ApplyStepperTemplate();
            WireCustomSort();
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
        if (customSortHandler != null
            && View?.Editor is DxGridListEditor { GridModel: { } gridModel })
        {
            gridModel.CustomSort = previousCustomSort;
        }

        customSortHandler = null;
        previousCustomSort = null;
        base.OnDeactivated();
    }

    private void WireCustomSort()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
            return;

        if (customSortHandler != null)
        {
            gridModel.CustomSort = previousCustomSort;
            customSortHandler = null;
            previousCustomSort = null;
        }

        previousCustomSort = gridModel.CustomSort;
        customSortHandler = e =>
        {
            previousCustomSort?.Invoke(e);
            if (e.Handled || !IsProgressStepsColumn(e.FieldName))
                return;

            // NotMapped ProgressListSortKey is often missing from Value1/Value2; use the BO.
            var key1 = SortKeyFromDataItem(e.DataItem1);
            var key2 = SortKeyFromDataItem(e.DataItem2);
            e.Result = key1.CompareTo(key2);
            e.Handled = true;
        };
        gridModel.CustomSort = customSortHandler;
    }

    private static int SortKeyFromDataItem(object? dataItem)
    {
        if (dataItem is ApplicationProfileInstance application)
            return application.ProgressListSortKey;

        return int.MaxValue;
    }

    internal void ApplyStepperTemplate()
    {
        if (View?.Editor is not DxGridListEditor gridListEditor)
            return;

        foreach (var columnModel in gridListEditor.GridDataColumnModels)
        {
            if (string.Equals(
                    columnModel.FieldName,
                    nameof(ApplicationProfileInstance.LatestProgressState),
                    StringComparison.Ordinal))
            {
                columnModel.Visible = false;
                continue;
            }

            if (!IsProgressStepsColumn(columnModel.FieldName))
                continue;

            // Persistent FieldName so CollectionSource can sort; CustomSort uses ProgressListSortKey (color + stage).
            var caption = columnModel.Caption;
            columnModel.FieldName = nameof(ApplicationProfileInstance.LatestPrimaryStateCode);
            columnModel.SortMode = GridColumnSortMode.Custom;
            columnModel.AllowSort = true;
            if (!string.IsNullOrWhiteSpace(caption))
                columnModel.Caption = caption;
            columnModel.MinWidth = 560;
            columnModel.Width = "640px";
            var resultColumn = gridListEditor.GridDataColumnModels.FirstOrDefault(c =>
                string.Equals(
                    c.FieldName,
                    nameof(ApplicationProfileInstance.ResultCoverageDisplay),
                    StringComparison.Ordinal));
            if (resultColumn != null)
                resultColumn.Visible = false;
            var lastOther = gridListEditor.GridDataColumnModels
                .Where(c => !ReferenceEquals(c, columnModel)
                    && !ReferenceEquals(c, resultColumn)
                    && c.Visible
                    && c.VisibleIndex >= 0)
                .Select(c => c.VisibleIndex)
                .DefaultIfEmpty(-1)
                .Max();
            columnModel.VisibleIndex = lastOther + 1;
            columnModel.CellDisplayTemplate = context => builder =>
            {
                if (context.DataItem is not ApplicationProfileInstance application)
                {
                    builder.AddContent(0, "\u2014");
                    return;
                }

                var steps = application.ListViewProgressSteps;
                if (steps.Count == 0)
                    steps = Frame.GetController<ApplicationListViewPreloadController>()?.ProgressStepsFor(application.ID)
                        ?? steps;
                if (steps.Count == 0)
                {
                    builder.AddContent(0, application.ProgressStepsDisplay);
                    return;
                }

                var chips = application.ListViewResultCoverageChips;
                if (chips.Count == 0)
                {
                    chips = Frame.GetController<ApplicationListViewPreloadController>()?.ResultChipsFor(application.ID)
                        ?? chips;
                }

                var showResultChips = ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(steps, chips);
                if (!showResultChips)
                    chips = Array.Empty<ApplicationWorkspaceIssuedResultListCoverage.Chip>();

                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", showResultChips && chips.Count > 0
                    ? "lv-progress-with-result"
                    : "lv-progress-with-result lv-progress-with-result--steps-only");
                builder.OpenElement(2, "div");
                builder.AddAttribute(3, "class", showResultChips && chips.Count > 0
                    ? "lv-progress-stepper lv-progress-stepper--with-stems"
                    : "lv-progress-stepper");
                var seq = 4;
                for (var i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    var tone = ApplicationWorkspaceListProgressSteps.Tone(step);
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "lv-progress-step lv-progress-step--" + tone);
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "lv-progress-step__track");
                    builder.OpenElement(seq++, "span");
                    builder.AddAttribute(seq++, "class", "lv-progress-step__dot");
                    builder.AddContent(seq++, ApplicationWorkspaceListProgressSteps.Glyph(step, i + 1));
                    builder.CloseElement();
                    if (i < steps.Count - 1)
                    {
                        var lineTone = string.Equals(step.State, "done", StringComparison.OrdinalIgnoreCase)
                            ? "done"
                            : "pending";
                        builder.OpenElement(seq++, "span");
                        builder.AddAttribute(seq++, "class", "lv-progress-step__line lv-progress-step__line--" + lineTone);
                        builder.CloseElement();
                    }

                    builder.CloseElement();
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "lv-progress-step__meta");
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "lv-progress-step__label");
                    builder.AddContent(seq++, step.Label);
                    builder.CloseElement();
                    if (!string.IsNullOrWhiteSpace(step.Date))
                    {
                        builder.OpenElement(seq++, "span");
                        builder.AddAttribute(seq++, "class", "lv-progress-step__date");
                        builder.AddContent(seq++, step.Date);
                        builder.CloseElement();
                    }
                    else if (!string.IsNullOrWhiteSpace(step.StatusLabel))
                    {
                        builder.OpenElement(seq++, "span");
                        builder.AddAttribute(seq++, "class", "lv-progress-step__status");
                        var statusText = step.StatusLabel;
                        var split = statusText.IndexOf(" /", StringComparison.Ordinal);
                        if (split > 0)
                        {
                            builder.AddContent(seq++, statusText[..split]);
                            builder.OpenElement(seq++, "span");
                            builder.AddAttribute(seq++, "class", "lv-progress-step__status-no");
                            builder.AddContent(seq++, statusText[(split + 1)..]);
                            builder.CloseElement();
                        }
                        else
                        {
                            builder.AddContent(seq++, statusText);
                        }

                        builder.CloseElement();
                    }

                    builder.CloseElement();
                    builder.CloseElement();
                }

                builder.CloseElement();

                if (showResultChips && chips.Count > 0)
                {
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "lv-result-neck");
                    builder.CloseElement();
                    ApplicationListViewResultChipRender.AppendStacked(builder, ref seq, chips);
                }

                builder.CloseElement();
            };
        }
    }

    private static bool IsProgressStepsColumn(string? fieldName) =>
        string.Equals(fieldName, nameof(ApplicationProfileInstance.ProgressStepsDisplay), StringComparison.Ordinal)
        || string.Equals(fieldName, nameof(ApplicationProfileInstance.ProgressListSortKey), StringComparison.Ordinal)
        || string.Equals(fieldName, nameof(ApplicationProfileInstance.LatestPrimaryStateCode), StringComparison.Ordinal);
}