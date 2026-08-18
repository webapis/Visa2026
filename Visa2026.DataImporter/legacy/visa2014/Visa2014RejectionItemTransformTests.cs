using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014RejectionItemTransformTests
{
    [Fact]
    public void TryParseRawRow_ValidRow_ParsesGuids()
    {
        var oid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var person = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var passport = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var rejection = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = oid.ToString("D"),
            ["PersonOid"] = person.ToString("D"),
            ["PassportOid"] = passport.ToString("D"),
            ["RejectionOid"] = rejection.ToString("D"),
        };

        Assert.True(Visa2014RejectionItemTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(oid, parsed.LegacyOid);
        Assert.Equal(person, parsed.LegacyPersonOid);
        Assert.Equal(passport, parsed.LegacyPassportOid);
        Assert.Equal(rejection, parsed.LegacyRejectionOid);
    }

    [Fact]
    public void TryParseRawRow_InvalidOid_ReturnsFalse()
    {
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = "not-a-guid",
        };

        Assert.False(Visa2014RejectionItemTransform.TryParseRawRow(row, out _));
    }

    [Fact]
    public void BuildExportRow_MissingPassport_PrefersFirstSkipReasonPersonThenPassport()
    {
        var raw = new Visa2014RejectionItemRawRow(
            LegacyOid: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            LegacyPersonOid: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            LegacyPassportOid: null,
            LegacyRejectionOid: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

        var export = Visa2014RejectionItemTransform.BuildExportRow(raw, out var skipReason);

        Assert.Equal("missing_fk:Passport", skipReason);
        Assert.Equal("PersonInInvitation", export["_legacyTable"]);
        Assert.Null(export["Passport"]);
        Assert.Equal(raw.LegacyPersonOid?.ToString("D"), export["Person"]);
        Assert.Null(export["Reason"]);
    }

    [Fact]
    public void BuildExportRow_Complete_MapsRejectionFk()
    {
        var person = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var passport = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var rejection = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var raw = new Visa2014RejectionItemRawRow(
            LegacyOid: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            LegacyPersonOid: person,
            LegacyPassportOid: passport,
            LegacyRejectionOid: rejection);

        var export = Visa2014RejectionItemTransform.BuildExportRow(raw, out var skipReason);

        Assert.Null(skipReason);
        Assert.Equal(person.ToString("D"), export["Person"]);
        Assert.Equal(passport.ToString("D"), export["Passport"]);
        Assert.Equal(rejection.ToString("D"), export["Rejection"]);
    }
}
