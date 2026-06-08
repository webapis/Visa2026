namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selector ids for toolbar actions on typed Person detail views.
/// </summary>
internal static class PersonDetailViewE2eActionHooks
{
    internal static IReadOnlyDictionary<string, string> SaveActionTestIdsByDetailViewId { get; } =
        CreateTestIdsByDetailViewId("save");

    internal static IReadOnlyDictionary<string, string> SaveAndCloseActionTestIdsByDetailViewId { get; } =
        CreateTestIdsByDetailViewId("save-and-close");

    internal static IReadOnlyDictionary<string, string> SaveAndNewActionTestIdsByDetailViewId { get; } =
        CreateTestIdsByDetailViewId("save-and-new");

    internal static IReadOnlyList<(string ActionId, IReadOnlyDictionary<string, string> TestIdsByDetailViewId)>
        ToolbarActions { get; } =
        [
            ("Save", SaveActionTestIdsByDetailViewId),
            ("SaveAndClose", SaveAndCloseActionTestIdsByDetailViewId),
            ("SaveAndNew", SaveAndNewActionTestIdsByDetailViewId),
        ];

    internal static bool TryGetSaveActionTestId(string? detailViewId, out string testId) =>
        SaveActionTestIdsByDetailViewId.TryGetValue(detailViewId ?? string.Empty, out testId!);

    internal static bool TryGetActionTestId(
        string actionId,
        string? detailViewId,
        out string testId)
    {
        foreach ((string mappedActionId, IReadOnlyDictionary<string, string> testIdsByDetailViewId) in ToolbarActions)
        {
            if (!string.Equals(mappedActionId, actionId, StringComparison.Ordinal))
            {
                continue;
            }

            return testIdsByDetailViewId.TryGetValue(detailViewId ?? string.Empty, out testId!);
        }

        testId = string.Empty;
        return false;
    }

    private static Dictionary<string, string> CreateTestIdsByDetailViewId(string actionSlug) =>
        new(StringComparer.Ordinal)
        {
            ["Person_DetailView_Employee"] = $"person-detail-employee-{actionSlug}",
            ["Person_DetailView_FamilyMember"] = $"person-detail-family-member-{actionSlug}",
            ["Person_DetailView_TemporaryVisitor"] = $"person-detail-temporary-visitor-{actionSlug}",
        };
}
