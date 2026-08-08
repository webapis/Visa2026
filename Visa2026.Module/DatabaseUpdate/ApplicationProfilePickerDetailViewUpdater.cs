using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.ApplicationProfilePicker;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationProfilePickerDetailViewUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != ApplicationProfilePickerViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items[nameof(ApplicationProfilePickerHost.PickerUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(ApplicationProfilePickerHost.PickerUi));
        item.PropertyName = nameof(ApplicationProfilePickerHost.PickerUi);
        item.Caption = string.Empty;
    }
}
