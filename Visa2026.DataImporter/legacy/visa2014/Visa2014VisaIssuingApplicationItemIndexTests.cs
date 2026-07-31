using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014VisaIssuingApplicationItemIndexTests
{
    [Fact]
    public void Build_ExtensionProcessNumber_WinsOverSibling()
    {
        var passport = Guid.NewGuid();
        var prevVisa = Guid.NewGuid();
        var nextVisa = Guid.NewGuid();
        var processPia = Guid.NewGuid();
        var siblingPia = Guid.NewGuid();

        var map = Visa2014VisaIssuingApplicationItemIndex.Build(
            processNumberLinks: [(nextVisa, processPia, IsExtensionApp: true)],
            visas:
            [
                (prevVisa, passport, new DateTime(2020, 1, 1)),
                (nextVisa, passport, new DateTime(2021, 1, 1)),
            ],
            extensionPias: [(siblingPia, prevVisa, new DateTime(2020, 6, 1))]);

        Assert.Equal(processPia, map[nextVisa].LegacyApplicationItemOid);
        Assert.Equal("processnumber", map[nextVisa].Source);
    }

    [Fact]
    public void Build_NonExtensionProcessNumber_YieldsToExtensionSibling()
    {
        var passport = Guid.NewGuid();
        var prevVisa = Guid.NewGuid();
        var nextVisa = Guid.NewGuid();
        var invitationPia = Guid.NewGuid();
        var extensionPia = Guid.NewGuid();

        var map = Visa2014VisaIssuingApplicationItemIndex.Build(
            processNumberLinks: [(nextVisa, invitationPia, IsExtensionApp: false)],
            visas:
            [
                (prevVisa, passport, new DateTime(2020, 1, 1)),
                (nextVisa, passport, new DateTime(2021, 1, 1)),
            ],
            extensionPias: [(extensionPia, prevVisa, new DateTime(2020, 6, 1))]);

        Assert.Equal(extensionPia, map[nextVisa].LegacyApplicationItemOid);
        Assert.Equal("extension_sibling", map[nextVisa].Source);
    }

    [Fact]
    public void Build_NonExtensionProcessNumber_UsedWhenNoSibling()
    {
        var passport = Guid.NewGuid();
        var visa = Guid.NewGuid();
        var invitationPia = Guid.NewGuid();

        var map = Visa2014VisaIssuingApplicationItemIndex.Build(
            processNumberLinks: [(visa, invitationPia, IsExtensionApp: false)],
            visas: [(visa, passport, new DateTime(2020, 1, 1))],
            extensionPias: []);

        Assert.Equal(invitationPia, map[visa].LegacyApplicationItemOid);
        Assert.Equal("processnumber", map[visa].Source);
    }

    [Fact]
    public void Build_UsesExtensionSiblingWhenProcessNumberMissing()
    {
        var passport = Guid.NewGuid();
        var prevVisa = Guid.NewGuid();
        var nextVisa = Guid.NewGuid();
        var pia = Guid.NewGuid();

        var map = Visa2014VisaIssuingApplicationItemIndex.Build(
            processNumberLinks: [],
            visas:
            [
                (prevVisa, passport, new DateTime(2020, 1, 1)),
                (nextVisa, passport, new DateTime(2021, 1, 1)),
            ],
            extensionPias: [(pia, prevVisa, new DateTime(2020, 6, 1))]);

        Assert.Equal(pia, map[nextVisa].LegacyApplicationItemOid);
        Assert.Equal("extension_sibling", map[nextVisa].Source);
        Assert.False(map.ContainsKey(prevVisa));
    }

    [Fact]
    public void Build_TieBreak_PicksLatestApplicationDateThenPiaOid()
    {
        var passport = Guid.NewGuid();
        var prevVisa = Guid.NewGuid();
        var nextVisa = Guid.NewGuid();
        var olderPia = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var newerPia = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var map = Visa2014VisaIssuingApplicationItemIndex.Build(
            processNumberLinks: [],
            visas:
            [
                (prevVisa, passport, new DateTime(2020, 1, 1)),
                (nextVisa, passport, new DateTime(2021, 1, 1)),
            ],
            extensionPias:
            [
                (olderPia, prevVisa, new DateTime(2020, 5, 1)),
                (newerPia, prevVisa, new DateTime(2020, 6, 1)),
            ]);

        Assert.Equal(newerPia, map[nextVisa].LegacyApplicationItemOid);
    }

    [Fact]
    public void Build_NoMatch_WhenPreviousVisaHadNoExtensionPia()
    {
        var passport = Guid.NewGuid();
        var prevVisa = Guid.NewGuid();
        var nextVisa = Guid.NewGuid();

        var map = Visa2014VisaIssuingApplicationItemIndex.Build(
            processNumberLinks: [],
            visas:
            [
                (prevVisa, passport, new DateTime(2020, 1, 1)),
                (nextVisa, passport, new DateTime(2021, 1, 1)),
            ],
            extensionPias: []);

        Assert.Empty(map);
    }
}