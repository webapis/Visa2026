namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selector ids for sidebar navigation (XAF navigation item Ids, not captions).
/// Add new groups here — one shared <see cref="NavigationE2eSelectorsController"/>.
/// </summary>
internal static class NavigationE2eHooks
{
    internal static IReadOnlyDictionary<string, string> TestIdsByNavigationItemId { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // People
            ["People"] = "nav-people",
            ["Employees"] = "nav-people-employees",
            ["FamilyMembers"] = "nav-people-family-members",
            ["TemporaryVisitors"] = "nav-people-temporary-visitors",
        };

    /// <summary>Leaf nav items: href from menu NavigateUrl when reference map misses.</summary>
    internal static IReadOnlyDictionary<string, string> TestIdsByNavigateUrl { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Person_ListView_Employees"] = "nav-people-employees",
            ["Person_ListView_FamilyMembers"] = "nav-people-family-members",
            ["Person_ListView_TemporaryVisitors"] = "nav-people-temporary-visitors",
        };
}
