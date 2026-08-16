using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Covers sanitize / legacy marital helpers without touching
/// <see cref="VisaFamilyMemberLinesHelperTests"/> (open coverage PR #19).
/// </summary>
public sealed class VisaFamilyMemberLinesHelperSanitizeTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("Ayşe (Yılmaz)", "Ayşe Yılmaz")]
    [InlineData("Name[legacy]{", "Namelegacy")]
    [InlineData("  Double   Spaces  ", "Double Spaces")]
    [InlineData("Trailing,", "Trailing")]
    [InlineData("Dash-", "Dash")]
    public void SanitizeFamilyMemberFullName_StripsNoise(string raw, string expected)
    {
        Assert.Equal(expected, VisaFamilyMemberLinesHelper.SanitizeFamilyMemberFullName(raw));
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData(" 1 ", true)]
    [InlineData("2", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("01", false)]
    public void IsLegacySingleMaritalStatus_OnlyLiteralOne(string legacyStatusInt, bool expected)
    {
        Assert.Equal(expected, VisaFamilyMemberLinesHelper.IsLegacySingleMaritalStatus(legacyStatusInt));
    }

    [Fact]
    public void Parse_AppliesSanitizeToFullNameSegment()
    {
        var rows = VisaFamilyMemberLinesHelper.Parse("Ayşe (Yılmaz); 12.10.1989; aýaly; TUR");

        Assert.Single(rows);
        Assert.Equal("Ayşe Yılmaz", rows[0].FullName);
    }
}
