using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// ListViews show localized <see cref="ApplicationProfileInstance.MonthName"/> instead of integer Month
/// (officers confused Month with Person count).
/// </summary>
public sealed class ApplicationProfileInstanceMonthNameColumnUpdater
    : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
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

            ReplaceMonthWithMonthName(listView);
        }
    }

    internal static void ReplaceMonthWithMonthName(IModelListView listView)
    {
        var monthColumn = listView.Columns[nameof(ApplicationProfileInstance.Month)]
            ?? listView.Columns.FirstOrDefault(c => c.PropertyName == nameof(ApplicationProfileInstance.Month));

        var monthIndex = monthColumn?.Index ?? -1;
        var monthCaption = monthColumn?.Caption;
        var monthWidth = monthColumn?.Width ?? 0;
        var monthSortIndex = monthColumn?.SortIndex ?? -1;
        var monthSortOrder = monthColumn?.SortOrder ?? DevExpress.Data.ColumnSortOrder.None;

        if (monthColumn != null)
            monthColumn.Index = -1;

        var nameColumn = listView.Columns[nameof(ApplicationProfileInstance.MonthName)]
            ?? listView.Columns.AddNode<IModelColumn>(nameof(ApplicationProfileInstance.MonthName));
        nameColumn.PropertyName = nameof(ApplicationProfileInstance.MonthName);
        if (!string.IsNullOrWhiteSpace(monthCaption))
            nameColumn.Caption = monthCaption;
        if (monthWidth > 0)
            nameColumn.Width = monthWidth;
        if (monthIndex >= 0)
            nameColumn.Index = monthIndex;
        else if (nameColumn.Index < 0)
            nameColumn.Index = 100;

        // Prefer chronological sort via stored Month when the list was sorted by Month.
        if (monthSortIndex >= 0 && monthColumn != null)
        {
            monthColumn.SortIndex = monthSortIndex;
            monthColumn.SortOrder = monthSortOrder;
            nameColumn.SortIndex = -1;
            nameColumn.SortOrder = DevExpress.Data.ColumnSortOrder.None;
        }
    }
}