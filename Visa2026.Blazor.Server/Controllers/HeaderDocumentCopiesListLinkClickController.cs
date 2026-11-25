using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using System.ComponentModel;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
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
            + InvitationItemCakylyklarNavigation.TargetViewIds + ";" + WorkPermitItemRegistryNavigation.TargetViewIds + ";"
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

        if (!TryResolveLinkColumn(e.Column, out var linkKind))
            return;

        var row = e.Grid.GetDataItem(e.VisibleIndex);
        if (!TryResolveLinkTarget(row, linkKind, out var family, out var parentId, out var contextItemId))
            return;

        const string linkClass = "app-header-document-copies-link";
        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? linkClass : $"{e.CssClass} {linkClass}";
        if (linkKind is CakylyklarLinkKind.InvitationNumber
            or CakylyklarLinkKind.UsedVisaNumber
            or CakylyklarLinkKind.WorkPermitNumber)
            e.CssClass += " app-header-document-copies-link--text";
        e.Attributes["role"] = "button";
        e.Attributes["tabindex"] = "0";
        var label = HeaderDocumentCopiesLocalization.Title(family);
        e.Attributes["title"] = label;
        e.Attributes["aria-label"] = label;
        e.Attributes["data-header-doc-family"] = family.ToString();
        e.Attributes["data-header-doc-parent-id"] = parentId.ToString("D");
        if (contextItemId is Guid contextId && contextId != Guid.Empty)
            e.Attributes["data-header-doc-context-item-id"] = contextId.ToString("D");

        // Çakylyklar: open the scan in the slot immediately (no catalog → Preview click).
        if ((InvitationItemCakylyklarNavigation.IsCakylyklarListView(View?.Id) || WorkPermitItemRegistryNavigation.IsRegistryListView(View?.Id)))
        {
            e.Attributes["data-header-doc-preview-only"] = "1";
        }
    }

    private bool TryResolveLinkColumn(IGridColumn? column, out CakylyklarLinkKind linkKind)
    {
        linkKind = CakylyklarLinkKind.DocumentCopies;
        if (column == null)
            return false;

        var fieldName = column is DxGridDataColumn dataColumn
            ? dataColumn.FieldName
            : column.Name;

        if (string.Equals(fieldName, "DocumentCopiesListLink", StringComparison.Ordinal))
            return true;

        if (!(InvitationItemCakylyklarNavigation.IsCakylyklarListView(View?.Id) || WorkPermitItemRegistryNavigation.IsRegistryListView(View?.Id)))
            return false;

        // Çakylyklar: Invitation № → invitation letter preview.
        if (string.Equals(
                fieldName,
                nameof(InvitationItem.CakylyklarInvitationNumberDisplay),
                StringComparison.Ordinal))
        {
            linkKind = CakylyklarLinkKind.InvitationNumber;
            return true;
        }

        // Çakylyklar Used: Linked detail (visa number) → visa beside passport.
        if (string.Equals(
                fieldName,
                nameof(InvitationItem.CakylyklarLinkedDetailDisplay),
                StringComparison.Ordinal))
        {
            linkKind = CakylyklarLinkKind.UsedVisaNumber;
            return true;
        }

        // Çakylyklar: Work permit № → work-permit copies preview.
        if (string.Equals(
                fieldName,
                nameof(InvitationItem.CakylyklarWorkPermitDisplay),
                StringComparison.Ordinal))
        {
            linkKind = CakylyklarLinkKind.WorkPermitNumber;
            return true;
        }
        // Work Permits registry: Work permit № → work-permit copies preview.
        if (WorkPermitItemRegistryNavigation.IsRegistryListView(View?.Id)
            && string.Equals(
                fieldName,
                nameof(WorkPermitItem.RegistryWorkPermitNumberDisplay),
                StringComparison.Ordinal))
        {
            linkKind = CakylyklarLinkKind.WorkPermitNumber;
            return true;
        }
        return false;
    }

    private static bool TryResolveLinkTarget(
        object? row,
        CakylyklarLinkKind linkKind,
        out HeaderDocumentCopiesFamily family,
        out Guid parentId,
        out Guid? contextItemId)
    {
        family = default;
        parentId = Guid.Empty;
        contextItemId = null;

        if (linkKind == CakylyklarLinkKind.UsedVisaNumber)
        {
            if (row is not InvitationItem item)
                return false;

            // Only Used rows with a stamped visa id become links.
            if (!string.Equals(item.ListViewLifeKey, InvitationLaterLife.Used, StringComparison.Ordinal))
                return false;

            var visaId = item.ListViewVisaId
                ?? (item.IssuedVisa != null && item.IssuedVisa.ID != Guid.Empty ? item.IssuedVisa.ID : null);
            if (visaId is not Guid id || id == Guid.Empty)
                return false;

            family = HeaderDocumentCopiesFamily.Visa;
            parentId = id;
            return true;
        }

        if (linkKind == CakylyklarLinkKind.WorkPermitNumber)
        {
            if (row is InvitationItem invItem)
            {
                if (string.IsNullOrWhiteSpace(invItem.CakylyklarWorkPermitDisplay))
                    return false;

                var workPermitId = invItem.ListViewWorkPermitId;
                if (workPermitId is not Guid wpId || wpId == Guid.Empty)
                    return false;

                family = HeaderDocumentCopiesFamily.WorkPermit;
                parentId = wpId;
                contextItemId = invItem.ListViewWorkPermitItemId is Guid itemId && itemId != Guid.Empty
                    ? itemId
                    : null;
                return true;
            }

            if (row is WorkPermitItem wpItem)
            {
                if (string.IsNullOrWhiteSpace(wpItem.RegistryWorkPermitNumberDisplay))
                    return false;

                var workPermitId = wpItem.ListViewWorkPermitId
                    ?? (wpItem.WorkPermit != null && wpItem.WorkPermit.ID != Guid.Empty
                        ? wpItem.WorkPermit.ID
                        : null);
                if (workPermitId is not Guid wpId || wpId == Guid.Empty)
                    return false;

                family = HeaderDocumentCopiesFamily.WorkPermit;
                parentId = wpId;
                contextItemId = wpItem.ID != Guid.Empty ? wpItem.ID : null;
                return true;
            }

            return false;
        }

        return HeaderDocumentCopiesListLinkResolution.TryResolve(row, out family, out parentId, out contextItemId);
    }

    private enum CakylyklarLinkKind
    {
        DocumentCopies,
        InvitationNumber,
        UsedVisaNumber,
        WorkPermitNumber,
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