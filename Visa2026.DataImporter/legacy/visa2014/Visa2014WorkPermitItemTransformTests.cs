using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014WorkPermitItemTransformTests
{
    private static readonly Guid ItemOid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EmployeeOid = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PassportOid = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PositionOid = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid LetterOid = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static Visa2014WorkPermitItemRawRow Raw(
        Guid? employee = null,
        Guid? passport = null,
        Guid? position = null,
        Guid? letter = null,
        Guid? location = null,
        DateTime? start = null,
        DateTime? end = null,
        string? number = "WP-1",
        string? asNumber = "AS-1",
        Guid? oid = null) =>
        new(
            LegacyOid: oid ?? ItemOid,
            LegacyEmployeeOid: employee,
            LegacyPassportOid: passport,
            LegacyPositionOid: position,
            LegacyWorkPermitLetterOid: letter,
            LegacyWorkPermitLocationOid: location,
            StartDate: start,
            ExpirationDate: end,
            WorkPermitNumber: number,
            ASNumber: asNumber);

    private static Visa2014WorkPermitItemRawRow ValidRaw(Guid? oid = null) =>
        Raw(
            employee: EmployeeOid,
            passport: PassportOid,
            position: PositionOid,
            letter: LetterOid,
            start: new DateTime(2024, 1, 1),
            end: new DateTime(2024, 12, 31),
            oid: oid);

    [Fact]
    public void TryParseRawRow_ValidRow_ParsesGuidsAndDates()
    {
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = ItemOid.ToString("D"),
            ["EmployeeOid"] = EmployeeOid.ToString("D"),
            ["PassportOid"] = PassportOid.ToString("D"),
            ["PositionOid"] = PositionOid.ToString("D"),
            ["WorkPermitLetterOid"] = LetterOid.ToString("D"),
            ["StartDateOfWorkPermit"] = "2024-02-01",
            ["ExpiringDateOfWorkPermit"] = "2024-11-30",
            ["AppruvalNumber"] = "  WP-9 ",
            ["ASNumber"] = "AS-9",
        };

        Assert.True(Visa2014WorkPermitItemTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(ItemOid, parsed.LegacyOid);
        Assert.Equal(EmployeeOid, parsed.LegacyEmployeeOid);
        Assert.Equal(PassportOid, parsed.LegacyPassportOid);
        Assert.Equal(PositionOid, parsed.LegacyPositionOid);
        Assert.Equal(LetterOid, parsed.LegacyWorkPermitLetterOid);
        Assert.Equal(new DateTime(2024, 2, 1), parsed.StartDate);
        Assert.Equal(new DateTime(2024, 11, 30), parsed.ExpirationDate);
        Assert.Equal("  WP-9 ", parsed.WorkPermitNumber);
        Assert.Equal("AS-9", parsed.ASNumber);
    }

    [Fact]
    public void TryParseRawRow_MissingOid_ReturnsFalse()
    {
        var row = new Dictionary<string, string?>
        {
            ["EmployeeOid"] = EmployeeOid.ToString("D"),
        };

        Assert.False(Visa2014WorkPermitItemTransform.TryParseRawRow(row, out _));
    }

    [Fact]
    public void BuildExportRow_MissingEmployee_SkipsWithReason()
    {
        var export = Visa2014WorkPermitItemTransform.BuildExportRow(
            ValidRaw() with { LegacyEmployeeOid = null },
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            bitColumnNames: [],
            locationRows: new Dictionary<Guid, IReadOnlyDictionary<string, string?>>(),
            cancellationIndex: Visa2014LegacyDocumentCancellationIndex.Empty,
            out var skipReason,
            out _);

        Assert.Equal("missing_fk:Employee", skipReason);
        Assert.Equal("WorkPermit", export["_legacyTable"]);
        Assert.Null(export["Person"]);
    }

    [Fact]
    public void BuildExportRow_InvalidDateRange_Skips()
    {
        var export = Visa2014WorkPermitItemTransform.BuildExportRow(
            ValidRaw() with
            {
                StartDate = new DateTime(2024, 6, 1),
                ExpirationDate = new DateTime(2024, 1, 1),
            },
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            bitColumnNames: [],
            locationRows: new Dictionary<Guid, IReadOnlyDictionary<string, string?>>(),
            cancellationIndex: Visa2014LegacyDocumentCancellationIndex.Empty,
            out var skipReason,
            out _);

        Assert.Equal("invalid_date_range:ExpirationDate<=StartDate", skipReason);
    }

    [Fact]
    public void BuildExportRow_BlankWorkPermitNumber_Skips()
    {
        var export = Visa2014WorkPermitItemTransform.BuildExportRow(
            ValidRaw() with { WorkPermitNumber = "   " },
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            bitColumnNames: [],
            locationRows: new Dictionary<Guid, IReadOnlyDictionary<string, string?>>(),
            cancellationIndex: Visa2014LegacyDocumentCancellationIndex.Empty,
            out var skipReason,
            out _);

        Assert.Equal("required_null:WorkPermitNumber", skipReason);
        Assert.Null(export["WorkPermitNumber"]);
    }

    [Fact]
    public void BuildExportRow_UsesLetterOidAsWorkPermitHeader_AndTrimsScalars()
    {
        var export = Visa2014WorkPermitItemTransform.BuildExportRow(
            ValidRaw() with { WorkPermitNumber = " WP-trim ", ASNumber = " AS-trim " },
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            bitColumnNames: [],
            locationRows: new Dictionary<Guid, IReadOnlyDictionary<string, string?>>(),
            cancellationIndex: Visa2014LegacyDocumentCancellationIndex.Empty,
            out var skipReason,
            out _);

        Assert.Null(skipReason);
        Assert.Equal(LetterOid.ToString("D"), export["WorkPermit"]);
        Assert.Equal("WP-trim", export["WorkPermitNumber"]);
        Assert.Equal("AS-trim", export["ASNumber"]);
        Assert.Equal("2024-01-01", export["StartDate"]);
        Assert.Equal("2024-12-31", export["ExpirationDate"]);
        Assert.Equal(false, export["IsCancelled"]);
    }

    [Fact]
    public void BuildExportRow_FallsBackToItemOidWhenLetterMissing_AndMarksCancelled()
    {
        var index = Visa2014LegacyDocumentCancellationIndex.FromWorkPermitOidsForTests([ItemOid]);
        var export = Visa2014WorkPermitItemTransform.BuildExportRow(
            ValidRaw() with { LegacyWorkPermitLetterOid = null },
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            bitColumnNames: [],
            locationRows: new Dictionary<Guid, IReadOnlyDictionary<string, string?>>(),
            cancellationIndex: index,
            out var skipReason,
            out _);

        Assert.Null(skipReason);
        Assert.Equal(ItemOid.ToString("D"), export["WorkPermit"]);
        Assert.Equal(true, export["IsCancelled"]);
    }

    [Fact]
    public void TransformRows_PartitionsImportAndSkipped()
    {
        var batch = Visa2014WorkPermitItemTransform.TransformRows(
            [
                ValidRaw(),
                ValidRaw(oid: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")) with
                {
                    LegacyPassportOid = null,
                },
            ],
            catalogs: new Dictionary<string, Visa2014LookupCatalog>(),
            bitColumnNames: [],
            locationRows: new Dictionary<Guid, IReadOnlyDictionary<string, string?>>(),
            cancellationIndex: Visa2014LegacyDocumentCancellationIndex.Empty,
            out var skipped,
            out _);

        Assert.Single(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("missing_fk:Passport", skipped[0]["_skipReason"]);
        Assert.Equal(2, batch.LegacyRowCount);
    }
}
