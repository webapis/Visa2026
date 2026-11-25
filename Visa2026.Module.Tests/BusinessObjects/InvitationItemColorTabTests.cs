using System.Collections.ObjectModel;
using System.Globalization;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class InvitationItemColorTabTests
{
    [Theory]
    [InlineData(InvitationLaterLife.InForce, InvitationItemColorTab.Ready)]
    [InlineData(InvitationLaterLife.Used, InvitationItemColorTab.Used)]
    [InlineData(InvitationLaterLife.Cancelled, InvitationItemColorTab.Cancelled)]
    [InlineData(InvitationLaterLife.Changed, InvitationItemColorTab.Changed)]
    [InlineData(InvitationLaterLife.ExpiredUnused, InvitationItemColorTab.Expired)]
    public void FromLaterLife_maps_expected_bucket(string life, string expected)
    {
        Assert.Equal(expected, InvitationItemColorTab.FromLaterLife(life));
    }

    [Fact]
    public void BuildTabs_always_includes_every_bucket_and_excludes_search_by_default()
    {
        var ready = ItemWithLife(InvitationLaterLife.InForce);
        var used = ItemWithLife(InvitationLaterLife.Used);

        var tabs = InvitationItemColorTab.BuildTabs([ready, used], selectedKey: InvitationItemColorTab.Used);

        Assert.Equal(InvitationItemColorTab.AllKey, tabs[0].Key);
        Assert.Equal(2, tabs[0].Count);
        Assert.Contains(tabs, t => t.Key == InvitationItemColorTab.Ready && t.Count == 1);
        Assert.Contains(tabs, t => t.Key == InvitationItemColorTab.Used && t.Count == 1);
        Assert.Contains(tabs, t => t.Key == InvitationItemColorTab.Cancelled && t.Count == 0);
        Assert.DoesNotContain(tabs, t => InvitationItemColorTab.IsSearchTab(t.Key));
        Assert.Equal(InvitationItemColorTab.OrderedBucketKeys.Count + 1, tabs.Count);
    }

    [Fact]
    public void BuildTabs_can_include_search_tab()
    {
        var tabs = InvitationItemColorTab.BuildTabs(
            [ItemWithLife(InvitationLaterLife.InForce)],
            selectedKey: InvitationItemColorTab.SearchKey,
            includeSearchTab: true,
            searchMatchCount: 1);

        Assert.Contains(tabs, t => t.Key == InvitationItemColorTab.SearchKey && t.Count == 1);
    }

    [Fact]
    public void GetSearchLabel_uses_turkmen()
    {
        Assert.Equal("Gözleg", InvitationItemColorTab.GetSearchLabel(new CultureInfo("tk-TM")));
    }


    [Theory]
    [InlineData(InvitationItemColorTab.Ready, "Taýýar")]
    [InlineData(InvitationItemColorTab.Used, "Ulanyldy")]
    [InlineData(InvitationItemColorTab.Cancelled, "Ýatyryldy")]
    [InlineData(InvitationItemColorTab.Changed, "Üýtgedildi")]
    [InlineData(InvitationItemColorTab.Expired, "Möhleti gutaran")]
    public void GetLabel_matches_tab_captions(string key, string expected)
    {
        Assert.Equal(expected, InvitationItemColorTab.GetLabel(key));
    }

    [Fact]
    public void StateDisplay_follows_stamped_life()
    {
        var item = ItemWithLife(InvitationLaterLife.Used);
        Assert.Equal("Ulanyldy", item.CakylyklarStateDisplay);
    }
    private static InvitationItem ItemWithLife(string life)
    {
        var item = new InvitationItem
        {
            Invitation = new Invitation(),

        };
        item.SetListViewLifeKey(life);
        return item;
    }
}