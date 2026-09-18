using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class FamilyMemberEducationCaptionTests
{
    [Fact]
    public void Child_age_under_18_prints_Caga_for_FMEIY_and_FMESP()
    {
        var line = new ApplicationRosterMergeLine
        {
            Person = new Person
            {
                IsEmployee = false,
                PersonRole = PersonRecordRole.FamilyMember,
                DateOfBirth = DateTime.Today.AddYears(-8),
            },
        };

        Assert.Equal("Çaga", line.FM_EducationLevelAndInstitutionTm);
        Assert.Equal("Çaga", line.FM_SpecialtyTm);
    }

    [Fact]
    public void Child_marital_Caga_prints_Caga()
    {
        var line = new ApplicationRosterMergeLine
        {
            Person = new Person
            {
                IsEmployee = false,
                PersonRole = PersonRecordRole.FamilyMember,
                DateOfBirth = default,
                MaritalStatus = new MaritalStatus { Name = "Çaga", LocalizationKey = "Minor" },
            },
        };

        Assert.Equal("Çaga", line.FM_EducationLevelAndInstitutionTm);
        Assert.Equal("Çaga", line.FM_SpecialtyTm);
    }

    [Fact]
    public void Adult_without_education_uses_fixed_Orta_defaults()
    {
        var line = new ApplicationRosterMergeLine
        {
            Person = new Person
            {
                IsEmployee = false,
                PersonRole = PersonRecordRole.FamilyMember,
                DateOfBirth = DateTime.Today.AddYears(-30),
            },
        };

        Assert.Equal(FamilyMemberEducationCaption.AdultLevelAndInstitutionFallback, line.FM_EducationLevelAndInstitutionTm);
        Assert.Equal(FamilyMemberEducationCaption.AdultSpecialtyFallback, line.FM_SpecialtyTm);
        Assert.Equal("Orta, Orta mekdep", line.FM_EducationLevelAndInstitutionTm);
        Assert.Equal("Orta bilim", line.FM_SpecialtyTm);
    }

    [Fact]
    public void Adult_with_education_uses_Education_BO()
    {
        var line = new ApplicationRosterMergeLine
        {
            Person = new Person
            {
                IsEmployee = false,
                PersonRole = PersonRecordRole.FamilyMember,
                DateOfBirth = DateTime.Today.AddYears(-40),
            },
            CurrentEducation = new Education
            {
                EducationLevel = new EducationLevel { NameTm = "Orta" },
                EducationInstitution = new EducationInstitution { NameTm = "Orta mekdep" },
                Specialty = new Specialty { NameTm = "Orta bilim" },
            },
        };

        Assert.Equal("Orta, Orta mekdep", line.FM_EducationLevelAndInstitutionTm);
        Assert.Equal("Orta bilim", line.FM_SpecialtyTm);
    }

    [Fact]
    public void Employee_uses_real_education_not_FM_defaults()
    {
        var line = new ApplicationRosterMergeLine
        {
            Person = new Person
            {
                IsEmployee = true,
                DateOfBirth = DateTime.Today.AddYears(-30),
            },
            CurrentEducation = new Education
            {
                EducationLevel = new EducationLevel { NameTm = "Ýokary" },
                EducationInstitution = new EducationInstitution { NameTm = "Gündogar" },
                Specialty = new Specialty { NameTm = "elektrik" },
            },
        };

        Assert.Equal("Ýokary, Gündogar", line.FM_EducationLevelAndInstitutionTm);
        Assert.Equal("elektrik", line.FM_SpecialtyTm);
    }

    [Fact]
    public void Sanawy_row_includes_FMEIY_and_FMESP_aliases()
    {
        var line = new ApplicationRosterMergeLine
        {
            Person = new Person
            {
                IsEmployee = false,
                PersonRole = PersonRecordRole.FamilyMember,
                DateOfBirth = DateTime.Today.AddYears(-6),
            },
        };
        var row = UserReportMergeDataHelper.BuildSanawyRowDictionary(line, 1);

        Assert.Equal("Çaga", Assert.IsType<string>(row["FM_EducationLevelAndInstitutionTm"]));
        Assert.Equal("Çaga", Assert.IsType<string>(row["FMEIY"]));
        Assert.Equal("Çaga", Assert.IsType<string>(row["FM_SpecialtyTm"]));
        Assert.Equal("Çaga", Assert.IsType<string>(row["FMESP"]));
    }
}