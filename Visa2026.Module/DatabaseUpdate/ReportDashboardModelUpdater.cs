using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.ReportDashboard;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Top-level Home navigation + startup item for the Report Dashboard.
/// </summary>
public sealed class ReportDashboardModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var rootNode = (IModelRootNavigationItems)node;
        var navigationItems = rootNode.Items;
        var views = rootNode.Application.Views;

        if (views["ReportDashboardHost_DetailView"] is not IModelDetailView detailView)
            return;

        var home = navigationItems["Home"]
            ?? navigationItems.AddNode<IModelNavigationItem>("Home");
        home.Caption = "Home";
        home.ImageName = "BO_Dashboard";
        home.Index = -100;

        var navItem = home.Items["ReportDashboard"]
            ?? home.Items.AddNode<IModelNavigationItem>("ReportDashboard");
        navItem.View = detailView;
        navItem.Caption = "Report Dashboard";
        navItem.ImageName = "BO_Report";
        navItem.Index = 0;

        rootNode.StartupNavigationItem = navItem;
    }
}
