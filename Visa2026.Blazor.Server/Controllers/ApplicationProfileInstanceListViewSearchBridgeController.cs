using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// DxGrid SearchBox only matches visible columns (person names and progress
/// result numbers are not). Route Application Profile Instance ListView grid
/// search through XAF FullTextSearch so linked-people and Netije belgisi criteria apply.
/// Also notifies the progress color-tab strip so Search/Gözleg can host cross-color hits.
/// </summary>
public sealed class ApplicationProfileInstanceListViewSearchBridgeController
    : ObjectViewController<ListView, ApplicationProfileInstance>
{
    private bool wired;

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
        Frame.GetController<ApplicationProfileInstanceProgressColorTabsController>()
            ?.NotifySearchTextChanged(searchText);
    }
}