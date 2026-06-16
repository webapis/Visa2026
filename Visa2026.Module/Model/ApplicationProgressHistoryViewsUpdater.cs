using System;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Progress history list views: combined status column (state + ministry), date, description, letter file name.
/// </summary>
public sealed class ApplicationProgressHistoryViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    private const string NestedListViewId = "Application_ProgressHistory_ListView";
    private const string StandaloneListViewId = "ApplicationProgress_ListView";

    private static readonly string[] ColumnOrder =
    [
        nameof(ApplicationProgress.StatusListLabel),
        nameof(ApplicationProgress.Date),
        nameof(ApplicationProgress.Description),
        nameof(ApplicationProgress.MinistryLetterFileName),
    ];

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        if (views[NestedListViewId] is IModelListView nestedListView)
            ConfigureColumns(nestedListView, ColumnOrder);

        if (views[StandaloneListViewId] is IModelListView standaloneListView)
            ConfigureColumns(standaloneListView, ColumnOrder);
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
        }
    }
}
