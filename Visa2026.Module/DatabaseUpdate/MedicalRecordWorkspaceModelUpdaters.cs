using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.MedicalRecordWorkspace;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class MedicalRecordWorkspaceDetailViewUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != MedicalRecordWorkspaceViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        detailView.Caption = MedicalRecordWorkspaceViewIds.Caption;
        if (detailView.Items[nameof(MedicalRecordWorkspaceHost.WorkspaceUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(MedicalRecordWorkspaceHost.WorkspaceUi));
        item.PropertyName = nameof(MedicalRecordWorkspaceHost.WorkspaceUi);
        item.Caption = string.Empty;
    }
}

public sealed class MedicalRecordWorkspaceLayoutUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewLayoutNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (FindDetailView(node) is not { } detailView
            || detailView.Id != MedicalRecordWorkspaceViewIds.DetailView)
        {
            return;
        }

        detailView.SetValue("CustomCSSClassName", "medical-records-workspace-detail");

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
                     && layoutItem.ViewItem?.Id == nameof(MedicalRecordWorkspaceHost.WorkspaceUi))
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

public sealed class MedicalRecordWorkspaceNavigationUpdater
    : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var root = (IModelRootNavigationItems)node;
        if (root.Application.Views[MedicalRecordWorkspaceViewIds.DetailView] is not IModelDetailView detailView)
            return;

        detailView.Caption = MedicalRecordWorkspaceViewIds.Caption;
        detailView.SetValue("CustomCSSClassName", "medical-records-workspace-detail");

        var item = root.Items[MedicalRecordWorkspaceViewIds.NavItemId]
            ?? root.Items.AddNode<IModelNavigationItem>(MedicalRecordWorkspaceViewIds.NavItemId);
        item.View = detailView;
        item.Caption = MedicalRecordWorkspaceViewIds.Caption;
        item.ImageName = "BO_Document";
        item.Index = MedicalRecordWorkspaceViewIds.NavIndex;
        item.Visible = true;
    }
}
