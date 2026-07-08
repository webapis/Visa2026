using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class LegacySyncDashboardDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != "LegacySyncDashboardHost_DetailView")
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items["DashboardUi"] == null)
        {
            var item = detailView.Items.AddNode<IModelMemberViewItem>("DashboardUi");
            item.PropertyName = nameof(LegacySyncDashboardHost.DashboardUi);
        }
    }
}
