namespace Visa2026.Tools.UiScenarioRunner;

internal static class RepoPaths
{
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Visa2026.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (Visa2026.slnx).");
    }

    public static string DefaultManifestPath() =>
        Path.Combine(FindRepoRoot(), "tools", "VerifyUiTestHooks", "hooks-manifest.json");

    public static string DefaultScenariosDir() =>
        Path.Combine(FindRepoRoot(), "tools", "UiScenarioRunner", "scenarios");

    public static string ScenarioYamlPath(string scenarioId) =>
        Path.Combine(DefaultScenariosDir(), $"{scenarioId}.yaml");
}
