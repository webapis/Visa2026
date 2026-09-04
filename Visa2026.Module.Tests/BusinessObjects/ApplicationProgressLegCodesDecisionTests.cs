using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileInstanceProgressLegCodesDecisionTests
{
    [Fact]
    public void GetReviewStateCodesForLeg_IncludesStartedForFirstLegOnly()
    {
        var leg1 = ApplicationProfileInstanceProgressLegCodes.GetReviewStateCodesForLeg(1);
        Assert.Equal(3, leg1.Count);
        Assert.Contains(ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1), leg1);
        Assert.Contains(ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1), leg1);
        Assert.Contains(ApplicationProfileInstanceProgressLegCodes.ReviewRejected(1), leg1);

        var leg3 = ApplicationProfileInstanceProgressLegCodes.GetReviewStateCodesForLeg(3);
        Assert.Equal(2, leg3.Count);
        Assert.Contains(ApplicationProfileInstanceProgressLegCodes.ReviewApproved(3), leg3);
        Assert.Contains(ApplicationProfileInstanceProgressLegCodes.ReviewRejected(3), leg3);
        Assert.DoesNotContain(ApplicationProfileInstanceProgressLegCodes.ReviewStarted(3), leg3);
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
        Assert.Equal(expected, ApplicationProfileInstanceProgressLegCodes.IsMinistryDecisionStateCode(stateCode));
}
