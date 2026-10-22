using Visa2026.Module.Services;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilderTests
{
    [Fact]
    public void BuildLinkedPeopleIdentityCriteria_UsesPeopleFirstAndLastName()
    {
        var criteria = ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
            .BuildLinkedPeopleIdentityCriteria("enes can");

        Assert.NotNull(criteria);
        var text = criteria.ToString();
        Assert.Contains("People", text, StringComparison.Ordinal);
        Assert.Contains("FirstName", text, StringComparison.Ordinal);
        Assert.Contains("LastName", text, StringComparison.Ordinal);
        Assert.Contains("And", text, StringComparison.Ordinal);
        Assert.Contains("enes", text, StringComparison.Ordinal);
        Assert.Contains("can", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildProgressResultNumberCriteria_MatchesAnyProgressResultNumber()
    {
        var criteria = ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
            .BuildProgressResultNumberCriteria("4/4114");

        Assert.NotNull(criteria);
        var text = criteria.ToString();
        Assert.Contains("ProgressHistory", text, StringComparison.Ordinal);
        Assert.Contains("ResultNumber", text, StringComparison.Ordinal);
        Assert.Contains("4/4114", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CompactResultNumberSearchKey_DropsSpacesAroundSlash()
    {
        Assert.Equal(
            "4/4114",
            ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder.CompactResultNumberSearchKey("4 / 4114"));
    }

    [Fact]
    public void OfficerShellApplicationSearch_MatchesFirstLastAndPassportTokens()
    {
        const string haystack = "John\nSmith\nA12345678\nVisa extension";

        Assert.True(OfficerShellApplicationSearch.Matches("john smith", haystack));
        Assert.True(OfficerShellApplicationSearch.Matches("A12345678", haystack));
        Assert.False(OfficerShellApplicationSearch.Matches("jane", haystack));
        Assert.True(OfficerShellApplicationSearch.Matches("  ", haystack));
    }
}