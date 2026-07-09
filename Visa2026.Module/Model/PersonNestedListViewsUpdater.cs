using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;

namespace Visa2026.Module.Model;

/// <summary>
/// Person detail nested collections that mirror application workflow output are browse-only on the person form.
/// </summary>
public sealed class PersonNestedListViewsUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var listViewId in PersonNestedCollectionLayout.ReadOnlyNestedListViewIds)
        {
            if (views[listViewId] is not IModelListView listView)
                continue;

            ConfigureReadOnlyNestedListView(listView);
        }
    }

    internal static void ConfigureReadOnlyNestedListView(IModelListView listView)
    {
        listView.AllowNew = false;
        listView.AllowDelete = false;
        listView.AllowEdit = false;
        listView.AllowLink = false;
        listView.AllowUnlink = false;
    }
}