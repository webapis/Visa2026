using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.Services.PersonIntake;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class PersonIntakeLookupSearchTests
{
    private static readonly Guid Turkiye = Guid.NewGuid();
    private static readonly Guid Turkmenistan = Guid.NewGuid();
    private static readonly Guid Azerbaijan = Guid.NewGuid();

    private static List<PersonIntakeLookupSearch.Entry> Countries() => new()
    {
        new(Azerbaijan, "Azerbaijani (Turkic)", 0),
        new(Turkmenistan, "Türkmenistan", 238),
        new(Turkiye, "Türkiye", 1106),
    };

    [Fact]
    public void Search_IgnoresDiacritics_AndRanksStartsWithThenUsage()
    {
        var result = PersonIntakeLookupSearch.Search("turk", Countries());

        Assert.Equal(3, result.Total);
        Assert.Equal(new[] { Turkiye, Turkmenistan, Azerbaijan }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public void Search_MatchesEveryTokenInAnyOrder()
    {
        var entries = new List<PersonIntakeLookupSearch.Entry>
        {
            new(Guid.NewGuid(), "Baş inžener", 31),
            new(Guid.NewGuid(), "Elektrik inženeri", 12),
        };

        var result = PersonIntakeLookupSearch.Search("inzener bas", entries);

        Assert.Single(result.Items);
        Assert.Equal("Baş inžener", result.Items[0].Text);
    }

    [Fact]
    public void Search_ExcludesSelectedValue_AndCapsResults()
    {
        var entries = Enumerable.Range(1, 10)
            .Select(i => new PersonIntakeLookupSearch.Entry(Guid.NewGuid(), $"Option {i}", i))
            .ToList();
        var excluded = entries[9].Id;

        var result = PersonIntakeLookupSearch.Search("option", entries, excluded);

        Assert.Equal(9, result.Total);
        Assert.Equal(PersonIntakeLookupSearch.MaxResults, result.Items.Count);
        Assert.DoesNotContain(result.Items, i => i.Id == excluded);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsNothing()
    {
        Assert.Empty(PersonIntakeLookupSearch.Search("   ", Countries()).Items);
    }

    [Fact]
    public void Browse_OrdersByUsageThenText()
    {
        var browse = PersonIntakeLookupSearch.Browse(Countries());

        Assert.Equal(new[] { Turkiye, Turkmenistan, Azerbaijan }, browse.Select(i => i.Id));
    }

    [Fact]
    public void ExactMatch_IsCaseAndDiacriticInsensitive()
    {
        Assert.Equal(Turkiye, PersonIntakeLookupSearch.ExactMatch("turkiye", Countries())?.Id);
        Assert.Null(PersonIntakeLookupSearch.ExactMatch("turk", Countries()));
    }

    [Fact]
    public void CodedEntries_RankByNameStart_AndExactMatchOnName()
    {
        var virgin = Guid.NewGuid();
        var entries = new List<PersonIntakeLookupSearch.Entry>
        {
            new(Turkiye, PersonIntakeLookupSearch.WithCode("TUR", "Türkiye"), 0),
            new(virgin, PersonIntakeLookupSearch.WithCode("VIR", "ABŞ-nyň Wirgin adalary"), 50),
        };

        Assert.Equal("TUR — Türkiye", entries[0].Text);
        Assert.Equal(Turkiye, PersonIntakeLookupSearch.Search("tur", entries).Items[0].Id);
        Assert.Equal(virgin, PersonIntakeLookupSearch.Search("vir", entries).Items[0].Id);
        Assert.Equal(virgin, PersonIntakeLookupSearch.Search("abs", entries).Items[0].Id);
        Assert.Equal(Turkiye, PersonIntakeLookupSearch.ExactMatch("turkiye", entries)?.Id);
        Assert.Equal("Türkiye", PersonIntakeLookupSearch.NamePart(entries[0].Text));
        Assert.Equal("Plain", PersonIntakeLookupSearch.NamePart("Plain"));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, true)]
    [InlineData(15, true)]
    [InlineData(16, false)]
    public void IsShortList_UsesThreshold(int count, bool expected)
    {
        Assert.Equal(expected, PersonIntakeLookupSearch.IsShortList(count));
    }
}
