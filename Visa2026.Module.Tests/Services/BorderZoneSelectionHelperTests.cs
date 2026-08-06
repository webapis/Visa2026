using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class BorderZoneSelectionHelperTests
{
    [Fact]
    public void ApplyDefaultIfEmpty_NullVisa_DoesNothing()
    {
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyDefaultIfEmpty_Blank_SetsNoneValue(string stored)
    {
        var visa = new Visa { BorderZoneLocation = stored };

        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(visa);

        Assert.Equal(BorderZoneSelectionHelper.NoneValue, visa.BorderZoneLocation);
    }

    [Fact]
    public void ApplyDefaultIfEmpty_ExistingValue_Preserves()
    {
        var visa = new Visa { BorderZoneLocation = "Mary|Lebap" };

        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(visa);

        Assert.Equal("Mary|Lebap", visa.BorderZoneLocation);
    }
}
