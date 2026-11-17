using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects.PersonIntake;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class PersonIntakeWizardHostDetailViewUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Id != PersonIntakeWizardViewIds.DetailView)
            return;

        var detailView = (IModelDetailView)node;
        if (detailView.Items[nameof(PersonIntakeWizardHost.WizardUi)] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(nameof(PersonIntakeWizardHost.WizardUi));
        item.PropertyName = nameof(PersonIntakeWizardHost.WizardUi);
        item.Caption = string.Empty;
    }
}
