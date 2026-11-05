using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class LinkReviewLocationChipsTests
{
    [Fact]
    public void Build_without_joined_merges_suggestion_sources()
    {
        var chips = LinkReviewLocationChips.Build(null, "Ahal, Mary", "Mary, Balkan", "Farap");
        Assert.Equal(4, chips.Count);
        Assert.Equal(LinkReviewLocationChipSource.Case, chips[0].Source);
        Assert.Equal("Ahal", chips[0].Value);
        Assert.Equal(LinkReviewLocationChipSource.Visa, chips.First(c => c.Value == "Balkan").Source);
        Assert.Equal(LinkReviewLocationChipSource.WorkPermitItem, chips.First(c => c.Value == "Farap").Source);
    }

    [Fact]
    public void Build_with_joined_shows_only_persisted_tokens()
    {
        var chips = LinkReviewLocationChips.Build("Garabogaz şäheri", "Ahal, Mary", "Mary, Balkan", "Farap");
        Assert.Single(chips);
        Assert.Equal(LinkReviewLocationChipSource.Joined, chips[0].Source);
        Assert.Equal("Garabogaz şäheri", chips[0].Value);
    }

    [Fact]
    public void DisplayPreferJoined_uses_joined_row_over_chip_union()
    {
        var chips = LinkReviewLocationChips.Build(null, "Ahal", "Mary", null);
        Assert.Equal("Serhetabat", LinkReviewLocationChips.DisplayPreferJoined("Serhetabat", chips));
        Assert.Equal("Ahal, Mary", LinkReviewLocationChips.DisplayPreferJoined(null, chips));
    }

    [Fact]
    public void Hide_excludes_token_from_join_union_only()
    {
        var chips = LinkReviewLocationChips.Build("Ahal, Mary", null, null).ToList();
        LinkReviewLocationChips.Hide(chips[0]);
        Assert.Equal("Mary", LinkReviewLocationChips.JoinVisible(chips));
        Assert.True(chips[0].Hidden);
    }

    [Fact]
    public void AddManual_adds_only_new_tokens()
    {
        var chips = LinkReviewLocationChips.Build("Ahal", null, null).ToList();
        LinkReviewLocationChips.AddManual(chips, "Ahal, Balkan");
        Assert.Equal(2, chips.Count);
        Assert.Contains(chips, c => c.Source == LinkReviewLocationChipSource.Manual && c.Value == "Balkan");
    }

    [Fact]
    public void Build_skips_yok_sentinel_tokens()
    {
        var chips = LinkReviewLocationChips.Build("Ýok", "Ýok", "Ahal");
        Assert.Single(chips);
        Assert.Equal("Ahal", chips[0].Value);
        Assert.Equal(LinkReviewLocationChipSource.WorkPermitItem, chips[0].Source);
    }

    [Fact]
    public void AddManual_unhides_existing_token()
    {
        var chips = LinkReviewLocationChips.Build("Ahal", null, null).ToList();
        LinkReviewLocationChips.Hide(chips[0]);
        LinkReviewLocationChips.AddManual(chips, "Ahal");
        Assert.False(chips[0].Hidden);
        Assert.Single(chips);
    }
}