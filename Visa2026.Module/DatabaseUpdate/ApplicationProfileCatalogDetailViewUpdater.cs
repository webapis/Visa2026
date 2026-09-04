using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.ApplicationProfileCatalog;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationProfileCatalogDetailViewUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != ApplicationProfileCatalogViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items[nameof(ApplicationProfileCatalogHost.CatalogUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(ApplicationProfileCatalogHost.CatalogUi));
        item.PropertyName = nameof(ApplicationProfileCatalogHost.CatalogUi);
        item.Caption = string.Empty;
    }
}