using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class VisaFamilyMemberLinesHelperMergeOptionsTests
{
    [Theory]
    [InlineData("1", true)]
    [InlineData(" 1 ", true)]
    [InlineData("2", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsLegacySingleMaritalStatus_MatchesLegacyStatusIntOne(string? legacy, bool expected)
    {
        Assert.Equal(expected, VisaFamilyMemberLinesHelper.IsLegacySingleMaritalStatus(legacy));
    }

    [Fact]
    public void MergeRelationshipOptionsForRow_AddsMissingName()
    {
        var baseOptions = new[]
        {
            new RelationshipLookupItem { Oid = Guid.Parse("11111111-1111-1111-1111-111111111111"), NameTm = "gyzy" },
        };
        var row = new VisaFamilyMemberLineDto
        {
            RelationshipNameTm = " aýaly ",
            RelationshipOid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        };

        var merged = VisaFamilyMemberLinesHelper.MergeRelationshipOptionsForRow(baseOptions, row);

        Assert.Equal(2, merged.Count);
        Assert.Contains(merged, o => o.NameTm == "aýaly" && o.Oid == row.RelationshipOid);
    }

    [Fact]
    public void MergeRelationshipOptionsForRow_DoesNotDuplicateExistingIgnoreCase()
    {
        var baseOptions = new[]
        {
            new RelationshipLookupItem { Oid = Guid.NewGuid(), NameTm = "Aýaly" },
        };
        var row = new VisaFamilyMemberLineDto { RelationshipNameTm = "aýaly" };

        var merged = VisaFamilyMemberLinesHelper.MergeRelationshipOptionsForRow(baseOptions, row);

        Assert.Same(baseOptions, merged);
        Assert.Single(merged);
    }

    [Fact]
    public void MergeRelationshipOptionsForRow_NullOrBlank_ReturnsBase()
    {
        var baseOptions = new[]
        {
            new RelationshipLookupItem { Oid = Guid.NewGuid(), NameTm = "gyzy" },
        };

        Assert.Same(baseOptions, VisaFamilyMemberLinesHelper.MergeRelationshipOptionsForRow(baseOptions, null));
        Assert.Same(
            baseOptions,
            VisaFamilyMemberLinesHelper.MergeRelationshipOptionsForRow(
                baseOptions,
                new VisaFamilyMemberLineDto { RelationshipNameTm = "  " }));
    }

    [Fact]
    public void MergeCountryOptionsForRow_AddsMissingCode()
    {
        var baseOptions = new[]
        {
            new CountryLookupItem { Oid = Guid.NewGuid(), Code = "TUR", NameTm = "Türkiýe" },
        };
        var row = new VisaFamilyMemberLineDto
        {
            CountryCode = " TKM ",
            CountryOid = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        };

        var merged = VisaFamilyMemberLinesHelper.MergeCountryOptionsForRow(baseOptions, row);

        Assert.Equal(2, merged.Count);
        Assert.Contains(merged, o => o.Code == "TKM" && o.Oid == row.CountryOid && o.NameTm == "TKM");
    }

    [Fact]
    public void MergeCountryOptionsForRow_DoesNotDuplicateExistingIgnoreCase()
    {
        var baseOptions = new[]
        {
            new CountryLookupItem { Oid = Guid.NewGuid(), Code = "tur", NameTm = "Türkiýe" },
        };
        var row = new VisaFamilyMemberLineDto { CountryCode = "TUR" };

        var merged = VisaFamilyMemberLinesHelper.MergeCountryOptionsForRow(baseOptions, row);

        Assert.Same(baseOptions, merged);
        Assert.Single(merged);
    }

    [Fact]
    public void ApplyRelationshipAndCountrySelection_ClearAndSet()
    {
        var row = new VisaFamilyMemberLineDto
        {
            RelationshipOid = Guid.NewGuid(),
            RelationshipNameTm = "aýaly",
            CountryOid = Guid.NewGuid(),
            CountryCode = "TUR",
        };

        VisaFamilyMemberLinesHelper.ApplyRelationshipSelection(row, relationship: null);
        VisaFamilyMemberLinesHelper.ApplyCountrySelection(row, country: null);

        Assert.Null(row.RelationshipOid);
        Assert.Equal("aýaly", row.RelationshipNameTm);
        Assert.Null(row.CountryOid);
        Assert.Equal("TUR", row.CountryCode);
    }

    [Fact]
    public void LoadOptions_NullObjectSpace_ReturnsEmpty()
    {
        Assert.Empty(VisaFamilyMemberLinesHelper.LoadRelationshipOptions(null));
        Assert.Empty(VisaFamilyMemberLinesHelper.LoadCountryOptions(null));
    }
}
