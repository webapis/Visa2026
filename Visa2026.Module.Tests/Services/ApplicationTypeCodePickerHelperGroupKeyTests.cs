using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationTypeCodePickerHelperGroupKeyTests
{
    [Theory]
    [InlineData("101", "1")]
    [InlineData("299", "2")]
    [InlineData("800", "8")]
    public void GetSelectionCodeGroupKey_ValidMinistryCode_ReturnsHundredsDigit(string code, string expected)
    {
        Assert.Equal(expected, ApplicationTypeCodePickerHelper.GetSelectionCodeGroupKey(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("12")]
    [InlineData("1001")]
    [InlineData("abc")]
    [InlineData("000")]
    [InlineData("900")]
    public void GetSelectionCodeGroupKey_Invalid_ReturnsNull(string? code)
    {
        Assert.Null(ApplicationTypeCodePickerHelper.GetSelectionCodeGroupKey(code));
    }
}
