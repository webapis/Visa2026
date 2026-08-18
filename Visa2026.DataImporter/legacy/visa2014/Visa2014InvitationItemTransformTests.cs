using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014InvitationItemTransformTests
{
    [Fact]
    public void TryParseRawRow_ValidRow_ParsesGuidsAndResult()
    {
        var oid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var person = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var passport = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var invitation = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = oid.ToString("D"),
            ["PersonOid"] = person.ToString("D"),
            ["PassportOid"] = passport.ToString("D"),
            ["InvitationOid"] = invitation.ToString("D"),
            ["ApplicationResultResult"] = "0",
        };

        Assert.True(Visa2014InvitationItemTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(oid, parsed.LegacyOid);
        Assert.Equal(person, parsed.LegacyPersonOid);
        Assert.Equal(passport, parsed.LegacyPassportOid);
        Assert.Equal(invitation, parsed.LegacyInvitationOid);
        Assert.Equal(0, parsed.ApplicationResultResult);
    }

    [Fact]
    public void TryParseRawRow_MissingOid_ReturnsFalse()
    {
        var row = new Dictionary<string, string?>
        {
            ["PersonOid"] = Guid.NewGuid().ToString("D"),
        };

        Assert.False(Visa2014InvitationItemTransform.TryParseRawRow(row, out _));
    }

    [Fact]
    public void BuildExportRow_MissingPerson_SkipsWithReason()
    {
        var raw = new Visa2014InvitationItemRawRow(
            LegacyOid: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            LegacyPersonOid: null,
            LegacyPassportOid: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            LegacyInvitationOid: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            ApplicationResultResult: 0);
        var index = Visa2014LegacyInvitationItemCancellationIndex.FromLegacyOidsForTests([]);

        var export = Visa2014InvitationItemTransform.BuildExportRow(raw, index, out var skipReason);

        Assert.Equal("missing_fk:Person", skipReason);
        Assert.Equal("PersonInInvitation", export["_legacyTable"]);
        Assert.Null(export["Person"]);
    }

    [Fact]
    public void BuildExportRow_IndexHit_SetsIsCancelledTrue()
    {
        var itemOid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var raw = new Visa2014InvitationItemRawRow(
            LegacyOid: itemOid,
            LegacyPersonOid: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            LegacyPassportOid: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            LegacyInvitationOid: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            ApplicationResultResult: 0);
        var index = Visa2014LegacyInvitationItemCancellationIndex.FromLegacyOidsForTests([itemOid]);

        var export = Visa2014InvitationItemTransform.BuildExportRow(raw, index, out var skipReason);

        Assert.Null(skipReason);
        Assert.Equal(true, export["IsCancelled"]);
    }
}
