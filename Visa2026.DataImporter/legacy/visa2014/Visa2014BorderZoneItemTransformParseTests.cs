using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Border-zone item (PersonInApplication) parse + skip gates used by insert-only catch-up import.
/// </summary>
public class Visa2014BorderZoneItemTransformParseTests
{
    [Fact]
    public void ExtractSql_TargetsBorderZoneTypeAndPersonInApplication()
    {
        var sql = Visa2014BorderZoneItemTransform.ExtractSql;

        Assert.Contains("PersonInApplication", sql, StringComparison.Ordinal);
        Assert.Contains("TypeOfApplicationForEmployee = 11", sql, StringComparison.Ordinal);
        Assert.Contains("ProcessNumber", sql, StringComparison.Ordinal);
        Assert.Contains("GCRecord IS NULL", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParseRawRow_RequiresOid()
    {
        Assert.False(Visa2014BorderZoneItemTransform.TryParseRawRow(
            new Dictionary<string, string?> { ["PersonOid"] = Guid.NewGuid().ToString("D") },
            out _));

        Assert.False(Visa2014BorderZoneItemTransform.TryParseRawRow(
            new Dictionary<string, string?> { ["Oid"] = "not-a-guid" },
            out _));
    }

    [Fact]
    public void TryParseRawRow_ParsesOptionalFks()
    {
        var oid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var person = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var passport = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var zone = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        Assert.True(Visa2014BorderZoneItemTransform.TryParseRawRow(
            new Dictionary<string, string?>
            {
                ["Oid"] = oid.ToString("D"),
                ["PersonOid"] = person.ToString("D"),
                ["PassportOid"] = passport.ToString("D"),
                ["BorderZoneOid"] = zone.ToString("D"),
                ["ParentProcessNumber"] = "AS-1",
            },
            out var parsed));

        Assert.Equal(oid, parsed.LegacyOid);
        Assert.Equal(person, parsed.LegacyPersonOid);
        Assert.Equal(passport, parsed.LegacyPassportOid);
        Assert.Equal(zone, parsed.LegacyBorderZoneOid);
        Assert.Equal("AS-1", parsed.ParentProcessNumber);
    }

    [Fact]
    public void TransformRows_SkipsNonPermitParentAndMissingFks()
    {
        var goodPerson = Guid.NewGuid();
        var goodPassport = Guid.NewGuid();
        var goodZone = Guid.NewGuid();

        var batch = Visa2014BorderZoneItemTransform.TransformRows(
        [
            new Visa2014BorderZoneItemRawRow(Guid.NewGuid(), goodPerson, goodPassport, goodZone, "NOT-A-PERMIT"),
            new Visa2014BorderZoneItemRawRow(Guid.NewGuid(), null, goodPassport, goodZone, "AS-100"),
            new Visa2014BorderZoneItemRawRow(Guid.NewGuid(), goodPerson, null, goodZone, "CO-200"),
            new Visa2014BorderZoneItemRawRow(Guid.NewGuid(), goodPerson, goodPassport, null, "AS-300"),
            new Visa2014BorderZoneItemRawRow(Guid.NewGuid(), goodPerson, goodPassport, goodZone, "AS-400"),
        ]);

        Assert.Equal(5, batch.LegacyRowCount);
        Assert.Single(batch.ImportRows);
        Assert.Equal(4, batch.Skipped.Count);
        Assert.Equal("parent_header_skipped", batch.Skipped[0]["_skipReason"]);
        Assert.Equal("missing_fk:Person", batch.Skipped[1]["_skipReason"]);
        Assert.Equal("missing_fk:Passport", batch.Skipped[2]["_skipReason"]);
        Assert.Equal("missing_fk:BorderZone", batch.Skipped[3]["_skipReason"]);
        Assert.Equal("import", batch.ImportRows[0]["_importAction"]);
        Assert.Equal(goodPerson.ToString("D"), batch.ImportRows[0]["Person"]);
    }
}
