using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Legacy VISA2014 created Education Orta / Orta mekdep / Orta bilim for an adult family member
/// with a birth country and no education rows. Visa2026 does the same for family members
/// age 18 or older. Children keep no Education row (People and links uses Caga).
/// VISA2014 import forces those three lookups on existing adult-FM Education rows
/// (country and graduation year stay from source).
/// </summary>
public static class FamilyMemberDefaultEducation
{
    public const string LevelNameTm = "Orta";
    public const string LevelLocalizationKey = "Secondary";
    public const string InstitutionNameTm = "Orta mekdep";
    public const string SpecialtyNameTm = "Orta bilim";

    public static bool IsAdultFamilyMember(bool isFamilyMember, DateTime? birthDate)
    {
        if (!isFamilyMember || birthDate is not { } birth || birth == default)
            return false;
        return AgeYears(birth) >= 18;
    }

    public static int AgeYears(DateTime birthDate)
    {
        if (birthDate == default)
            return 0;

        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;
        return age < 0 ? 0 : age;
    }

    public static bool ShouldEnsure(Person? person, bool ignoreImportGuard = false)
    {
        if (!ignoreImportGuard && MigrationImportContext.IsDataImport)
            return false;
        if (person == null || person.IsEmployee)
            return false;
        if (person.PersonRole == PersonRecordRole.TemporaryVisitor)
            return false;
        if (ChildDependentEducationCaption.Applies(person))
            return false;
        if (!IsAdultFamilyMember(isFamilyMember: true, person.DateOfBirth))
            return false;
        if (person.Educations != null && person.Educations.Any(e => e != null))
            return false;
        return true;
    }

    public static Education? Ensure(IObjectSpace? objectSpace, Person person, bool ignoreImportGuard = false)
    {
        if (objectSpace == null || !ShouldEnsure(person, ignoreImportGuard))
            return null;

        var level = FindLookup<EducationLevel>(objectSpace, LevelNameTm);
        var institution = FindLookup<EducationInstitution>(objectSpace, InstitutionNameTm);
        var specialty = FindLookup<Specialty>(objectSpace, SpecialtyNameTm);
        if (level == null || institution == null || specialty == null)
            return null;

        var country = person.CountryOfBirth;
        if (country == null)
            return null;

        var education = objectSpace.CreateObject<Education>();
        education.Person = person;
        education.EducationLevel = level;
        education.EducationInstitution = institution;
        education.Specialty = specialty;
        education.EducationCountry = country;
        education.GraduationYear = GraduationYearFor(person.DateOfBirth);

        person.Educations ??= new System.Collections.ObjectModel.ObservableCollection<Education>();
        if (!person.Educations.Contains(education))
            person.Educations.Add(education);

        return education;
    }

    public static bool TryApplyLookups(IObjectSpace? objectSpace, Education education, out bool changed)
    {
        changed = false;
        if (objectSpace == null || education == null)
            return false;

        var level = FindLookup<EducationLevel>(objectSpace, LevelNameTm);
        var institution = FindLookup<EducationInstitution>(objectSpace, InstitutionNameTm);
        var specialty = FindLookup<Specialty>(objectSpace, SpecialtyNameTm);
        if (level == null || institution == null || specialty == null)
            return false;

        changed = education.EducationLevel != level
            || education.EducationInstitution != institution
            || education.Specialty != specialty;
        education.EducationLevel = level;
        education.EducationInstitution = institution;
        education.Specialty = specialty;
        return true;
    }

    public static string? GraduationYearFor(DateTime dateOfBirth)
    {
        if (dateOfBirth == default)
            return null;

        var text = (dateOfBirth.Year + 17).ToString();
        return Education.IsValidGraduationYear(text) ? text : null;
    }

    private static T? FindLookup<T>(IObjectSpace objectSpace, string nameTm)
        where T : LookupBase
    {
        var match = objectSpace.GetObjectsQuery<T>()
            .FirstOrDefault(x => x.NameTm == nameTm);
        if (match != null)
            return match;

#pragma warning disable CS0618
        return objectSpace.GetObjectsQuery<T>()
            .FirstOrDefault(x => x.Name == nameTm);
#pragma warning restore CS0618
    }
}