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
    public void BuildPersonIdentityCriteria_AndsMultipleTokens()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPersonIdentityCriteria("enes can");

        Assert.NotNull(criteria);
        Assert.Contains("And", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("enes", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("can", criteria.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPersonIdentityCriteria_FoldsTurkishLastNameTokenAndStoredField()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPersonIdentityCriteria("Hasan Y\u0131lmaz");

        Assert.NotNull(criteria);
        var text = criteria.ToString();
        Assert.Contains("yilmaz", text, StringComparison.Ordinal);
        Assert.Contains("Replace", text, StringComparison.Ordinal);
        Assert.Contains("LastName", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPassportNumberCriteria_UsesRelatedPassportsCollection()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPassportNumberCriteria("ab123");

        Assert.NotNull(criteria);
        Assert.Contains("Passports", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("ab123", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("Replace", criteria.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CompactPassportSearchKey_StripsSpacesAndHyphens()
    {
        Assert.Equal(
            "u86993401",
            PersonListViewFullTextSearchCriteriaBuilder.CompactPassportSearchKey("U86993401"));
        Assert.Equal(
            "u86993401",
            PersonListViewFullTextSearchCriteriaBuilder.CompactPassportSearchKey("U 86993401"));
        Assert.Equal(
            "ias476479",
            PersonListViewFullTextSearchCriteriaBuilder.CompactPassportSearchKey("I-AŞ 476479"));
        Assert.Equal(
            string.Empty,
            PersonListViewFullTextSearchCriteriaBuilder.CompactPassportSearchKey("   "));
    }

    [Fact]
    public void BuildPassportNumberCriteria_SpacedNumberIsSingleCompactKey()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPassportNumberCriteria("U 86993401");

        Assert.NotNull(criteria);
        var text = criteria.ToString();
        Assert.Contains("u86993401", text, StringComparison.Ordinal);
        Assert.DoesNotContain(" And ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CombineOr_SkipsNullParts()
    {
        var left = CriteriaOperator.Parse("Contains([FirstName], 'a')");
        var combined = PersonListViewFullTextSearchCriteriaBuilder.CombineOr(left, null);

        Assert.Same(left, combined);
    }
}
