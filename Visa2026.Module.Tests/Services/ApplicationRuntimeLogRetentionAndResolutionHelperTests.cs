using System;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationRuntimeLogRetentionHelperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-30)]
    public void TryGetCutoffUtc_NonPositiveRetention_ReturnsNull(int retentionDays)
    {
        Assert.Null(ApplicationRuntimeLogRetentionHelper.TryGetCutoffUtc(
            retentionDays,
            new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void TryGetCutoffUtc_SubtractsRetentionDaysFromUtcNow()
    {
        var now = new DateTime(2026, 8, 5, 15, 30, 0, DateTimeKind.Utc);
        var cutoff = ApplicationRuntimeLogRetentionHelper.TryGetCutoffUtc(30, now);

        Assert.Equal(new DateTime(2026, 7, 6, 15, 30, 0, DateTimeKind.Utc), cutoff);
    }

    [Fact]
    public void DefaultBatchSize_Is500()
    {
        Assert.Equal(500, ApplicationRuntimeLogRetentionHelper.DefaultBatchSize);
    }
}

public sealed class ApplicationRuntimeLogResolutionHelperTests
{
    [Fact]
    public void ApplyStatus_Acknowledged_SetsAcknowledgedAtOnce()
    {
        var row = new ApplicationRuntimeLog
        {
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open,
        };
        var first = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var second = new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.Acknowledged,
            first,
            resolvedBy: "officer",
            resolutionNotes: null,
            fixCommitHash: null,
            agentRunId: null);

        Assert.Equal(ApplicationRuntimeLogResolutionStatus.Acknowledged, row.ResolutionStatus);
        Assert.Equal(first, row.AcknowledgedAtUtc);
        Assert.Null(row.ResolvedAtUtc);
        Assert.Equal("officer", row.ResolvedBy);

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.InProgress,
            second,
            resolvedBy: null,
            resolutionNotes: "working",
            fixCommitHash: null,
            agentRunId: null);

        Assert.Equal(first, row.AcknowledgedAtUtc);
        Assert.Equal("working", row.ResolutionNotes);
        Assert.Null(row.ResolvedAtUtc);
    }

    [Fact]
    public void ApplyStatus_Fixed_SetsResolvedAtAndTruncatesFields()
    {
        var row = new ApplicationRuntimeLog();
        var when = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc);
        var longNotes = new string('n', 5000);
        var longHash = new string('a', 100);

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.Fixed,
            when,
            resolvedBy: new string('u', 200),
            resolutionNotes: longNotes,
            fixCommitHash: longHash,
            agentRunId: "run-123");

        Assert.Equal(ApplicationRuntimeLogResolutionStatus.Fixed, row.ResolutionStatus);
        Assert.Equal(when, row.AcknowledgedAtUtc);
        Assert.Equal(when, row.ResolvedAtUtc);
        Assert.Equal(128, row.ResolvedBy.Length);
        Assert.Equal(4000, row.ResolutionNotes.Length);
        Assert.Equal(64, row.FixCommitHash.Length);
        Assert.Equal("run-123", row.AgentRunId);
    }

    [Fact]
    public void ApplyStatus_Ignored_SetsResolvedAtWithoutRequiringNotes()
    {
        var row = new ApplicationRuntimeLog();
        var when = new DateTime(2026, 8, 5, 11, 0, 0, DateTimeKind.Utc);

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.Ignored,
            when,
            resolvedBy: "bot",
            resolutionNotes: null,
            fixCommitHash: null,
            agentRunId: null);

        Assert.Equal(when, row.ResolvedAtUtc);
        Assert.Equal(when, row.AcknowledgedAtUtc);
    }

    [Fact]
    public void ApplyStatus_Open_DoesNotStampAcknowledgedOrResolved()
    {
        var row = new ApplicationRuntimeLog
        {
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Fixed,
            AcknowledgedAtUtc = new DateTime(2026, 7, 1, DateTimeKind.Utc),
            ResolvedAtUtc = new DateTime(2026, 7, 2, DateTimeKind.Utc),
        };

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.Open,
            new DateTime(2026, 8, 5, DateTimeKind.Utc),
            resolvedBy: null,
            resolutionNotes: null,
            fixCommitHash: null,
            agentRunId: null);

        Assert.Equal(ApplicationRuntimeLogResolutionStatus.Open, row.ResolutionStatus);
        Assert.Equal(new DateTime(2026, 7, 1, DateTimeKind.Utc), row.AcknowledgedAtUtc);
        Assert.Equal(new DateTime(2026, 7, 2, DateTimeKind.Utc), row.ResolvedAtUtc);
    }

    [Fact]
    public void ToSummary_CopiesKeyFields()
    {
        var id = Guid.NewGuid();
        var row = new ApplicationRuntimeLog
        {
            ID = id,
            OccurredAtUtc = new DateTime(2026, 8, 4, 8, 0, 0, DateTimeKind.Utc),
            Severity = ApplicationRuntimeLogSeverity.Error,
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.InProgress,
            ErrorCode = "PDF_BATCH_FAILED",
            Category = "Pdf",
            Message = "boom",
            ExceptionType = "InvalidOperationException",
            StackTrace = "at X",
            UserName = "admin",
            CorrelationId = "c1",
            RequestPath = "/api",
            MachineName = "host",
            DeploymentEnvironment = ApplicationRuntimeLogDeploymentEnvironment.LocalVisualStudio,
            ApplicationVersion = "1.2.3",
            RelatedBatchId = Guid.NewGuid(),
            SentryEventId = "sev",
            AcknowledgedAtUtc = new DateTime(2026, 8, 4, 9, 0, 0, DateTimeKind.Utc),
            ResolvedBy = "dev",
            ResolutionNotes = "looking",
            FixCommitHash = "abc",
            AgentRunId = "run",
        };

        var summary = ApplicationRuntimeLogResolutionHelper.ToSummary(row);

        Assert.Equal(id, summary.Id);
        Assert.Equal(row.OccurredAtUtc, summary.OccurredAtUtc);
        Assert.Equal(row.Severity, summary.Severity);
        Assert.Equal(row.ResolutionStatus, summary.ResolutionStatus);
        Assert.Equal("PDF_BATCH_FAILED", summary.ErrorCode);
        Assert.Equal("boom", summary.Message);
        Assert.Equal(row.RelatedBatchId, summary.RelatedBatchId);
        Assert.Equal("dev", summary.ResolvedBy);
        Assert.Equal("run", summary.AgentRunId);
    }
}
