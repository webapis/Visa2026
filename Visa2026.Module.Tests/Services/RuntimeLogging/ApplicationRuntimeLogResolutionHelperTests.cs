using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogResolutionHelperTests
{
    [Fact]
    public void ApplyStatus_fixed_sets_resolved_fields()
    {
        var utcNow = new DateTime(2026, 6, 8, 14, 0, 0, DateTimeKind.Utc);
        var row = new ApplicationRuntimeLog
        {
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open
        };

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            ApplicationRuntimeLogResolutionStatus.Fixed,
            utcNow,
            "cursor-agent",
            "Fixed schema SQL.",
            "abc1234",
            "agent-run-1");

        Assert.Equal(ApplicationRuntimeLogResolutionStatus.Fixed, row.ResolutionStatus);
        Assert.Equal(utcNow, row.AcknowledgedAtUtc);
        Assert.Equal(utcNow, row.ResolvedAtUtc);
        Assert.Equal("cursor-agent", row.ResolvedBy);
        Assert.Equal("Fixed schema SQL.", row.ResolutionNotes);
        Assert.Equal("abc1234", row.FixCommitHash);
        Assert.Equal("agent-run-1", row.AgentRunId);
    }

    [Fact]
    public void ToSummary_maps_resolution_fields()
    {
        var row = new ApplicationRuntimeLog
        {
            ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            OccurredAtUtc = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc),
            Severity = ApplicationRuntimeLogSeverity.Error,
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open,
            ErrorCode = "PDF-BATCH-001",
            Message = "Batch failed"
        };

        var summary = ApplicationRuntimeLogResolutionHelper.ToSummary(row);

        Assert.Equal(row.ID, summary.Id);
        Assert.Equal("PDF-BATCH-001", summary.ErrorCode);
        Assert.Equal(ApplicationRuntimeLogResolutionStatus.Open, summary.ResolutionStatus);
    }
}
