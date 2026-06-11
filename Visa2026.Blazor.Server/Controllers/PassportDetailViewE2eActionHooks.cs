namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selector ids for toolbar actions on <c>Passport_DetailView</c>.
/// </summary>
internal static class PassportDetailViewE2eActionHooks
{
    public const string DetailViewId = "Passport_DetailView";

    internal static IReadOnlyDictionary<string, string> SaveActionTestIdsByDetailViewId { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DetailViewId] = "passport-detail-save",
        };

    internal static IReadOnlyList<(string ActionId, IReadOnlyDictionary<string, string> TestIdsByDetailViewId)>
        ToolbarActions { get; } =
        [
            ("Save", SaveActionTestIdsByDetailViewId),
        ];

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
}
