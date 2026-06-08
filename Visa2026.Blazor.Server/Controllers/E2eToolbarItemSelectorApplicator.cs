using DevExpress.ExpressApp.Blazor.Components.Models;

namespace Visa2026.Blazor.Server.Controllers;

internal static class E2eToolbarItemSelectorApplicator
{
    internal static void Apply(DxToolbarItemModel toolbarItem, string testId) =>
        E2eActionControlSelectorSupport.ApplyCssClassAndTestId(toolbarItem, testId);
}
