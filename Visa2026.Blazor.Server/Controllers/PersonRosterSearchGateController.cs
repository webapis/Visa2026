using Visa2026.Module;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Visa2026.Blazor.Server.Components;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Employees and Family Members use the roster search bar. The grid stays hidden until a search,
/// then the same bar filters the list. The built-in grid search box stays off.
/// </summary>
public sealed class PersonRosterSearchGateController : ObjectViewController<ListView, Person>
{
    private const string ActiveKey = "RosterSearchGate";

    private readonly PersonRosterSearchState presentation = new();
    private string submittedQuery = string.Empty;
    private CancellationTokenSource? countRefreshCts;
    private FilterController? filterController;
    private RenderFragment<GridEmptyDataAreaTemplateContext>? previousEmptyTemplate;
    private RenderFragment<GridToolbarTemplateContext>? previousToolbarTemplate;
    private bool templateCaptured;

    public PersonRosterSearchGateController()
    {
        TargetViewId = PersonRosterListViewIds.Employees + ";" + PersonRosterListViewIds.EmployeesIncomplete + ";" + PersonRosterListViewIds.FamilyMembers + ";" + PersonRosterListViewIds.FamilyMembersIncomplete;
        TargetViewNesting = Nesting.Root;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        submittedQuery = string.Empty;
        filterController = Frame.GetController<FilterController>();
        if (filterController?.FullTextFilterAction != null)
        {
            filterController.FullTextFilterAction.Executed += OnSearchFinished;
            filterController.FullTextFilterAction.ExecuteCanceled += OnSearchFinished;
            filterController.FullTextFilterAction.HandleException += OnSearchFailed;
        }

        if (View?.CollectionSource != null)
        {
            View.CollectionSource.CollectionChanged += OnCollectionChanged;
            View.CollectionSource.CollectionReloaded += OnCollectionChanged;
        }

        ApplyActionVisibility();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        templateCaptured = false;
        ApplyPresentation();
    }

    protected override void OnDeactivated()
    {
        if (filterController?.FullTextFilterAction != null)
        {
            filterController.FullTextFilterAction.Active.RemoveItem(ActiveKey);
            filterController.FullTextFilterAction.Executed -= OnSearchFinished;
            filterController.FullTextFilterAction.ExecuteCanceled -= OnSearchFinished;
            filterController.FullTextFilterAction.HandleException -= OnSearchFailed;
        }

        CancelCountRefresh();
        if (View?.CollectionSource != null)
        {
            View.CollectionSource.CollectionChanged -= OnCollectionChanged;
            View.CollectionSource.CollectionReloaded -= OnCollectionChanged;
        }

        SetCountActions(visible: true);
        RestoreEmptyTemplate();
        RestoreToolbarTemplate();
        filterController = null;
        submittedQuery = string.Empty;
        templateCaptured = false;
        base.OnDeactivated();
    }

    private void OnSearchRequested(string text) => ExecuteSearch(text);

    private void OnClearRequested() => ExecuteSearch(string.Empty);

    /// <summary>All or Incomplete was chosen while a search was showing. Drop the query.</summary>
    public void ClearSearch() => ExecuteSearch(string.Empty);

    private void ExecuteSearch(string? text)
    {
        submittedQuery = text?.Trim() ?? string.Empty;
        presentation.SetBusy(!string.IsNullOrWhiteSpace(submittedQuery));
        if (filterController?.FullTextFilterAction == null)
        {
            ApplyPresentation();
            return;
        }

        // The list search box stays inactive. DoExecute refuses an inactive action.
        // Batch with the tab-criteria change so the grid reloads once.
        if (View?.CollectionSource != null)
            View.CollectionSource.BeginUpdateCriteria();
        try
        {
            filterController.FullTextFilterAction.Active[ActiveKey] = true;
            filterController.FullTextFilterAction.DoExecute(submittedQuery);
        }
        finally
        {
            if (View?.CollectionSource != null)
                View.CollectionSource.EndUpdateCriteria();
        }
    }

    private void OnSearchFinished(object sender, ActionBaseEventArgs e) => ApplyPresentation();

    private void OnSearchFailed(object sender, HandleExceptionEventArgs e) => ApplyPresentation();

    private void ApplyPresentation()
    {
        ApplyActionVisibility();
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
            return;

        // The visible "Text to search..." field is the grid's own search box.
        gridModel.ShowSearchBox = false;

        // The grid reloads after a search and the action value is empty again.
        // The text the officer submitted is what decides card vs no-match warning.
        var query = string.IsNullOrWhiteSpace(submittedQuery) ? CurrentSearchText() : submittedQuery;
        presentation.Publish(false, query);
        ScheduleResultCount();

        if (templateCaptured)
            return;

        previousEmptyTemplate = gridModel.EmptyDataAreaTemplate;
        previousToolbarTemplate = gridModel.ToolbarTemplate;
        templateCaptured = true;

        var search = EventCallback.Factory.Create<string>(this, OnSearchRequested);
        var clear = EventCallback.Factory.Create(this, OnClearRequested);
        var tabs = Frame.GetController<PersonRosterColorTabsController>()?.Strip;
        gridModel.EmptyDataAreaTemplate = _ => builder =>
        {
            builder.OpenComponent<PersonRosterSearchGate>(0);
            builder.AddAttribute(1, "State", presentation);
            builder.CloseComponent();
        };

        gridModel.ToolbarTemplate = _ => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "visa-roster-list-head");
            if (tabs != null)
            {
                builder.OpenComponent<PersonRosterColorTabs>(2);
                builder.AddAttribute(3, "Model", tabs);
                builder.CloseComponent();
            }

            builder.OpenComponent<PersonRosterSearchBar>(4);
            builder.AddAttribute(5, "State", presentation);
            builder.AddAttribute(6, "OnSearch", search);
            builder.AddAttribute(7, "OnClear", clear);
            builder.CloseComponent();
            builder.CloseElement();
        };
    }

    private void ApplyActionVisibility()
    {
        // The built-in "Text to search" box never shows on these lists.
        if (filterController?.FullTextFilterAction != null)
            filterController.FullTextFilterAction.Active[ActiveKey] = false;

        var query = string.IsNullOrWhiteSpace(submittedQuery) ? CurrentSearchText() : submittedQuery;
        SetCountActions(!string.IsNullOrWhiteSpace(query));
    }

    private void SetCountActions(bool visible)
    {
        var total = Frame?.GetController<ListViewTotalCountController>();
        if (total == null)
            return;

        foreach (ActionBase action in total.Actions)
        {
            if (action.Id is "ListViewTotalCount" or "ListViewClearFilters")
                action.Active[ActiveKey] = visible;
        }
    }

    private void RestoreEmptyTemplate()
    {
        if (templateCaptured && View?.Editor is DxGridListEditor { GridModel: { } gridModel })
            gridModel.EmptyDataAreaTemplate = previousEmptyTemplate;

        previousEmptyTemplate = null;
    }

    private void RestoreToolbarTemplate()
    {
        if (templateCaptured && View?.Editor is DxGridListEditor { GridModel: { } gridModel })
            gridModel.ToolbarTemplate = previousToolbarTemplate;

        previousToolbarTemplate = null;
    }

    private string? CurrentSearchText() => filterController?.FullTextFilterAction?.Value as string;

    private void OnCollectionChanged(object? sender, EventArgs e) => ScheduleResultCount();

    private void ScheduleResultCount()
    {
        CancelCountRefresh();
        if (string.IsNullOrWhiteSpace(submittedQuery) && string.IsNullOrWhiteSpace(CurrentSearchText()))
        {
            presentation.SetResultCount(null);
            return;
        }

        countRefreshCts = new CancellationTokenSource();
        var token = countRefreshCts.Token;
        _ = RefreshResultCountAsync(token);
    }

    private async Task RefreshResultCountAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(200, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (View is not { IsDisposed: false })
            return;

        try
        {
            presentation.SetResultCount(View.CollectionSource?.GetCount() ?? 0);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void CancelCountRefresh()
    {
        countRefreshCts?.Cancel();
        countRefreshCts?.Dispose();
        countRefreshCts = null;
    }
}
