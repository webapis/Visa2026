using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Replaces LatestProgressState with the compact workspace stepper
/// on instance ListViews (immediately before Ýüztutmanyň netijesi).
/// </summary>
public sealed class ApplicationProfileInstanceProgressStepsColumnUpdater
    : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public const string ColumnId = nameof(ApplicationProfileInstance.ProgressStepsDisplay);
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

            EnsureColumn(listView);
        }
    }

    internal static void EnsureColumn(IModelListView listView)
    {
        HideColumn(listView, ReplacedColumnId);
        HideColumn(listView, nameof(ApplicationProfileInstance.ProgressSlaStatement));
        HideColumn(listView, nameof(ApplicationProfileInstance.MigrationSlaStatement));

        var column = listView.Columns[ColumnId]
            ?? listView.Columns.AddNode<IModelColumn>(ColumnId);
        column.PropertyName = ColumnId;
        column.Width = 520;
        if (string.IsNullOrWhiteSpace(column.Caption))
            column.Caption = "Application progress";

        var result = listView.Columns[nameof(ApplicationProfileInstance.ResultCoverageDisplay)];
        if (result is { Index: >= 0 })
        {
            column.Index = result.Index;
            result.Index = result.Index + 1;
            return;
        }

        var lastVisible = listView.Columns
            .Where(c => !ReferenceEquals(c, column) && c.Index >= 0)
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