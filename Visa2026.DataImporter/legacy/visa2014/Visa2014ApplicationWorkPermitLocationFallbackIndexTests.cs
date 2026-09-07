using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ApplicationWorkPermitLocationFallbackIndexTests
{
    [Fact]
    public void PickMajorityLocationOid_PicksHighestCountThenMinPia()
    {
        var a = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var b = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var pia1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var pia2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var picked = Visa2014ApplicationWorkPermitLocationFallbackIndex.PickMajorityLocationOid(
        [
            (b, 1, pia1),
            (a, 2, pia2),
        ]);

        Assert.Equal(a, picked);
    }

    [Fact]
    public void ApplyWhenEmpty_FillsOnlyAdditionalWpLocationWhenHeaderEmpty()
    {
        var additionalOid = Guid.NewGuid();
        var invitationOid = Guid.NewGuid();
        var alreadyFilledOid = Guid.NewGuid();
        var loc = "Mary saheri";

        var rows = new List<Dictionary<string, object?>>
        {
            new(StringComparer.Ordinal)
            {
                ["_legacyRowId"] = additionalOid,
                ["ApplicationType"] = "App_Additional_WP_location",
                ["MovementPermitLocation"] = null,
            },
            new(StringComparer.Ordinal)
            {
                ["_legacyRowId"] = invitationOid,
                ["ApplicationType"] = "App_Inv",
                ["MovementPermitLocation"] = null,
            },
            new(StringComparer.Ordinal)
            {
                ["_legacyRowId"] = alreadyFilledOid,
                ["ApplicationType"] = "App_Additional_WP_location",
                ["MovementPermitLocation"] = "header name",
            },
        };

        Visa2014ApplicationWorkPermitLocationFallbackIndex.ApplyWhenEmpty(
            rows,
            new Dictionary<Guid, string>
            {
                [additionalOid] = loc,
                [invitationOid] = loc,
                [alreadyFilledOid] = "from wp",
            });

        Assert.Equal(loc, rows[0]["MovementPermitLocation"]);
        Assert.Null(rows[1]["MovementPermitLocation"]);
        Assert.Equal("header name", rows[2]["MovementPermitLocation"]);
    }
}