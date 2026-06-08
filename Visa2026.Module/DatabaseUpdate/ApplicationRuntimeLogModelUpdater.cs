using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Operations navigation for server runtime error inbox (administrators).</summary>
public sealed class ApplicationRuntimeLogModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var rootNode = (IModelRootNavigationItems)node;
        var navigationItems = rootNode.Items;

        var operations = navigationItems["Operations"]
            ?? navigationItems.AddNode<IModelNavigationItem>("Operations");
        operations.Caption ??= "Operations";
        operations.ImageName ??= "BO_Task";

        if (navigationItems["Default"]?.Items["ApplicationRuntimeLog"] is IModelNavigationItem defaultNavItem)
            defaultNavItem.Remove();

        var navItem = operations.Items["ApplicationRuntimeLog"]
            ?? operations.Items.AddNode<IModelNavigationItem>("ApplicationRuntimeLog");
        navItem.View = rootNode.Application.Views["ApplicationRuntimeLog_ListView"];
        navItem.Caption ??= "Runtime errors";
        navItem.ImageName = "BO_Validation";
    }
}
