using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model
{
    public class RecycleBinViewNodesGeneratorUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
    {
        public override void UpdateNode(ModelNode node)
        {
            IModelViews views = (IModelViews)node;
            string targetViewId = "ApplicationItem_ListView_RecycleBin";

            if (views[targetViewId] == null)
            {
                IModelListView targetView = views.AddNode<IModelListView>(targetViewId);
                targetView.ModelClass = views.Application.BOModel.GetClass(typeof(ApplicationItem));
                targetView.Caption = "Recycle Bin (Applications)";
            }
        }
    }
}