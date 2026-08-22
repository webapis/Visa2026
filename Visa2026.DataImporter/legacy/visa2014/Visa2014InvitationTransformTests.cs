using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014InvitationTransformTests
{
    [Fact]
    public void TryParseRawRow_ValidRow_ParsesFields()
    {
        var oid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var app = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = oid.ToString("D"),
            ["InvitationNumber"] = " INV-9 ",
            ["IssuedDate"] = "2024-01-10",
            ["DateOfExpire"] = "2024-04-10",
            ["ApplicationOid"] = app.ToString("D"),
        };

        Assert.True(Visa2014InvitationTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(oid, parsed.LegacyOid);
        Assert.Equal(" INV-9 ", parsed.InvitationNumber);
        Assert.Equal(new DateTime(2024, 1, 10), parsed.IssuedDate);
        Assert.Equal(new DateTime(2024, 4, 10), parsed.DateOfExpire);
        Assert.Equal(app, parsed.LegacyApplicationOid);
    }

    [Fact]
    public void TryParseRawRow_MissingOid_ReturnsFalse()
    {
        var row = new Dictionary<string, string?>
        {
            ["InvitationNumber"] = "INV-1",
        };

        Assert.False(Visa2014InvitationTransform.TryParseRawRow(row, out _));
    }

    [Fact]
    public void TransformRows_MissingRequiredFields_SkipsWithReasons()
    {
        var batch = Visa2014InvitationTransform.TransformRows(
            [
                new Visa2014InvitationRawRow(Guid.NewGuid(), null, new DateTime(2024, 1, 1), new DateTime(2024, 4, 1), Guid.NewGuid()),
                new Visa2014InvitationRawRow(Guid.NewGuid(), "INV-A", null, new DateTime(2024, 4, 1), Guid.NewGuid()),
                new Visa2014InvitationRawRow(Guid.NewGuid(), "INV-B", new DateTime(2024, 1, 1), null, Guid.NewGuid()),
            ],
            out var skipped,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Equal(3, skipped.Count);
        Assert.Equal("required_null:InvitationNumber", skipped[0]["_skipReason"]);
        Assert.Equal("required_null:IssuedDate", skipped[1]["_skipReason"]);
        Assert.Equal("required_null:DateOfExpire", skipped[2]["_skipReason"]);
    }

    [Fact]
    public void TransformRows_DuplicateInvitationNumbers_SuffixAllWithLegacyOidTail()
    {
        var oid1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var oid2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var app = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var issued = new DateTime(2024, 1, 1);
        var expire = new DateTime(2024, 4, 1);

        var batch = Visa2014InvitationTransform.TransformRows(
            [
                new Visa2014InvitationRawRow(oid1, "inv-100", issued, expire, app),
                new Visa2014InvitationRawRow(oid2, "INV-100", issued, expire, app),
            ],
            out var skipped,
            out var dedupeSummary);

        Assert.Empty(skipped);
        Assert.Equal(2, batch.ImportRows.Count);
        Assert.Single(dedupeSummary);
        Assert.Equal("INV:INV-100", dedupeSummary[0]["_dedupeGroupId"]);
        Assert.Equal(2, dedupeSummary[0]["memberCount"]);

        var byOid = batch.ImportRows.ToDictionary(r => (Guid)r["_legacyRowId"]!);
        Assert.Equal("inv-100" + oid1.ToString("N")[^8..], byOid[oid1]["InvitationNumber"]);
        Assert.Equal("INV-100" + oid2.ToString("N")[^8..], byOid[oid2]["InvitationNumber"]);
        Assert.Equal("Month3", byOid[oid1]["VisaPeriodKey"]);
    }

    [Fact]
    public void TransformRows_UniqueInvitationNumber_ImportsWithoutSuffix()
    {
        var oid = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var batch = Visa2014InvitationTransform.TransformRows(
            [
                new Visa2014InvitationRawRow(
                    oid,
                    "  UNIQUE-1  ",
                    new DateTime(2024, 1, 1),
                    new DateTime(2024, 7, 1),
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")),
            ],
            out var skipped,
            out var dedupeSummary);

        Assert.Empty(skipped);
        Assert.Empty(dedupeSummary);
        Assert.Single(batch.ImportRows);
        Assert.Equal("UNIQUE-1", batch.ImportRows[0]["InvitationNumber"]);
        Assert.Equal("Month6", batch.ImportRows[0]["VisaPeriodKey"]);
        Assert.Equal("", batch.ImportRows[0]["_dedupeGroupId"]);
    }
}
