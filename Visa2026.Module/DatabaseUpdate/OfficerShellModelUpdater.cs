using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.OfficerShell;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Application navigation item for the officer shell (staged / in-process / templates).
/// </summary>
public sealed class OfficerShellModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public const string NavItemId = "OfficerShell";

    public override void UpdateNode(ModelNode node)
    {
        var rootNode = (IModelRootNavigationItems)node;
        var navigationItems = rootNode.Items;
        var views = rootNode.Application.Views;

        if (views[OfficerShellViewIds.DetailView] is not IModelDetailView detailView)
            return;

        detailView.Caption = "Application Profiles";

        var applicationNav = navigationItems["Application"]
            ?? navigationItems.AddNode<IModelNavigationItem>("Application");
        applicationNav.Caption ??= "Application";

        var navItem = applicationNav.Items[NavItemId]
            ?? applicationNav.Items.AddNode<IModelNavigationItem>(NavItemId);
        navItem.View = detailView;
        navItem.Caption = "Application Profiles";
        navItem.ImageName = "BO_List";
        navItem.Index = 0;
    }
}
