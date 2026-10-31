using System.Linq;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Compact progress stepper. Header sort uses the same FieldName as Completion percentage
/// (one sort — no separate progress/CustomSort).
/// </summary>
public sealed class ApplicationListViewProgressStepsController : ViewController<ListView>
{
    private CancellationTokenSource? deferredApplyCts;

    public ApplicationListViewProgressStepsController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyStepperTemplate();
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

    internal void ApplyStepperTemplate()
    {
        if (View?.Editor is not DxGridListEditor gridListEditor)
            return;

        if (View.Editor is DxGridListEditor listEditor)
            listEditor.PreventServerSideOperationsForNonPersistentMembers = false;

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

            if (!IsProgressStepperColumn(columnModel))
                continue;

            // Same FieldName as Tamamlanma göterimi — one shared % sort (progress CustomSort ignored).
            var caption = columnModel.Caption;
            columnModel.FieldName = nameof(ApplicationProfileInstance.CompletionPercentage);
            columnModel.SortMode = GridColumnSortMode.Value;
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

            PlaceProgressThenPercent(gridListEditor, columnModel);
            Frame.GetController<ApplicationListViewCompletionPercentageController>()?.ApplyPercentTemplate();

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

                chips = ApplicationWorkspaceIssuedResultListCoverage.ForDisplay(chips);
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

                    if (!string.IsNullOrWhiteSpace(step.StatusLabel))
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

    private static void PlaceProgressThenPercent(DxGridListEditor gridListEditor, DxGridDataColumnModel progressColumn)
    {
        var percentColumn = gridListEditor.GridDataColumnModels.FirstOrDefault(c =>
            !ReferenceEquals(c, progressColumn)
            && string.Equals(c.FieldName, nameof(ApplicationProfileInstance.CompletionPercentage), StringComparison.Ordinal));

        var lastOther = gridListEditor.GridDataColumnModels
            .Where(c => !ReferenceEquals(c, progressColumn)
                && !ReferenceEquals(c, percentColumn)
                && c.Visible
                && c.VisibleIndex >= 0)
            .Select(c => c.VisibleIndex)
            .DefaultIfEmpty(-1)
            .Max();

        progressColumn.VisibleIndex = lastOther + 1;
        if (percentColumn != null)
            percentColumn.VisibleIndex = progressColumn.VisibleIndex + 1;
    }

    private static bool IsProgressStepperColumn(DxGridDataColumnModel column)
    {
        var fieldName = column.FieldName;
        if (string.Equals(fieldName, nameof(ApplicationProfileInstance.ProgressStepsDisplay), StringComparison.Ordinal)
            || string.Equals(fieldName, nameof(ApplicationProfileInstance.ProgressListSortKey), StringComparison.Ordinal)
            || string.Equals(fieldName, nameof(ApplicationProfileInstance.LatestPrimaryStateCode), StringComparison.Ordinal))
            return true;

        // After remap: stepper is the wide CompletionPercentage column (not the narrow % label).
        return string.Equals(fieldName, nameof(ApplicationProfileInstance.CompletionPercentage), StringComparison.Ordinal)
            && column.MinWidth >= 560;
    }
}
