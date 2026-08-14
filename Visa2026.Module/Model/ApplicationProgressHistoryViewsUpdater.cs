using System;
using System.Linq;
using DevExpress.Data;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.Xpo.DB;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Progress history list views: combined status column (state + ministry), date, description, letter file name.
/// </summary>
public sealed class ApplicationProfileInstanceProgressHistoryViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    private const string NestedListViewId = "Application_ProgressHistory_ListView";
    private const string StandaloneListViewId = "ApplicationProfileInstanceProgress_ListView";

    private static readonly string[] ColumnOrder =
    [
        nameof(ApplicationProfileInstanceProgress.Order),
        nameof(ApplicationProfileInstanceProgress.StatusListLabel),
        nameof(ApplicationProfileInstanceProgress.Date),
        nameof(ApplicationProfileInstanceProgress.ProcessNumber),
        nameof(ApplicationProfileInstanceProgress.Description),
        nameof(ApplicationProfileInstanceProgress.MinistryLetterFileName),
    ];

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        if (views[NestedListViewId] is IModelListView nestedListView)
            ConfigureListView(nestedListView, ColumnOrder);

        if (views[StandaloneListViewId] is IModelListView standaloneListView)
            ConfigureListView(standaloneListView, ColumnOrder);
    }

    private static void ConfigureListView(IModelListView listView, string[] visiblePropertyNames)
    {
        ConfigureColumns(listView, visiblePropertyNames);
        DisableInteractiveColumnSort(listView);
        EnsureTimelineSortOrder(listView);
    }

    private static void DisableInteractiveColumnSort(IModelListView listView)
    {
        foreach (var column in listView.Columns)
        {
            column.SortIndex = -1;
            column.SortOrder = ColumnSortOrder.None;
        }
    }

    private static void EnsureTimelineSortOrder(IModelListView listView)
    {
        EnsureSortProperty(listView, nameof(ApplicationProfileInstanceProgress.Order), SortingDirection.Ascending);
    }

    private static void EnsureSortProperty(
        IModelListView listView,
        string propertyName,
        SortingDirection direction)
    {
        var sortNode = listView.Sorting[propertyName]
            ?? listView.Sorting.AddNode<IModelSortProperty>(propertyName);
        sortNode.PropertyName = propertyName;
        sortNode.Direction = direction;
    }

    private static void ConfigureColumns(IModelListView listView, string[] visiblePropertyNames)
    {
        var visible = visiblePropertyNames
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.Ordinal);

        foreach (var column in listView.Columns.ToList())
        {
            if (visible.TryGetValue(column.PropertyName ?? string.Empty, out var index))
                column.Index = index;
            else
                column.Index = -1;
        }

        for (var i = 0; i < visiblePropertyNames.Length; i++)
        {
            var name = visiblePropertyNames[i];
            var column = listView.Columns[name] ?? listView.Columns.AddNode<IModelColumn>(name);
            column.PropertyName = name;
            column.Index = i;
            if (string.Equals(name, nameof(ApplicationProfileInstanceProgress.Order), StringComparison.Ordinal))
            {
                column.Width = 60;
                column.SortIndex = -1;
                column.SortOrder = ColumnSortOrder.None;
            }
        }
    }
}
