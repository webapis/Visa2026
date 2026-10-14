using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Report Dashboard is a root folder. The folder opens Overview.
/// Each category is a child that opens that report on the same view.
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

        var navItem = navigationItems[ReportDashboardNavigationItems.RootId]
            ?? navigationItems.AddNode<IModelNavigationItem>(ReportDashboardNavigationItems.RootId);
        navItem.View = detailView;
        navItem.Caption = "Report Dashboard";
        navItem.ImageName = "BO_Report";
        navItem.Index = -100;

        var index = 0;
        foreach (var category in ReportDashboardCatalog.Categories)
        {
            var id = ReportDashboardNavigationItems.ChildId(category);
            var child = navItem.Items[id] ?? navItem.Items.AddNode<IModelNavigationItem>(id);
            child.View = detailView;
            child.Caption = ReportDashboardNavigationItems.EnglishCaption(category);
            child.Index = index++;
        }

        rootNode.StartupNavigationItem = navItem;
    }
}
