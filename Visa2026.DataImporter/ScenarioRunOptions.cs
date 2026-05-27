namespace Visa2026.DataImporter;

/// <summary>
/// Targeted scenario runs: clear-before-import and/or sync (PATCH).
/// </summary>
public sealed class ScenarioRunOptions
{
    /// <summary>Delete matching rows from yaml, then POST fresh (<c>--clear-scenario</c>).</summary>
    public HashSet<string> ClearScenarioNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>PATCH existing rows (<c>--sync-scenario</c> / <c>--sync</c>).</summary>
    public HashSet<string> SyncScenarioNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>When true, sync every scenario marked <c>sync: true</c> in data.yaml.</summary>
    public bool SyncAllFlaggedInYaml { get; set; }

    public bool HasTargetedRun =>
        ClearScenarioNames.Count > 0 || SyncScenarioNames.Count > 0 || SyncAllFlaggedInYaml;

    public bool ShouldRunScenario(string scenarioName, bool syncFlagInYaml)
    {
        if (!HasTargetedRun)
            return true;

        return ShouldClearScenario(scenarioName)
            || ShouldSyncScenario(scenarioName, syncFlagInYaml);
    }

    public bool ShouldClearScenario(string scenarioName) =>
        ClearScenarioNames.Contains(scenarioName);

    public bool ShouldSyncScenario(string scenarioName, bool syncFlagInYaml) =>
        SyncScenarioNames.Contains(scenarioName) || (SyncAllFlaggedInYaml && syncFlagInYaml);
}
