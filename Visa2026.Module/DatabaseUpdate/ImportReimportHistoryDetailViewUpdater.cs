using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures the custom Import history editor is on the host DetailView.
/// </summary>
public sealed class ImportReimportHistoryDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != "ImportReimportHistoryHost_DetailView")
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items["HistoryUi"] == null)
        {
            var item = detailView.Items.AddNode<IModelMemberViewItem>("HistoryUi");
            item.PropertyName = nameof(ImportReimportHistoryHost.HistoryUi);
        }
    }
}
