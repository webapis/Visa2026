using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.ReportDashboard;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ReportDashboardDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != "ReportDashboardHost_DetailView")
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items["DashboardUi"] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>("DashboardUi");
        item.PropertyName = nameof(ReportDashboardHost.DashboardUi);
        item.Caption = string.Empty;
    }
}