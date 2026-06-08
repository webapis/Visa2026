namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selector ids for toolbar actions on typed Person ListViews.
/// </summary>
internal static class PersonListViewE2eActionHooks
{
    internal static IReadOnlyDictionary<string, string> NewActionTestIdsByListViewId { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Person_ListView_Employees"] = "person-list-employees-new",
            ["Person_ListView_FamilyMembers"] = "person-list-family-members-new",
            ["Person_ListView_TemporaryVisitors"] = "person-list-temporary-visitors-new",
        };

    internal static IReadOnlyDictionary<string, string> DeleteActionTestIdsByListViewId { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Person_ListView_Employees"] = "person-list-employees-delete",
            ["Person_ListView_FamilyMembers"] = "person-list-family-members-delete",
            ["Person_ListView_TemporaryVisitors"] = "person-list-temporary-visitors-delete",
        };

    internal static bool TryGetNewActionTestId(string? listViewId, out string testId) =>
        NewActionTestIdsByListViewId.TryGetValue(listViewId ?? string.Empty, out testId!);

    internal static bool TryGetDeleteActionTestId(string? listViewId, out string testId) =>
        DeleteActionTestIdsByListViewId.TryGetValue(listViewId ?? string.Empty, out testId!);
}
