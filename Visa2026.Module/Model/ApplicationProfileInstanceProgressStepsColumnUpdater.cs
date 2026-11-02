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
    /// Default width for office + up to five ministry legs + hand-in + CO number, plus the result chips.
    /// An officer drag replaces this for that user; the new width is a user model difference.
    /// </summary>
    public const int ProgressColumnWidth = 1120;

    /// <summary>Smallest width an officer can drag the progress column down to.</summary>
    public const int ProgressColumnMinWidth = 280;
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

    internal static void EnsureColumn(IModelListView listView)
    {
        HideColumn(listView, ReplacedColumnId);
        HideColumn(listView, nameof(ApplicationProfileInstance.ProgressSlaStatement));
        HideColumn(listView, nameof(ApplicationProfileInstance.MigrationSlaStatement));
        HideColumn(listView, nameof(ApplicationProfileInstance.ResultCoverageDisplay));

        var column = listView.Columns[ColumnId]
            ?? listView.Columns.AddNode<IModelColumn>(ColumnId);
        column.PropertyName = ColumnId;
        column.Width = ProgressColumnWidth;

        var completionId = nameof(ApplicationProfileInstance.CompletionPercentage);
        var lastVisible = listView.Columns
            .Where(c => !ReferenceEquals(c, column)
                && c.Index >= 0
                && !string.Equals(c.Id, completionId, StringComparison.Ordinal))
            .Select(c => c.Index)
            .DefaultIfEmpty(-1)
            .Max();
        column.Index = lastVisible + 1;
    }

    private static void HideColumn(IModelListView listView, string columnId)
    {
        var column = listView.Columns[columnId];
        if (column != null)
            column.Index = -1;
    }
}