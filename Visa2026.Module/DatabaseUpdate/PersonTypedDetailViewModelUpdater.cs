using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Clones typed Person detail views after default Items exist; copies Items when a layer added layout only.
/// </summary>
public sealed class PersonTypedDetailViewModelUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        if (node.Parent is not IModelDetailView detailView)
            return;

        if (detailView.Id == PersonDetailViewIds.Default)
        {
            PersonTypedDetailViewFactory.SyncTypedDetailViews(detailView.Application.Views);
            return;
        }

        if (detailView.Id is not PersonDetailViewIds.Employee and not PersonDetailViewIds.FamilyMember)
            return;

        if (detailView.Application.Views[PersonDetailViewIds.Default] is not IModelDetailView defaultDetailView)
            return;

        PersonDetailViewItemsCopyHelper.CopyItemsFromDefault(defaultDetailView, detailView);
    }
}
