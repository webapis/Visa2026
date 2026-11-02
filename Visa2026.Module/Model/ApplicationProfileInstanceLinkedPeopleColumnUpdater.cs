using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Places the linked-people column immediately before the compact progress stepper.
/// </summary>
public sealed class ApplicationProfileInstanceLinkedPeopleColumnUpdater
    : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public const string ColumnId = nameof(ApplicationProfileInstance.LinkedPeopleDisplay);
    public const int ColumnWidth = 280;

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
        column.Width = ColumnWidth;

        var progress = listView.Columns[ApplicationProfileInstanceProgressStepsColumnUpdater.ColumnId];
        if (progress == null || progress.Index < 0 || column.Index == progress.Index - 1)
            return;

        var target = progress.Index;
        foreach (var other in listView.Columns)
        {
            if (ReferenceEquals(other, column) || other.Index < target)
                continue;
            other.Index++;
        }

        column.Index = target;
    }
}
