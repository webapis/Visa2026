using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using System.ComponentModel;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Work Permits registry CO cell: process number · date opens the Application Profile Instance workspace.
/// </summary>
public sealed class WorkPermitItemRegistryApplicationLinkController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private CancellationTokenSource? deferredApplyCts;
    private ListViewProcessCurrentObjectController? processCurrentObjectController;

    public WorkPermitItemRegistryApplicationLinkController()
    {
        TargetViewId = WorkPermitItemRegistryNavigation.TargetViewIds;
        TargetObjectType = typeof(WorkPermitItem);
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
        ApplyLinkClickHandlers();
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
            ApplyLinkClickHandlers();
    }

    private void OnCustomHandleProcessSelectedItem(object? sender, HandledEventArgs e)
    {
        if (HeaderDocumentCopiesLinkClickGate.ConsumePending())
            e.Handled = true;
    }

    private void ApplyLinkClickHandlers()
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
            ApplyApplicationLinkStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private static void ApplyApplicationLinkStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataCell || e.VisibleIndex < 0)
            return;

        var fieldName = e.Column is DxGridDataColumn dataColumn
            ? dataColumn.FieldName
            : e.Column?.Name;
        if (!string.Equals(
                fieldName,
                nameof(WorkPermitItem.RegistryProcessNumberDisplay),
                StringComparison.Ordinal))
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not WorkPermitItem item)
            return;

        var applicationId = item.ListViewApplicationProfileInstanceId
            ?? (item.WorkPermit?.ApplicationProfileInstance != null
                && item.WorkPermit.ApplicationProfileInstance.ID != Guid.Empty
                    ? item.WorkPermit.ApplicationProfileInstance.ID
                    : null);
        if (applicationId is not Guid id || id == Guid.Empty)
            return;

        if (string.IsNullOrWhiteSpace(item.RegistryProcessNumberDisplay))
            return;

        const string linkClass = "app-cakylyklar-application-link";
        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? linkClass : $"{e.CssClass} {linkClass}";
        e.Attributes["role"] = "button";
        e.Attributes["tabindex"] = "0";
        e.Attributes["title"] = item.RegistryProcessNumberDisplay;
        e.Attributes["aria-label"] = item.RegistryProcessNumberDisplay;
        e.Attributes["data-application-id"] = id.ToString("D");
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