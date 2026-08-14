using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Covers <see cref="ApplicationProgressStateCodes.IsTerminalOutcome"/> used by Report Dashboard
/// unfinished-person filtering — broader than process-only workflow terminal flags.
/// </summary>
public class ApplicationProgressStateCodesTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(ApplicationProgressStateCodes.IsBeingPrepared, false)]
    [InlineData(ApplicationProgressStateCodes.Review1Started, false)]
    [InlineData(ApplicationProgressStateCodes.Review1Approved, false)]
    [InlineData(ApplicationProgressStateCodes.Review2Started, false)]
    [InlineData(ApplicationProgressStateCodes.Review2Approved, false)]
    [InlineData(ApplicationProgressStateCodes.ProcessStarted, false)]
    [InlineData(ApplicationProgressStateCodes.ProcessIssued, true)]
    [InlineData(ApplicationProgressStateCodes.ProcessRejected, true)]
    [InlineData(ApplicationProgressStateCodes.ProcessCancelled, true)]
    [InlineData(ApplicationProgressStateCodes.Review1Rejected, true)]
    [InlineData(ApplicationProgressStateCodes.Review2Rejected, true)]
    [InlineData("  process_issued  ", true)]
    [InlineData("3_REVIEW_REJECTED", true)]
    [InlineData("review_rejected", false)]
    public void IsTerminalOutcome_MatchesFinalOutcomesIncludingReviewRejected(
        string? code,
        bool expected)
    {
        Assert.Equal(expected, ApplicationProgressStateCodes.IsTerminalOutcome(code));
    }

    [Fact]
    public void IsTerminalOutcome_IncludesReviewRejected_UnlikeProcessOnlyTerminalHelper()
    {
        // Dashboard unfinished filter treats ministry rejection as terminal;
        // ApplicationProgressProfileResolver.IsProcessTerminalStateCode does not.
        Assert.True(ApplicationProgressStateCodes.IsTerminalOutcome(
            ApplicationProgressStateCodes.Review1Rejected));
        Assert.False(ApplicationProgressProfileResolver.IsProcessTerminalStateCode(
            ApplicationProgressStateCodes.Review1Rejected));
    }
}
