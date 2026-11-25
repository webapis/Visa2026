using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using System.ComponentModel;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Çakylyklar CO → workspace; Application column → Application Profile Instance DetailView.
/// </summary>
public sealed class InvitationItemCakylyklarApplicationLinkController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private CancellationTokenSource? deferredApplyCts;
    private ListViewProcessCurrentObjectController? processCurrentObjectController;

    public InvitationItemCakylyklarApplicationLinkController()
    {
        TargetViewId = InvitationItemCakylyklarNavigation.TargetViewIds;
        TargetObjectType = typeof(InvitationItem);
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
            ApplyProcessNumberLinkStyle(e);
            ApplyApplicationDetailLinkStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private static Guid? ResolveApplicationId(InvitationItem item) =>
        item.ListViewApplicationProfileInstanceId
        ?? (item.Invitation?.ApplicationProfileInstance != null
            && item.Invitation.ApplicationProfileInstance.ID != Guid.Empty
                ? item.Invitation.ApplicationProfileInstance.ID
                : null);

    private static void ApplyProcessNumberLinkStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataCell || e.VisibleIndex < 0)
            return;

        var fieldName = e.Column is DxGridDataColumn dataColumn
            ? dataColumn.FieldName
            : e.Column?.Name;
        if (!string.Equals(
                fieldName,
                nameof(InvitationItem.CakylyklarProcessNumberDisplay),
                StringComparison.Ordinal))
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not InvitationItem item)
            return;

        var applicationId = ResolveApplicationId(item);
        if (applicationId is not Guid id || id == Guid.Empty)
            return;

        if (string.IsNullOrWhiteSpace(item.CakylyklarProcessNumberDisplay))
            return;

        const string linkClass = "app-cakylyklar-application-link";
        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? linkClass : $"{e.CssClass} {linkClass}";
        e.Attributes["role"] = "button";
        e.Attributes["tabindex"] = "0";
        e.Attributes["title"] = item.CakylyklarProcessNumberDisplay;
        e.Attributes["aria-label"] = item.CakylyklarProcessNumberDisplay;
        e.Attributes["data-application-id"] = id.ToString("D");
    }

    private static void ApplyApplicationDetailLinkStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataCell || e.VisibleIndex < 0)
            return;

        var fieldName = e.Column is DxGridDataColumn dataColumn
            ? dataColumn.FieldName
            : e.Column?.Name;
        if (!string.Equals(
                fieldName,
                nameof(InvitationItem.CakylyklarApplicationDisplay),
                StringComparison.Ordinal))
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not InvitationItem item)
            return;

        var applicationId = ResolveApplicationId(item);
        if (applicationId is not Guid id || id == Guid.Empty)
            return;

        if (string.IsNullOrWhiteSpace(item.CakylyklarApplicationDisplay))
            return;

        const string linkClass = "app-cakylyklar-application-detail-link";
        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? linkClass : $"{e.CssClass} {linkClass}";
        e.Attributes["role"] = "button";
        e.Attributes["tabindex"] = "0";
        e.Attributes["title"] = item.CakylyklarApplicationDisplay;
        e.Attributes["aria-label"] = item.CakylyklarApplicationDisplay;
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