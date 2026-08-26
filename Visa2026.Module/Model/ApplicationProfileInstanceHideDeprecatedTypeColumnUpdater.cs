using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>
/// Hides deprecated <see cref="ApplicationProfileInstance.ApplicationType"/> on instance ListViews.
/// Officers use <see cref="ApplicationProfileInstance.ApplicationProfile"/> (template) instead.
/// </summary>
public sealed class ApplicationProfileInstanceHideDeprecatedTypeColumnUpdater
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
    ];

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var listViewId in ApplicationInstanceListViewIds)
        {
            if (views[listViewId] is not IModelListView listView)
                continue;

            HideApplicationTypeColumn(listView);
        }
    }

    internal static void HideApplicationTypeColumn(IModelListView listView)
    {
        var column = listView.Columns[nameof(ApplicationProfileInstance.ApplicationType)];
        if (column == null)
            return;

        column.Index = -1;
    }
}