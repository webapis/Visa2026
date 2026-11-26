using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.EmployeeSearch;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class EmployeeSearchDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != EmployeeSearchViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        detailView.Caption = EmployeeSearchViewIds.Caption;
        if (detailView.Items[nameof(EmployeeSearchHost.SearchUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(EmployeeSearchHost.SearchUi));
        item.PropertyName = nameof(EmployeeSearchHost.SearchUi);
        item.Caption = string.Empty;
    }
}

public sealed class EmployeeSearchLayoutUpdater : ModelNodesGeneratorUpdater<ModelDetailViewLayoutNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (FindDetailView(node) is not { } detailView || detailView.Id != EmployeeSearchViewIds.DetailView)
            return;

        detailView.SetValue("CustomCSSClassName", "employee-search-detail");

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
                     && layoutItem.ViewItem?.Id == nameof(EmployeeSearchHost.SearchUi))
            {
                layoutItem.ShowCaption = false;
                layoutItem.RelativeSize = 100;
                layoutItem.SetValue("CustomCSSClassName", "xaf-fill-available");
            }
        }
    }

    private static IModelDetailView FindDetailView(IModelNode node)
    {
        while (node != null)
        {
            if (node is IModelDetailView detailView)
                return detailView;

            node = node.Parent;
        }

        return null;
    }

    private static IEnumerable<IModelViewLayoutElement> EnumerateLayoutElements(IModelNode node)
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

public sealed class EmployeeSearchNavigationUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var root = (IModelRootNavigationItems)node;
        if (root.Application.Views[EmployeeSearchViewIds.DetailView] is not IModelDetailView detailView)
            return;

        detailView.Caption = EmployeeSearchViewIds.Caption;
        detailView.SetValue("CustomCSSClassName", "employee-search-detail");

        var item = root.Items[EmployeeSearchViewIds.NavItemId]
            ?? root.Items.AddNode<IModelNavigationItem>(EmployeeSearchViewIds.NavItemId);
        item.View = detailView;
        item.Caption = EmployeeSearchViewIds.Caption;
        item.ImageName = "Action_Search";
        item.Index = EmployeeSearchViewIds.NavIndex;
        item.Visible = true;
    }
}
