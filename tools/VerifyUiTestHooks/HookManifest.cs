using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visa2026.Tools.VerifyUiTestHooks;

public sealed class HookManifest
{
    public int Version { get; init; }
    public List<HookScenario> Scenarios { get; init; } = [];

    public static HookManifest Load(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<HookManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Could not parse manifest: {path}");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

public sealed class HookScenario
{
    public string Id { get; init; } = "";
    public string Description { get; init; } = "";
    public string? StartPath { get; init; }
    public string? StartPathEnv { get; init; }
    public bool RequiresAuth { get; init; }
    public List<HookCheck> Checks { get; init; } = [];
}

public sealed class HookCheck
{
    public string Id { get; init; } = "";
    public string Target { get; init; } = "";
    public string Type { get; init; } = "";
    public List<string> Selectors { get; init; } = [];
    public bool ClickOnBehavior { get; init; } = true;
}

public sealed record VerifyOptions(
    string BaseUrl,
    string UserName,
    string Password,
    bool Headless,
    int TimeoutMs,
    string? StartUrl,
    IReadOnlyList<string> ScenarioIds);

public sealed record CheckResult(
    string ScenarioId,
    string CheckId,
    string Target,
    string? WinningSelector,
    bool AccessOk,
    bool BehaviorOk,
    string? Error);

public sealed record ScenarioResult(
    string ScenarioId,
    bool Skipped,
    string? SkipReason,
    IReadOnlyList<CheckResult> Checks);
