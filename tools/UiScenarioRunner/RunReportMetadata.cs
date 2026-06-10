namespace Visa2026.Tools.UiScenarioRunner;

internal sealed record RunReportMetadata(
    string? ApplicationVersion,
    string? GitSha,
    string? GitRef,
    string? BaseUrl,
    string? WorkflowRunId,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt)
{
    public static RunReportMetadata Create(string baseUrl, DateTimeOffset startedAt)
    {
        string? version = Environment.GetEnvironmentVariable("APP_VERSION");
        string? sha = Environment.GetEnvironmentVariable("GITHUB_SHA");
        string? gitRef = Environment.GetEnvironmentVariable("GITHUB_REF");
        string? runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");

        return new RunReportMetadata(
            ApplicationVersion: version,
            GitSha: sha,
            GitRef: gitRef,
            BaseUrl: baseUrl,
            WorkflowRunId: runId,
            StartedAt: startedAt,
            FinishedAt: DateTimeOffset.UtcNow);
    }

    public string? WorkflowRunUrl =>
        !string.IsNullOrWhiteSpace(WorkflowRunId)
        && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_REPOSITORY"))
            ? $"https://github.com/{Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")}/actions/runs/{WorkflowRunId}"
            : null;
}
