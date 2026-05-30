using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.Feedback;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Places Status on the UserFeedback detail form (model may omit it when the BO was extended after first deploy).</summary>
public sealed class UserFeedbackDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    private const string DetailViewId = "UserFeedback_DetailView";

    public override void UpdateNode(ModelNode node)
    {
        if (node.Parent is not IModelDetailView detailView || detailView.Id != DetailViewId)
            return;

        EnsureMemberItem(detailView, nameof(UserFeedback.Status));
        EnsureLayoutItem(detailView, nameof(UserFeedback.Status), insertAfter: nameof(UserFeedback.Severity));
    }

    private static void EnsureMemberItem(IModelDetailView detailView, string propertyName)
    {
        if (detailView.Items[propertyName] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(propertyName);
        item.PropertyName = propertyName;
    }

    private static void EnsureLayoutItem(IModelDetailView detailView, string propertyName, string insertAfter)
    {
        if (detailView.Layout == null || FindLayoutViewItem(detailView.Layout, propertyName) != null)
            return;

        var anchor = FindLayoutViewItem(detailView.Layout, insertAfter);
        if (anchor?.Parent is not IModelLayoutGroup parentGroup)
            return;

        var layoutItem = parentGroup.AddNode<IModelLayoutViewItem>(propertyName);
        layoutItem.ViewItem = detailView.Items[propertyName];
        layoutItem.Index = anchor.Index + 1;
        layoutItem.RelativeSize = anchor.RelativeSize;
    }

    private static IModelLayoutViewItem? FindLayoutViewItem(IModelNode root, string viewItemId)
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
