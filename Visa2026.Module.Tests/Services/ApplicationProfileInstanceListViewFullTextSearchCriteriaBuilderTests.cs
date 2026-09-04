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
    public void BuildLinkedPeoplePassportCriteria_UsesPeopleAndInstancePassports()
    {
        var criteria = ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
            .BuildLinkedPeoplePassportCriteria("ab123");

        Assert.NotNull(criteria);
        var text = criteria.ToString();
        Assert.Contains("People", text, StringComparison.Ordinal);
        Assert.Contains("Passports", text, StringComparison.Ordinal);
        Assert.Contains("PassportNumber", text, StringComparison.Ordinal);
        Assert.Contains("ab123", text, StringComparison.Ordinal);
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