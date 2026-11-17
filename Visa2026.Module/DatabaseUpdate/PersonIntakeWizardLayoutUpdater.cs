using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures the wizard layout item exists. Hiding classic layout uses <c>Model.xafml</c> (Removed on separated nodes);
/// do not call <c>Removed</c> on inherited layout groups from code — XAF throws on non-separated nodes.
/// </summary>
public sealed class PersonIntakeWizardLayoutUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewLayoutNodesGenerator>
{
    private static readonly string[] ViewIds =
    [
        PersonDetailViewIds.EmployeeIntake,
        PersonDetailViewIds.FamilyMemberIntake
    ];

    public override void UpdateNode(ModelNode node)
    {
        if (FindDetailView(node) is not { } detailView || !ViewIds.Contains(detailView.Id))
            return;

        const string propertyName = nameof(Person.PersonIntakeWizardUi);
        EnsureLayoutItem(detailView, propertyName);

        var layoutItem = FindLayoutViewItem(node, propertyName);
        if (layoutItem != null)
            layoutItem.ShowCaption = false;
    }

    private static void EnsureLayoutItem(IModelDetailView detailView, string propertyName)
    {
        if (detailView.Layout == null || FindLayoutViewItem(detailView.Layout, propertyName) != null)
            return;

        if (detailView.Items[propertyName] == null)
            return;

        var main = FindLayoutGroup(detailView.Layout, "Main");
        if (main == null)
            return;

        var layoutItem = main.AddNode<IModelLayoutViewItem>(propertyName);
        layoutItem.ViewItem = detailView.Items[propertyName];
        layoutItem.Index = 0;
        layoutItem.RelativeSize = 100;
        layoutItem.ShowCaption = false;
    }

    private static IModelLayoutGroup? FindLayoutGroup(IModelNode root, string groupId)
    {
        foreach (var group in EnumerateLayoutGroups(root))
        {
            if (string.Equals(group.Id, groupId, System.StringComparison.Ordinal))
                return group;
        }

        return null;
    }

    private static IEnumerable<IModelLayoutGroup> EnumerateLayoutGroups(IModelNode? node)
    {
        if (node == null)
            yield break;

        if (node is IModelLayoutGroup group)
            yield return group;

        if (node is not ModelNode modelNode || modelNode.Nodes == null)
            yield break;

        foreach (ModelNode child in modelNode.Nodes)
        {
            if (child == null)
                continue;
            foreach (var nested in EnumerateLayoutGroups(child))
                yield return nested;
        }
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
}