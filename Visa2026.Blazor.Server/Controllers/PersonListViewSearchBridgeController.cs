using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// DxGrid SearchBox only matches visible columns (passport number is not one of them).
/// Route Person ListView grid search text through XAF FullTextSearch so
/// <see cref="Visa2026.Module.Controllers.PersonListViewPassportFullTextSearchController"/>
/// passport Exists criteria apply. Disable per-column grid search to avoid double-filtering.
/// </summary>
public sealed class PersonListViewSearchBridgeController : ObjectViewController<ListView, Person>
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
    }
}