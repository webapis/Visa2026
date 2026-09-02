using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationRuntimeLogResolutionHelperTests
{
    [Fact]
    public void ApplyStatus_Acknowledged_SetsAcknowledgedAtOnce()
    {
        var row = new ApplicationRuntimeLog
        {
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open,
        };
        var first = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        var second = first.AddHours(2);

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.Acknowledged,
            first,
            resolvedBy: "dev",
            resolutionNotes: null,
            fixCommitHash: null,
            agentRunId: null);

        Assert.Equal(ApplicationRuntimeLogResolutionStatus.Acknowledged, row.ResolutionStatus);
        Assert.Equal(first, row.AcknowledgedAtUtc);
        Assert.Equal("dev", row.ResolvedBy);
        Assert.Null(row.ResolvedAtUtc);

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.InProgress,
            second,
            resolvedBy: null,
            resolutionNotes: "working",
            fixCommitHash: null,
            agentRunId: "run-1");

        Assert.Equal(first, row.AcknowledgedAtUtc);
        Assert.Equal("working", row.ResolutionNotes);
        Assert.Equal("run-1", row.AgentRunId);
        Assert.Null(row.ResolvedAtUtc);
    }

    [Fact]
    public void ApplyStatus_Fixed_SetsResolvedAtAndTruncatesLongFields()
    {
        var row = new ApplicationRuntimeLog();
        var now = new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Utc);
        var longNotes = new string('n', 5000);
        var longHash = new string('a', 100);

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.Fixed,
            now,
            resolvedBy: new string('u', 200),
            resolutionNotes: longNotes,
            fixCommitHash: longHash,
            agentRunId: new string('r', 200));

        Assert.Equal(ApplicationRuntimeLogResolutionStatus.Fixed, row.ResolutionStatus);
        Assert.Equal(now, row.AcknowledgedAtUtc);
        Assert.Equal(now, row.ResolvedAtUtc);
        Assert.Equal(128, row.ResolvedBy.Length);
        Assert.Equal(4000, row.ResolutionNotes.Length);
        Assert.Equal(64, row.FixCommitHash.Length);
        Assert.Equal(128, row.AgentRunId.Length);
    }

    [Fact]
    public void ApplyStatus_Ignored_SetsResolvedAtWithoutRequiringNotes()
    {
        var row = new ApplicationRuntimeLog();
        var now = new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc);

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.Ignored,
            now,
            resolvedBy: "ops",
            resolutionNotes: null,
            fixCommitHash: null,
            agentRunId: null);

        Assert.Equal(ApplicationRuntimeLogResolutionStatus.Ignored, row.ResolutionStatus);
        Assert.Equal(now, row.ResolvedAtUtc);
        Assert.Equal(now, row.AcknowledgedAtUtc);
    }

    [Fact]
    public void ToSummary_CopiesIdentityAndResolutionFields()
    {
        var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var row = new ApplicationRuntimeLog
        {
            ID = id,
            OccurredAtUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            Severity = ApplicationRuntimeLogSeverity.Error,
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open,
            ErrorCode = "E1",
            Category = "Cat",
            Message = "boom",
            ExceptionType = "InvalidOperationException",
            UserName = "admin",
            CorrelationId = "c-1",
            RequestPath = "/x",
            MachineName = "host",
            DeploymentEnvironment = ApplicationRuntimeLogDeploymentEnvironment.LocalVisualStudio,
            ApplicationVersion = "1.0",
        };

        var summary = ApplicationRuntimeLogResolutionHelper.ToSummary(row);

        Assert.Equal(id, summary.Id);
        Assert.Equal(row.OccurredAtUtc, summary.OccurredAtUtc);
        Assert.Equal(row.Severity, summary.Severity);
        Assert.Equal(row.Message, summary.Message);
        Assert.Equal(row.ErrorCode, summary.ErrorCode);
        Assert.Equal(row.DeploymentEnvironment, summary.DeploymentEnvironment);
        Assert.Equal(row.ApplicationVersion, summary.ApplicationVersion);
    }
}
