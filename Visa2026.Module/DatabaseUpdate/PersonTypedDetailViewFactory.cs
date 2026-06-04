using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures typed Person detail views exist with <see cref="IModelDetailView.ModelClass"/> and Items cloned from the default view.
/// </summary>
internal static class PersonTypedDetailViewFactory
{
    public static void SyncTypedDetailViews(IModelViews modelViews)
    {
        if (modelViews[PersonDetailViewIds.Default] is not IModelDetailView defaultDetailView
            || defaultDetailView.Items.Count == 0)
        {
            return;
        }

        EnsureTypedDetailView(modelViews, PersonDetailViewIds.Employee, defaultDetailView);
        EnsureTypedDetailView(modelViews, PersonDetailViewIds.FamilyMember, defaultDetailView);
        EnsureTypedDetailView(modelViews, PersonDetailViewIds.TemporaryVisitor, defaultDetailView);
    }

    private static void EnsureTypedDetailView(
        IModelViews modelViews,
        string detailViewId,
        IModelDetailView defaultDetailView)
    {
        if (defaultDetailView is not ModelNode defaultNode
            || defaultNode.Parent is not ModelNode viewsNode)
        {
            return;
        }

        if (modelViews[detailViewId] is IModelDetailView typedDetailView)
        {
            if (typedDetailView.ModelClass == null && defaultDetailView.ModelClass != null)
                typedDetailView.ModelClass = defaultDetailView.ModelClass;

            if (typedDetailView.Items.Count == 0)
                PersonDetailViewItemsCopyHelper.CopyItemsFromDefault(defaultDetailView, typedDetailView);

            return;
        }

        viewsNode.AddClonedNode(defaultNode, detailViewId);
        if (modelViews[detailViewId] is IModelDetailView clonedDetailView)
        {
            if (clonedDetailView.ModelClass == null && defaultDetailView.ModelClass != null)
                clonedDetailView.ModelClass = defaultDetailView.ModelClass;

            PersonDetailViewItemsCopyHelper.CopyItemsFromDefault(defaultDetailView, clonedDetailView);
        }
    }
}
