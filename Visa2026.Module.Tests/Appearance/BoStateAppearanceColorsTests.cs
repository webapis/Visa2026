using Visa2026.Module.Appearance;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Appearance;

public class BoStateAppearanceColorsTests
{
    [Fact]
    public void TryGet_KnownProgressCodes_ReturnsAppearance()
    {
        Assert.True(BoStateAppearanceColors.TryGet(
            ApplicationProgressStateCodes.ProcessIssued, out var issued));
        Assert.Equal(ApplicationProgressStateCodes.ProcessIssued, issued.StateCode);
        Assert.Equal(
            $"visa-progress-row--state-{ApplicationProgressStateCodes.ProcessIssued}",
            issued.RowCssClass);

        Assert.True(BoStateAppearanceColors.TryGet(
            ApplicationProgressSlaCodes.Overdue, out var overdue));
        Assert.Equal(ApplicationProgressSlaCodes.Overdue, overdue.StateCode);
        Assert.Equal($"visa-progress-row--state-{ApplicationProgressSlaCodes.Overdue}", overdue.RowCssClass);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NOT_A_REAL_STATE")]
    public void TryGet_MissingOrBlank_ReturnsFalse(string stateCode)
    {
        Assert.False(BoStateAppearanceColors.TryGet(stateCode, out _));
    }

    [Fact]
    public void TryGet_IsCaseInsensitive()
    {
        Assert.True(BoStateAppearanceColors.TryGet(
            ApplicationProgressStateCodes.ProcessCancelled.ToLowerInvariant(),
            out var appearance));
        Assert.Equal(ApplicationProgressStateCodes.ProcessCancelled, appearance.StateCode);
    }

    [Fact]
    public void ToRowCssClass_PrefixesStateToken()
    {
        Assert.Equal(
            "visa-progress-row--state-PROCESS_STARTED",
            BoStateAppearanceColors.ToRowCssClass("PROCESS_STARTED"));
    }
}
