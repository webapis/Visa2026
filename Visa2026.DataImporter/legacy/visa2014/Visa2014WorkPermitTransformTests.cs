using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014WorkPermitTransformTests
{
    [Fact]
    public void TryParseRawRow_ValidRow_DefaultsSourceTable()
    {
        var oid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = oid.ToString("D"),
            ["WorkPermitNumber"] = "WP-9",
            ["IssuedDate"] = "2024-02-15",
        };

        Assert.True(Visa2014WorkPermitTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(oid, parsed.LegacyOid);
        Assert.Equal("WP-9", parsed.WorkPermitNumber);
        Assert.Equal(new DateTime(2024, 2, 15), parsed.IssuedDate);
        Assert.Equal("WorkPermitLetter", parsed.SourceTable);
    }

    [Fact]
    public void TryParseRawRow_MissingOid_ReturnsFalse()
    {
        Assert.False(Visa2014WorkPermitTransform.TryParseRawRow(
            new Dictionary<string, string?> { ["WorkPermitNumber"] = "WP-1" },
            out _));
    }

    [Fact]
    public void TransformRows_MissingRequiredFields_SkipsWithReasons()
    {
        var batch = Visa2014WorkPermitTransform.TransformRows(
            [
                new Visa2014WorkPermitRawRow(Guid.NewGuid(), null, new DateTime(2024, 1, 1), "WorkPermitLetter"),
                new Visa2014WorkPermitRawRow(Guid.NewGuid(), "WP-A", null, "WorkPermitLetter"),
            ],
            out var skipped,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Equal(2, skipped.Count);
        Assert.Equal("required_null:WorkPermitNumber", skipped[0]["_skipReason"]);
        Assert.Equal("required_null:IssuedDate", skipped[1]["_skipReason"]);
    }

    [Fact]
    public void TransformRows_DuplicateNumbers_SuffixAndPreserveLegacyTable()
    {
        var oid1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var oid2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var issued = new DateTime(2024, 3, 1);

        var batch = Visa2014WorkPermitTransform.TransformRows(
            [
                new Visa2014WorkPermitRawRow(oid1, "wp-77", issued, "WorkPermit"),
                new Visa2014WorkPermitRawRow(oid2, "WP-77", issued, "WorkPermitLetter"),
            ],
            out var skipped,
            out var dedupeSummary);

        Assert.Empty(skipped);
        Assert.Equal(2, batch.ImportRows.Count);
        Assert.Single(dedupeSummary);
        Assert.Equal("WPN:WP-77", dedupeSummary[0]["_dedupeGroupId"]);

        var byOid = batch.ImportRows.ToDictionary(r => (Guid)r["_legacyRowId"]!);
        Assert.Equal("WorkPermit", byOid[oid1]["_legacyTable"]);
        Assert.Equal("WorkPermitLetter", byOid[oid2]["_legacyTable"]);
        Assert.Equal("wp-77" + oid1.ToString("N")[^8..], byOid[oid1]["WorkPermitNumber"]);
        Assert.Equal("WP-77" + oid2.ToString("N")[^8..], byOid[oid2]["WorkPermitNumber"]);
    }
}
