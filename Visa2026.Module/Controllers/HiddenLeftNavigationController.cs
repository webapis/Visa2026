using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Removes retired operational lists from the left menu for every role, including Administrators.
/// Records stay available from a person or an application.
/// </summary>
public sealed class HiddenLeftNavigationController : WindowController
{
    private const string Reason = "HiddenFromLeftNav";
    private bool _applying;

    private static readonly HashSet<string> HiddenTopLevelIds = new(StringComparer.Ordinal)
    {
        "Invitation",
        "InvitationItem",
        "Rejection",
        "RejectionItem",
        "BorderZone",
        "BorderZoneItem",
        "TemporaryVisitors",
        "Visa Tracking",
        "Work Permit Tracking",
    };

    private static readonly HashSet<string> HiddenViewIds = new(StringComparer.Ordinal)
    {
        "Invitation_ListView",
        "InvitationItem_ListView",
        "WorkPermit_ListView",
        "WorkPermitItem_ListView",
        "Rejection_ListView",
        "RejectionItem_ListView",
        "BorderZone_ListView",
        "BorderZoneItem_ListView",
        "Person_ListView_TemporaryVisitors",
        "VisaExtensionStatus_ListView",
        "VisaExtensionTracking_ListView",
        "VisaTransferStatus_ListView",
        "VisaCancelExtStatus_ListView",
        "VisaCancellationStatus_ListView",
        "WorkPermitExtensionStatus_ListView",
        "WorkPermitExtensionTracking_ListView",
    };

    public HiddenLeftNavigationController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        var navigation = Frame.GetController<ShowNavigationItemController>();
        if (navigation == null)
            return;

        navigation.ItemsInitialized += OnItemsInitialized;
        if (navigation.ShowNavigationItemAction != null)
            navigation.ShowNavigationItemAction.ItemsChanged += OnItemsChanged;

        Apply(navigation.ShowNavigationItemAction?.Items, parentId: null);
    }

    protected override void OnDeactivated()
    {
        var navigation = Frame.GetController<ShowNavigationItemController>();
        if (navigation != null)
        {
            navigation.ItemsInitialized -= OnItemsInitialized;
            if (navigation.ShowNavigationItemAction != null)
                navigation.ShowNavigationItemAction.ItemsChanged -= OnItemsChanged;
        }

        base.OnDeactivated();
    }

    private void OnItemsInitialized(object sender, EventArgs e) =>
        Apply((sender as ShowNavigationItemController)?.ShowNavigationItemAction?.Items, parentId: null);

    private void OnItemsChanged(object sender, EventArgs e)
    {
        if (_applying)
            return;

        Apply((sender as ChoiceActionBase)?.Items, parentId: null);
    }

    private void Apply(ChoiceActionItemCollection items, string parentId)
    {
        if (items == null || _applying)
            return;

        _applying = true;
        try
        {
            HideItems(items, parentId);
        }
        finally
        {
            _applying = false;
        }
    }

    private static void HideItems(ChoiceActionItemCollection items, string parentId)
    {
        foreach (ChoiceActionItem item in items)
        {
            if (ShouldHide(item, parentId)
                && (!item.Active.Contains(Reason) || item.Active[Reason]))
            {
                item.Active[Reason] = false;
            }

            if (item.Items.Count > 0)
                HideItems(item.Items, item.Id);
        }
    }

    private static bool ShouldHide(ChoiceActionItem item, string parentId)
    {
        if (item.Id == "TemporaryVisitors")
            return true;

        if (parentId == null && HiddenTopLevelIds.Contains(item.Id))
            return true;

        if (parentId == "WorkPermit" && item.Id is "WorkPermit" or "WorkPermitItem")
            return true;

        return item.Data is IModelNavigationItem model
            && model.View?.Id is string viewId
            && HiddenViewIds.Contains(viewId);
    }
}