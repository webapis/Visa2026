using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class LegacySyncDashboardModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var rootNode = (IModelRootNavigationItems)node;
        var navigationItems = rootNode.Items;
        var views = rootNode.Application.Views;

        var operations = navigationItems["Operations"]
            ?? navigationItems.AddNode<IModelNavigationItem>("Operations");
        operations.Caption ??= "Operations";
        operations.ImageName ??= "BO_Task";

        if (views["LegacySyncDashboardHost_DetailView"] is not IModelDetailView detailView)
            return;

        var navItem = operations.Items["LegacySyncDashboard"]
            ?? operations.Items.AddNode<IModelNavigationItem>("LegacySyncDashboard");
        navItem.View = detailView;
        navItem.Caption = Localization.VisaUiMessages.Get("LegacySync.Title");
        navItem.ImageName = "BO_List";
    }
}