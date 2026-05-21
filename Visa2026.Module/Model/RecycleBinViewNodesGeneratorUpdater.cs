using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Model
{
    public class RecycleBinViewNodesGeneratorUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
    {
        public override void UpdateNode(ModelNode node)
        {
            IModelViews views = (IModelViews)node;

            // Find all types that implement ISoftDelete and are visible in the UI
            var softDeleteTypes = XafTypesInfo.Instance.PersistentTypes
                .Where(ti => typeof(ISoftDelete).IsAssignableFrom(ti.Type) && !ti.IsAbstract && ti.IsVisible)
                .Select(ti => ti.Type);

            foreach (var type in softDeleteTypes)
            {
                string targetViewId = $"{type.Name}_ListView_RecycleBin";

                IModelListView targetView;
                if (views[targetViewId] == null)
                {
                    targetView = views.AddNode<IModelListView>(targetViewId);
                    targetView.ModelClass = views.Application.BOModel.GetClass(type);
                    // Use the class's caption for a user-friendly name
                    targetView.Caption = VisaUiMessages.Format("RecycleBin.ViewCaption", targetView.ModelClass.Caption);
                }
                else
                {
                    targetView = (IModelListView)views[targetViewId];
                }

                // Add DateDeleted column if it doesn't exist
                if (targetView.Columns["DateDeleted"] == null)
                {
                    var dateColumn = targetView.Columns.AddNode<IModelColumn>("DateDeleted");
                    dateColumn.PropertyName = "DateDeleted";
                    dateColumn.Index = -1; // Add to the end
                }

                // Add DeletedBy column if it doesn't exist
                if (targetView.Columns["DeletedBy"] == null)
                {
                    var userColumn = targetView.Columns.AddNode<IModelColumn>("DeletedBy");
                    userColumn.PropertyName = "DeletedBy";
                    userColumn.Index = -1; // Add to the end
                }
            }
        }
    }
}