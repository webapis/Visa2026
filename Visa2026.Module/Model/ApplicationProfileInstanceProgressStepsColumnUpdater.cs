using System;
using DevExpress.ExpressApp;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Replaces LatestProgressState with the compact workspace stepper
/// on instance ListViews (last visible column; result chips merge into this cell).
/// </summary>
public sealed class ApplicationProfileInstanceProgressStepsColumnUpdater
    : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public const string ColumnId = nameof(ApplicationProfileInstance.ProgressStepsDisplay);

    /// <summary>
    /// Width of the Ýagdaý syny link. Progress and document states open in the A4 dialog.
    /// </summary>
    public const int ProgressColumnWidth = 160;

    /// <summary>Same as ProgressColumnWidth so drag cannot shrink the progress column.</summary>
    public const int ProgressColumnMinWidth = ProgressColumnWidth;
    public const string ReplacedColumnId = nameof(ApplicationProfileInstance.LatestProgressState);

    private static readonly string[] ApplicationInstanceListViewIds =
    [
        ApplicationProfileInstanceProgressRouteNavigation.LegacySourceListView,
        ApplicationProfileInstanceProgressRouteNavigation.SourceListView,
        ApplicationProfileInstanceProgressRouteNavigation.ListViewViaMinistries,
        ApplicationProfileInstanceProgressRouteNavigation.ListViewDirectMigration,
        ApplicationProfileInstanceProgressRouteNavigation.ListViewStaged,
        ApplicationProfileInstanceProgressRouteNavigation.ListViewInProcess,
        global::Visa2026.Module.PersonNestedCollectionLayout.ApplicationProfileInstancesListView,
    ];

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var listViewId in ApplicationInstanceListViewIds)
        {
            if (views[listViewId] is not IModelListView listView)
                continue;

            // Client mode so DxGrid CustomSort (completion %) runs; Queryable ignores Custom.
            listView.DataAccessMode = CollectionSourceDataAccessMode.Client;
            EnsureColumn(listView);
        }
    }

    public const string ColorTabStateColumnId = nameof(ApplicationProfileInstance.ProgressColorTabSortOrder);

    internal static void EnsureColumn(IModelListView listView)
    {
        HideColumn(listView, ReplacedColumnId);
        HideColumn(listView, nameof(ApplicationProfileInstance.ProgressSlaStatement));
        HideColumn(listView, nameof(ApplicationProfileInstance.MigrationSlaStatement));
        HideColumn(listView, nameof(ApplicationProfileInstance.ResultCoverageDisplay));

        // Drop legacy label-bound State column if present (sort must use ProgressColorTabSortOrder).
        HideColumn(listView, nameof(ApplicationProfileInstance.ProgressColorTabDisplay));

        // State: bind int sort key; Blazor cell template shows the color-tab label; hide unless Ähli.
        var stateColumn = listView.Columns[ColorTabStateColumnId]
            ?? listView.Columns.AddNode<IModelColumn>(ColorTabStateColumnId);
        stateColumn.PropertyName = ColorTabStateColumnId;
        stateColumn.Caption = "State";

        var column = listView.Columns[ColumnId]
            ?? listView.Columns.AddNode<IModelColumn>(ColumnId);
        column.PropertyName = ColumnId;
        column.Width = ProgressColumnWidth;

        var completionId = nameof(ApplicationProfileInstance.CompletionPercentage);
        var lastVisible = listView.Columns
            .Where(c => !ReferenceEquals(c, column)
                && !ReferenceEquals(c, stateColumn)
                && c.Index >= 0
                && !string.Equals(c.Id, completionId, StringComparison.Ordinal)
                && !string.Equals(c.Id, ColorTabStateColumnId, StringComparison.Ordinal))
            .Select(c => c.Index)
            .DefaultIfEmpty(-1)
            .Max();
        stateColumn.Index = lastVisible + 1;
        column.Index = stateColumn.Index + 1;
    }

    private static void HideColumn(IModelListView listView, string columnId)
    {
        var column = listView.Columns[columnId];
        if (column != null)
            column.Index = -1;
    }
}