using Visa2026.DataImporter;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014ApplicationProgressSeedHelperTests
{
    [Fact]
    public void IsInitializerSeed_RequiresBeingPreparedAndBlankDescription()
    {
        var seed = new ApplicationProgress
        {
            State = new ApplicationState { Code = Visa2014ApplicationProgressSeedHelper.InitialStateCode },
            Description = "",
        };
        var withDescription = new ApplicationProgress
        {
            State = new ApplicationState { Code = Visa2014ApplicationProgressSeedHelper.InitialStateCode },
            Description = "officer note",
        };
        var wrongState = new ApplicationProgress
        {
            State = new ApplicationState { Code = "PROCESS_STARTED" },
            Description = "",
        };

        Assert.True(Visa2014ApplicationProgressSeedHelper.IsInitializerSeed(seed));
        Assert.True(Visa2014ApplicationProgressSeedHelper.IsInitializerSeed(new ApplicationProgress
        {
            State = new ApplicationState { Code = "is_being_prepared" },
            Description = "   ",
        }));
        Assert.False(Visa2014ApplicationProgressSeedHelper.IsInitializerSeed(withDescription));
        Assert.False(Visa2014ApplicationProgressSeedHelper.IsInitializerSeed(wrongState));
        Assert.False(Visa2014ApplicationProgressSeedHelper.IsInitializerSeed(new ApplicationProgress
        {
            State = null,
            Description = "",
        }));
    }

    [Theory]
    [InlineData("app:prepare", "prepare")]
    [InlineData("legacy:123:prepare", "prepare")]
    [InlineData("no-colon", null)]
    [InlineData("trailing:", null)]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void ExtractStepCode_ParsesTrailingSegmentAfterLastColon(string? syntheticKey, string? expected)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (syntheticKey is not null)
            row["_syntheticStepKey"] = syntheticKey;

        Assert.Equal(expected, Visa2014ApplicationProgressSeedHelper.ExtractStepCode(row));
    }

    [Fact]
    public void IsPrepareSyntheticStep_RequiresBeingPreparedAndPrepareKey()
    {
        var prepare = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["State"] = Visa2014ApplicationProgressSeedHelper.InitialStateCode,
            ["_syntheticStepKey"] = "app-1:prepare",
        };
        var wrongState = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["State"] = "PROCESS_STARTED",
            ["_syntheticStepKey"] = "app-1:prepare",
        };
        var wrongStep = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["State"] = Visa2014ApplicationProgressSeedHelper.InitialStateCode,
            ["_syntheticStepKey"] = "app-1:issued",
        };

        Assert.True(Visa2014ApplicationProgressSeedHelper.IsPrepareSyntheticStep(prepare));
        Assert.False(Visa2014ApplicationProgressSeedHelper.IsPrepareSyntheticStep(wrongState));
        Assert.False(Visa2014ApplicationProgressSeedHelper.IsPrepareSyntheticStep(wrongStep));
    }

    [Fact]
    public void DatesMatch_ComparesDateComponentOnly()
    {
        var left = new DateTime(2024, 6, 2, 8, 30, 0);
        var sameDay = new DateTime(2024, 6, 2, 23, 59, 59);
        var nextDay = new DateTime(2024, 6, 3, 0, 0, 0);

        Assert.True(Visa2014ApplicationProgressSeedHelper.DatesMatch(left, sameDay));
        Assert.False(Visa2014ApplicationProgressSeedHelper.DatesMatch(left, nextDay));
    }
}
