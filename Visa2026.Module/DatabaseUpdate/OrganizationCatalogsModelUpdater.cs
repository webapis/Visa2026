using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.OrganizationCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Configuration folder item for Organization catalogs (kept off left nav; inline New/Edit on create and case).
/// </summary>
public sealed class OrganizationCatalogsModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public const string NavItemId = "OrganizationCatalogs";

    public override void UpdateNode(ModelNode node)
    {
        var rootNode = (IModelRootNavigationItems)node;
        var navigationItems = rootNode.Items;
        var views = rootNode.Application.Views;

        if (views[OrganizationCatalogsViewIds.DetailView] is not IModelDetailView detailView)
            return;

        detailView.Caption = OrganizationCatalogsViewIds.Caption;
        detailView.SetValue("CustomCSSClassName", "org-catalogs-detail");

        if (navigationItems["Configuration"] is not IModelNavigationItem configuration)
            configuration = navigationItems.AddNode<IModelNavigationItem>("Configuration");

        if (configuration.Items["OrganizationCatalogsHost"] is IModelNavigationItem legacyHostList)
            legacyHostList.Remove();

        var navItem = configuration.Items[NavItemId]
            ?? configuration.Items.AddNode<IModelNavigationItem>(NavItemId);
        navItem.View = detailView;
        navItem.Caption = OrganizationCatalogsViewIds.Caption;
        navItem.ImageName = "BO_Organization";
        navItem.Index = 1;
        navItem.Visible = false;
    }
}
