using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class BorderZoneSelectionHelperTests
{
    [Fact]
    public void ApplyDefaultIfEmpty_SetsNoneWhenBlank()
    {
        var visa = new Visa { BorderZoneLocation = null };
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(visa);
        Assert.Equal(BorderZoneSelectionHelper.NoneValue, visa.BorderZoneLocation);

        visa.BorderZoneLocation = "   ";
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(visa);
        Assert.Equal(BorderZoneSelectionHelper.NoneValue, visa.BorderZoneLocation);
    }

    [Fact]
    public void ApplyDefaultIfEmpty_PreservesExistingSelection()
    {
        var visa = new Visa { BorderZoneLocation = "Daşoguz, Farap" };
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(visa);
        Assert.Equal("Daşoguz, Farap", visa.BorderZoneLocation);
    }

    [Fact]
    public void ApplyDefaultIfEmpty_NullVisa_IsNoOp()
    {
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(null);
    }

    [Fact]
    public void IsNoneValue_MatchesSentinelIgnoringCase()
    {
        Assert.True(BorderZoneSelectionHelper.IsNoneValue(BorderZoneSelectionHelper.NoneValue));
        Assert.True(BorderZoneSelectionHelper.IsNoneValue("ýok"));
        Assert.False(BorderZoneSelectionHelper.IsNoneValue("Daşoguz"));
    }
}
