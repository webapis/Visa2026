using DevExpress.Data.Filtering;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class PersonListViewFullTextSearchCriteriaBuilderTests
{
    [Fact]
    public void BuildPersonIdentityCriteria_MatchesFoldedNameToken()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPersonIdentityCriteria("enes");

        Assert.NotNull(criteria);
        Assert.Contains("FirstName", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("enes", criteria.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPersonIdentityCriteria_IncludesPersonalNumber()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPersonIdentityCriteria("p-100");

        Assert.NotNull(criteria);
        Assert.Contains("PersonalNumber", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("p-100", criteria.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPersonIdentityCriteria_FoldsDiacriticsBeforeMatch()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPersonIdentityCriteria("G\u00fcl");

        Assert.NotNull(criteria);
        Assert.Contains("gul", criteria.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("G\u00fcl", criteria.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPersonIdentityCriteria_AndsMultipleTokens()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPersonIdentityCriteria("enes can");

        Assert.NotNull(criteria);
        Assert.Contains("And", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("enes", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("can", criteria.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildPersonIdentityCriteria_WhitespaceOrEmpty_ReturnsNull(string? searchText)
    {
        Assert.Null(PersonListViewFullTextSearchCriteriaBuilder.BuildPersonIdentityCriteria(searchText!));
    }

    [Fact]
    public void BuildPassportNumberCriteria_UsesRelatedPassportsCollection()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPassportNumberCriteria("ab123");

        Assert.NotNull(criteria);
        Assert.Contains("Passports", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("ab123", criteria.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPassportNumberCriteria_AndsMultipleTokens()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPassportNumberCriteria("ab 123");

        Assert.NotNull(criteria);
        Assert.Contains("And", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("ab", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("123", criteria.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildPassportNumberCriteria_WhitespaceOrEmpty_ReturnsNull(string? searchText)
    {
        Assert.Null(PersonListViewFullTextSearchCriteriaBuilder.BuildPassportNumberCriteria(searchText!));
    }

    [Fact]
    public void CombineOr_SkipsNullParts()
    {
        var left = CriteriaOperator.Parse("Contains([FirstName], 'a')");
        var combined = PersonListViewFullTextSearchCriteriaBuilder.CombineOr(left, null);

        Assert.Same(left, combined);
    }

    [Fact]
    public void CombineOr_AllNull_ReturnsNull()
    {
        Assert.Null(PersonListViewFullTextSearchCriteriaBuilder.CombineOr(null, null));
    }

    [Fact]
    public void CombineOr_TwoParts_BuildsOrGroup()
    {
        var left = CriteriaOperator.Parse("Contains([FirstName], 'a')");
        var right = CriteriaOperator.Parse("Contains([LastName], 'b')");

        var combined = PersonListViewFullTextSearchCriteriaBuilder.CombineOr(left, right);

        Assert.NotNull(combined);
        Assert.Contains("Or", combined.ToString(), StringComparison.Ordinal);
        Assert.Contains("FirstName", combined.ToString(), StringComparison.Ordinal);
        Assert.Contains("LastName", combined.ToString(), StringComparison.Ordinal);
    }
}
