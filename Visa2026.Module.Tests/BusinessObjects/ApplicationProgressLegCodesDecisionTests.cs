using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressLegCodesDecisionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void GetReviewStateCodesForLeg_ExcludesReviewStarted(int leg)
    {
        var codes = ApplicationProgressLegCodes.GetReviewStateCodesForLeg(leg);

        Assert.Equal(2, codes.Count);
        Assert.Contains(ApplicationProgressLegCodes.ReviewApproved(leg), codes);
        Assert.Contains(ApplicationProgressLegCodes.ReviewRejected(leg), codes);
        Assert.DoesNotContain(ApplicationProgressLegCodes.ReviewStarted(leg), codes);
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
