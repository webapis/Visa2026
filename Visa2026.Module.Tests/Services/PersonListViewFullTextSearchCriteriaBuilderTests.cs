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
    public void BuildPassportNumberCriteria_UsesRelatedPassportsCollection()
    {
        var criteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPassportNumberCriteria("ab123");

        Assert.NotNull(criteria);
        Assert.Contains("Passports", criteria.ToString(), StringComparison.Ordinal);
        Assert.Contains("ab123", criteria.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CombineOr_SkipsNullParts()
    {
        var left = CriteriaOperator.Parse("Contains([FirstName], 'a')");
        var combined = PersonListViewFullTextSearchCriteriaBuilder.CombineOr(left, null);

        Assert.Same(left, combined);
    }
}
