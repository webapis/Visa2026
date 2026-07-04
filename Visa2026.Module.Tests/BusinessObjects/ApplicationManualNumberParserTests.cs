using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationManualNumberParserTests
{
    [Theory]
    [InlineData("7/-1105", "7", "1105")]
    [InlineData("6/-6/-1098", "6", "1098")]
    [InlineData("6/-5/-871", "6", "871")]
    [InlineData("6/5/871", "6", "871")]
    [InlineData("1105", null, "1105")]
    public void Parse_LegacyFormats_ExtractsSequence(string manual, string? expectedPrefix, string expectedNumber)
    {
        ApplicationManualNumberParser.Parse(manual, out var full, out var prefix, out var number);

        Assert.Equal(manual, full);
        Assert.Equal(expectedPrefix, prefix);
        Assert.Equal(expectedNumber, number);
    }

    [Fact]
    public void Parse_DoesNotDoubleApplyMonthFormat()
    {
        ApplicationManualNumberParser.Parse("7/-1105", out _, out _, out var number);
        Assert.Equal("1105", number);
        Assert.DoesNotContain("/", number);
    }
}
