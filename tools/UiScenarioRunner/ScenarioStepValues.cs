namespace Visa2026.Tools.UiScenarioRunner;

/// <summary>
/// Parses YAML step payloads. <c>timeout</c> is a maximum condition-wait (ms), never a fixed sleep.
/// </summary>
internal readonly record struct HookStep(string HookId, int? TimeoutMs);

internal readonly record struct TextStep(string Text, int? TimeoutMs);

internal static class ScenarioStepValues
{
    /// <summary>
    /// Parses hook-based steps (wait-for, assert-visible, click, select-tab):
    /// <c>hook-id</c> or <c>{ hook: hook-id, timeout: 15000 }</c> (timeout in ms).
    /// </summary>
    internal static HookStep ParseHookStep(object value, string stepKind)
    {
        if (value is string hookId)
        {
            if (string.IsNullOrWhiteSpace(hookId))
            {
                throw new InvalidOperationException($"{stepKind} step hook id must not be empty.");
            }

            return new HookStep(hookId, null);
        }

        Dictionary<string, string> fields = ToStringDictionary(value);
        string id = fields.GetValueOrDefault("hook", "");
        if (string.IsNullOrWhiteSpace(id))
        {
            id = fields.GetValueOrDefault("id", "");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException(
                $"{stepKind} step object requires 'hook' (or 'id'). Example: {{ hook: person-first-name, timeout: 15000 }}");
        }

        int? timeoutMs = TryParsePositiveMs(fields, "timeout")
            ?? TryParsePositiveMs(fields, "timeoutMs");

        return new HookStep(id, timeoutMs);
    }

    /// <summary>
    /// Parses click-text steps:
    /// <c>visible text</c> or <c>{ text: "...", timeout: 15000 }</c> (timeout in ms).
    /// </summary>
    internal static TextStep ParseTextStep(object value)
    {
        if (value is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("click-text step text must not be empty.");
            }

            return new TextStep(text, null);
        }

        Dictionary<string, string> fields = ToStringDictionary(value);
        string resolved = fields.GetValueOrDefault("text", "");
        if (string.IsNullOrWhiteSpace(resolved))
        {
            resolved = fields.GetValueOrDefault("value", "");
        }

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException(
                "click-text step object requires 'text' (or 'value'). Example: { text: E2E-PPT-EMP-001, timeout: 20000 }");
        }

        int? timeoutMs = TryParsePositiveMs(fields, "timeout")
            ?? TryParsePositiveMs(fields, "timeoutMs");

        return new TextStep(resolved, timeoutMs);
    }

    internal static string FormatHookStepDetail(HookStep step) =>
        step.TimeoutMs.HasValue ? $"{step.HookId} (timeout={step.TimeoutMs}ms)" : step.HookId;

    internal static string FormatTextStepDetail(TextStep step) =>
        step.TimeoutMs.HasValue ? $"{step.Text} (timeout={step.TimeoutMs}ms)" : step.Text;

    private static int? TryParsePositiveMs(Dictionary<string, string> fields, string key)
    {
        if (!fields.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!int.TryParse(raw, out int ms) || ms <= 0)
        {
            throw new InvalidOperationException($"Step '{key}' must be a positive integer (milliseconds). Got: {raw}");
        }

        return ms;
    }

    private static Dictionary<string, string> ToStringDictionary(object value)
    {
        if (value is Dictionary<object, object> objMap)
        {
            return objMap.ToDictionary(
                static p => p.Key.ToString() ?? "",
                static p => p.Value?.ToString() ?? "");
        }

        if (value is Dictionary<string, object> strObjMap)
        {
            return strObjMap.ToDictionary(static p => p.Key, static p => p.Value?.ToString() ?? "");
        }

        if (value is Dictionary<string, string> strMap)
        {
            return strMap;
        }

        if (value is IReadOnlyDictionary<object, object> readOnlyObjMap)
        {
            return readOnlyObjMap.ToDictionary(
                static p => p.Key.ToString() ?? "",
                static p => p.Value?.ToString() ?? "");
        }

        if (value is IReadOnlyDictionary<string, object> readOnlyStrMap)
        {
            return readOnlyStrMap.ToDictionary(static p => p.Key, static p => p.Value?.ToString() ?? "");
        }

        throw new InvalidOperationException(
            $"Expected a string or mapping for step value. Got: {value.GetType().Name}");
    }
}
