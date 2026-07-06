using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class MinistryReviewSlaHelperTests
{
    [Fact]
    public void TryValidateSlaValues_BlocksWhenMaxDaysMissing()
    {
        Assert.False(MinistryReviewSlaHelper.TryValidateSlaValues(0, 8, out var error));
        Assert.Equal(Visa2026.Module.Localization.VisaUiMessages.Get("MinistryReviewSlaSettings.NotConfigured"), error);
    }

    [Fact]
    public void TryValidateSlaValues_BlocksWhenWarningNotLessThanMax()
    {
        Assert.False(MinistryReviewSlaHelper.TryValidateSlaValues(10, 10, out var error));
        Assert.Equal(Visa2026.Module.Localization.VisaUiMessages.Get("MinistryReviewSlaSettings.WarningDaysInvalid"), error);
    }

    [Fact]
    public void TryValidateSlaValues_AllowsValidValues()
    {
        Assert.True(MinistryReviewSlaHelper.TryValidateSlaValues(4, 1, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryValidateSlaValues_AllowsNullWarning()
    {
        Assert.True(MinistryReviewSlaHelper.TryValidateSlaValues(4, null, out var error));
        Assert.Null(error);
    }
}
