using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Covers migration / spouse helpers added for manual-only visa family outputs.
/// Kept separate from <see cref="VisaFamilyMemberLinesHelperTests"/> to avoid open coverage-PR file conflicts.
/// </summary>
public sealed class VisaFamilyMemberLinesHelperMigrationTests
{
    [Fact]
    public void FormatLinesFromFamilyMembers_SkipsIncompleteAndOrdersByName()
    {
        var spouse = new Person
        {
            FirstName = "Zeynep",
            LastName = "Yilmaz",
            DateOfBirth = new DateTime(1989, 10, 12, 0, 0, 0, DateTimeKind.Unspecified),
            Relationship = new Relationship { NameTm = "aýaly", ID = Guid.NewGuid() },
            Nationality = new Country { Code = "TUR", ID = Guid.NewGuid() }
        };
        var child = new Person
        {
            FirstName = "Ali",
            LastName = "Yilmaz",
            DateOfBirth = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            Relationship = new Relationship { NameTm = "ogoly", ID = Guid.NewGuid() },
            Nationality = new Country { Code = "TUR", ID = Guid.NewGuid() }
        };
        var incomplete = new Person
        {
            FirstName = "No",
            LastName = "Relationship",
            DateOfBirth = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
        };
        var noBirth = new Person
        {
            FirstName = "No",
            LastName = "Birth",
            Relationship = new Relationship { NameTm = "gyzy" }
        };
        var employee = new Person
        {
            FamilyMembers = new ObservableCollection<Person> { spouse, incomplete, child, noBirth }
        };

        var text = VisaFamilyMemberLinesHelper.FormatLinesFromFamilyMembers(employee);

        Assert.NotNull(text);
        var rows = VisaFamilyMemberLinesHelper.Parse(text);
        Assert.Equal(2, rows.Count);
        Assert.Equal("Ali Yilmaz", rows[0].FullName);
        Assert.Equal("ogoly", rows[0].RelationshipNameTm);
        Assert.Equal("TUR", rows[0].CountryCode);
        Assert.Equal("Zeynep Yilmaz", rows[1].FullName);
        Assert.Equal("aýaly", rows[1].RelationshipNameTm);
    }

    [Fact]
    public void FormatLinesFromFamilyMembers_NullOrEmptyCollection_ReturnsNull()
    {
        Assert.Null(VisaFamilyMemberLinesHelper.FormatLinesFromFamilyMembers(null));
        Assert.Null(VisaFamilyMemberLinesHelper.FormatLinesFromFamilyMembers(new Person()));
        Assert.Null(VisaFamilyMemberLinesHelper.FormatLinesFromFamilyMembers(
            new Person { FamilyMembers = new ObservableCollection<Person>() }));
    }

    [Theory]
    [InlineData("SPOUSE", true)]
    [InlineData("wife", true)]
    [InlineData("Husband", true)]
    [InlineData("adamsy", true)]
    [InlineData("eri", true)]
    [InlineData("gyzy", false)]
    [InlineData("", false)]
    public void IsSpouseRelationshipNameTm_MatchesKnownSpouseLabels(string name, bool expected)
    {
        Assert.Equal(expected, VisaFamilyMemberLinesHelper.IsSpouseRelationshipNameTm(name, objectSpace: null));
    }

    [Fact]
    public void IsSpouseRelationship_MatchesCodeOrLocalizedNames()
    {
        Assert.False(VisaFamilyMemberLinesHelper.IsSpouseRelationship(null));
        Assert.True(VisaFamilyMemberLinesHelper.IsSpouseRelationship(new Relationship { Code = "wife" }));
        Assert.True(VisaFamilyMemberLinesHelper.IsSpouseRelationship(new Relationship { Name = "Employee Spouse" }));
        Assert.True(VisaFamilyMemberLinesHelper.IsSpouseRelationship(new Relationship { NameTm = "aýaly" }));
        Assert.False(VisaFamilyMemberLinesHelper.IsSpouseRelationship(new Relationship { NameTm = "gyzy", Code = "DAUGHTER" }));
    }

    [Fact]
    public void FindSpouseLine_PrefersEnglishSpouseTokenWhenPresent()
    {
        const string manual =
            "Child One; 01.01.2010; gyzy; TUR\n" +
            "Partner Two; 02.02.1988; HUSBAND; TUR";

        var spouse = VisaFamilyMemberLinesHelper.FindSpouseLine(manual, objectSpace: null);

        Assert.NotNull(spouse);
        Assert.Equal("Partner Two", spouse.FullName);
    }

    [Fact]
    public void SplitFullNameForPdf_HandlesEmptySingleTokenAndTrailingSpace()
    {
        VisaFamilyMemberLinesHelper.SplitFullNameForPdf(null, out var firstNull, out var lastNull);
        Assert.Null(firstNull);
        Assert.Null(lastNull);

        VisaFamilyMemberLinesHelper.SplitFullNameForPdf("Madonna", out var firstOnly, out var lastOnly);
        Assert.Equal("Madonna", firstOnly);
        Assert.Null(lastOnly);

        VisaFamilyMemberLinesHelper.SplitFullNameForPdf("Trailing ", out var firstTrail, out var lastTrail);
        Assert.Equal("Trailing", firstTrail);
        Assert.Null(lastTrail);
    }
}
