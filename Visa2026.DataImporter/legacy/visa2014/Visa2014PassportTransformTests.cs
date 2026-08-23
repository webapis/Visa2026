using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014PassportTransformTests
{
    private static readonly Guid PersonOid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static IReadOnlyDictionary<string, Visa2014LookupCatalog> CountryCatalog(
        string legacy = "TURKEY",
        string target = "TUR",
        string unmappedPolicy = "block_row") =>
        new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase)
        {
            ["Country"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "Country",
                TargetMatchProperty = "Code",
                UnmappedPolicy = unmappedPolicy,
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [legacy] = target,
                },
            },
        };

    private static Visa2014PassportRawRow Raw(
        Guid oid,
        string? number,
        DateTime? issue,
        DateTime? expiration,
        string? authority = "Ashgabat",
        string? issuedCountry = "TURKEY",
        string? typeL = "P",
        string? mgCode = "") =>
        new(
            LegacyOid: oid,
            PassportNumber: number,
            TypeOfPassportL: typeL,
            MgCode: mgCode,
            IssueDate: issue,
            ExpirationDate: expiration,
            Authority: authority,
            LegacyIssuedCountry: issuedCountry,
            LegacyPersonOid: PersonOid,
            HasPassportCopy: false,
            PassportCopyByteLength: 0);

    [Fact]
    public void TryParseRawRow_ValidRow_ParsesFields()
    {
        var oid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var person = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = oid.ToString("D"),
            ["LegacyPersonOid"] = person.ToString("D"),
            ["PassportNumber"] = " P123 ",
            ["TypeOfPassportL"] = "P",
            ["MgCode"] = "11",
            ["PassportIssuedDate"] = "2020-01-15",
            ["PassportExpiringDate"] = "2030-01-15",
            ["PassportIssuedPlace"] = "Ashgabat",
            ["LegacyIssuedCountry"] = "TURKEY",
            ["HasPassportCopy"] = "1",
            ["PassportCopyByteLength"] = "42",
        };

        Assert.True(Visa2014PassportTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(oid, parsed.LegacyOid);
        Assert.Equal(person, parsed.LegacyPersonOid);
        Assert.Equal(" P123 ", parsed.PassportNumber);
        Assert.Equal(new DateTime(2020, 1, 15), parsed.IssueDate);
        Assert.Equal(new DateTime(2030, 1, 15), parsed.ExpirationDate);
        Assert.True(parsed.HasPassportCopy);
        Assert.Equal(42, parsed.PassportCopyByteLength);
    }

    [Fact]
    public void TryParseRawRow_MissingPersonOid_ReturnsFalse()
    {
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = Guid.NewGuid().ToString("D"),
            ["PassportNumber"] = "P1",
        };

        Assert.False(Visa2014PassportTransform.TryParseRawRow(row, out _));
    }

    [Fact]
    public void TransformRows_MissingRequiredFields_SkipsWithCombinedReason()
    {
        var catalogs = CountryCatalog();
        var batch = Visa2014PassportTransform.TransformRows(
            [
                Raw(Guid.NewGuid(), null, new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)),
                Raw(Guid.NewGuid(), "P-A", null, new DateTime(2030, 1, 1)),
                Raw(Guid.NewGuid(), "P-B", new DateTime(2020, 1, 1), null),
                Raw(Guid.NewGuid(), "P-C", new DateTime(2020, 1, 1), new DateTime(2030, 1, 1), authority: "  "),
            ],
            catalogs,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Equal(4, skipped.Count);
        Assert.All(skipped, s => Assert.Equal(
            "required_null:PassportNumber|IssueDate|ExpirationDate|Authority",
            s["_reason"]));
    }

    [Fact]
    public void TransformRows_InvalidDateRange_SkipsUnlessPermitSupplementMode()
    {
        var oid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var catalogs = CountryCatalog();
        var issue = new DateTime(2024, 6, 1);
        var expiration = new DateTime(2024, 5, 1);

        var normal = Visa2014PassportTransform.TransformRows(
            [Raw(oid, "PP-RANGE", issue, expiration)],
            catalogs,
            out var skipped,
            out _,
            out _);

        Assert.Empty(normal.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("invalid_date_range:ExpirationDate<=IssueDate", skipped[0]["_reason"]);

        var supplement = Visa2014PassportTransform.TransformRows(
            [Raw(oid, "PP-RANGE", issue, expiration)],
            catalogs,
            out var supplementSkipped,
            out _,
            out _,
            permitSupplementMode: true);

        Assert.Empty(supplementSkipped);
        Assert.Single(supplement.ImportRows);
        Assert.Equal(true, supplement.ImportRows[0]["_legacy_dateRangeCoerced"]);
        Assert.Equal(issue.AddDays(1), supplement.ImportRows[0]["ExpirationDate"]);
    }

    [Fact]
    public void TransformRows_SentinelPassportNumber_AppendsLegacyOidTail()
    {
        var oid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var catalogs = CountryCatalog();

        var batch = Visa2014PassportTransform.TransformRows(
            [Raw(oid, "AF000000000", new DateTime(2020, 1, 1), new DateTime(2030, 1, 1))],
            catalogs,
            out var skipped,
            out _,
            out _);

        Assert.Empty(skipped);
        Assert.Single(batch.ImportRows);
        Assert.Equal("AF000000000" + oid.ToString("N")[^8..], batch.ImportRows[0]["PassportNumber"]);
        Assert.Equal("P", batch.ImportRows[0]["PassportType"]);
        Assert.Equal("TUR", batch.ImportRows[0]["IssuedCountry"]);
    }

    [Fact]
    public void TransformRows_DuplicatePassportNumbers_KeepsCanonicalByMostRecentIssueDate()
    {
        var older = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var newer = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var catalogs = CountryCatalog();

        var batch = Visa2014PassportTransform.TransformRows(
            [
                Raw(older, "pp-100", new DateTime(2018, 1, 1), new DateTime(2028, 1, 1)),
                Raw(newer, "PP-100", new DateTime(2021, 1, 1), new DateTime(2031, 1, 1)),
            ],
            catalogs,
            out var skipped,
            out _,
            out var dedupeSummary);

        Assert.Empty(skipped);
        Assert.Equal(1, batch.DedupeMergedCount);
        Assert.Single(batch.ImportRows);
        Assert.Equal(newer, batch.ImportRows[0]["_legacyRowId"]);
        Assert.Equal("PP-100", batch.ImportRows[0]["PassportNumber"]);
        Assert.Single(dedupeSummary);
        Assert.Equal("PPN:PP-100", dedupeSummary[0]["_dedupeGroupId"]);
        Assert.Equal(newer, dedupeSummary[0]["canonical_legacyRowId"]);
    }

    [Fact]
    public void TransformRows_MissingIssuedCountry_SkipsWithRequiredNull()
    {
        var catalogs = CountryCatalog();
        var batch = Visa2014PassportTransform.TransformRows(
            [Raw(
                Guid.NewGuid(),
                "PP-NO-COUNTRY",
                new DateTime(2020, 1, 1),
                new DateTime(2030, 1, 1),
                issuedCountry: null)],
            catalogs,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("required_null:IssuedCountry", skipped[0]["_reason"]);
    }
}
