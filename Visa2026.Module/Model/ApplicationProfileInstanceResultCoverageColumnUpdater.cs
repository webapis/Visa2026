using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Places <see cref="ApplicationProfileInstance.ResultCoverageDisplay"/> last
/// on instance ListViews (same people coverage as Result-tab Netije syny).
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

            EnsureColumn(listView);
        }
    }

    internal static void EnsureColumn(IModelListView listView)
    {
        var column = listView.Columns[ColumnId]
            ?? listView.Columns.AddNode<IModelColumn>(ColumnId);
        column.PropertyName = ColumnId;
        column.Width = 560;
        if (string.IsNullOrWhiteSpace(column.Caption))
            column.Caption = "Application result";

        var lastVisible = listView.Columns
            .Where(c => !ReferenceEquals(c, column) && c.Index >= 0)
            .Select(c => c.Index)
            .DefaultIfEmpty(-1)
            .Max();
        column.Index = lastVisible + 1;
    }
}
