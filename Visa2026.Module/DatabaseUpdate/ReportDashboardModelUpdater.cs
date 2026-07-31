using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using System.Linq;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Top-level navigation + startup item for the Report Dashboard.
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

        // Legacy: Report Dashboard lived under Home — promote to root and drop empty Home.
        if (navigationItems["Home"] is IModelNavigationItem home)
        {
            if (home.Items["ReportDashboard"] is IModelNavigationItem nested)
                nested.Remove();
            if (!home.Items.Any())
                home.Remove();
        }

        var navItem = navigationItems["ReportDashboard"]
            ?? navigationItems.AddNode<IModelNavigationItem>("ReportDashboard");
        navItem.View = detailView;
        navItem.Caption = "Report Dashboard";
        navItem.ImageName = "BO_Report";
        navItem.Index = -100;

        rootNode.StartupNavigationItem = navItem;
    }
}