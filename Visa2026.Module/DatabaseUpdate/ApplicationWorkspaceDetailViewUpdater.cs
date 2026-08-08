using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.ApplicationWorkspace;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationWorkspaceDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != ApplicationWorkspaceViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items[nameof(ApplicationWorkspaceHost.WorkspaceUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(ApplicationWorkspaceHost.WorkspaceUi));
        item.PropertyName = nameof(ApplicationWorkspaceHost.WorkspaceUi);
        item.Caption = string.Empty;
    }
}
