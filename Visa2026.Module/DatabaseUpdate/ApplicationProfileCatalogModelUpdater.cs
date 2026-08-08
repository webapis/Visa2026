using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.ApplicationProfileCatalog;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Configuration navigation item for the Application Profile catalog DetailView.
/// </summary>
public sealed class ApplicationProfileCatalogModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public const string NavItemId = "ApplicationProfileCatalog";

    public override void UpdateNode(ModelNode node)
    {
        var rootNode = (IModelRootNavigationItems)node;
        var navigationItems = rootNode.Items;
        var views = rootNode.Application.Views;

        if (views[ApplicationProfileCatalogViewIds.DetailView] is not IModelDetailView detailView)
            return;

        detailView.Caption = "Application Profile";

        var configuration = navigationItems["Configuration"]
            ?? navigationItems.AddNode<IModelNavigationItem>("Configuration");
        configuration.Caption ??= "Configuration";

        // Strip native BO list / accidental host list entries under Configuration.
        if (configuration.Items["ApplicationProfile"] is IModelNavigationItem legacyList)
            legacyList.Remove();
        if (configuration.Items["ApplicationProfileCatalogHost"] is IModelNavigationItem legacyHostList)
            legacyHostList.Remove();

        var navItem = configuration.Items[NavItemId]
            ?? configuration.Items.AddNode<IModelNavigationItem>(NavItemId);
        navItem.View = detailView;
        navItem.Caption = "Application Profile";
        navItem.ImageName = "BO_List";
        navItem.Index = 0;
    }
}