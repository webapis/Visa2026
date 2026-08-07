using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class CommaSeparatedSelectionHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ýok")]
    [InlineData("ýok")]
    public void ParseSelected_NoneOrBlank_ReturnsEmpty(string stored)
    {
        Assert.Empty(CommaSeparatedSelectionHelper.ParseSelected(stored));
    }

    [Fact]
    public void ParseSelected_SplitsTrimsAndDedupesCaseInsensitively()
    {
        var selected = CommaSeparatedSelectionHelper.ParseSelected(" Alpha , beta,ALPHA, Ýok , gamma ");

        Assert.Equal(new[] { "Alpha", "beta", "gamma" }, selected);
    }

    [Fact]
    public void ParseSelected_CustomNoneValue_IsTreatedAsEmpty()
    {
        Assert.Empty(CommaSeparatedSelectionHelper.ParseSelected("None", noneValue: "None"));
        Assert.Equal(new[] { "Zone A" }, CommaSeparatedSelectionHelper.ParseSelected("None, Zone A", noneValue: "None"));
    }

    [Fact]
    public void FormatSelected_NullOrEmpty_ReturnsNoneValue()
    {
        Assert.Equal(CommaSeparatedSelectionHelper.NoneValue, CommaSeparatedSelectionHelper.FormatSelected(null));
        Assert.Equal(CommaSeparatedSelectionHelper.NoneValue, CommaSeparatedSelectionHelper.FormatSelected(Array.Empty<string>()));
        Assert.Equal("N/A", CommaSeparatedSelectionHelper.FormatSelected(null, noneValue: "N/A"));
    }

    [Fact]
    public void FormatSelected_JoinsDistinctTrimmedLabels()
    {
        var formatted = CommaSeparatedSelectionHelper.FormatSelected(
            new[] { " Alpha ", "beta", "ALPHA", "Ýok", "  ", "gamma" });

        Assert.Equal("Alpha, beta, gamma", formatted);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("Ýok", true)]
    [InlineData("ýOK", true)]
    [InlineData("Zone", false)]
    public void IsNoneValue_MatchesBlankAndDefaultNone(string stored, bool expected)
    {
        Assert.Equal(expected, CommaSeparatedSelectionHelper.IsNoneValue(stored));
    }

    [Fact]
    public void ReplaceLabel_RewritesMatchingEntryCaseInsensitively()
    {
        var result = CommaSeparatedSelectionHelper.ReplaceLabel("Alpha, Beta, Gamma", "beta", "BETA-NEW");

        Assert.Equal("Alpha, BETA-NEW, Gamma", result);
    }

    [Fact]
    public void ReplaceLabel_NoMatchOrInvalidArgs_LeavesStoredUnchanged()
    {
        Assert.Equal("Alpha, Beta", CommaSeparatedSelectionHelper.ReplaceLabel("Alpha, Beta", "Missing", "X"));
        Assert.Equal("Alpha", CommaSeparatedSelectionHelper.ReplaceLabel("Alpha", "", "X"));
        Assert.Equal(CommaSeparatedSelectionHelper.NoneValue, CommaSeparatedSelectionHelper.ReplaceLabel(null, "A", "B"));
    }

    [Theory]
    [InlineData("Alpha, Beta", "beta", true)]
    [InlineData("Alpha, Beta", "Gamma", false)]
    [InlineData("Ýok", "Alpha", false)]
    [InlineData(null, "Alpha", false)]
    [InlineData("Alpha", "  ", false)]
    public void ContainsLabel_MatchesCaseInsensitively(string stored, string label, bool expected)
    {
        Assert.Equal(expected, CommaSeparatedSelectionHelper.ContainsLabel(stored, label));
    }
}
