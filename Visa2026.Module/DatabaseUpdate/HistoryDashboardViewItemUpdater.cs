using DevExpress.ExpressApp.Model;
using Visa2026.Module.Localization;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;

namespace Visa2026.Module.DatabaseUpdate
{
    public class HistoryDashboardViewItemUpdater : ModelNodesGeneratorUpdater<ModelDetailViewItemsNodesGenerator>
    {
        public override void UpdateNode(ModelNode node)
        {
            // Check if we are in the Visa Detail View
            if (node.Parent is IModelDetailView detailView && detailView.Id == "Visa_DetailView")
            {
                // Add the DashboardViewItem if it doesn't exist
                if (detailView.Items["History"] == null)
                {
                    var dashboardItem = detailView.Items.AddNode<IModelDashboardViewItem>("History");
                    dashboardItem.View = detailView.Application.Views["StateChangeLog_ListView"] as IModelView;
                    dashboardItem.Caption = VisaUiMessages.Get("Dashboard.History");
                }
            }
        }
    }
}