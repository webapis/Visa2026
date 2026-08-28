using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Organization singleton resolution — duplicate rows must pick a stable keeper for reports.
/// </summary>
public sealed class OrganizationSingletonHelperTests
{
    private sealed class Row
    {
        public string? Name { get; init; }
    }

    [Fact]
    public void TryGetFromCandidates_Empty_ReturnsNull()
    {
        Assert.Null(OrganizationSingletonHelper.TryGetFromCandidates<Row>([], r => r.Name));
    }

    [Fact]
    public void TryGetFromCandidates_SingleBlank_ReturnsThatRow()
    {
        var blank = new Row { Name = "  " };
        var chosen = OrganizationSingletonHelper.TryGetFromCandidates([blank], r => r.Name);
        Assert.Same(blank, chosen);
    }

    [Fact]
    public void TryGetFromCandidates_SinglePopulated_ReturnsIt()
    {
        var a = new Row { Name = "Çalık" };
        var blank = new Row { Name = null };
        var chosen = OrganizationSingletonHelper.TryGetFromCandidates([blank, a], r => r.Name);
        Assert.Same(a, chosen);
    }

    [Fact]
    public void TryGetFromCandidates_MultiplePopulated_OrdersByKey()
    {
        var b = new Row { Name = "Beta" };
        var a = new Row { Name = "Alpha" };
        var chosen = OrganizationSingletonHelper.TryGetFromCandidates([b, a], r => r.Name);
        Assert.Same(a, chosen);
    }

    [Fact]
    public void TryGetFromCandidates_TieBreakerWins()
    {
        var a = new Row { Name = "Alpha" };
        var b = new Row { Name = "Beta" };
        var chosen = OrganizationSingletonHelper.TryGetFromCandidates(
            [a, b],
            r => r.Name,
            tieBreaker: list => list.First(r => r.Name == "Beta"));
        Assert.Same(b, chosen);
    }

    [Fact]
    public void ChooseKeeper_PrefersPopulatedAlphabetical()
    {
        var blank = new Row { Name = "" };
        var z = new Row { Name = "Zeta" };
        var a = new Row { Name = "Alpha" };
        var keeper = OrganizationSingletonHelper.ChooseKeeper([blank, z, a], r => r.Name);
        Assert.Same(a, keeper);
    }

    [Fact]
    public void ChooseKeeper_AllBlank_ReturnsFirst()
    {
        var first = new Row { Name = null };
        var second = new Row { Name = "  " };
        var keeper = OrganizationSingletonHelper.ChooseKeeper([first, second], r => r.Name);
        Assert.Same(first, keeper);
    }

    [Fact]
    public void ChooseKeeper_CustomChooser()
    {
        var a = new Row { Name = "Alpha" };
        var b = new Row { Name = "Beta" };
        var keeper = OrganizationSingletonHelper.ChooseKeeper(
            [a, b],
            r => r.Name,
            chooseKeeper: list => list.Last());
        Assert.Same(b, keeper);
    }
}
