using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014VisaTransformExportTests
{
    private static readonly Guid PassportOid = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Visa2014LegacyDocumentCancellationIndex EmptyCancellation =
        Visa2014LegacyDocumentCancellationIndex.Empty;

    private static IReadOnlyDictionary<string, Visa2014LookupCatalog> EmptyCatalogs() =>
        new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase);

    private static Visa2014VisaRawRow Raw(
        Guid oid,
        string? number,
        DateTime? issue,
        DateTime? start,
        DateTime? expiration,
        string? category = "Multiple",
        string? categoryMg = "",
        string? issuedPlace = "Ashgabat",
        string? typeL = "WP",
        string? mgCode = "11",
        bool isFamilyMember = false,
        string? asNumber = "PN-1") =>
        new(
            LegacyOid: oid,
            VisaNumber: number,
            TypeOfVisaL: typeL,
            MgCode: mgCode,
            CategoryOfVisaL: category,
            CategoryMgCode: categoryMg,
            IssuedPlaceOfVisaL: issuedPlace,
            IssueDate: issue,
            StartDate: start,
            ExpirationDate: expiration,
            LegacyPassportOid: PassportOid,
            AsNumber: asNumber,
            LegacyPersonInApplicationOid: null,
            IsFamilyMemberPerson: isFamilyMember,
            BzDasoguz: false,
            BzTagtabazar: false,
            BzSerhetabat: false,
            BzYoloten: false,
            BzFarap: false,
            BzGarabogaz: false,
            BzSarahs: false,
            BzEtrek: false,
            HasBorderZoneFk: false,
            HasVisaDocument: false,
            VisaDocumentByteLength: 0);

    [Fact]
    public void TryParseRawRow_ValidRow_ParsesFields()
    {
        var oid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var passport = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var pia = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = oid.ToString("D"),
            ["LegacyPassportOid"] = passport.ToString("D"),
            ["VisaNumber"] = " V-9 ",
            ["TypeOfVisaL"] = "WP",
            ["MgCode"] = "11",
            ["CategoryOfVisaL"] = "Multiple",
            ["CategoryMgCode"] = "2",
            ["IssuedPlaceOfVisaL"] = "Ashgabat",
            ["VisaIssuedDate"] = "2024-01-10",
            ["VisaStartDate"] = "2024-01-15",
            ["VisaEndDate"] = "2024-07-15",
            ["ASNumber"] = "  PN-77  ",
            ["LegacyPersonInApplicationOid"] = pia.ToString("D"),
            ["IsFamilyMemberPerson"] = "1",
            ["HasVisaDocument"] = "1",
            ["VisaDocumentByteLength"] = "99",
        };

        Assert.True(Visa2014VisaTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(oid, parsed.LegacyOid);
        Assert.Equal(passport, parsed.LegacyPassportOid);
        Assert.Equal(" V-9 ", parsed.VisaNumber);
        Assert.Equal(new DateTime(2024, 1, 10), parsed.IssueDate);
        Assert.Equal(new DateTime(2024, 1, 15), parsed.StartDate);
        Assert.Equal(new DateTime(2024, 7, 15), parsed.ExpirationDate);
        Assert.Equal("PN-77", parsed.AsNumber);
        Assert.Equal(pia, parsed.LegacyPersonInApplicationOid);
        Assert.True(parsed.IsFamilyMemberPerson);
        Assert.True(parsed.HasVisaDocument);
        Assert.Equal(99, parsed.VisaDocumentByteLength);
    }

    [Fact]
    public void TryParseRawRow_MissingPassportOid_ReturnsFalse()
    {
        Assert.False(Visa2014VisaTransform.TryParseRawRow(
            new Dictionary<string, string?>
            {
                ["Oid"] = Guid.NewGuid().ToString("D"),
                ["VisaNumber"] = "V1",
            },
            out _));
    }

    [Fact]
    public void TransformRows_MissingRequiredFields_SkipsWithReasons()
    {
        var batch = Visa2014VisaTransform.TransformRows(
            [
                Raw(Guid.NewGuid(), null, new DateTime(2024, 1, 1), new DateTime(2024, 1, 2), new DateTime(2024, 7, 1)),
                Raw(Guid.NewGuid(), "V-A", null, new DateTime(2024, 1, 2), new DateTime(2024, 7, 1)),
                Raw(Guid.NewGuid(), "V-B", new DateTime(2024, 1, 1), null, new DateTime(2024, 7, 1)),
                Raw(Guid.NewGuid(), "V-C", new DateTime(2024, 1, 1), new DateTime(2024, 1, 2), null),
            ],
            EmptyCatalogs(),
            EmptyCancellation,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Equal(4, skipped.Count);
        Assert.All(skipped, s => Assert.Equal(
            "required_null:VisaNumber|IssueDate|StartDate|ExpirationDate",
            s["_reason"]));
    }

    [Fact]
    public void TransformRows_InvalidDateRange_Skips()
    {
        var batch = Visa2014VisaTransform.TransformRows(
            [Raw(
                Guid.NewGuid(),
                "V-RANGE",
                new DateTime(2024, 1, 1),
                new DateTime(2024, 6, 1),
                new DateTime(2024, 5, 1))],
            EmptyCatalogs(),
            EmptyCancellation,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("invalid_date_range:ExpirationDate<=StartDate", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_MissingCategory_Skips()
    {
        var batch = Visa2014VisaTransform.TransformRows(
            [Raw(
                Guid.NewGuid(),
                "V-CAT",
                new DateTime(2024, 1, 1),
                new DateTime(2024, 1, 2),
                new DateTime(2024, 7, 1),
                category: "  ")],
            EmptyCatalogs(),
            EmptyCancellation,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("required_null:VisaCategory", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_MissingIssuedPlace_Skips()
    {
        var batch = Visa2014VisaTransform.TransformRows(
            [Raw(
                Guid.NewGuid(),
                "V-PLACE",
                new DateTime(2024, 1, 1),
                new DateTime(2024, 1, 2),
                new DateTime(2024, 7, 1),
                issuedPlace: null)],
            EmptyCatalogs(),
            EmptyCancellation,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("required_null:VisaIssuedPlace", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_SkipLabelIssuedPlace_SkipsAsUnmapped()
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase)
        {
            ["VisaIssuedPlace"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "VisaIssuedPlace",
                TargetMatchProperty = "Name",
                UnmappedPolicy = "block_row",
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Ashgabat"] = "Ashgabat",
                },
            },
        };

        var batch = Visa2014VisaTransform.TransformRows(
            [Raw(
                Guid.NewGuid(),
                "V-SKIP-PLACE",
                new DateTime(2024, 1, 1),
                new DateTime(2024, 1, 2),
                new DateTime(2024, 7, 1),
                issuedPlace: "London")],
            catalogs,
            EmptyCancellation,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("unmapped_lookup:VisaIssuedPlace:London", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_SentinelVisaNumber_AppendsLegacyOidTail_AndDefaultsVisaType()
    {
        var oid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var batch = Visa2014VisaTransform.TransformRows(
            [Raw(
                oid,
                "AFV0000000",
                new DateTime(2024, 1, 1),
                new DateTime(2024, 1, 2),
                new DateTime(2024, 7, 1),
                isFamilyMember: false)],
            EmptyCatalogs(),
            EmptyCancellation,
            out var skipped,
            out _,
            out _);

        Assert.Empty(skipped);
        Assert.Single(batch.ImportRows);
        Assert.Equal("AFV0000000" + oid.ToString("N")[^8..], batch.ImportRows[0]["VisaNumber"]);
        Assert.Equal("WP", batch.ImportRows[0]["VisaType"]);
        Assert.Equal("Multiple", batch.ImportRows[0]["VisaCategory"]);
        Assert.Equal("Ashgabat", batch.ImportRows[0]["VisaIssuedPlace"]);
        Assert.Equal("PN-1", batch.ImportRows[0]["ProcessNumber"]);
        Assert.Equal(false, batch.ImportRows[0]["IsCancelled"]);
        Assert.Equal("Ýok", batch.ImportRows[0]["BorderZoneLocation"]);
    }

    [Fact]
    public void TransformRows_FamilyMemberPerson_ForcesFmVisaType()
    {
        var batch = Visa2014VisaTransform.TransformRows(
            [Raw(
                Guid.NewGuid(),
                "V-FM",
                new DateTime(2024, 1, 1),
                new DateTime(2024, 1, 2),
                new DateTime(2024, 7, 1),
                isFamilyMember: true)],
            EmptyCatalogs(),
            EmptyCancellation,
            out var skipped,
            out _,
            out _);

        Assert.Empty(skipped);
        Assert.Single(batch.ImportRows);
        Assert.Equal("FM", batch.ImportRows[0]["VisaType"]);
        Assert.Equal("family_member->FM", batch.ImportRows[0]["_legacy_VisaTypePersonOverride"]);
    }

    [Fact]
    public void TransformRows_DuplicateVisaNumbers_KeepsCanonicalByMostRecentEndDate()
    {
        var olderEnd = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var newerEnd = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var batch = Visa2014VisaTransform.TransformRows(
            [
                Raw(
                    olderEnd,
                    "v-100",
                    new DateTime(2023, 1, 1),
                    new DateTime(2023, 1, 2),
                    new DateTime(2023, 7, 1)),
                Raw(
                    newerEnd,
                    "V-100",
                    new DateTime(2024, 1, 1),
                    new DateTime(2024, 1, 2),
                    new DateTime(2024, 12, 1)),
            ],
            EmptyCatalogs(),
            EmptyCancellation,
            out var skipped,
            out _,
            out var dedupeSummary);

        Assert.Empty(skipped);
        Assert.Equal(1, batch.DedupeMergedCount);
        Assert.Single(batch.ImportRows);
        Assert.Equal(newerEnd, batch.ImportRows[0]["_legacyRowId"]);
        Assert.Equal("V-100", batch.ImportRows[0]["VisaNumber"]);
        Assert.Single(dedupeSummary);
        Assert.Equal("VNO:V-100", dedupeSummary[0]["_dedupeGroupId"]);
        Assert.Equal(newerEnd, dedupeSummary[0]["canonical_legacyRowId"]);
    }
}
