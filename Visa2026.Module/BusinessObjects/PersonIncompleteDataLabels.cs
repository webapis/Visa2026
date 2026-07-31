using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.BusinessObjects;

/// <summary>Stable English labels for Person incomplete missing-area flags (dashboard chart axis).</summary>
public static class PersonIncompleteDataLabels
{
    public const string PersonalData = "Personal data";
    public const string Passport = "Passport";
    public const string Cv = "CV";
    public const string Photo = "Photo";
    public const string Education = "Education";
    public const string Medical = "Medical";
    public const string Address = "Address";
    public const string FamilyDocs = "Family docs";
    public const string Other = "Other";

    public static readonly string[] ChartOrder =
    [
        PersonalData, Passport, Cv, Photo, Education, Medical, Address, FamilyDocs, Other
    ];

    public static string FormatMissingAreas(
        bool personalData, bool passport, bool cv, bool photo,
        bool education, bool medical, bool address, bool familyDocs, bool other)
    {
        var parts = new List<string>(9);
        if (personalData) parts.Add(PersonalData);
        if (passport) parts.Add(Passport);
        if (cv) parts.Add(Cv);
        if (photo) parts.Add(Photo);
        if (education) parts.Add(Education);
        if (medical) parts.Add(Medical);
        if (address) parts.Add(Address);
        if (familyDocs) parts.Add(FamilyDocs);
        if (other) parts.Add(Other);
        return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
    }

    public static string PersonRoleLabel(PersonRecordRole role) => role switch
    {
        PersonRecordRole.Employee => "Employee",
        PersonRecordRole.FamilyMember => "Family Member",
        PersonRecordRole.TemporaryVisitor => "Temporary Visitor",
        _ => role.ToString()
    };

    public static string FormatMarked(System.DateTime? markedOn, string markedBy)
    {
        if (markedOn == null || markedOn == default(System.DateTime))
            return string.IsNullOrWhiteSpace(markedBy) ? string.Empty : markedBy.Trim();
        var date = markedOn.Value.ToString("dd.MM.yyyy");
        return string.IsNullOrWhiteSpace(markedBy) ? date : $"{date} · {markedBy.Trim()}";
    }
}