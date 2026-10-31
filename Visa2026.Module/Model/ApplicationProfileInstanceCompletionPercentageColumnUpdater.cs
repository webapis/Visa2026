using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Places <see cref="ApplicationProfileInstance.CompletionPercentage"/> immediately
/// before the compact progress stepper on instance ListViews.
/// </summary>
public sealed class ApplicationProfileInstanceCompletionPercentageColumnUpdater
    : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public const string ColumnId = nameof(ApplicationProfileInstance.CompletionPercentage);
    public const string ProgressColumnId = nameof(ApplicationProfileInstance.ProgressStepsDisplay);

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

        var progress = listView.Columns[ProgressColumnId];
        if (progress != null && progress.Index >= 0)
        {
            column.Index = progress.Index;
            progress.Index = progress.Index + 1;
            return;
        }

        var lastVisible = listView.Columns
            .Where(c => !ReferenceEquals(c, column) && c.Index >= 0)
            .Select(c => c.Index)
            .DefaultIfEmpty(-1)
            .Max();
        column.Index = lastVisible + 1;
    }
}