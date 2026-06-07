using System.Text.Json;

namespace Visa2026.Tools.UiScenarioRunner;

internal sealed class HookResolver
{
    private readonly Dictionary<string, IReadOnlyList<string>> _selectorsByHookId;

    private HookResolver(Dictionary<string, IReadOnlyList<string>> selectorsByHookId)
    {
        _selectorsByHookId = selectorsByHookId;
    }

    public static HookResolver Load(string manifestPath)
    {
        string json = File.ReadAllText(manifestPath);
        using var doc = JsonDocument.Parse(json);
        var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        if (!doc.RootElement.TryGetProperty("scenarios", out JsonElement scenarios))
        {
            throw new InvalidOperationException("hooks-manifest.json has no scenarios array.");
        }

        foreach (JsonElement scenario in scenarios.EnumerateArray())
        {
            if (!scenario.TryGetProperty("checks", out JsonElement checks))
            {
                continue;
            }

            foreach (JsonElement check in checks.EnumerateArray())
            {
                string id = check.GetProperty("id").GetString()
                    ?? throw new InvalidOperationException("Hook check missing id.");
                var selectors = new List<string>();
                foreach (JsonElement selector in check.GetProperty("selectors").EnumerateArray())
                {
                    selectors.Add(selector.GetString() ?? throw new InvalidOperationException("Empty selector."));
                }

                map[id] = selectors;
            }
        }

        return new HookResolver(map);
    }

    public IReadOnlyList<string> GetSelectors(string hookId)
    {
        if (!_selectorsByHookId.TryGetValue(hookId, out IReadOnlyList<string>? selectors))
        {
            throw new InvalidOperationException(
                $"Unknown hook id '{hookId}'. Add it to tools/VerifyUiTestHooks/hooks-manifest.json and docs/UI_TEST_HOOKS.md.");
        }

        return selectors;
    }
}
