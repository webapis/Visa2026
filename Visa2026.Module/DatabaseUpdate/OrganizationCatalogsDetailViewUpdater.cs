using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.OrganizationCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class OrganizationCatalogsDetailViewUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != OrganizationCatalogsViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        detailView.Caption = OrganizationCatalogsViewIds.Caption;
        if (detailView.Items[nameof(OrganizationCatalogsHost.CatalogsUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(OrganizationCatalogsHost.CatalogsUi));
        item.PropertyName = nameof(OrganizationCatalogsHost.CatalogsUi);
        item.Caption = string.Empty;
    }
}
