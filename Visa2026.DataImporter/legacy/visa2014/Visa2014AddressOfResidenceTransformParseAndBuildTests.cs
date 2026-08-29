using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Covers AddressOfResidence SQL-row parse and import-row build edges
/// (distinct from site-address builders covered elsewhere).
/// </summary>
public sealed class Visa2014AddressOfResidenceTransformParseAndBuildTests
{
    private static readonly Guid LegacyOid = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb");
    private static readonly Guid PersonOid = Guid.Parse("cccccccc-4444-5555-6666-dddddddddddd");

    private static IReadOnlyDictionary<string, Visa2014LookupCatalog> RegionCatalog(
        params (string legacy, string target)[] pairs) =>
        new Dictionary<string, Visa2014LookupCatalog>(StringComparer.Ordinal)
        {
            ["Region"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "Region",
                TargetMatchProperty = "Code",
                UnmappedPolicy = "block_row",
                LegacyToTarget = pairs.ToDictionary(p => p.legacy, p => p.target, StringComparer.Ordinal),
            },
        };

    [Fact]
    public void TryParseRawRow_MissingOid_ReturnsFalse()
    {
        var row = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["LegacyPersonOid"] = PersonOid.ToString("D"),
            ["AddressLine"] = "Street 1",
        };

        Assert.False(Visa2014AddressOfResidenceTransform.TryParseRawRow(row, out _));
    }

    [Fact]
    public void TryParseRawRow_MissingPersonOid_ReturnsFalse()
    {
        var row = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Oid"] = LegacyOid.ToString("D"),
            ["AddressLine"] = "Street 1",
        };

        Assert.False(Visa2014AddressOfResidenceTransform.TryParseRawRow(row, out _));
    }

    [Fact]
    public void TryParseRawRow_ParsesScalarsAndExpiration()
    {
        var row = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Oid"] = LegacyOid.ToString("D"),
            ["LegacyPersonOid"] = PersonOid.ToString("D"),
            ["TypeOfDocument"] = "Patent",
            ["RegionMgCode"] = "AS",
            ["RegionName"] = "Aşgabat",
            ["CityMgCode"] = "AS69",
            ["CityName"] = "Aşgabat şäheri",
            ["AddressLine"] = "Köpetdag 12",
            ["ExpirationDate"] = "2026-12-31",
        };

        Assert.True(Visa2014AddressOfResidenceTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(LegacyOid, parsed.LegacyOid);
        Assert.Equal(PersonOid, parsed.LegacyPersonOid);
        Assert.Equal("Patent", parsed.DocumentType);
        Assert.Equal("AS", parsed.RegionMgCode);
        Assert.Equal("AS69", parsed.CityMgCode);
        Assert.Equal("Köpetdag 12", parsed.AddressLine);
        Assert.Equal(new DateTime(2026, 12, 31), parsed.ExpirationDate);
    }

    [Fact]
    public void TryParseRawRow_BlankMgCodesBecomeNull_InvalidExpirationNull()
    {
        var row = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Oid"] = LegacyOid.ToString("D"),
            ["LegacyPersonOid"] = PersonOid.ToString("D"),
            ["RegionMgCode"] = "  ",
            ["CityMgCode"] = "",
            ["ExpirationDate"] = "not-a-date",
        };

        Assert.True(Visa2014AddressOfResidenceTransform.TryParseRawRow(row, out var parsed));
        Assert.Null(parsed.RegionMgCode);
        Assert.Null(parsed.CityMgCode);
        Assert.Null(parsed.ExpirationDate);
    }

    [Fact]
    public void TryBuildImportRow_UnknownRegion_FailsWithUnmappedReason()
    {
        var catalogs = RegionCatalog(("AS", "AS"));
        var raw = new Visa2014AddressOfResidenceRawRow(
            LegacyOid,
            PersonOid,
            DocumentType: "Patent",
            RegionMgCode: "ZZ",
            RegionName: null,
            CityMgCode: "AS69",
            CityName: null,
            AddressLine: "Street 1",
            ExpirationDate: null);

        Assert.False(Visa2014AddressOfResidenceTransform.TryBuildImportRow(
            raw, catalogs, LegacyOid, out _, out var skipReason));
        Assert.StartsWith("Region:", skipReason);
    }

    [Fact]
    public void TryBuildImportRow_PatentPrivateHouse_SetsExpirationAndType()
    {
        var catalogs = RegionCatalog(("AS", "AS"));
        var expire = new DateTime(2027, 6, 15);
        var raw = new Visa2014AddressOfResidenceRawRow(
            LegacyOid,
            PersonOid,
            DocumentType: "Patent",
            RegionMgCode: "AS",
            RegionName: null,
            CityMgCode: "AS69",
            CityName: null,
            AddressLine: "Köpetdag etraby 5",
            ExpirationDate: expire);

        Assert.True(Visa2014AddressOfResidenceTransform.TryBuildImportRow(
            raw, catalogs, LegacyOid, out var importRow, out var skipReason));
        Assert.Null(skipReason);
        Assert.NotNull(importRow);
        Assert.Equal("PrivateHouse", importRow!["Type"]);
        Assert.Equal("Aşgabat şäheri", importRow["Region"]);
        Assert.Equal("Aşgabat şäheri", importRow["City"]);
        Assert.Equal("2027-06-15", importRow["ExpirationDate"]);
        Assert.Equal(LegacyOid.ToString(), importRow["_legacyRowId"]);
        Assert.Equal("AddressOfResidence", importRow["_legacyTable"]);
        Assert.Null(importRow["Lodging"]);
        Assert.Null(importRow["Hotel"]);
        Assert.Null(importRow["Hospital"]);
    }

    [Fact]
    public void TryBuildImportRow_MyhmanhanaHospitalLine_MapsHospitalType()
    {
        var catalogs = RegionCatalog(("AS", "AS"));
        var raw = new Visa2014AddressOfResidenceRawRow(
            LegacyOid,
            PersonOid,
            DocumentType: "myhmanhana",
            RegionMgCode: "AS",
            RegionName: null,
            CityMgCode: "AS69",
            CityName: null,
            AddressLine: "Aşgabat hassahana №3",
            ExpirationDate: new DateTime(2026, 1, 1));

        Assert.True(Visa2014AddressOfResidenceTransform.TryBuildImportRow(
            raw, catalogs, LegacyOid, out var importRow, out _));
        Assert.Equal("Hospital", importRow!["Type"]);
        // Non-PrivateHouse types do not keep expiration on the import row.
        Assert.Null(importRow["ExpirationDate"]);
        Assert.False(string.IsNullOrWhiteSpace(importRow["Hospital"] as string));
    }

    [Fact]
    public void TryBuildImportRow_UnknownDocumentType_MapsOther()
    {
        var catalogs = RegionCatalog(("MR", "MR"));
        var raw = new Visa2014AddressOfResidenceRawRow(
            LegacyOid,
            PersonOid,
            DocumentType: "SomethingElse",
            RegionMgCode: "MR",
            RegionName: null,
            CityMgCode: "MR19",
            CityName: null,
            AddressLine: "Mary şäheri 1",
            ExpirationDate: null);

        Assert.True(Visa2014AddressOfResidenceTransform.TryBuildImportRow(
            raw, catalogs, LegacyOid, out var importRow, out _));
        Assert.Equal("Other", importRow!["Type"]);
        Assert.Equal("Mary welaýaty", importRow["Region"]);
        Assert.Equal("Mary şäheri", importRow["City"]);
        Assert.False(string.IsNullOrWhiteSpace(importRow["OtherSite"] as string));
    }
}
