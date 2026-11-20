using System;
using System.Linq;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceAddressLookupTests
{
    [Fact]
    public void DeduplicateAddressOptions_CollapsesSameFoldedLabel()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var options = new[]
        {
            Opt(a, "Mary welaýatynyň, Mary şäheri, Senagat zolagy"),
            Opt(b, "Mary welaýatynyň, Mary şäheri, Senagat zolagy"),
            Opt(Guid.NewGuid(), "Aşgabat şäheri, 1945 köç"),
        };

        var deduped = ApplicationWorkspaceCaseHeaderFieldsHelper.DeduplicateAddressOptions(options);
        Assert.Equal(2, deduped.Count);
        Assert.Contains(deduped, o => o.Id == a);
        Assert.DoesNotContain(deduped, o => o.Id == b);
    }

    [Fact]
    public void DeduplicateAddressOptions_PrefersSelectedId()
    {
        var keep = Guid.NewGuid();
        var drop = Guid.NewGuid();
        var options = new[]
        {
            Opt(drop, "Garabogaz awtomobil ýoly"),
            Opt(keep, "Garabogaz awtomobil ýoly"),
        };

        var deduped = ApplicationWorkspaceCaseHeaderFieldsHelper.DeduplicateAddressOptions(options, preferId: keep);
        Assert.Equal(keep, Assert.Single(deduped).Id);
    }

    [Fact]
    public void AddressOptionMatchesSearch_FindsFoldedSubstring()
    {
        var option = Opt(
            Guid.NewGuid(),
            "Balkan welaýatynyň, Türkmenbaşy etraby, T-1-nji Garabogaz awtomobil ýoluň");
        Assert.True(ApplicationWorkspaceCaseHeaderFieldsHelper.AddressOptionMatchesSearch(option, "garabo"));
        Assert.False(ApplicationWorkspaceCaseHeaderFieldsHelper.AddressOptionMatchesSearch(option, "xyzzy"));
    }

    private static ApplicationWorkspaceLookupOption Opt(Guid id, string label) =>
        new()
        {
            Id = id,
            DisplayName = label,
            SearchText = ApplicationWorkspaceCaseHeaderFieldsHelper.BuildDiacriticSearchHaystack(label),
        };
}