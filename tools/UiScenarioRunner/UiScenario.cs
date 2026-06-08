using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Visa2026.Tools.UiScenarioRunner;

internal sealed class UiScenario
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public bool RequiresAuth { get; set; } = true;
    public string? BaseUrl { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public List<Dictionary<string, object>> Steps { get; set; } = [];

    public static UiScenario LoadYaml(string path)
    {
        string yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        UiScenario scenario = deserializer.Deserialize<UiScenario>(yaml)
            ?? throw new InvalidOperationException($"Could not parse scenario: {path}");

        if (string.IsNullOrWhiteSpace(scenario.Id))
        {
            throw new InvalidOperationException($"Scenario id missing in {path}");
        }

        return scenario;
    }
}

internal sealed record RunOptions(
    string BaseUrl,
    string DefaultUser,
    string DefaultPassword,
    bool Headless,
    int TimeoutMs,
    int SlowMoMs,
    string? ScreenshotDir,
    bool ScreenshotEachStep,
    int PauseAfterSaveMs,
    string ManifestPath);

internal sealed record StepResult(int Index, string StepKind, bool Ok, string? Detail, string? Error);

internal sealed record ScenarioRunResult(
    string ScenarioId,
    bool Ok,
    IReadOnlyList<StepResult> Steps,
    string? Error);
