using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Visa2026.Module.DatabaseUpdate;

internal static class PersonDetailViewItemsCopyHelper
{
    public static void CopyItemsFromDefault(IModelDetailView defaultDetailView, IModelDetailView typedDetailView)
    {
        if (typedDetailView.Items.Count > 0)
            return;

        if (defaultDetailView is not ModelNode defaultDetailNode || typedDetailView is not ModelNode typedDetailNode)
            return;

        var defaultItemsNode = (ModelNode)defaultDetailView.Items;
        var typedItemsNode = (ModelNode)typedDetailView.Items;
        if (defaultItemsNode.Nodes == null)
            return;

        foreach (ModelNode itemNode in defaultItemsNode.Nodes)
        {
            if (itemNode?.Id == null || typedItemsNode[itemNode.Id] != null)
                continue;

            typedItemsNode.AddClonedNode(itemNode, itemNode.Id);
        }
    }
}
