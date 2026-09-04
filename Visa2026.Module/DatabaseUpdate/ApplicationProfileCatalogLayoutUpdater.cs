using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.ApplicationProfileCatalog;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationProfileCatalogLayoutUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewLayoutNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (FindDetailView(node) is not { } detailView
            || detailView.Id != ApplicationProfileCatalogViewIds.DetailView)
        {
            return;
        }

        detailView.SetValue("CustomCSSClassName", "ap-catalog-detail");

        foreach (var element in EnumerateLayoutElements(node))
        {
            if (element is IModelLayoutGroup group)
            {
                group.RelativeSize = 100;
                if (string.Equals(group.Id, "Main", StringComparison.Ordinal))
                    group.SetValue("CustomCSSClassName", "xaf-fill-root");
                else
                {
                    group.ShowCaption = false;
                    group.SetValue("CustomCSSClassName", "xaf-fill-available");
                }
            }
            else if (element is IModelLayoutViewItem layoutItem
                     && layoutItem.ViewItem?.Id == nameof(ApplicationProfileCatalogHost.CatalogUi))
            {
                layoutItem.ShowCaption = false;
                layoutItem.RelativeSize = 100;
                layoutItem.SetValue("CustomCSSClassName", "xaf-fill-available");
            }
        }
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

    private static IEnumerable<IModelViewLayoutElement> EnumerateLayoutElements(IModelNode? node)
    {
        if (node == null)
            yield break;

        if (node is IModelViewLayoutElement layoutElement)
            yield return layoutElement;

        if (node is not ModelNode modelNode || modelNode.Nodes == null)
            yield break;

        foreach (ModelNode child in modelNode.Nodes)
        {
            if (child == null)
                continue;

            foreach (var nested in EnumerateLayoutElements(child))
                yield return nested;
        }
    }
}