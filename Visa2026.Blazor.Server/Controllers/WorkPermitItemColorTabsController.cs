using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Blazor;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Visa2026.Blazor.Server.Components;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Color-tab filter above the Work Permits (Employee) <see cref="WorkPermitItem"/> ListView.
/// </summary>
public sealed class WorkPermitItemColorTabsController : ViewController<ListView>
{
    private const string StoragePrefix = "visa2026.workPermitColorTab.";

    private IReadOnlyList<WorkPermitItemColorTab.TabInfo> tabs =
        Array.Empty<WorkPermitItemColorTab.TabInfo>();

    private List<WorkPermitItem> countCache = [];
    private string selectedKey = WorkPermitItemColorTab.AllKey;
    private string searchText = string.Empty;
    private bool searchTabSticky;
    private bool storageLoaded;
    private bool suppressCollectionHandler;
    private RenderFragment<GridToolbarTemplateContext>? previousToolbarTemplate;
    private readonly WorkPermitItemColorTabsModel _stripModel = new();
    private bool _toolbarWired;

    public WorkPermitItemColorTabsController()
    {
        TargetObjectType = typeof(WorkPermitItem);
        TargetViewId = WorkPermitItemRegistryNavigation.TargetViewIds;
    }

    public void NotifySearchTextChanged(string? text)
    {
        if (View is not { IsDisposed: false })
            return;

        searchText = text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(searchText))
        {
            searchTabSticky = true;
            selectedKey = WorkPermitItemColorTab.SearchKey;
        }

        RebuildCountCacheAndApply(reloadToolbar: true);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        View.CollectionSource.CollectionChanged += CollectionSource_CollectionChanged;
        View.CollectionSource.CollectionReloaded += CollectionSource_CollectionReloaded;
        _ = InitializeAsync();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyToolbar();
    }

    protected override void OnDeactivated()
    {
        View.CollectionSource.CollectionChanged -= CollectionSource_CollectionChanged;
        View.CollectionSource.CollectionReloaded -= CollectionSource_CollectionReloaded;
        RestoreToolbar();
        _toolbarWired = false;
        _stripModel.LoadingKey = null;
        storageLoaded = false;
        searchTabSticky = false;
        searchText = string.Empty;
        countCache = [];
        base.OnDeactivated();
    }

    private async Task InitializeAsync()
    {
        if (View is not { IsDisposed: false })
            return;

        if (!storageLoaded)
        {
            var loaded = await LoadSelectionAsync();
            selectedKey = WorkPermitItemColorTab.IsSearchTab(loaded)
                ? WorkPermitItemColorTab.AllKey
                : loaded;
            storageLoaded = true;
        }

        RebuildCountCacheAndApply(reloadToolbar: true);
    }

    private void CollectionSource_CollectionChanged(object? sender, EventArgs e)
    {
        if (suppressCollectionHandler)
            return;
    }

    private void CollectionSource_CollectionReloaded(object? sender, EventArgs e)
    {
        if (suppressCollectionHandler)
            return;

        RebuildCountCacheAndApply(reloadToolbar: true);
    }

    private async Task OnTabSelectedAsync(string key)
    {
        var normalized = WorkPermitItemColorTab.NormalizeSelection(key, tabs);
        if (string.Equals(normalized, selectedKey, StringComparison.OrdinalIgnoreCase)
            && !WorkPermitItemColorTab.IsSearchTab(normalized))
            return;

        if (WorkPermitItemColorTab.IsSearchTab(normalized))
        {
            selectedKey = WorkPermitItemColorTab.SearchKey;
            searchTabSticky = true;
            RebuildTabsFromCache();
            ApplySelectionFilter();
            ApplyToolbar();
            return;
        }

        var leavingSearch = searchTabSticky
            || WorkPermitItemColorTab.IsSearchTab(selectedKey);
        searchTabSticky = false;
        selectedKey = normalized;
        await SaveSelectionAsync(selectedKey);

        if (leavingSearch)
        {
            ClearFullTextSearch();
            searchText = string.Empty;
            RebuildCountCacheAndApply(reloadToolbar: true);
            return;
        }

        RebuildTabsFromCache();
        ApplySelectionFilter();
        ApplyToolbar();
    }

    private void RebuildCountCacheAndApply(bool reloadToolbar)
    {
        if (View is not { IsDisposed: false } || suppressCollectionHandler)
            return;

        suppressCollectionHandler = true;
        try
        {
            ClearColorCriteria();
            countCache = View.CollectionSource.List?
                .OfType<WorkPermitItem>()
                .ToList()
                ?? [];

            WorkPermitItemRegistryListStamp.Apply(ObjectSpace, countCache);

            RebuildTabsFromCache();
            ApplySelectionFilter();
        }
        finally
        {
            suppressCollectionHandler = false;
        }

        if (reloadToolbar)
            ApplyToolbar();
    }

    private void RebuildTabsFromCache()
    {
        var includeSearch = searchTabSticky || !string.IsNullOrEmpty(searchText);
        var emptySearchSelected =
            WorkPermitItemColorTab.IsSearchTab(selectedKey)
            && string.IsNullOrEmpty(searchText);

        var searchCount = emptySearchSelected
            ? 0
            : (includeSearch && !string.IsNullOrEmpty(searchText) ? countCache.Count : 0);

        tabs = WorkPermitItemColorTab.BuildTabs(
            countCache,
            selectedKey,
            includeSearchTab: includeSearch,
            searchMatchCount: searchCount);

        selectedKey = WorkPermitItemColorTab.NormalizeSelection(selectedKey, tabs);
        PublishStrip();
    }

    private void ApplySelectionFilter()
    {
        if (View is not { IsDisposed: false })
            return;

        suppressCollectionHandler = true;
        try
        {
            if (WorkPermitItemColorTab.IsSearchTab(selectedKey))
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    View.CollectionSource.Criteria[WorkPermitItemColorTab.CriteriaKey] =
                        CriteriaOperator.Parse("1 = 0");
                }
                else
                {
                    View.CollectionSource.Criteria.Remove(WorkPermitItemColorTab.CriteriaKey);
                }

                return;
            }

            if (string.IsNullOrEmpty(selectedKey))
            {
                View.CollectionSource.Criteria.Remove(WorkPermitItemColorTab.CriteriaKey);
                return;
            }

            View.CollectionSource.Criteria[WorkPermitItemColorTab.CriteriaKey] =
                CriteriaOperator.Parse(
                    $"{nameof(WorkPermitItem.WorkPermitColorTabKey)} = ?",
                    selectedKey);
        }
        finally
        {
            suppressCollectionHandler = false;
        }
    }

    private void ClearColorCriteria()
    {
        View.CollectionSource.Criteria.Remove(WorkPermitItemColorTab.CriteriaKey);
    }

    private void ClearFullTextSearch()
    {
        try
        {
            var filterController = Frame.GetController<FilterController>();
            filterController?.FullTextFilterAction?.DoExecute(string.Empty);
            if (View?.Editor is DxGridListEditor { GridModel: { } gridModel })
                gridModel.SearchText = string.Empty;
        }
        catch
        {
            // best-effort
        }
    }

    private void ApplyToolbar()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
            return;

        previousToolbarTemplate ??= gridModel.ToolbarTemplate;
        _stripModel.Tabs = tabs;
        _stripModel.SelectedKey = selectedKey;
        _stripModel.OnSelect = EventCallback.Factory.Create<string>(this, OnTabSelectedAsync);

        if (!_toolbarWired)
        {
            gridModel.ToolbarTemplate = _ => builder =>
            {
                builder.OpenComponent<WorkPermitItemColorTabs>(0);
                builder.AddAttribute(1, "Model", _stripModel);
                builder.CloseComponent();
            };
            _toolbarWired = true;
        }

        PublishStrip();
    }

    private void PublishStrip()
    {
        _stripModel.Tabs = tabs;
        _stripModel.SelectedKey = selectedKey;
        _stripModel.NotifyChanged();
    }

    private void RestoreToolbar()
    {
        if (View?.Editor is DxGridListEditor { GridModel: { } gridModel }
            && previousToolbarTemplate != null)
        {
            gridModel.ToolbarTemplate = previousToolbarTemplate;
        }

        previousToolbarTemplate = null;
    }

    private string StorageKey =>
        StoragePrefix + (View?.Id ?? WorkPermitItemRegistryNavigation.ListViewEmployeesId);

    private async Task<string> LoadSelectionAsync()
    {
        try
        {
            var js = Application?.ServiceProvider?.GetService(typeof(IJSRuntime)) as IJSRuntime;
            if (js == null)
                return WorkPermitItemColorTab.AllKey;

            var loaded = await js.InvokeAsync<string?>("visaProgressColorTabs.load", StorageKey);
            return loaded?.Trim() ?? WorkPermitItemColorTab.AllKey;
        }
        catch
        {
            return WorkPermitItemColorTab.AllKey;
        }
    }

    private async Task SaveSelectionAsync(string key)
    {
        try
        {
            var js = Application?.ServiceProvider?.GetService(typeof(IJSRuntime)) as IJSRuntime;
            if (js == null)
                return;

            await js.InvokeVoidAsync("visaProgressColorTabs.save", StorageKey, key ?? WorkPermitItemColorTab.AllKey);
        }
        catch
        {
            // best-effort
        }
    }
}