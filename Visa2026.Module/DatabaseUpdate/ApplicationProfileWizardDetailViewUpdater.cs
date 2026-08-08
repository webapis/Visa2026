using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.ApplicationProfileWizard;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationProfileWizardDetailViewUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != ApplicationProfileWizardViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items[nameof(ApplicationProfileWizardHost.WizardUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(ApplicationProfileWizardHost.WizardUi));
        item.PropertyName = nameof(ApplicationProfileWizardHost.WizardUi);
        item.Caption = string.Empty;
    }
}
