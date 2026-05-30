using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Operations navigation for developer feedback triage (administrators).</summary>
public sealed class UserFeedbackModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public override void UpdateNode(ModelNode node)
    {
        var rootNode = (IModelRootNavigationItems)node;
        var navigationItems = rootNode.Items;

        var operations = navigationItems["Operations"]
            ?? navigationItems.AddNode<IModelNavigationItem>("Operations");
        operations.Caption ??= "Operations";
        operations.ImageName ??= "BO_Task";

        // DefaultClassOptions can still emit a Default/UserFeedback node from cached model — remove it.
        if (navigationItems["Default"]?.Items["UserFeedback"] is IModelNavigationItem defaultNavItem)
            defaultNavItem.Remove();

        var navItem = operations.Items["UserFeedback"]
            ?? operations.Items.AddNode<IModelNavigationItem>("UserFeedback");
        navItem.View = rootNode.Application.Views["UserFeedback_ListView"];
        navItem.Caption = "User feedback";
        navItem.ImageName = "BO_Note";
    }
}
