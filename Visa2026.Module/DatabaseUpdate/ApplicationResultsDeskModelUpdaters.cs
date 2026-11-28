using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.ApplicationResultsDesk;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationResultsDeskDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != ApplicationResultsDeskViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        detailView.Caption = ApplicationResultsDeskViewIds.Caption;
        if (detailView.Items[nameof(ApplicationResultsDeskHost.DeskUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(ApplicationResultsDeskHost.DeskUi));
        item.PropertyName = nameof(ApplicationResultsDeskHost.DeskUi);
        item.Caption = string.Empty;
    }
}

public sealed class ApplicationResultsDeskLayoutUpdater : ModelNodesGeneratorUpdater<ModelDetailViewLayoutNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (FindDetailView(node) is not { } detailView || detailView.Id != ApplicationResultsDeskViewIds.DetailView)
            return;

        detailView.SetValue("CustomCSSClassName", "application-results-desk-detail");

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
                     && layoutItem.ViewItem?.Id == nameof(ApplicationResultsDeskHost.DeskUi))
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

public sealed class ApplicationResultsDeskNavigationUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var root = (IModelRootNavigationItems)node;
        if (root.Application.Views[ApplicationResultsDeskViewIds.DetailView] is not IModelDetailView detailView)
            return;

        detailView.Caption = ApplicationResultsDeskViewIds.Caption;
        detailView.SetValue("CustomCSSClassName", "application-results-desk-detail");

        var item = root.Items[ApplicationResultsDeskViewIds.NavItemId]
            ?? root.Items.AddNode<IModelNavigationItem>(ApplicationResultsDeskViewIds.NavItemId);
        item.View = detailView;
        item.Caption = ApplicationResultsDeskViewIds.Caption;
        item.ImageName = "BO_List";
        item.Index = ApplicationResultsDeskViewIds.NavIndex;
        item.Visible = true;
    }
}
