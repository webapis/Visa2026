using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationProfileCatalog;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Application Profiles folder item for the Application Profile catalog DetailView.
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

        detailView.Caption = ApplicationProfileInstanceProgressRouteNavigation.CaptionTemplates;
        detailView.SetValue("CustomCSSClassName", "ap-catalog-detail");

        if (navigationItems["Configuration"] is IModelNavigationItem configuration)
        {
            if (configuration.Items["ApplicationProfile"] is IModelNavigationItem legacyList)
                legacyList.Remove();
            if (configuration.Items["ApplicationProfileCatalogHost"] is IModelNavigationItem legacyHostList)
                legacyHostList.Remove();
            if (configuration.Items[NavItemId] is IModelNavigationItem legacyCatalog)
                legacyCatalog.Remove();
        }

        var applicationNav = navigationItems["Application"]
            ?? navigationItems.AddNode<IModelNavigationItem>("Application");
        applicationNav.Caption = ApplicationProfileInstanceProgressRouteNavigation.CaptionGroup;

        var navItem = applicationNav.Items[NavItemId]
            ?? applicationNav.Items.AddNode<IModelNavigationItem>(NavItemId);
        navItem.View = detailView;
        navItem.Caption = ApplicationProfileInstanceProgressRouteNavigation.CaptionTemplates;
        navItem.ImageName = "BO_List";
        navItem.Index = 2;
    }
}
