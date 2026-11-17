using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Keeps CompletionPercentage off instance ListViews. The percent is on the state preview.
/// </summary>
public sealed class ApplicationProfileInstanceCompletionPercentageColumnUpdater
    : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public const string ColumnId = nameof(ApplicationProfileInstance.CompletionPercentage);

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
        var column = listView.Columns[ColumnId]
            ?? listView.Columns.AddNode<IModelColumn>(ColumnId);
        column.PropertyName = ColumnId;
        column.Width = 90;
        column.Index = -1;
    }
}