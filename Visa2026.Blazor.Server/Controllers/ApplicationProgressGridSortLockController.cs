using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using DevExpress.ExpressApp.Blazor.SystemModule;
using DevExpress.ExpressApp.Model;
using Visa2026.Module.Appearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Progress history ListViews: fixed timeline sort, show all rows (no inner scroll), latest-step row highlight.
/// Configures the XAF grid component model only (never the live DxGrid instance during render).
/// </summary>
public sealed class ApplicationProfileInstanceProgressGridSortLockController : ViewController<ListView>
{
    private static readonly string[] TargetListViewIds =
    [
        "Application_ProgressHistory_ListView",
        "ApplicationProfileInstanceProgress_ListView",
    ];

    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private EventHandler? collectionChangedHandler;

    public ApplicationProfileInstanceProgressGridSortLockController()
    {
        TargetObjectType = typeof(ApplicationProfileInstanceProgress);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (!TargetListViewIds.Contains(View.Id))
            return;

        collectionChangedHandler ??= (_, _) => OnProgressHistoryCollectionChanged();
        View.CollectionSource.CollectionChanged += collectionChangedHandler;
    }

    protected override void OnDeactivated()
    {
        if (TargetListViewIds.Contains(View.Id) && collectionChangedHandler != null)
            View.CollectionSource.CollectionChanged -= collectionChangedHandler;

        if (customizeElementHandler != null
            && View.Editor is DxGridListEditor { GridModel: { } gridModel })
        {
            gridModel.CustomizeElement = previousCustomizeElement;
        }

        customizeElementHandler = null;
        previousCustomizeElement = null;
        base.OnDeactivated();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (!TargetListViewIds.Contains(View.Id))
            return;

        if (View.Editor is not DxGridListEditor gridListEditor)
            return;

        ApplyShowAllRows();

        gridListEditor.BeginUpdate();
        try
        {
            ApplySortLock(gridListEditor);
            ApplyRowAppearance(gridListEditor);
        }
        finally
        {
            gridListEditor.EndUpdate();
        }
    }

    private void ApplyShowAllRows()
    {
        if (View.Model is IModelListViewBlazor blazorModel)
        {
            blazorModel.ShowAllRows = true;
            blazorModel.VirtualScrollingEnabled = false;
        }
    }

    private void OnProgressHistoryCollectionChanged()
    {
        if (View.CollectionSource is PropertyCollectionSource { MasterObject: ApplicationProfileInstance application })
        {
            application = ObjectSpace.GetObject(application);
            ObjectSpace.SetModified(application);
        }

        View.Refresh();
    }

    private void ApplyRowAppearance(DxGridListEditor gridListEditor)
    {
        var gridModel = gridListEditor.GridModel;
        if (customizeElementHandler != null)
        {
            gridModel.CustomizeElement = previousCustomizeElement;
            customizeElementHandler = null;
            previousCustomizeElement = null;
        }

        previousCustomizeElement = gridModel.CustomizeElement;
        customizeElementHandler = e =>
        {
            previousCustomizeElement?.Invoke(e);
            ApplyLatestProgressRowStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private void ApplyLatestProgressRowStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataRow || e.VisibleIndex < 0)
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not ApplicationProfileInstanceProgress progress
            || !IsLatestProgress(progress))
            return;

        var stateCode = progress.State?.Code?.Trim();
        if (string.IsNullOrEmpty(stateCode)
            || !BoStateAppearanceColors.TryGet(stateCode, out var appearance))
        {
            e.CssClass = AppendCssClass(e.CssClass, "visa-progress-history-latest");
            return;
        }

        e.CssClass = AppendCssClass(
            e.CssClass,
            $"{appearance.RowCssClass} visa-progress-row visa-progress-history-latest");
    }

    private bool IsLatestProgress(ApplicationProfileInstanceProgress progress)
    {
        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(progress.ApplicationProfileInstance?.ProgressHistory, ObjectSpace);
        if (latest == null)
            return false;

        if (ReferenceEquals(latest, progress))
            return true;

        return latest.ID != Guid.Empty && latest.ID == progress.ID;
    }

    private static string AppendCssClass(string? existing, string cssClass) =>
        string.IsNullOrEmpty(existing) ? cssClass : $"{existing} {cssClass}";

    private static void ApplySortLock(DxGridListEditor gridListEditor)
    {
        gridListEditor.GridModel.AllowSort = false;

        foreach (DxGridDataColumnModel columnModel in gridListEditor.GridDataColumnModels)
            columnModel.AllowSort = false;

        ApplyFixedTimelineSort(gridListEditor.GridDataColumnModels);
    }

    private static void ApplyFixedTimelineSort(IEnumerable<DxGridDataColumnModel> columnModels)
    {
        foreach (var columnModel in columnModels)
        {
            columnModel.AllowSort = false;
            columnModel.SortIndex = -1;
            columnModel.SortOrder = GridColumnSortOrder.None;
        }

        var orderColumn = columnModels.FirstOrDefault(column =>
            string.Equals(column.FieldName, nameof(ApplicationProfileInstanceProgress.Order), StringComparison.Ordinal));
        if (orderColumn == null)
            return;

        orderColumn.AllowSort = false;
        orderColumn.SortIndex = 0;
        orderColumn.SortOrder = GridColumnSortOrder.Ascending;
    }
}
