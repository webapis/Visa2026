using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Routes Çakylyklar grid search through FullTextSearch and notifies color tabs (Gözleg).
/// </summary>
public sealed class InvitationItemListViewSearchBridgeController
    : ObjectViewController<ListView, InvitationItem>
{
    private bool wired;

    public InvitationItemListViewSearchBridgeController()
    {
        TargetViewId = InvitationItemCakylyklarNavigation.TargetViewIds;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (wired)
            return;

        if (View.Editor is not DxGridListEditor gridListEditor)
            return;

        foreach (var column in gridListEditor.GridDataColumnModels)
            column.SearchEnabled = false;

        gridListEditor.GridModel.SearchTextChanged = EventCallback.Factory.Create<string>(this, OnGridSearchTextChanged);
        wired = true;
    }

    protected override void OnDeactivated()
    {
        wired = false;
        base.OnDeactivated();
    }

    private void OnGridSearchTextChanged(string searchText)
    {
        var filterController = Frame.GetController<FilterController>();
        filterController?.FullTextFilterAction?.DoExecute(searchText ?? string.Empty);
        Frame.GetController<InvitationItemColorTabsController>()
            ?.NotifySearchTextChanged(searchText);
    }
}