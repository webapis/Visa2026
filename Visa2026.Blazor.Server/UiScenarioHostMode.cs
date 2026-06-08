namespace Visa2026.Blazor.Server;

/// <summary>
/// When enabled (UI scenario host on :5052), disables TabbedMDI layout restore so each logon
/// starts without tabs left from prior Playwright runs for the same user.
/// </summary>
internal static class UiScenarioHostMode
{
    internal static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("VISA2026_UI_SCENARIOS"), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("VISA2026_UI_SCENARIOS"), "1", StringComparison.Ordinal);
}
