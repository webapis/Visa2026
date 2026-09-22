using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Terminal outcome codes drive ListView flags, dashboard filters, and workflow stop conditions.
/// </summary>
public sealed class ApplicationProgressStateCodesTerminalTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(ApplicationProgressStateCodes.IsBeingPrepared, false)]
    [InlineData(ApplicationProgressStateCodes.ProcessStarted, false)]
    [InlineData(ApplicationProgressStateCodes.Review1Started, false)]
    [InlineData(ApplicationProgressStateCodes.Review1Approved, false)]
    [InlineData(ApplicationProgressStateCodes.ProcessIssued, true)]
    [InlineData(ApplicationProgressStateCodes.ProcessRejected, true)]
    [InlineData(ApplicationProgressStateCodes.ProcessCancelled, true)]
    [InlineData(ApplicationProgressStateCodes.Review1Rejected, true)]
    [InlineData(ApplicationProgressStateCodes.Review2Rejected, true)]
    [InlineData("process_issued", true)]
    [InlineData(" CUSTOM_REVIEW_REJECTED ", true)]
    [InlineData("NOT_A_TERMINAL", false)]
    public void IsTerminalOutcome_KnownCodes(string? code, bool expected)
    {
        Assert.Equal(expected, ApplicationProgressStateCodes.IsTerminalOutcome(code));
    }
}
