using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.StateNotifications;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Navigation + detail layout for the state-notification inbox UI prototype.
/// </summary>
public sealed class BoStateNotificationInboxModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
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

        if (views["BoStateNotificationInboxHost_DetailView"] is not IModelDetailView detailView)
            return;

        var navItem = operations.Items["StateNotifications"]
            ?? operations.Items.AddNode<IModelNavigationItem>("StateNotifications");
        navItem.View = detailView;
        navItem.Caption = "State notifications";
        navItem.ImageName = "BO_Validation";
    }
}
