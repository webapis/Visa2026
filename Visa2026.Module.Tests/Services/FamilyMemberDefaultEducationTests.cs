using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class FamilyMemberDefaultEducationTests
{
    [Fact]
    public void Adult_family_member_without_education_should_ensure()
    {
        var person = AdultFamilyMember(30);
        Assert.True(FamilyMemberDefaultEducation.ShouldEnsure(person));
        Assert.Equal("Orta", FamilyMemberDefaultEducation.LevelNameTm);
        Assert.Equal("Orta mekdep", FamilyMemberDefaultEducation.InstitutionNameTm);
        Assert.Equal("Orta bilim", FamilyMemberDefaultEducation.SpecialtyNameTm);
        Assert.Equal("Secondary", FamilyMemberDefaultEducation.LevelLocalizationKey);
    }

    [Fact]
    public void Age_18_is_adult_family_member()
    {
        Assert.True(FamilyMemberDefaultEducation.IsAdultFamilyMember(true, DateTime.Today.AddYears(-18)));
        Assert.False(FamilyMemberDefaultEducation.IsAdultFamilyMember(true, DateTime.Today.AddYears(-17)));
        Assert.False(FamilyMemberDefaultEducation.IsAdultFamilyMember(false, DateTime.Today.AddYears(-30)));
        Assert.False(FamilyMemberDefaultEducation.IsAdultFamilyMember(true, null));
    }

    [Fact]
    public void Age_18_family_member_should_ensure()
    {
        Assert.True(FamilyMemberDefaultEducation.ShouldEnsure(AdultFamilyMember(18)));
    }

    [Fact]
    public void Child_family_member_does_not_ensure()
    {
        Assert.False(FamilyMemberDefaultEducation.ShouldEnsure(AdultFamilyMember(8)));
        Assert.False(FamilyMemberDefaultEducation.ShouldEnsure(new Person
        {
            IsEmployee = false,
            PersonRole = PersonRecordRole.FamilyMember,
            DateOfBirth = default,
            MaritalStatus = new MaritalStatus { Name = "Çaga", LocalizationKey = "Minor" },
        }));
    }

    [Fact]
    public void Age_17_family_member_does_not_ensure()
    {
        Assert.False(FamilyMemberDefaultEducation.ShouldEnsure(AdultFamilyMember(17)));
    }

    [Fact]
    public void Employee_does_not_ensure()
    {
        var person = AdultFamilyMember(30);
        person.IsEmployee = true;
        person.PersonRole = PersonRecordRole.Employee;
        Assert.False(FamilyMemberDefaultEducation.ShouldEnsure(person));
    }

    [Fact]
    public void Existing_education_skips_ensure()
    {
        var person = AdultFamilyMember(30);
        person.Educations.Add(new Education());
        Assert.False(FamilyMemberDefaultEducation.ShouldEnsure(person));
    }

    [Fact]
    public void Missing_date_of_birth_skips_ensure()
    {
        var person = AdultFamilyMember(30);
        person.DateOfBirth = default;
        Assert.False(FamilyMemberDefaultEducation.ShouldEnsure(person));
    }

    [Fact]
    public void Graduation_year_is_birth_year_plus_17()
    {
        var dob = new DateTime(1990, 3, 15);
        Assert.Equal("2007", FamilyMemberDefaultEducation.GraduationYearFor(dob));
    }

    [Fact]
    public void Ensure_without_object_space_is_null()
    {
        Assert.Null(FamilyMemberDefaultEducation.Ensure(null, AdultFamilyMember(30)));
    }

    private static Person AdultFamilyMember(int ageYears) =>
        new()
        {
            IsEmployee = false,
            PersonRole = PersonRecordRole.FamilyMember,
            DateOfBirth = DateTime.Today.AddYears(-ageYears),
        };
}