using System.Globalization;
using System.Linq;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Model;
using Visa2026.Module.Services;
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

        // Custom progress CellDisplayTemplate column — fixed width, not officer-resizable.
        if (IsProgressStepperColumn(column))
        {
            SnapProgressColumnWidth(column);
            return;
        }

        if (!TryParsePx(widthText, out var pixels))
            return;

        var modelId = ModelColumnId(column);
        if (string.IsNullOrEmpty(modelId))
            return;

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

    private void SnapProgressColumnWidth(DxGridDataColumnModel column)
    {
        var fixedPx = ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth;
        var text = fixedPx.ToString(CultureInfo.InvariantCulture) + "px";
        Interlocked.Increment(ref ignoreWidthSave);
        try
        {
            if (column.MinWidth != fixedPx)
                column.MinWidth = fixedPx;
            if (!string.Equals(column.Width, text, StringComparison.Ordinal))
                column.Width = text;

            var modelColumn = View?.Model?.Columns[ApplicationProfileInstanceProgressStepsColumnUpdater.ColumnId];
            if (modelColumn != null && modelColumn.Width != fixedPx)
                modelColumn.Width = fixedPx;
        }
        finally
        {
            Interlocked.Decrement(ref ignoreWidthSave);
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
                ? ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth
                : modelColumn.Width;
            if (pixels <= 0)
                continue;

            if (progress)
            {
                if (column.MinWidth != ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth)
                    column.MinWidth = ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth;
                if (modelColumn.Width != ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth)
                    modelColumn.Width = ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth;
            }

            var text = pixels.ToString(CultureInfo.InvariantCulture) + "px";
            if (!string.Equals(column.Width, text, StringComparison.Ordinal))
                column.Width = text;
        }
    }

    private static int FixedProgressColumnWidth() =>
        ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth;

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
            columnModel.MinWidth = ApplicationProfileInstanceProgressStepsColumnUpdater.ProgressColumnWidth;
            columnModel.Width = FixedProgressColumnWidth().ToString(CultureInfo.InvariantCulture) + "px";
            var resultColumn = gridListEditor.GridDataColumnModels.FirstOrDefault(c =>
                string.Equals(
                    c.FieldName,
                    nameof(ApplicationProfileInstance.ResultCoverageDisplay),
                    StringComparison.Ordinal));
            if (resultColumn != null)
                resultColumn.Visible = false;

            PlaceProgressThenPercent(gridListEditor, columnModel);
            Frame.GetController<ApplicationListViewCompletionPercentageController>()?.ApplyPercentTemplate();

            columnModel.Caption = VisaUiMessages.Get("ApplicationProfileInstance.StatePreview.Open");
            columnModel.CellDisplayTemplate = context => builder =>
            {
                if (context.DataItem is not ApplicationProfileInstance application)
                {
                    builder.AddContent(0, "\u2014");
                    return;
                }

                var id = application.ID;
                var label = VisaUiMessages.Get("ApplicationProfileInstance.StatePreview.Open");
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "class", "lv-state-preview-link");
                builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => OpenStatePreview(id)));
                builder.AddContent(4, label);
                builder.CloseElement();
            };
        }
    }


    /// <summary>
    /// Re-run State / State-preview / % column order (e.g. after Ähli shows ProgressColorTabDisplay).
    /// </summary>
    public void ReapplyColumnPlacement()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: not null } gridListEditor)
            return;

        var progressColumn = gridListEditor.GridDataColumnModels.FirstOrDefault(IsProgressStepperColumn);
        if (progressColumn == null)
            return;

        PlaceProgressThenPercent(gridListEditor, progressColumn);
    }
    private void OpenStatePreview(Guid instanceId)
    {
        ApplicationListRowActivationGate.SuppressBriefly();
        Application?.ServiceProvider
            ?.GetService<IApplicationProfileInstanceStatePreviewService>()
            ?.Open(instanceId, View?.Id);
    }

    private static void PlaceProgressThenPercent(DxGridListEditor gridListEditor, DxGridDataColumnModel progressColumn)
    {
        var percentColumn = gridListEditor.GridDataColumnModels.FirstOrDefault(c =>
            !ReferenceEquals(c, progressColumn)
            && string.Equals(c.FieldName, nameof(ApplicationProfileInstance.CompletionPercentage), StringComparison.Ordinal));

        var stateColumn = gridListEditor.GridDataColumnModels.FirstOrDefault(c =>
            string.Equals(
                c.FieldName,
                nameof(ApplicationProfileInstance.ProgressColorTabDisplay),
                StringComparison.Ordinal));

        var lastOther = gridListEditor.GridDataColumnModels
            .Where(c => !ReferenceEquals(c, progressColumn)
                && !ReferenceEquals(c, percentColumn)
                && !ReferenceEquals(c, stateColumn)
                && c.Visible
                && c.VisibleIndex >= 0)
            .Select(c => c.VisibleIndex)
            .DefaultIfEmpty(-1)
            .Max();

        // State (color-tab label) immediately before State preview when Ähli shows it.
        if (stateColumn != null && stateColumn.Visible)
        {
            stateColumn.AllowSort = true;
            stateColumn.SortMode = GridColumnSortMode.Value;
            stateColumn.VisibleIndex = lastOther + 1;
            progressColumn.VisibleIndex = stateColumn.VisibleIndex + 1;
        }
        else
        {
            progressColumn.VisibleIndex = lastOther + 1;
        }

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