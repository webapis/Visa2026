using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.StateNotifications;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures the inbox custom editor is registered on the detail view (Browsable was blocking auto-generation).
/// </summary>
public sealed class BoStateNotificationInboxDetailViewUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != "BoStateNotificationInboxHost_DetailView")
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items["InboxUi"] == null)
        {
            var item = detailView.Items.AddNode<IModelMemberViewItem>("InboxUi");
            item.PropertyName = nameof(BoStateNotificationInboxHost.InboxUi);
        }
    }
}
