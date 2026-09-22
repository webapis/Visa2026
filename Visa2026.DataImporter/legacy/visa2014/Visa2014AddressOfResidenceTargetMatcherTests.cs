using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014AddressOfResidenceTargetMatcherTests
{
    [Theory]
    [InlineData("Lodging", 0)]
    [InlineData("Hotel", 1)]
    [InlineData("PrivateHouse", 2)]
    [InlineData("Hospital", 3)]
    [InlineData("Other", 4)]
    [InlineData("Unknown", -1)]
    [InlineData("", -1)]
    public void MapResidenceTypeToSqlValue_MatchesLegacyEnum(string typeText, int expected)
    {
        Assert.Equal(expected, Visa2014AddressOfResidenceTargetMatcher.MapResidenceTypeToSqlValue(typeText));
    }
}
