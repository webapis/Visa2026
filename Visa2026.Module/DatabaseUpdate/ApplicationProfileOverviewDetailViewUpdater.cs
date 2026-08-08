using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.ApplicationProfileOverview;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationProfileOverviewDetailViewUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != ApplicationProfileOverviewViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items[nameof(ApplicationProfileOverviewHost.OverviewUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(ApplicationProfileOverviewHost.OverviewUi));
        item.PropertyName = nameof(ApplicationProfileOverviewHost.OverviewUi);
        item.Caption = string.Empty;
    }
}
