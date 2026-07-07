using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;

namespace Visa2026.Module.Model;

/// <summary>
/// Shows the grid search box (Find Panel) on every ListView by default.
/// </summary>
public sealed class ListViewShowFindPanelModelUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (IModelListView listView in views.OfType<IModelListView>())
        {
            if (listView is IModelListViewShowFindPanel findPanel)
                findPanel.ShowFindPanel = true;
        }
    }
}
