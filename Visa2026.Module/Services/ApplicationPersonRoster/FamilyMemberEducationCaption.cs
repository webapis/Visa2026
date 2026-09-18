using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Family-member roster education captions (sanaw Bilimi / Hünäri columns).
/// Child (age < 18 or MaritalStatus Çaga) → <c>Çaga</c>.
/// Adult dependent → Education BO when set, else fixed Orta defaults.
/// Employees → real education values (no FM defaults).
/// </summary>
public static class FamilyMemberEducationCaption
{
    public const string ChildText = "Çaga";
    public const string AdultLevelAndInstitutionFallback = "Orta, Orta mekdep";
    public const string AdultSpecialtyFallback = "Orta bilim";

    public static bool UsesFamilyMemberRules(Person? person) =>
        person != null && !person.IsEmployee;

    public static string LevelAndInstitutionTm(Person? person, string? educationLevelAndInstitution)
    {
        if (!UsesFamilyMemberRules(person))
            return educationLevelAndInstitution ?? string.Empty;

        if (ChildDependentEducationCaption.Applies(person))
            return ChildText;

        return string.IsNullOrWhiteSpace(educationLevelAndInstitution)
            ? AdultLevelAndInstitutionFallback
            : educationLevelAndInstitution.Trim();
    }

    public static string SpecialtyTm(Person? person, string? educationSpecialty)
    {
        if (!UsesFamilyMemberRules(person))
            return educationSpecialty ?? string.Empty;

        if (ChildDependentEducationCaption.Applies(person))
            return ChildText;

        return string.IsNullOrWhiteSpace(educationSpecialty)
            ? AdultSpecialtyFallback
            : educationSpecialty.Trim();
    }
}