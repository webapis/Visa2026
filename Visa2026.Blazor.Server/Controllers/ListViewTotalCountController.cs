using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Templates;
using DevExpress.Persistent.Base;
using Visa2026.Module.Localization;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Shows the localized total item count as a caption-only, right-aligned toolbar action on every DxGrid ListView.
/// XAF ribbon layout owns the visible toolbar row, so a toolbar action (RecordsNavigation, right aligned) is the
/// reliable placement — the DxGrid's own ToolbarContainer/SearchBoxTemplate is not used by the XAF frame.
/// </summary>
public sealed class ListViewTotalCountController : ViewController<ListView>
{
    private const string TotalCountActionId = "ListViewTotalCount";

    private readonly SimpleAction totalCountAction;
    private EventHandler<ComponentInstanceCapturedEventArgs<IGrid>>? gridCapturedHandler;
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private CancellationTokenSource? deferredApplyCts;
    private int lastShownCount = -1;

    public ListViewTotalCountController()
    {
        // Root list views only: the toolbar's RecordsNavigation container is not rendered on nested
        // (in-DetailView) list views, so nested counts are shown in the tab caption instead
        // (see NestedListViewTabCountController).
        TargetViewNesting = Nesting.Root;

        totalCountAction = new SimpleAction(this, TotalCountActionId, PredefinedCategory.RecordsNavigation)
        {
            Caption = FormatCaption(0),
            PaintStyle = ActionItemPaintStyle.Caption,
            ImageName = string.Empty,
            SelectionDependencyType = SelectionDependencyType.Independent,
            ConfirmationMessage = null,
        };
        totalCountAction.Execute += (_, _) => { };
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (ShouldSkipListView())
        {
            totalCountAction.Active["NotSupportedView"] = false;
            return;
        }

        View.CollectionSource.CollectionChanged += CollectionSource_Changed;
        UpdateCaption();
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

        gridCapturedHandler ??= (_, _) => UpdateCaption();
        gridListEditor.GridModel.ComponentInstanceCaptured += gridCapturedHandler;

        // Refresh the caption after client-side data shaping (search box, filter row, column filters).
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
            if (e.ElementType == GridElementType.HeaderRow)
                UpdateCaption();
        };
        gridListEditor.GridModel.CustomizeElement = customizeElementHandler;
    }

    private void CollectionSource_Changed(object sender, EventArgs e) => UpdateCaption();

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
            UpdateCaption();
    }

    private void UpdateCaption()
    {
        if (ShouldSkipListView())
            return;

        int count = ResolveCount();
        if (count == lastShownCount)
            return;

        lastShownCount = count;
        totalCountAction.Caption = FormatCaption(count);
    }

    private int ResolveCount()
    {
        if (View?.Editor is DxGridListEditor { GridModel.ComponentInstance: { } grid })
            return ListViewGridTotalCountConfigurator.ResolveFilteredCount(grid);

        return View?.CollectionSource.GetCount() ?? 0;
    }

    private static string FormatCaption(int count) => VisaUiMessages.Format("Grid.TotalCount", count);

    private bool ShouldSkipListView() =>
        View.Id.EndsWith("_LookupListView", StringComparison.Ordinal);
}
