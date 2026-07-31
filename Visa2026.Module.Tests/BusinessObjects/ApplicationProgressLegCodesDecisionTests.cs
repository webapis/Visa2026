using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressLegCodesDecisionTests
{
    [Fact]
    public void GetReviewStateCodesForLeg_IncludesStartedForFirstLegOnly()
    {
        var leg1 = ApplicationProgressLegCodes.GetReviewStateCodesForLeg(1);
        Assert.Equal(3, leg1.Count);
        Assert.Contains(ApplicationProgressLegCodes.ReviewStarted(1), leg1);
        Assert.Contains(ApplicationProgressLegCodes.ReviewApproved(1), leg1);
        Assert.Contains(ApplicationProgressLegCodes.ReviewRejected(1), leg1);

        var leg3 = ApplicationProgressLegCodes.GetReviewStateCodesForLeg(3);
        Assert.Equal(2, leg3.Count);
        Assert.Contains(ApplicationProgressLegCodes.ReviewApproved(3), leg3);
        Assert.Contains(ApplicationProgressLegCodes.ReviewRejected(3), leg3);
        Assert.DoesNotContain(ApplicationProgressLegCodes.ReviewStarted(3), leg3);
    }

    [Theory]
    [InlineData("1_REVIEW_APPROVED", true)]
    [InlineData("2_REVIEW_REJECTED", true)]
    [InlineData("3_REVIEW_APPROVED", true)]
    [InlineData("5_REVIEW_REJECTED", true)]
    [InlineData("1_REVIEW_STARTED", false)]
    [InlineData("IS_BEING_PREPARED", false)]
    [InlineData("PROCESS_ISSUED", false)]
    [InlineData(null, false)]
    public void IsMinistryDecisionStateCode_RecognizesApprovedAndRejectedOnly(string? stateCode, bool expected) =>
        Assert.Equal(expected, ApplicationProgressLegCodes.IsMinistryDecisionStateCode(stateCode));
}
