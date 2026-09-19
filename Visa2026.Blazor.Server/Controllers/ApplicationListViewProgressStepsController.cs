using System.Linq;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Renders the compact workspace progress stepper in the instance ListView column.
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

    private void ApplyStepperTemplate()
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

            if (!string.Equals(
                    columnModel.FieldName,
                    nameof(ApplicationProfileInstance.ProgressStepsDisplay),
                    StringComparison.Ordinal))
                continue;

            columnModel.AllowSort = false;
            columnModel.MinWidth = 420;
            columnModel.Width = "520px";
            var resultColumn = gridListEditor.GridDataColumnModels.FirstOrDefault(c =>
                string.Equals(
                    c.FieldName,
                    nameof(ApplicationProfileInstance.ResultCoverageDisplay),
                    StringComparison.Ordinal));
            var lastOther = gridListEditor.GridDataColumnModels
                .Where(c => !ReferenceEquals(c, columnModel)
                    && !ReferenceEquals(c, resultColumn)
                    && c.VisibleIndex >= 0)
                .Select(c => c.VisibleIndex)
                .DefaultIfEmpty(-1)
                .Max();
            columnModel.VisibleIndex = lastOther + 1;
            if (resultColumn != null)
                resultColumn.VisibleIndex = lastOther + 2;
            columnModel.CellDisplayTemplate = context => builder =>
            {
                if (context.DataItem is not ApplicationProfileInstance application)
                {
                    builder.AddContent(0, "\u2014");
                    return;
                }

                var steps = application.ListViewProgressSteps;
                if (steps.Count == 0)
                {
                    builder.AddContent(0, application.ProgressStepsDisplay);
                    return;
                }

                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "lv-progress-stepper");
                var seq = 2;
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
                        builder.AddContent(seq++, step.StatusLabel);
                        builder.CloseElement();
                    }

                    builder.CloseElement();
                    builder.CloseElement();
                }

                builder.CloseElement();
            };
        }
    }
}