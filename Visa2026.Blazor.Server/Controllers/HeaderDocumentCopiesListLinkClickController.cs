using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using System.ComponentModel;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Makes the header document copies ListView link column open the preview slot for that row.
/// </summary>
public sealed class HeaderDocumentCopiesListLinkClickController : ViewController<ListView>
{
    public HeaderDocumentCopiesListLinkClickController()
    {
        TargetViewId =
            "WorkPermit_ListView;WorkPermitItem_ListView;"
            + "Invitation_ListView;InvitationItem_ListView;"
            + "Rejection_ListView;RejectionItem_ListView;"
            + "BorderZone_ListView;BorderZoneItem_ListView";
    }

    private static readonly HashSet<Type> SupportedTypes = new()
    {
        typeof(WorkPermit),
        typeof(WorkPermitItem),
        typeof(Invitation),
        typeof(InvitationItem),
        typeof(Rejection),
        typeof(RejectionItem),
        typeof(BorderZone),
        typeof(BorderZoneItem),
    };

    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private CancellationTokenSource? deferredApplyCts;
    private ListViewProcessCurrentObjectController? processCurrentObjectController;

    protected override void OnActivated()
    {
        base.OnActivated();
        Active["SupportedType"] = SupportedTypes.Contains(View.ObjectTypeInfo.Type);

        if (!Active["SupportedType"])
            return;

        processCurrentObjectController = Frame.GetController<ListViewProcessCurrentObjectController>();
        if (processCurrentObjectController != null)
            processCurrentObjectController.CustomHandleProcessSelectedItem += OnCustomHandleProcessSelectedItem;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (!Active["SupportedType"])
            return;

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

        if (View is { IsDisposed: false } && Active["SupportedType"])
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
            ApplyLinkCellStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private void ApplyLinkCellStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataCell || e.VisibleIndex < 0)
            return;

        if (!IsDocumentCopiesLinkColumn(e.Column))
            return;

        var row = e.Grid.GetDataItem(e.VisibleIndex);
        if (!HeaderDocumentCopiesListLinkResolution.TryResolve(row, out var family, out var parentId, out var contextItemId))
            return;

        const string linkClass = "app-header-document-copies-link";
        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? linkClass : $"{e.CssClass} {linkClass}";
        e.Attributes["role"] = "button";
        e.Attributes["tabindex"] = "0";
        var label = HeaderDocumentCopiesLocalization.Title(family);
        e.Attributes["title"] = label;
        e.Attributes["aria-label"] = label;
        e.Attributes["data-header-doc-family"] = family.ToString();
        e.Attributes["data-header-doc-parent-id"] = parentId.ToString("D");
        if (contextItemId is Guid contextId && contextId != Guid.Empty)
            e.Attributes["data-header-doc-context-item-id"] = contextId.ToString("D");
    }

    private static bool IsDocumentCopiesLinkColumn(IGridColumn? column)
    {
        if (column == null)
            return false;

        if (column is DxGridDataColumn dataColumn)
        {
            return string.Equals(
                dataColumn.FieldName,
                "DocumentCopiesListLink",
                StringComparison.Ordinal);
        }

        return string.Equals(column.Name, "DocumentCopiesListLink", StringComparison.Ordinal);
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
