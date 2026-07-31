using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Navigation item under Operations for Import reimport history (Administrators).
/// </summary>
public sealed class ImportReimportHistoryModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
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

        if (views["ImportReimportHistoryHost_DetailView"] is not IModelDetailView detailView)
            return;

        var navItem = operations.Items["ImportReimportHistory"]
            ?? operations.Items.AddNode<IModelNavigationItem>("ImportReimportHistory");
        navItem.View = detailView;
        navItem.Caption = "Import reimport history";
        navItem.ImageName = "BO_Report";
    }
}
