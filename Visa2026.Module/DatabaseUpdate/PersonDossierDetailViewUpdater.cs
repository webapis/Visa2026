using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.PersonDossier;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// The dossier editor draws its own header, so the generated layout caption ("Dossier Ui") is suppressed.
/// </summary>
public sealed class PersonDossierDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewLayoutNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (FindDetailView(node) is not { } detailView
            || detailView.Id != PersonDossierViewIds.DetailView)
        {
            return;
        }

        var layoutItem = FindLayoutViewItem(node, nameof(PersonDossierHost.DossierUi));
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
