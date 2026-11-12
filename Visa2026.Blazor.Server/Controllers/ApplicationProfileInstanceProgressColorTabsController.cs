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

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Color-tab filter above Application Profile Instance ListViews
/// (one tab per painted progress color; Search/Gözleg tab for cross-color hits).
/// </summary>
public sealed class ApplicationProfileInstanceProgressColorTabsController : ViewController<ListView>
{
    private const string StoragePrefix = "visa2026.progressColorTab.";

    private IReadOnlyList<ApplicationProfileInstanceProgressColorTab.TabInfo> tabs =
        Array.Empty<ApplicationProfileInstanceProgressColorTab.TabInfo>();

    /// <summary>
    /// Unfiltered-by-color snapshot used for tab counts (avoids clearing criteria on every tab click).
    /// </summary>
    private List<ApplicationProfileInstance> countCache = [];

    private string selectedKey = ApplicationProfileInstanceProgressColorTab.AllKey;
    private string searchText = string.Empty;
    private bool searchTabSticky;
    private bool storageLoaded;
    private bool suppressCollectionHandler;
    private RenderFragment<GridToolbarTemplateContext>? previousToolbarTemplate;

    public ApplicationProfileInstanceProgressColorTabsController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    /// <summary>
    /// Called from the ListView search bridge when DxGrid search text changes.
    /// </summary>
    public void NotifySearchTextChanged(string? text)
    {
        if (View is not { IsDisposed: false })
            return;

        searchText = text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(searchText))
        {
            searchTabSticky = true;
            selectedKey = ApplicationProfileInstanceProgressColorTab.SearchKey;
        }

        // Search criteria already applied by FilterController — rebuild counts, then filter once.
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
            selectedKey = ApplicationProfileInstanceProgressColorTab.IsSearchTab(loaded)
                ? ApplicationProfileInstanceProgressColorTab.AllKey
                : loaded;
            storageLoaded = true;
        }

        RebuildCountCacheAndApply(reloadToolbar: true);
    }

    private void CollectionSource_CollectionChanged(object? sender, EventArgs e)
    {
        if (suppressCollectionHandler)
            return;

        // Do not clear/re-apply color criteria here — that caused tab-switch flicker.
        // Counts stay on countCache until reload/search rebuilds them.
    }

    private void CollectionSource_CollectionReloaded(object? sender, EventArgs e)
    {
        if (suppressCollectionHandler)
            return;

        RebuildCountCacheAndApply(reloadToolbar: true);
    }

    /// <summary>
    /// Tab click: swap filter in one criteria write (no clear-to-all flash).
    /// </summary>
    private async Task OnTabSelectedAsync(string key)
    {
        var normalized = ApplicationProfileInstanceProgressColorTab.NormalizeSelection(key, tabs);
        if (string.Equals(normalized, selectedKey, StringComparison.OrdinalIgnoreCase)
            && !ApplicationProfileInstanceProgressColorTab.IsSearchTab(normalized))
            return;

        if (ApplicationProfileInstanceProgressColorTab.IsSearchTab(normalized))
        {
            selectedKey = ApplicationProfileInstanceProgressColorTab.SearchKey;
            searchTabSticky = true;
            RebuildTabsFromCache();
            ApplySelectionFilter();
            ApplyToolbar();
            return;
        }

        var leavingSearch = searchTabSticky
            || ApplicationProfileInstanceProgressColorTab.IsSearchTab(selectedKey);
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
            // Snapshot without our color filter — required for accurate multi-color counts.
            ClearColorCriteria();
            countCache = View.CollectionSource.List?
                .OfType<ApplicationProfileInstance>()
                .ToList()
                ?? [];

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
            ApplicationProfileInstanceProgressColorTab.IsSearchTab(selectedKey)
            && string.IsNullOrEmpty(searchText);

        var searchCount = emptySearchSelected
            ? 0
            : (includeSearch && !string.IsNullOrEmpty(searchText) ? countCache.Count : 0);

        // Sticky-empty Search: color counts from full cache; search count 0.
        // Active search: cache is search-filtered (FullTextSearch), counts partition that set.
        tabs = ApplicationProfileInstanceProgressColorTab.BuildTabs(
            countCache,
            selectedKey,
            includeSearchTab: includeSearch,
            searchMatchCount: searchCount);

        selectedKey = ApplicationProfileInstanceProgressColorTab.NormalizeSelection(selectedKey, tabs);
    }

    /// <summary>
    /// Sets ProgressColorTab criteria in one step. Replaces an existing color filter
    /// without Remove-first so the grid does not flash the unfiltered list.
    /// </summary>
    private void ApplySelectionFilter()
    {
        if (View is not { IsDisposed: false })
            return;

        suppressCollectionHandler = true;
        try
        {
            if (ApplicationProfileInstanceProgressColorTab.IsSearchTab(selectedKey))
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    View.CollectionSource.Criteria[ApplicationProfileInstanceProgressColorTab.CriteriaKey] =
                        CriteriaOperator.Parse("1 = 0");
                }
                else
                {
                    View.CollectionSource.Criteria.Remove(
                        ApplicationProfileInstanceProgressColorTab.CriteriaKey);
                }

                return;
            }

            if (string.IsNullOrEmpty(selectedKey))
            {
                View.CollectionSource.Criteria.Remove(
                    ApplicationProfileInstanceProgressColorTab.CriteriaKey);
                return;
            }

            View.CollectionSource.Criteria[ApplicationProfileInstanceProgressColorTab.CriteriaKey] =
                CriteriaOperator.Parse(
                    $"{nameof(ApplicationProfileInstance.ProgressColorTabKey)} = ?",
                    selectedKey);
        }
        finally
        {
            suppressCollectionHandler = false;
        }
    }

    private void ClearColorCriteria()
    {
        View.CollectionSource.Criteria.Remove(
            ApplicationProfileInstanceProgressColorTab.CriteriaKey);
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
            // Search clear is best-effort.
        }
    }

    private void ApplyToolbar()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
            return;

        previousToolbarTemplate ??= gridModel.ToolbarTemplate;
        var currentTabs = tabs;
        var currentSelected = selectedKey;
        gridModel.ToolbarTemplate = _ => builder =>
        {
            builder.OpenComponent<ApplicationProfileInstanceProgressColorTabs>(0);
            builder.AddAttribute(1, "Tabs", currentTabs);
            builder.AddAttribute(2, "SelectedKey", currentSelected);
            builder.AddAttribute(
                3,
                "OnSelect",
                EventCallback.Factory.Create<string>(this, OnTabSelectedAsync));
            builder.CloseComponent();
        };
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
        StoragePrefix + (View?.Id ?? "ApplicationProfileInstance");

    private async Task<string> LoadSelectionAsync()
    {
        try
        {
            var js = Application?.ServiceProvider?.GetService(typeof(IJSRuntime)) as IJSRuntime;
            if (js == null)
                return ApplicationProfileInstanceProgressColorTab.AllKey;

            var raw = await js.InvokeAsync<string>("visaProgressColorTabs.load", StorageKey);
            return raw?.Trim() ?? ApplicationProfileInstanceProgressColorTab.AllKey;
        }
        catch
        {
            return ApplicationProfileInstanceProgressColorTab.AllKey;
        }
    }

    private async Task SaveSelectionAsync(string key)
    {
        if (ApplicationProfileInstanceProgressColorTab.IsSearchTab(key))
            return;

        try
        {
            var js = Application?.ServiceProvider?.GetService(typeof(IJSRuntime)) as IJSRuntime;
            if (js == null)
                return;

            await js.InvokeVoidAsync(
                "visaProgressColorTabs.save",
                StorageKey,
                string.IsNullOrEmpty(key) ? null : key);
        }
        catch
        {
            // Browser storage may be unavailable.
        }
    }
}