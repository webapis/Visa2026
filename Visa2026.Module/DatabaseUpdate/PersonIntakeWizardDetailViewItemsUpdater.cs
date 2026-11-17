using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class PersonIntakeWizardDetailViewItemsUpdater
    : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    private static readonly string[] ViewIds =
    [
        PersonDetailViewIds.EmployeeIntake,
        PersonDetailViewIds.FamilyMemberIntake
    ];

    public override void UpdateNode(ModelNode node)
    {
        if (node is not IModelDetailView detailView || !ViewIds.Contains(detailView.Id))
            return;

        const string propertyName = nameof(Person.PersonIntakeWizardUi);
        if (detailView.Items[propertyName] != null)
            return;

        var item = detailView.Items.AddNode<IModelMemberViewItem>(propertyName);
        item.PropertyName = propertyName;
        item.Caption = string.Empty;
        item.Index = -3000;
    }
}