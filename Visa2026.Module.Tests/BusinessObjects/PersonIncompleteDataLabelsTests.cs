using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public sealed class PersonIncompleteDataLabelsTests
{
    [Fact]
    public void ChartOrder_MatchesNineStableEnglishLabels()
    {
        Assert.Equal(
            new[]
            {
                PersonIncompleteDataLabels.PersonalData,
                PersonIncompleteDataLabels.Passport,
                PersonIncompleteDataLabels.Cv,
                PersonIncompleteDataLabels.Photo,
                PersonIncompleteDataLabels.Education,
                PersonIncompleteDataLabels.Medical,
                PersonIncompleteDataLabels.Address,
                PersonIncompleteDataLabels.FamilyDocs,
                PersonIncompleteDataLabels.Other,
            },
            PersonIncompleteDataLabels.ChartOrder);
    }

    [Fact]
    public void FormatMissingAreas_NoFlags_ReturnsEmpty()
    {
        Assert.Equal(
            string.Empty,
            PersonIncompleteDataLabels.FormatMissingAreas(
                false, false, false, false, false, false, false, false, false));
    }

    [Fact]
    public void FormatMissingAreas_SelectedFlags_JoinsInChartOrder()
    {
        var formatted = PersonIncompleteDataLabels.FormatMissingAreas(
            personalData: true,
            passport: false,
            cv: true,
            photo: false,
            education: false,
            medical: true,
            address: false,
            familyDocs: false,
            other: true);

        Assert.Equal("Personal data, CV, Medical, Other", formatted);
    }

    [Theory]
    [InlineData(PersonRecordRole.Employee, "Employee")]
    [InlineData(PersonRecordRole.FamilyMember, "Family Member")]
    [InlineData(PersonRecordRole.TemporaryVisitor, "Temporary Visitor")]
    public void PersonRoleLabel_KnownRoles_UseStableEnglish(PersonRecordRole role, string expected)
    {
        Assert.Equal(expected, PersonIncompleteDataLabels.PersonRoleLabel(role));
    }

    [Fact]
    public void FormatMarked_NullDate_ReturnsTrimmedBylineOrEmpty()
    {
        Assert.Equal(string.Empty, PersonIncompleteDataLabels.FormatMarked(null, null));
        Assert.Equal(string.Empty, PersonIncompleteDataLabels.FormatMarked(null, "   "));
        Assert.Equal("officer.a", PersonIncompleteDataLabels.FormatMarked(null, "  officer.a  "));
    }

    [Fact]
    public void FormatMarked_DateOnly_UsesDdMmYyyy()
    {
        Assert.Equal(
            "15.03.2026",
            PersonIncompleteDataLabels.FormatMarked(new DateTime(2026, 3, 15), "  "));
    }

    [Fact]
    public void FormatMarked_DateAndByline_JoinsWithMiddleDot()
    {
        Assert.Equal(
            "15.03.2026 · officer.a",
            PersonIncompleteDataLabels.FormatMarked(new DateTime(2026, 3, 15), "  officer.a  "));
    }
}
