using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.OfficerShell;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class OfficerShellDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != OfficerShellViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items[nameof(OfficerShellHost.ShellUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(OfficerShellHost.ShellUi));
        item.PropertyName = nameof(OfficerShellHost.ShellUi);
        item.Caption = string.Empty;
    }
}
