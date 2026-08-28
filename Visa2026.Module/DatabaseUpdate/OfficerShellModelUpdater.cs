using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Strips the retired officer-shell navigation item. Staged / in-process
/// live as native items under the Application Profiles folder.
/// </summary>
public sealed class OfficerShellModelUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
{
    public const string NavItemId = "OfficerShell";

    public override void UpdateNode(ModelNode node)
    {
        var rootNode = (IModelRootNavigationItems)node;
        var applicationNav = rootNode.Items["Application"];
        if (applicationNav == null)
            return;

        if (applicationNav.Items[NavItemId] is IModelNavigationItem leftover)
            leftover.Remove();

        if (applicationNav.Items["OfficerShellHost"] is IModelNavigationItem leftoverHost)
            leftoverHost.Remove();
    }
}
