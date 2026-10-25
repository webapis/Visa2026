using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// FullTextSearch Result Number needles must compact spaces and require every
/// token to hit the same ProgressHistory.ResultNumber (e.g. "4" + "4114").
/// </summary>
public class ApplicationProfileInstanceProgressResultNumberSearchCriteriaTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("4 / 4114", "4/4114")]
    [InlineData("\t12/\t2026\n", "12/2026")]
    public void CompactResultNumberSearchKey_DropsAllWhitespace(string? token, string expected) =>
        Assert.Equal(
            expected,
            ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder.CompactResultNumberSearchKey(token));

    [Fact]
    public void BuildProgressResultNumberCriteria_BlankSearch_ReturnsNull()
    {
        Assert.Null(
            ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder.BuildProgressResultNumberCriteria("   "));
        Assert.Null(
            ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder.BuildProgressResultNumberCriteria(""));
    }

    [Fact]
    public void BuildProgressResultNumberCriteria_SpacedTokens_RequireSameResultNumber()
    {
        // PersonSearchTokens splits on whitespace → "4", "/", "4114"; each needle is compacted
        // and ANDed inside one ProgressHistory association (same ResultNumber must hold all).
        var criteria = ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
            .BuildProgressResultNumberCriteria("4 / 4114");

        Assert.NotNull(criteria);
        var text = criteria!.ToString();
        Assert.Contains("ProgressHistory", text, System.StringComparison.Ordinal);
        Assert.Contains("ResultNumber", text, System.StringComparison.Ordinal);
        Assert.Contains("And", text, System.StringComparison.Ordinal);
        Assert.Contains("Contains(Lower([ResultNumber])", text, System.StringComparison.Ordinal);
    }
}
