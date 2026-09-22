using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014PersonTransformTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    [InlineData("...")]
    [InlineData(".-.-")]
    public void IsDashDotPlaceholderPersonalNumber_TrueForBlankAndDashDotOnly(string? raw)
    {
        Assert.True(Visa2014PersonTransform.IsDashDotPlaceholderPersonalNumber(raw));
    }

    [Theory]
    [InlineData("56872306030")]
    [InlineData("A1")]
    [InlineData("--x--")]
    public void IsDashDotPlaceholderPersonalNumber_FalseWhenAlphanumericPresent(string raw)
    {
        Assert.False(Visa2014PersonTransform.IsDashDotPlaceholderPersonalNumber(raw));
    }

    [Theory]
    [InlineData(null, "0")]
    [InlineData("---", "0")]
    [InlineData("  56872306030  ", "56872306030")]
    public void NormalizePersonalNumber_PlaceholderToZeroElseTrim(string? raw, string expected)
    {
        Assert.Equal(expected, Visa2014PersonTransform.NormalizePersonalNumber(raw));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("0", true)]
    [InlineData("56872306030", false)]
    public void IsSentinelPersonalNumber_RecognizesEmptyAndZero(string normalized, bool expected)
    {
        Assert.Equal(expected, Visa2014PersonTransform.IsSentinelPersonalNumber(normalized));
    }

    [Fact]
    public void BuildDuplicatePersonalNumber_AppendsDubSuffix()
    {
        Assert.Equal(
            "56872306030_dub2",
            Visa2014PersonTransform.BuildDuplicatePersonalNumber("56872306030", 2));
    }

    [Fact]
    public void BuildIdentityDedupeKey_RequiresFirstLastDob()
    {
        var complete = Person(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "John",
            "Doe",
            new DateTime(1990, 1, 2),
            personalNumber: "0");

        Assert.Equal("JOHN|DOE|1990-01-02", Visa2014PersonTransform.BuildIdentityDedupeKey(complete));

        Assert.Equal(
            "",
            Visa2014PersonTransform.BuildIdentityDedupeKey(
                Person(Guid.NewGuid(), "John", null, new DateTime(1990, 1, 2), "0")));
    }

    [Fact]
    public void TransformRows_MissingRequiredFields_Skipped()
    {
        var raw = Person(Guid.NewGuid(), firstName: null, lastName: "Doe", birthDate: null, personalNumber: "1");
        var catalogs = EmptyCatalogs();

        var batch = Visa2014PersonTransform.TransformRows(
            [raw], catalogs, out var skipped, out _, out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("required_null:FirstName|LastName|DateOfBirth", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_DuplicatePersonalNumber_SuffixesLaterRows()
    {
        var keep = Person(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "Ada",
            "Lovelace",
            new DateTime(1815, 12, 10),
            "56872306030",
            middleName: "Extra",
            hasPhoto: true);
        var dup = Person(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            "Ada",
            "Clone",
            new DateTime(1990, 1, 1),
            "56872306030");

        var batch = Visa2014PersonTransform.TransformRows(
            [keep, dup], EmptyCatalogs(), out var skipped, out _, out var dedupeSummary);

        Assert.Empty(skipped);
        Assert.Equal(2, batch.ImportRows.Count);
        Assert.Equal(1, batch.DedupeMergedCount);

        var byOid = batch.ImportRows.ToDictionary(r => (Guid)r["_legacyRowId"]!);
        Assert.Equal("56872306030", byOid[keep.LegacyOid]["PersonalNumber"]);
        Assert.Equal("import", byOid[keep.LegacyOid]["_importAction"]);
        Assert.Equal("56872306030_dub1", byOid[dup.LegacyOid]["PersonalNumber"]);
        Assert.Equal("duplicate_suffix", byOid[dup.LegacyOid]["_importAction"]);

        Assert.Single(dedupeSummary);
        Assert.Equal("PN:56872306030", dedupeSummary[0]["_dedupeGroupId"]);
    }

    [Fact]
    public void TransformRows_SentinelIdentityDuplicates_GetDubSuffixOnZero()
    {
        var keep = Person(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "Asim",
            "ANUL",
            new DateTime(1958, 4, 4),
            "---",
            middleName: "M",
            hasPhoto: true);
        var dup = Person(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            "Asim",
            "ANUL",
            new DateTime(1958, 4, 4),
            "...");

        var batch = Visa2014PersonTransform.TransformRows(
            [keep, dup], EmptyCatalogs(), out _, out _, out var dedupeSummary);

        var byOid = batch.ImportRows.ToDictionary(r => (Guid)r["_legacyRowId"]!);
        Assert.Equal("0", byOid[keep.LegacyOid]["PersonalNumber"]);
        Assert.Equal("0_dub1", byOid[dup.LegacyOid]["PersonalNumber"]);
        Assert.Equal("duplicate_suffix", byOid[dup.LegacyOid]["_importAction"]);
        Assert.Contains(dedupeSummary, s => Equals(s["_dedupeGroupId"], "ID:ASIM|ANUL|1958-04-04"));
    }

    private static Dictionary<string, Visa2014LookupCatalog> EmptyCatalogs() =>
        new(StringComparer.Ordinal);

    private static Visa2014PersonRawRow Person(
        Guid oid,
        string? firstName,
        string? lastName,
        DateTime? birthDate,
        string? personalNumber,
        string? middleName = null,
        bool hasPhoto = false) =>
        new(
            LegacyOid: oid,
            FirstName: firstName,
            LastName: lastName,
            MiddleName: middleName,
            BirthDate: birthDate,
            BirthPlace: null,
            LegacyBirthCountry: null,
            ForeignAddress: null,
            LegacyForeignAddressCountry: null,
            LegacyGender: null,
            IsEmployee: true,
            IsFamilyMember: false,
            LegacyEmployeeOid: null,
            LegacyRelationship: null,
            LegacyProjectContract: null,
            LegacyMaritalStatusStatus: null,
            LegacyMaritalStatusText: null,
            ActivePerson: true,
            HasPhoto: hasPhoto,
            PhotoByteLength: hasPhoto ? 10 : 0,
            PhotoSha256: hasPhoto ? "abc" : null,
            RawPersonalNumber: personalNumber,
            LegacyNationality: null,
            LegacySubcontractorName: null);
}
