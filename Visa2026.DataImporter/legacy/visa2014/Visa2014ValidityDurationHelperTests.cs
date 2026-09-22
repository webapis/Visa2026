using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014ValidityDurationHelperTests
{
    [Theory]
    [InlineData("2024-01-01", "2024-04-01", 91)]
    [InlineData("2024-01-01", "2024-01-01", 0)]
    [InlineData("2024-06-01", "2024-05-01", -31)]
    public void ComputeDaySpan_UsesDatePartsOnly(string start, string expire, int expected)
    {
        var startDate = DateTime.Parse(start).AddHours(15);
        var expireDate = DateTime.Parse(expire).AddHours(3);

        Assert.Equal(expected, Visa2014ValidityDurationHelper.ComputeDaySpan(startDate, expireDate));
    }

    [Theory]
    [InlineData(90, 90)]
    [InlineData(180, 180)]
    [InlineData(365, 365)]
    [InlineData(100, 90)]
    [InlineData(140, 180)]
    [InlineData(200, 180)]
    [InlineData(270, 180)]
    [InlineData(320, 365)]
    [InlineData(0, 90)]
    [InlineData(1000, 365)]
    public void ClosestCandidateDaySpan_PicksNearestThenSmallerOnTie(int actual, int expected)
    {
        // Tie-break: OrderBy abs distance ThenBy candidate ascending (90 before 180 when equidistant).
        Assert.Equal(expected, Visa2014ValidityDurationHelper.ClosestCandidateDaySpan(actual));
    }

    [Fact]
    public void ClosestCandidateDaySpan_EquidistantPrefersSmallerCandidate()
    {
        // Midpoint between 90 and 180 is 135 — abs equal; ThenBy picks 90.
        Assert.Equal(90, Visa2014ValidityDurationHelper.ClosestCandidateDaySpan(135));
    }

    [Theory]
    [InlineData(90, "Month3")]
    [InlineData(180, "Month6")]
    [InlineData(365, "Year1")]
    [InlineData(91, "91")]
    [InlineData(0, "0")]
    public void LocalizationKeyForDaySpan_MapsKnownCandidates(int days, string expected)
    {
        Assert.Equal(expected, Visa2014ValidityDurationHelper.LocalizationKeyForDaySpan(days));
    }
}
