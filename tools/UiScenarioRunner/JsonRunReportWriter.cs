using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visa2026.Tools.UiScenarioRunner;

internal static class JsonRunReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Write(string path, IReadOnlyList<ScenarioRunResult> results, RunReportMetadata metadata)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var payload = new
        {
            metadata.ApplicationVersion,
            metadata.GitSha,
            metadata.GitRef,
            metadata.BaseUrl,
            metadata.WorkflowRunId,
            workflowRunUrl = metadata.WorkflowRunUrl,
            startedAt = metadata.StartedAt,
            finishedAt = metadata.FinishedAt,
            passed = results.Count(r => r.Ok),
            failed = results.Count(r => !r.Ok),
            total = results.Count,
            scenarios = results.Select(r => new
            {
                id = r.ScenarioId,
                ok = r.Ok,
                error = r.Error,
                steps = r.Steps.Select(s => new
                {
                    index = s.Index,
                    kind = s.StepKind,
                    ok = s.Ok,
                    detail = s.Detail,
                    error = s.Error,
                }),
            }),
        };

        File.WriteAllText(path, JsonSerializer.Serialize(payload, Options));
    }
}
