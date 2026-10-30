using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Hides <see cref="ApplicationProfileInstance.ResultCoverageDisplay"/> on instance ListViews.
/// Coverage chips render inside the Application progress column after migration is reached.
/// </summary>
public sealed class ApplicationProfileInstanceResultCoverageColumnUpdater
    : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public const string ColumnId = nameof(ApplicationProfileInstance.ResultCoverageDisplay);

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

            HideColumn(listView);
        }
    }

    internal static void HideColumn(IModelListView listView)
    {
        var column = listView.Columns[ColumnId]
            ?? listView.Columns.AddNode<IModelColumn>(ColumnId);
        column.PropertyName = ColumnId;
        column.Index = -1;
    }
}
