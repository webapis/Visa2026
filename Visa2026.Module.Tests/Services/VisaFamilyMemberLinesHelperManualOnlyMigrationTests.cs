using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Covers manual-only visa family APIs added when master-data FamilyMembers fallback was removed
/// (FormatLinesFromFamilyMembers migration path, spouse detection, PDF name split, selection apply).
/// </summary>
public sealed class VisaFamilyMemberLinesHelperManualOnlyMigrationTests
{
    [Fact]
    public void FormatLinesFromFamilyMembers_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(VisaFamilyMemberLinesHelper.FormatLinesFromFamilyMembers(null));
        Assert.Null(VisaFamilyMemberLinesHelper.FormatLinesFromFamilyMembers(new Person { FamilyMembers = null! }));
        Assert.Null(VisaFamilyMemberLinesHelper.FormatLinesFromFamilyMembers(new Person
        {
            FamilyMembers = new ObservableCollection<Person>(),
        }));
    }

    [Fact]
    public void FormatLinesFromFamilyMembers_SkipsMissingRelationshipOrBirthDate_OrdersByName()
    {
        var spouseRel = new Relationship { ID = Guid.NewGuid(), NameTm = "aýaly" };
        var childRel = new Relationship { ID = Guid.NewGuid(), NameTm = "gyzy" };
        var tur = new Country { ID = Guid.NewGuid(), Code = "TUR" };

        var employee = new Person
        {
            FamilyMembers = new ObservableCollection<Person>
            {
                new Person
                {
                    FirstName = "Zeynep",
                    LastName = "Yılmaz",
                    DateOfBirth = new DateTime(2012, 3, 26),
                    Relationship = childRel,
                    Nationality = tur,
                },
                new Person
                {
                    FirstName = "Ayşe",
                    LastName = "Yılmaz",
                    DateOfBirth = new DateTime(1989, 10, 12),
                    Relationship = spouseRel,
                    Nationality = tur,
                },
                new Person
                {
                    FirstName = "NoRel",
                    LastName = "Skip",
                    DateOfBirth = new DateTime(2000, 1, 1),
                    Relationship = null,
                    Nationality = tur,
                },
                new Person
                {
                    FirstName = "NoDob",
                    LastName = "Skip",
                    DateOfBirth = default,
                    Relationship = childRel,
                    Nationality = tur,
                },
            },
        };

        var text = VisaFamilyMemberLinesHelper.FormatLinesFromFamilyMembers(employee);

        Assert.NotNull(text);
        var lines = text!.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("Ayşe Yılmaz; 12.10.1989; aýaly; TUR", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("Zeynep Yılmaz; 26.03.2012; gyzy; TUR", lines[1], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("aýaly", true)]
    [InlineData("ADAMSY", true)]
    [InlineData("eri", true)]
    [InlineData("SPOUSE", true)]
    [InlineData("Wife", true)]
    [InlineData("husband", true)]
    [InlineData("gyzy", false)]
    public void IsSpouseRelationshipNameTm_MatchesKnownPatterns(string? nameTm, bool expected)
    {
        Assert.Equal(expected, VisaFamilyMemberLinesHelper.IsSpouseRelationshipNameTm(nameTm, objectSpace: null));
    }

    [Fact]
    public void IsSpouseRelationship_MatchesCodeNameAndNameTm()
    {
        Assert.False(VisaFamilyMemberLinesHelper.IsSpouseRelationship(null));

        Assert.True(VisaFamilyMemberLinesHelper.IsSpouseRelationship(new Relationship { Code = "SPOUSE" }));
        Assert.True(VisaFamilyMemberLinesHelper.IsSpouseRelationship(new Relationship { Code = "wife" }));
        Assert.True(VisaFamilyMemberLinesHelper.IsSpouseRelationship(new Relationship { Name = "Employee Spouse" }));
        Assert.True(VisaFamilyMemberLinesHelper.IsSpouseRelationship(new Relationship { NameTm = "aýaly" }));
        Assert.False(VisaFamilyMemberLinesHelper.IsSpouseRelationship(new Relationship
        {
            Code = "CHILD",
            Name = "Daughter",
            NameTm = "gyzy",
        }));
    }

    [Fact]
    public void FindSpouseLine_ReturnsNullWhenNoSpouseRow()
    {
        const string manual = "Child One; 01.01.2010; gyzy; TUR";
        Assert.Null(VisaFamilyMemberLinesHelper.FindSpouseLine(manual, objectSpace: null));
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", null, null)]
    [InlineData("   ", null, null)]
    [InlineData("Madonna", "Madonna", null)]
    [InlineData("John ", "John", null)]
    [InlineData("  Jane Doe  ", "Jane", "Doe")]
    public void SplitFullNameForPdf_HandlesEdgeCases(string? fullName, string? expectedFirst, string? expectedLast)
    {
        VisaFamilyMemberLinesHelper.SplitFullNameForPdf(fullName, out var first, out var last);
        Assert.Equal(expectedFirst, first);
        Assert.Equal(expectedLast, last);
    }

    [Fact]
    public void ApplyRelationshipSelection_ClearsOrSetsOidAndNameTm()
    {
        var row = new VisaFamilyMemberLineDto
        {
            RelationshipOid = Guid.NewGuid(),
            RelationshipNameTm = "stale",
        };

        VisaFamilyMemberLinesHelper.ApplyRelationshipSelection(row, relationship: null);
        Assert.Null(row.RelationshipOid);
        Assert.Equal("stale", row.RelationshipNameTm);

        var relId = Guid.NewGuid();
        VisaFamilyMemberLinesHelper.ApplyRelationshipSelection(
            row,
            new Relationship { ID = relId, NameTm = "ogly", Name = "Son" });
        Assert.Equal(relId, row.RelationshipOid);
        Assert.Equal("ogly", row.RelationshipNameTm);
    }

    [Fact]
    public void ApplyCountrySelection_ClearsOrSetsOidAndCode()
    {
        var row = new VisaFamilyMemberLineDto
        {
            CountryOid = Guid.NewGuid(),
            CountryCode = "XXX",
        };

        VisaFamilyMemberLinesHelper.ApplyCountrySelection(row, country: null);
        Assert.Null(row.CountryOid);
        Assert.Equal("XXX", row.CountryCode);

        var countryId = Guid.NewGuid();
        VisaFamilyMemberLinesHelper.ApplyCountrySelection(
            row,
            new Country { ID = countryId, Code = " TUR " });
        Assert.Equal(countryId, row.CountryOid);
        Assert.Equal("TUR", row.CountryCode);
    }

    [Fact]
    public void FormatVisaPdfMaritalFamilyBlockFromRows_PutsCountryOnlyOnLastSegment()
    {
        var rows = new[]
        {
            new VisaFamilyMemberLineDto
            {
                FullName = "Ayşe Yılmaz",
                BirthDate = new DateTime(1989, 10, 12),
                RelationshipNameTm = "aýaly",
                CountryCode = "TUR",
            },
            new VisaFamilyMemberLineDto
            {
                FullName = "Zeynep Yılmaz",
                BirthDate = new DateTime(2012, 3, 26),
                RelationshipNameTm = "gyzy",
                CountryCode = "TUR",
            },
        };

        var block = VisaFamilyMemberLinesHelper.FormatVisaPdfMaritalFamilyBlockFromRows(rows);

        Assert.Equal(
            "AÝALY Ayşe Yılmaz 12.10.1989, GYZY Zeynep Yılmaz 26.03.2012 TUR.",
            block);
    }
}
