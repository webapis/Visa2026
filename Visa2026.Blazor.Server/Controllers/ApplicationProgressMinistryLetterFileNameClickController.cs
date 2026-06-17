using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using System.ComponentModel;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Controllers;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Makes ministry letter file names clickable in progress history grids; opens the progress letters catalog slot.
/// </summary>
public sealed class ApplicationProgressMinistryLetterFileNameClickController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private CancellationTokenSource? deferredApplyCts;
    private ListViewProcessCurrentObjectController? processCurrentObjectController;

    public ApplicationProgressMinistryLetterFileNameClickController()
    {
        TargetObjectType = typeof(ApplicationProgress);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        processCurrentObjectController = Frame.GetController<ListViewProcessCurrentObjectController>();
        if (processCurrentObjectController != null)
            processCurrentObjectController.CustomHandleProcessSelectedItem += OnCustomHandleProcessSelectedItem;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyFileNameClickHandlers();
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
            ApplyFileNameClickHandlers();
    }

    private void OnCustomHandleProcessSelectedItem(object? sender, HandledEventArgs e)
    {
        if (ProgressLetterLinkClickGate.ConsumePending())
            e.Handled = true;
    }

    private void ApplyFileNameClickHandlers()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
            return;

        if (customizeElementHandler != null)
        {
            gridModel.CustomizeElement = previousCustomizeElement;
            customizeElementHandler = null;
            previousCustomizeElement = null;
        }

        previousCustomizeElement = gridModel.CustomizeElement;
        customizeElementHandler = e =>
        {
            previousCustomizeElement?.Invoke(e);
            ApplyFileNameCellStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private void ApplyFileNameCellStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataCell || e.VisibleIndex < 0)
            return;

        if (!IsMinistryLetterFileNameColumn(e.Column))
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not ApplicationProgress progress)
            return;

        if (progress.MinistryLetterFile == null || string.IsNullOrWhiteSpace(progress.MinistryLetterFileName))
            return;

        var applicationId = ApplicationProgressParentContext.GetApplicationId(Frame, ObjectSpace, View);
        if (applicationId == Guid.Empty && progress.Application != null)
        {
            var key = ObjectSpace.GetKeyValue(progress.Application);
            if (key is Guid guid)
                applicationId = guid;
        }

        var linkClass = "app-progress-letter-link";
        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? linkClass : $"{e.CssClass} {linkClass}";
        e.Attributes["role"] = "button";
        e.Attributes["tabindex"] = "0";
        e.Attributes["title"] = progress.MinistryLetterFileName;
        e.Attributes["data-application-id"] = applicationId.ToString("D");
        e.Attributes["data-progress-id"] = progress.ID.ToString("D");
    }

    private static bool IsMinistryLetterFileNameColumn(IGridColumn? column)
    {
        if (column == null)
            return false;

        if (column is DxGridDataColumn dataColumn)
        {
            return string.Equals(
                dataColumn.FieldName,
                nameof(ApplicationProgress.MinistryLetterFileName),
                StringComparison.Ordinal);
        }

        return string.Equals(
            column.Name,
            nameof(ApplicationProgress.MinistryLetterFileName),
            StringComparison.Ordinal);
    }

    protected override void OnDeactivated()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = null;

        if (processCurrentObjectController != null)
        {
            processCurrentObjectController.CustomHandleProcessSelectedItem -= OnCustomHandleProcessSelectedItem;
            processCurrentObjectController = null;
        }

        if (customizeElementHandler != null
            && View?.Editor is DxGridListEditor { GridModel: { } gridModel })
        {
            gridModel.CustomizeElement = previousCustomizeElement;
        }

        customizeElementHandler = null;
        previousCustomizeElement = null;
        base.OnDeactivated();
    }
}
