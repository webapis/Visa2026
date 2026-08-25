using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014EducationTransformTests
{
    private static readonly Guid EducationOid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PersonOid = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static IReadOnlyDictionary<string, Visa2014LookupCatalog> Catalogs(
        string institutionPolicy = "skip_row",
        string countryPolicy = "skip_row",
        string specialtyPolicy = "skip_row") =>
        new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase)
        {
            ["EducationLevel"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "EducationLevel",
                TargetMatchProperty = "Name",
                UnmappedPolicy = "use_default",
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Bachelor:1"] = "Bachelor",
                },
            },
            ["EducationInstitution"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "EducationInstitution",
                TargetMatchProperty = "Name",
                UnmappedPolicy = institutionPolicy,
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["MIT"] = "Mit",
                },
            },
            ["Country"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "Country",
                TargetMatchProperty = "Code",
                UnmappedPolicy = countryPolicy,
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["USA"] = "USA",
                },
            },
            ["Specialty"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "Specialty",
                TargetMatchProperty = "Name",
                UnmappedPolicy = specialtyPolicy,
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["CS"] = "ComputerScience",
                },
            },
        };

    private static Visa2014EducationRawRow Raw(
        string? levelTitle = "Bachelor",
        string? levelCode = "1",
        string? institution = "MIT",
        string? countryMg = "USA",
        string? specialty = "CS",
        DateTime? end = null) =>
        new(
            LegacyOid: EducationOid,
            TitleOfEducationLevel: levelTitle,
            EducationLevelMgCode: levelCode,
            TitleOfInstitution: institution,
            CountryMgCode: countryMg,
            CountryName: null,
            CountryNameL: null,
            TitleOfSpeciality: specialty,
            EducationEndDate: end ?? new DateTime(2018, 6, 15),
            LegacyPersonOid: PersonOid);

    [Fact]
    public void TryParseRawRow_ValidRow_RequiresOidAndPerson()
    {
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = EducationOid.ToString("D"),
            ["LegacyPersonOid"] = PersonOid.ToString("D"),
            ["TitleOfEducationLevel"] = "Master",
            ["EducationLevelMgCode"] = "2",
            ["TitleOfIEducationInstitution"] = "Oxford",
            ["EducationCountryCode"] = "GBR-UK",
            ["TitleOfSpeciality"] = "Law",
            ["EducationEndDate"] = "2019-07-01",
        };

        Assert.True(Visa2014EducationTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(EducationOid, parsed.LegacyOid);
        Assert.Equal(PersonOid, parsed.LegacyPersonOid);
        Assert.Equal("Master", parsed.TitleOfEducationLevel);
        Assert.Equal("Oxford", parsed.TitleOfInstitution);
        Assert.Equal("GBR-UK", parsed.CountryMgCode);
        Assert.Equal(new DateTime(2019, 7, 1), parsed.EducationEndDate);
    }

    [Fact]
    public void TryParseRawRow_MissingPerson_ReturnsFalse()
    {
        Assert.False(Visa2014EducationTransform.TryParseRawRow(
            new Dictionary<string, string?> { ["Oid"] = EducationOid.ToString("D") },
            out _));
    }

    [Fact]
    public void BuildExportRow_ValidLookups_MapsTargetsAndGraduationYear()
    {
        var export = Visa2014EducationTransform.BuildExportRow(
            Raw(),
            Catalogs(),
            out var skipReason,
            out var unmapped);

        Assert.Null(skipReason);
        Assert.Empty(unmapped);
        Assert.Equal("Bachelor", export["EducationLevel"]);
        Assert.Equal("Mit", export["EducationInstitution"]);
        Assert.Equal("USA", export["EducationCountry"]);
        Assert.Equal("ComputerScience", export["Specialty"]);
        Assert.Equal("2018", export["GraduationYear"]);
        Assert.Equal(PersonOid.ToString("D"), export["Person"]);
        Assert.Equal("Bachelor:1", export["_legacy_EducationLevelComposite"]);
    }

    [Fact]
    public void BuildExportRow_UnknownEducationLevel_DefaultsSpecialSecondary()
    {
        var export = Visa2014EducationTransform.BuildExportRow(
            Raw(levelTitle: "Unknown", levelCode: "99"),
            Catalogs(),
            out var skipReason,
            out var unmapped);

        Assert.Null(skipReason);
        Assert.Equal("SpecialSecondary", export["EducationLevel"]);
        Assert.Contains(unmapped, u => u.Contains("EducationLevel", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildExportRow_MissingInstitution_Skips()
    {
        var export = Visa2014EducationTransform.BuildExportRow(
            Raw(institution: " "),
            Catalogs(),
            out var skipReason,
            out _);

        Assert.Equal("required_null:EducationInstitution", skipReason);
        Assert.Null(export["EducationInstitution"]);
    }

    [Fact]
    public void BuildExportRow_UnmappedCountryWithSkipPolicy_Skips()
    {
        var export = Visa2014EducationTransform.BuildExportRow(
            Raw(countryMg: "ZZZ"),
            Catalogs(),
            out var skipReason,
            out var unmapped);

        Assert.Equal("unmapped_lookup:Country:ZZZ", skipReason);
        Assert.Contains(unmapped, u => u.Contains("Country", StringComparison.Ordinal));
    }

    [Fact]
    public void TransformRows_PartitionsSkippedLookupFailures()
    {
        var batch = Visa2014EducationTransform.TransformRows(
            [
                Raw(),
                Raw(specialty: "UnknownSpec"),
            ],
            Catalogs(),
            out var skipped,
            out var unmappedDistinct,
            out _);

        Assert.Single(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("unmapped_lookup:Specialty:UnknownSpec", skipped[0]["_skipReason"]);
        Assert.NotEmpty(unmappedDistinct);
        Assert.Equal(2, batch.LegacyRowCount);
    }
}
