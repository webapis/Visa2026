using System.Globalization;
using System.Linq;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Microsoft.AspNetCore.Components;
using Visa2026.Blazor.Server.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
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
    private int ignoreWidthSave;

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

        SuppressWidthSave();
        if (View?.Editor is DxGridListEditor { GridModel.ComponentInstance: { } grid })
            grid.AutoFitColumnWidths();
        RestoreProgressColumnWidth();
    }

    /// <summary>
    /// Best-fit runs after the list opens and would otherwise report those widths as a user resize.
    /// </summary>
    internal void SuppressWidthSave()
    {
        Interlocked.Increment(ref ignoreWidthSave);
        _ = ReleaseWidthSaveSuppressionAsync();
    }

    private async Task ReleaseWidthSaveSuppressionAsync()
    {
        try
        {
            await Task.Yield();
            await Task.Delay(500);
        }
        catch (OperationCanceledException)
        {
        }

        Interlocked.Decrement(ref ignoreWidthSave);
    }

    private void OnColumnWidthChanged(DxGridDataColumnModel column, string? widthText)
    {
        if (Volatile.Read(ref ignoreWidthSave) > 0)
            return;
        if (View is not { IsDisposed: false, Model: not null })
            return;
        if (!TryParsePx(widthText, out var pixels))
            return;

        var modelId = ModelColumnId(column);
        if (string.IsNullOrEmpty(modelId))
            return;

        if (string.Equals(modelId, ApplicationProfileInstanceProgressStepsColumnUpdater.ColumnId, StringComparison.Ordinal)
            && pixels < ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnMinWidth)
            pixels = ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnMinWidth;

        var modelColumn = View.Model.Columns[modelId];
        if (modelColumn == null || Math.Abs(modelColumn.Width - pixels) < 4)
            return;

        modelColumn.Width = pixels;
        var text = pixels.ToString(CultureInfo.InvariantCulture) + "px";
        if (!string.Equals(column.Width, text, StringComparison.Ordinal))
        {
            Interlocked.Increment(ref ignoreWidthSave);
            try
            {
                column.Width = text;
            }
            finally
            {
                Interlocked.Decrement(ref ignoreWidthSave);
            }
        }

        try
        {
            Application?.SaveModelChanges();
        }
        catch (InvalidOperationException)
        {
        }
    }

    internal void RestoreProgressColumnWidth()
    {
        SuppressWidthSave();
        if (View?.Editor is not DxGridListEditor editor || View.Model == null || editor.GridModel == null)
            return;

        foreach (var column in editor.GridDataColumnModels)
        {
            var modelId = ModelColumnId(column);
            if (string.IsNullOrEmpty(modelId))
                continue;

            var modelColumn = View.Model.Columns[modelId];
            if (modelColumn == null)
                continue;

            var progress = IsProgressStepperColumn(column);
            if (!progress && modelColumn is ModelNode node && !node.IsValueModified("Width"))
                continue;

            var pixels = progress
                ? SavedProgressColumnWidth()
                : modelColumn.Width;
            if (pixels <= 0)
                continue;

            if (progress && column.MinWidth != ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnMinWidth)
                column.MinWidth = ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnMinWidth;

            var text = pixels.ToString(CultureInfo.InvariantCulture) + "px";
            if (!string.Equals(column.Width, text, StringComparison.Ordinal))
                column.Width = text;
        }
    }

    private int SavedProgressColumnWidth()
    {
        var width = View?.Model?.Columns[ApplicationProfileInstanceProgressStepsColumnUpdater.ColumnId]?.Width ?? 0;
        if (width < ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnMinWidth)
            width = ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth;
        return width;
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
        // GridModel is not always ready on first OnViewControlsCreated / deferred apply.
        if (gridListEditor.GridModel == null)
        {
            ScheduleDeferredApply();
            return;
        }

        SuppressWidthSave();
        if (View.Editor is DxGridListEditor listEditor)
            listEditor.PreventServerSideOperationsForNonPersistentMembers = false;

        gridListEditor.GridModel.ColumnResizeMode = GridColumnResizeMode.ColumnsContainer;
        foreach (var columnModel in gridListEditor.GridDataColumnModels)
        {
            if (string.IsNullOrEmpty(columnModel.Name) && !string.IsNullOrEmpty(columnModel.FieldName))
                columnModel.Name = columnModel.FieldName;

            var captured = columnModel;
            captured.WidthChanged = EventCallback.Factory.Create<string>(this, width => OnColumnWidthChanged(captured, width));
        }

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
            columnModel.Name = ApplicationProfileInstanceProgressStepsColumnUpdater.ColumnId;
            columnModel.MinWidth = ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnMinWidth;
            columnModel.Width = SavedProgressColumnWidth().ToString(CultureInfo.InvariantCulture) + "px";
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

                var alignViaMinistry = string.Equals(
                    View.Id,
                    ApplicationProfileInstanceProgressRouteNavigation.ListViewViaMinistries,
                    StringComparison.Ordinal);
                if (alignViaMinistry)
                    steps = ApplicationWorkspaceListProgressSteps.ExpandViaMinistryTail(steps);

                var tailAt = alignViaMinistry
                    ? steps.ToList().FindIndex(step => ApplicationWorkspaceListProgressSteps.IsViaMinistryTailStep(step.Key))
                    : -1;
                if (tailAt < 0)
                    alignViaMinistry = false;

                var wrapperClass = showResultChips && chips.Count > 0
                    ? "lv-progress-with-result"
                    : "lv-progress-with-result lv-progress-with-result--steps-only";
                if (alignViaMinistry)
                    wrapperClass += " lv-progress-with-result--via-ministry";

                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", wrapperClass);
                builder.OpenElement(2, "div");
                var stepperClass = showResultChips && chips.Count > 0
                    ? "lv-progress-stepper lv-progress-stepper--with-stems"
                    : "lv-progress-stepper";
                if (alignViaMinistry)
                    stepperClass += " lv-progress-stepper--via-ministry";
                builder.AddAttribute(3, "class", stepperClass);
                var seq = 4;
                var ownerViewId = View.Id;
                var applicationId = application.ID;

                void RenderSteps(IReadOnlyList<ApplicationWorkspaceListProgressSteps.Step> group, int numberFrom, bool lineAfterLast)
                {
                    for (var i = 0; i < group.Count; i++)
                    {
                        var step = group[i];
                        var tone = ApplicationWorkspaceListProgressSteps.Tone(step);
                        var drawLine = i < group.Count - 1 || lineAfterLast;
                        builder.OpenElement(seq++, "div");
                        builder.AddAttribute(seq++, "class", "lv-progress-step lv-progress-step--" + tone);
                        builder.OpenElement(seq++, "div");
                        builder.AddAttribute(seq++, "class", "lv-progress-step__track");
                        builder.OpenElement(seq++, "span");
                        builder.AddAttribute(seq++, "class", "lv-progress-step__dot");
                        builder.AddContent(seq++, ApplicationWorkspaceListProgressSteps.Glyph(step, numberFrom + i));
                        builder.CloseElement();
                        if (drawLine)
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

                        if (step.HasDocuments)
                        {
                            var viewFiles = VisaUiMessages.Get("PersonDossier.Progress.ViewFile");
                            builder.OpenComponent<ProgressMinistryLetterPreviewLink>(seq++);
                            builder.AddAttribute(seq++, nameof(ProgressMinistryLetterPreviewLink.ApplicationProfileInstanceId), applicationId);
                            builder.AddAttribute(seq++, nameof(ProgressMinistryLetterPreviewLink.ProgressId), (Guid?)step.DocumentProgressId);
                            builder.AddAttribute(seq++, nameof(ProgressMinistryLetterPreviewLink.PreviewAll), true);
                            builder.AddAttribute(seq++, nameof(ProgressMinistryLetterPreviewLink.LinkText), viewFiles);
                            builder.AddAttribute(seq++, nameof(ProgressMinistryLetterPreviewLink.FileName), viewFiles);
                            builder.AddAttribute(seq++, nameof(ProgressMinistryLetterPreviewLink.OwnerViewId), ownerViewId);
                            builder.AddAttribute(seq++, nameof(ProgressMinistryLetterPreviewLink.CssClass), "lv-progress-step__files");
                            builder.CloseComponent();
                        }

                        builder.CloseElement();
                        builder.CloseElement();
                    }
                }

                if (alignViaMinistry)
                {
                    var lead = steps.Take(tailAt).ToList();
                    var tail = steps.Skip(tailAt).ToList();
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "lv-progress-stepper__lead");
                    RenderSteps(lead, 1, lineAfterLast: lead.Count > 0 && tail.Count > 0);
                    builder.CloseElement();
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "lv-progress-stepper__tail");
                    RenderSteps(tail, lead.Count + 1, lineAfterLast: false);
                    builder.CloseElement();
                }
                else
                {
                    RenderSteps(steps, 1, lineAfterLast: false);
                }

                builder.CloseElement();

                if (alignViaMinistry || (showResultChips && chips.Count > 0))
                {
                    if (alignViaMinistry)
                    {
                        builder.OpenElement(seq++, "div");
                        builder.AddAttribute(seq++, "class", "lv-result-slot");
                    }

                    if (showResultChips && chips.Count > 0)
                    {
                        builder.OpenElement(seq++, "div");
                        builder.AddAttribute(seq++, "class", "lv-result-neck");
                        builder.CloseElement();
                        ApplicationListViewResultChipRender.AppendStacked(builder, ref seq, chips);
                    }

                    if (alignViaMinistry)
                        builder.CloseElement();
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

    private static string? ModelColumnId(DxGridDataColumnModel column)
    {
        if (IsProgressStepperColumn(column))
            return ApplicationProfileInstanceProgressStepsColumnUpdater.ColumnId;
        if (!string.IsNullOrEmpty(column.Name))
            return column.Name;
        return column.FieldName;
    }

    private static bool TryParsePx(string? text, out int pixels)
    {
        pixels = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        if (text.EndsWith('%'))
            return false;
        if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            text = text[..^2].Trim();

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value <= 0)
            return false;

        pixels = (int)Math.Round(value);
        return pixels > 0;
    }

    private static bool IsProgressStepperColumn(DxGridDataColumnModel column)
    {
        if (string.Equals(
                column.Name,
                ApplicationProfileInstanceProgressStepsColumnUpdater.ColumnId,
                StringComparison.Ordinal))
            return true;

        var fieldName = column.FieldName;
        return string.Equals(fieldName, nameof(ApplicationProfileInstance.ProgressStepsDisplay), StringComparison.Ordinal)
            || string.Equals(fieldName, nameof(ApplicationProfileInstance.ProgressListSortKey), StringComparison.Ordinal)
            || string.Equals(fieldName, nameof(ApplicationProfileInstance.LatestPrimaryStateCode), StringComparison.Ordinal);
    }
}
