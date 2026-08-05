using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationTypeCodePickerHelperTests
{
    [Theory]
    [InlineData("101", "1")]
    [InlineData("201", "2")]
    [InlineData("301", "3")]
    [InlineData("401", "4")]
    [InlineData("501", "5")]
    [InlineData("601", "6")]
    [InlineData("701", "7")]
    [InlineData("809", "8")]
    public void GetSelectionCodeGroupKey_MapsHundredsDigit(string code, string expectedGroup)
    {
        Assert.Equal(expectedGroup, ApplicationTypeCodePickerHelper.GetSelectionCodeGroupKey(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("10")]
    [InlineData("1010")]
    [InlineData("abc")]
    [InlineData("001")]
    [InlineData("901")]
    public void GetSelectionCodeGroupKey_InvalidOrOutOfRange_ReturnsNull(string code)
    {
        Assert.Null(ApplicationTypeCodePickerHelper.GetSelectionCodeGroupKey(code));
    }
}
