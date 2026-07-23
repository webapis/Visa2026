using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Templates;
using DevExpress.Persistent.Base;
using Visa2026.Blazor.Server.Localization;
using Visa2026.Module.Localization;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Shows the localized total item count as a caption-only, right-aligned toolbar action on every DxGrid ListView.
/// When column filters / search are active, also shows a clickable Clear filters action and highlights
/// filtered column headers. XAF ribbon layout owns the visible toolbar row, so toolbar actions are the
/// reliable placement — the DxGrid's own ToolbarContainer/SearchBoxTemplate is not used by the XAF frame.
/// </summary>
public sealed class ListViewTotalCountController : ViewController<ListView>
{
    private const string TotalCountActionId = "ListViewTotalCount";
    private const string ClearFiltersActionId = "ListViewClearFilters";
    private const string FilteredHeaderCssClass = "visa-grid-header-filtered";

    private readonly SimpleAction totalCountAction;
    private readonly SimpleAction clearFiltersAction;
    private EventHandler<ComponentInstanceCapturedEventArgs<IGrid>>? gridCapturedHandler;
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private CancellationTokenSource? deferredApplyCts;
    private int lastShownCount = -1;
    private bool? lastShownFiltered;

    public ListViewTotalCountController()
    {
        // Root list views only: the toolbar's RecordsNavigation container is not rendered on nested
        // (in-DetailView) list views, so nested counts are shown in the tab caption instead
        // (see NestedListViewTabCountController).
        TargetViewNesting = Nesting.Root;

        totalCountAction = new SimpleAction(this, TotalCountActionId, PredefinedCategory.RecordsNavigation)
        {
            Caption = FormatTotalCaption(0),
            PaintStyle = ActionItemPaintStyle.Caption,
            ImageName = string.Empty,
            SelectionDependencyType = SelectionDependencyType.Independent,
            ConfirmationMessage = null,
        };
        totalCountAction.Execute += (_, _) => { };

        clearFiltersAction = new SimpleAction(this, ClearFiltersActionId, PredefinedCategory.RecordsNavigation)
        {
            Caption = VisaLocalization.GetGridClearFiltersText(),
            PaintStyle = ActionItemPaintStyle.Caption,
            ImageName = string.Empty,
            SelectionDependencyType = SelectionDependencyType.Independent,
            ConfirmationMessage = null,
        };
        clearFiltersAction.Active["HasGridFilter"] = false;
        clearFiltersAction.Execute += ClearFiltersAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (ShouldSkipListView())
        {
            totalCountAction.Active["NotSupportedView"] = false;
            clearFiltersAction.Active["NotSupportedView"] = false;
            return;
        }

        clearFiltersAction.Caption = VisaLocalization.GetGridClearFiltersText();
        View.CollectionSource.CollectionChanged += CollectionSource_Changed;
        UpdateToolbar();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (ShouldSkipListView())
            return;

        WireGrid();
        ScheduleDeferredApply();
    }

    protected override void OnDeactivated()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = null;

        View.CollectionSource.CollectionChanged -= CollectionSource_Changed;

        if (View?.Editor is DxGridListEditor gridListEditor)
        {
            if (gridCapturedHandler != null)
                gridListEditor.GridModel.ComponentInstanceCaptured -= gridCapturedHandler;

            if (customizeElementHandler != null)
                gridListEditor.GridModel.CustomizeElement = previousCustomizeElement;
        }

        gridCapturedHandler = null;
        customizeElementHandler = null;
        previousCustomizeElement = null;
        base.OnDeactivated();
    }

    private void WireGrid()
    {
        if (View.Editor is not DxGridListEditor gridListEditor)
            return;

        ListViewGridTotalCountConfigurator.EnsureCountSummary(gridListEditor);

        gridCapturedHandler ??= (_, _) => UpdateToolbar();
        gridListEditor.GridModel.ComponentInstanceCaptured += gridCapturedHandler;

        // Refresh after client-side data shaping (search box, filter row, column filters)
        // and mark filtered column headers.
        if (customizeElementHandler != null)
        {
            gridListEditor.GridModel.CustomizeElement = previousCustomizeElement;
            customizeElementHandler = null;
            previousCustomizeElement = null;
        }

        previousCustomizeElement = gridListEditor.GridModel.CustomizeElement;
        customizeElementHandler = e =>
        {
            previousCustomizeElement?.Invoke(e);
            ApplyFilteredHeaderHighlight(e);
            if (e.ElementType == GridElementType.HeaderRow || e.ElementType == GridElementType.HeaderCell)
                UpdateToolbar();
        };
        gridListEditor.GridModel.CustomizeElement = customizeElementHandler;
    }

    private static void ApplyFilteredHeaderHighlight(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.HeaderCell)
            return;

        if (e.Column is not IGridDataColumn dataColumn || string.IsNullOrEmpty(dataColumn.FieldName))
            return;

        if (ReferenceEquals(e.Grid.GetFieldFilterCriteria(dataColumn.FieldName), null))
            return;

        e.CssClass = string.IsNullOrEmpty(e.CssClass)
            ? FilteredHeaderCssClass
            : $"{e.CssClass} {FilteredHeaderCssClass}";
    }

    private void CollectionSource_Changed(object sender, EventArgs e) => UpdateToolbar();

    private void ClearFiltersAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View?.Editor is DxGridListEditor { GridModel.ComponentInstance: { } grid })
            ListViewGridFilterState.Clear(grid);

        UpdateToolbar();
    }

    private void ScheduleDeferredApply()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = new CancellationTokenSource();
        CancellationToken token = deferredApplyCts.Token;
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

        if (View is { IsDisposed: false })
            UpdateToolbar();
    }

    private void UpdateToolbar()
    {
        if (ShouldSkipListView())
            return;

        int count = ResolveCount();
        bool filtered = HasActiveGridFilter();
        if (count == lastShownCount && lastShownFiltered == filtered)
            return;

        lastShownCount = count;
        lastShownFiltered = filtered;

        // Total stays a non-clickable count; Clear filters is the one-click affordance.
        totalCountAction.Caption = FormatTotalCaption(count);
        clearFiltersAction.Caption = VisaLocalization.GetGridClearFiltersText();
        clearFiltersAction.Active["HasGridFilter"] = filtered;
    }

    private int ResolveCount()
    {
        if (View?.Editor is DxGridListEditor { GridModel.ComponentInstance: { } grid })
            return ListViewGridTotalCountConfigurator.ResolveFilteredCount(grid);

        return View?.CollectionSource.GetCount() ?? 0;
    }

    private static string FormatTotalCaption(int count) => VisaUiMessages.Format("Grid.TotalCount", count);

    private bool HasActiveGridFilter()
    {
        if (View?.Editor is not DxGridListEditor { GridModel.ComponentInstance: { } grid })
            return false;

        return ListViewGridFilterState.HasActiveFilter(grid);
    }

    private bool ShouldSkipListView() =>
        View.Id.EndsWith("_LookupListView", StringComparison.Ordinal);
}