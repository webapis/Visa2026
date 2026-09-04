using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.ApplicationProfilePicker;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationProfilePickerLayoutUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewLayoutNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (FindDetailView(node) is not { } detailView
            || detailView.Id != ApplicationProfilePickerViewIds.DetailView)
        {
            return;
        }

        var layoutItem = FindLayoutViewItem(node, nameof(ApplicationProfilePickerHost.PickerUi));
        if (layoutItem != null)
            layoutItem.ShowCaption = false;
    }

    private static IModelDetailView? FindDetailView(IModelNode? node)
    {
        while (node != null)
        {
            if (node is IModelDetailView detailView)
                return detailView;

            node = node.Parent;
        }

        return null;
    }

    private static IModelLayoutViewItem? FindLayoutViewItem(IModelNode? root, string viewItemId)
    {
        foreach (var item in EnumerateLayoutViewItems(root))
        {
            if (item.ViewItem?.Id == viewItemId)
                return item;
        }

        return null;
    }

    private static IEnumerable<IModelLayoutViewItem> EnumerateLayoutViewItems(IModelNode? node)
    {
        if (node == null)
            yield break;

        if (node is IModelLayoutViewItem layoutViewItem)
            yield return layoutViewItem;

        if (node is not ModelNode modelNode || modelNode.Nodes == null)
            yield break;

        foreach (ModelNode child in modelNode.Nodes)
        {
            if (child == null)
                continue;

            foreach (var nested in EnumerateLayoutViewItems(child))
                yield return nested;
        }
    }
}
