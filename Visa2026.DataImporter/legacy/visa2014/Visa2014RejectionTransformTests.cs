using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014RejectionTransformTests
{
    [Fact]
    public void TryParseRawRow_ValidRow_ParsesFields()
    {
        var oid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var app = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = oid.ToString("D"),
            ["RejectedDocNumber"] = "REJ-1",
            ["IssuedDate"] = "2024-05-01",
            ["ApplicationOid"] = app.ToString("D"),
        };

        Assert.True(Visa2014RejectionTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(oid, parsed.LegacyOid);
        Assert.Equal("REJ-1", parsed.RejectedDocNumber);
        Assert.Equal(new DateTime(2024, 5, 1), parsed.IssuedDate);
        Assert.Equal(app, parsed.LegacyApplicationOid);
    }

    [Fact]
    public void TryParseRawRow_MissingOid_ReturnsFalse()
    {
        Assert.False(Visa2014RejectionTransform.TryParseRawRow(
            new Dictionary<string, string?> { ["RejectedDocNumber"] = "REJ-1" },
            out _));
    }

    [Fact]
    public void TransformRows_MissingRequiredFields_SkipsWithReasons()
    {
        var app = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var batch = Visa2014RejectionTransform.TransformRows(
            [
                new Visa2014RejectionRawRow(Guid.NewGuid(), null, new DateTime(2024, 1, 1), app),
                new Visa2014RejectionRawRow(Guid.NewGuid(), "REJ-A", null, app),
                new Visa2014RejectionRawRow(Guid.NewGuid(), "REJ-B", new DateTime(2024, 1, 1), null),
            ],
            out var skipped,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Equal(3, skipped.Count);
        Assert.Equal("required_null:RejectedDocNumber", skipped[0]["_skipReason"]);
        Assert.Equal("required_null:IssuedDate", skipped[1]["_skipReason"]);
        Assert.Equal("required_null:Application", skipped[2]["_skipReason"]);
    }

    [Fact]
    public void TransformRows_DuplicateDocNumbers_SuffixAllWithLegacyOidTail()
    {
        var oid1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var oid2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var app = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var issued = new DateTime(2024, 6, 1);

        var batch = Visa2014RejectionTransform.TransformRows(
            [
                new Visa2014RejectionRawRow(oid1, "rej-9", issued, app),
                new Visa2014RejectionRawRow(oid2, "REJ-9", issued, app),
            ],
            out var skipped,
            out var dedupeSummary);

        Assert.Empty(skipped);
        Assert.Equal(2, batch.ImportRows.Count);
        Assert.Single(dedupeSummary);
        Assert.Equal("REJ:REJ-9", dedupeSummary[0]["_dedupeGroupId"]);

        var byOid = batch.ImportRows.ToDictionary(r => (Guid)r["_legacyRowId"]!);
        Assert.Equal("rej-9" + oid1.ToString("N")[^8..], byOid[oid1]["RejectedDocNumber"]);
        Assert.Equal("REJ-9" + oid2.ToString("N")[^8..], byOid[oid2]["RejectedDocNumber"]);
        Assert.Equal(app.ToString("D"), byOid[oid1]["Application"]);
        Assert.Equal("2024-06-01", byOid[oid1]["Date"]);
    }
}
