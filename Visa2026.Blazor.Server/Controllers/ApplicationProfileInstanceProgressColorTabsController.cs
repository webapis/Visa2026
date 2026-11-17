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
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

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
    private IApplicationProfileInstanceDeletedNotifier? _followUpNotifier;
    private readonly ApplicationProfileInstanceProgressColorTabsModel _stripModel = new();
    private bool _toolbarWired;
    private int _switchGeneration;

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
        _followUpNotifier = Application?.ServiceProvider?.GetService<IApplicationProfileInstanceDeletedNotifier>();
        if (_followUpNotifier != null)
            _followUpNotifier.SpecialFollowUpChanged += OnSpecialFollowUpChanged;
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
        if (_followUpNotifier != null)
        {
            _followUpNotifier.SpecialFollowUpChanged -= OnSpecialFollowUpChanged;
            _followUpNotifier = null;
        }

        RestoreToolbar();
        _toolbarWired = false;
        _stripModel.LoadingKey = null;
        storageLoaded = false;
        searchTabSticky = false;
        searchText = string.Empty;
        countCache = [];
        base.OnDeactivated();
    }

    private void OnSpecialFollowUpChanged(Guid instanceId, bool enabled)
    {
        if (View is not { IsDisposed: false } || ObjectSpace == null || ObjectSpace.IsDisposed)
            return;

        var local = ObjectSpace.GetObjectByKey<ApplicationProfileInstance>(instanceId);
        if (local != null)
        {
            try
            {
                ObjectSpace.ReloadObject(local);
            }
            catch
            {
                // Reload is best-effort; live follow-up query still corrects the tab count.
            }
        }

        RebuildCountCacheAndApply(reloadToolbar: true);
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
    /// Selection + spinner update immediately; spinner clears after the grid reloads.
    /// </summary>
    private async Task OnTabSelectedAsync(string key)
    {
        var normalized = ApplicationProfileInstanceProgressColorTab.NormalizeSelection(key, tabs);
        if (string.Equals(normalized, selectedKey, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(_stripModel.LoadingKey)
            && !ApplicationProfileInstanceProgressColorTab.IsSearchTab(normalized))
            return;

        var leavingSearch = searchTabSticky
            || ApplicationProfileInstanceProgressColorTab.IsSearchTab(selectedKey);
        var gen = System.Threading.Interlocked.Increment(ref _switchGeneration);
        selectedKey = normalized;
        _stripModel.LoadingKey = normalized;
        PublishStrip();

        if (ApplicationProfileInstanceProgressColorTab.IsSearchTab(normalized))
        {
            searchTabSticky = true;
            RebuildTabsFromCache();
            await ApplyFilterAndWaitForGridAsync(gen);
            return;
        }

        searchTabSticky = false;
        await SaveSelectionAsync(selectedKey);

        if (leavingSearch)
        {
            ClearFullTextSearch();
            searchText = string.Empty;
            RebuildCountCacheAndApply(reloadToolbar: false);
            PublishStrip();
            await WaitForGridIdleAsync(gen);
            return;
        }

        RebuildTabsFromCache();
        await ApplyFilterAndWaitForGridAsync(gen);
    }

    private async Task ApplyFilterAndWaitForGridAsync(int gen)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnReloaded(object? sender, EventArgs e) => tcs.TrySetResult(true);

        if (View?.CollectionSource != null)
            View.CollectionSource.CollectionReloaded += OnReloaded;

        try
        {
            ApplySelectionFilter();
            PublishStrip();
            await Task.WhenAny(tcs.Task, Task.Delay(2000));
            await Task.Delay(50);
        }
        finally
        {
            if (View?.CollectionSource != null)
                View.CollectionSource.CollectionReloaded -= OnReloaded;
        }

        if (gen != System.Threading.Volatile.Read(ref _switchGeneration))
            return;

        _stripModel.LoadingKey = null;
        PublishStrip();
    }

    private async Task WaitForGridIdleAsync(int gen)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnReloaded(object? sender, EventArgs e) => tcs.TrySetResult(true);

        if (View?.CollectionSource != null)
            View.CollectionSource.CollectionReloaded += OnReloaded;

        try
        {
            await Task.WhenAny(tcs.Task, Task.Delay(2000));
            await Task.Delay(50);
        }
        finally
        {
            if (View?.CollectionSource != null)
                View.CollectionSource.CollectionReloaded -= OnReloaded;
        }

        if (gen != System.Threading.Volatile.Read(ref _switchGeneration))
            return;

        _stripModel.LoadingKey = null;
        PublishStrip();
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

            StampApplicationResultRejectionFlags(countCache);

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

    /// <summary>
    /// Garşylyk berildi counts/filter need Rejection headers; ListView rows often omit the nav.
    /// </summary>
    private void StampApplicationResultRejectionFlags(IReadOnlyList<ApplicationProfileInstance> applications)
    {
        if (applications.Count == 0 || ObjectSpace == null)
            return;

        var ids = applications.Select(a => a.ID).Distinct().ToList();
        var withRejection = ObjectSpace.GetObjectsQuery<Rejection>()
            .Where(r => r.ApplicationProfileInstance != null && ids.Contains(r.ApplicationProfileInstance.ID))
            .Select(r => r.ApplicationProfileInstance!.ID)
            .Distinct()
            .ToHashSet();

        foreach (var application in applications)
            application.SetListViewHasApplicationResultRejection(withRejection.Contains(application.ID));
    }

    /// <summary>
    /// Special-follow-up is saved on a separate ObjectSpace; list entities can keep a stale flag.
    /// Count from a live query so the tab clears when no row is flagged.
    /// </summary>
    private HashSet<Guid> ResolveLiveSpecialFollowUpIds()
    {
        if (ObjectSpace == null || ObjectSpace.IsDisposed || countCache.Count == 0)
            return [];

        var ids = countCache.Select(a => a.ID).ToList();
        return ObjectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.IsSpecialFollowUp && ids.Contains(a.ID))
            .Select(a => a.ID)
            .ToHashSet();
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
            searchMatchCount: searchCount,
            specialFollowUpIds: ResolveLiveSpecialFollowUpIds());

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

            if (ApplicationProfileInstanceProgressColorTab.IsSpecialFollowUpTab(selectedKey))
            {
                View.CollectionSource.Criteria[ApplicationProfileInstanceProgressColorTab.CriteriaKey] =
                    CriteriaOperator.Parse(
                        $"{nameof(ApplicationProfileInstance.IsSpecialFollowUp)} = True");
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
        _stripModel.OnSelect = EventCallback.Factory.Create<string>(this, OnTabSelectedAsync);
        PublishStrip(notify: false);

        if (!_toolbarWired)
        {
            _toolbarWired = true;
            var model = _stripModel;
            gridModel.ToolbarTemplate = _ => builder =>
            {
                builder.OpenComponent<ApplicationProfileInstanceProgressColorTabs>(0);
                builder.AddAttribute(1, "Model", model);
                builder.CloseComponent();
            };
        }

        PublishStrip();
    }

    private void PublishStrip(bool notify = true)
    {
        _stripModel.Tabs = tabs;
        _stripModel.SelectedKey = selectedKey;
        if (notify)
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