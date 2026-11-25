using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class PersonRosterColorTabTests
{
    [Fact]
    public void AllAndSearch_HaveNoExtraCriteria()
    {
        Assert.Null(PersonRosterColorTab.CriteriaFor(PersonRosterColorTab.All));
        Assert.Null(PersonRosterColorTab.CriteriaFor(PersonRosterColorTab.Search));
        Assert.Null(PersonRosterColorTab.CriteriaFor(null));
    }

    [Fact]
    public void Incomplete_UsesTheIncompleteFlag()
    {
        var criteria = PersonRosterColorTab.CriteriaFor(PersonRosterColorTab.Incomplete)!.ToString();
        Assert.Contains("IsDataIncomplete", criteria, StringComparison.Ordinal);
    }

    [Fact]
    public void Search_IsRecognizedAndOtherKeysFallBackToAll()
    {
        Assert.True(PersonRosterColorTab.IsSearch(PersonRosterColorTab.Search));
        Assert.Equal(PersonRosterColorTab.All, PersonRosterColorTab.Normalize("active-visas"));
        Assert.Equal(PersonRosterColorTab.Incomplete, PersonRosterColorTab.Normalize(PersonRosterColorTab.Incomplete));
    }
}
