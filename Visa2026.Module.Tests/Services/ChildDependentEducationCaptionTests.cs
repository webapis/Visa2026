using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ChildDependentEducationCaptionTests
{
    [Fact]
    public void Child_dependent_under_18_uses_Caga()
    {
        var person = new Person
        {
            IsEmployee = false,
            PersonRole = PersonRecordRole.FamilyMember,
            DateOfBirth = DateTime.Today.AddYears(-8),
        };

        Assert.True(ChildDependentEducationCaption.Applies(person));
        Assert.Equal(ChildDependentEducationCaption.Text, ChildDependentEducationCaption.OverrideOrNull(person));

        var line = new ApplicationRosterMergeLine { Person = person };
        Assert.Equal("Çaga", line.Education_LevelAndInstitutionTm);
    }

    [Fact]
    public void Child_dependent_with_Caga_marital_status_uses_Caga()
    {
        var person = new Person
        {
            IsEmployee = false,
            PersonRole = PersonRecordRole.FamilyMember,
            DateOfBirth = default,
            MaritalStatus = new MaritalStatus { Name = "Çaga", LocalizationKey = "Minor" },
        };

        Assert.Equal("Çaga", new ApplicationRosterMergeLine { Person = person }.Education_LevelAndInstitutionTm);
    }

    [Fact]
    public void Adult_dependent_keeps_education_or_empty()
    {
        var person = new Person
        {
            IsEmployee = false,
            PersonRole = PersonRecordRole.FamilyMember,
            DateOfBirth = DateTime.Today.AddYears(-30),
        };

        Assert.False(ChildDependentEducationCaption.Applies(person));
        Assert.Equal(string.Empty, new ApplicationRosterMergeLine { Person = person }.Education_LevelAndInstitutionTm);
    }

    [Fact]
    public void Employee_does_not_use_Caga()
    {
        var person = new Person
        {
            IsEmployee = true,
            DateOfBirth = DateTime.Today.AddYears(-10),
        };

        Assert.False(ChildDependentEducationCaption.Applies(person));
    }

    [Fact]
    public void Cancel_visa_sanaw_row_includes_education_level_and_institution()
    {
        var person = new Person
        {
            IsEmployee = false,
            PersonRole = PersonRecordRole.FamilyMember,
            DateOfBirth = DateTime.Today.AddYears(-6),
        };
        var row = UserReportMergeDataHelper.BuildWizaYatyrylmakSanawRowDictionary(
            new ApplicationRosterMergeLine { Person = person },
            1);

        Assert.Equal("Çaga", Assert.IsType<string>(row["Education_LevelAndInstitutionTm"]));
        Assert.Equal("Çaga", Assert.IsType<string>(row["EGIY"]));
    }
}
