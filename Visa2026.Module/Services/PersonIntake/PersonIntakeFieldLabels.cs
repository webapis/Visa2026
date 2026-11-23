using System;
using DevExpress.ExpressApp.Utils;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

public static class PersonIntakeFieldLabels
{
    /// <summary>Localized caption of the matching <see cref="Person"/> member (same text as the native Person form).</summary>
    public static string Label(string targetKey)
    {
        var member = PersonMember(targetKey);
        var caption = member == null ? null : ModelCaption(typeof(Person), member);
        return string.IsNullOrWhiteSpace(caption) ? Fallback(targetKey) : caption;
    }

    /// <summary>Localized caption of a member on a child record type (Passport, Education, ...).</summary>
    public static string MemberLabel(Type type, string member, string fallback)
    {
        var caption = ModelCaption(type, member);
        return string.IsNullOrWhiteSpace(caption) ? fallback : caption;
    }

    public static bool CanDefer(string targetKey) => targetKey switch
    {
        PersonPendingTargets.FirstName => false,
        PersonPendingTargets.LastName => false,
        PersonPendingTargets.Age => false,
        PersonPendingTargets.SponsoringEmployee => false,
        PersonPendingTargets.Relationship => false,
        _ => true
    };

    private static string PersonMember(string targetKey)
    {
        if (string.IsNullOrEmpty(targetKey))
            return null;
        if (targetKey.StartsWith("Person.", StringComparison.Ordinal))
            return targetKey.Substring("Person.".Length);
        return targetKey switch
        {
            PersonPendingTargets.Passport => nameof(Person.Passports),
            PersonPendingTargets.Education => nameof(Person.Educations),
            PersonPendingTargets.MedicalRecord => nameof(Person.MedicalRecords),
            PersonPendingTargets.AddressOfResidence => nameof(Person.AddressesOfResidence),
            PersonPendingTargets.PersonDocument => nameof(Person.Documents),
            PersonPendingTargets.TravelHistory => nameof(Person.TravelHistories),
            PersonPendingTargets.EmployeeSalary => nameof(Person.Salaries),
            PersonPendingTargets.EmployeePositionHistory => nameof(Person.PositionHistory),
            PersonPendingTargets.WorkDuty => nameof(Person.WorkDuties),
            PersonPendingTargets.PersonFamilyRelationDocument => nameof(Person.FamilyRelationDocuments),
            _ => null
        };
    }

    private static string ModelCaption(Type type, string member)
    {
        if (type == null || string.IsNullOrEmpty(member) || CaptionHelper.ApplicationModel == null)
            return null;
        try
        {
            return CaptionHelper.GetMemberCaption(type, member);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Fallback(string targetKey) => targetKey switch
    {
        PersonPendingTargets.FirstName => "First name",
        PersonPendingTargets.LastName => "Last name",
        PersonPendingTargets.DateOfBirth => "Date of birth",
        PersonPendingTargets.Age => "Age",
        PersonPendingTargets.Gender => "Gender",
        PersonPendingTargets.VisaApplicationFamilyMembersText => "Family members for visa (manual)",
        PersonPendingTargets.PersonalNumber => "Personal number",
        PersonPendingTargets.Photo => "Photo",
        PersonPendingTargets.BirthPlace => "Birth place",
        PersonPendingTargets.CountryOfBirth => "Country of birth",
        PersonPendingTargets.Nationality => "Nationality",
        PersonPendingTargets.MaritalStatus => "Marital status",
        PersonPendingTargets.ForeignAddress => "Foreign address",
        PersonPendingTargets.ForeignAddressCountry => "Foreign address country",
        PersonPendingTargets.ProjectContract => "Project contract",
        PersonPendingTargets.Subcontractor => "Company (Subcontractor)",
        PersonPendingTargets.SponsoringEmployee => "Sponsoring employee",
        PersonPendingTargets.Relationship => "Relationship",
        PersonPendingTargets.Passport => "Passport",
        PersonPendingTargets.Education => "Education",
        PersonPendingTargets.MedicalRecord => "Medical record",
        PersonPendingTargets.AddressOfResidence => "Address of residence",
        PersonPendingTargets.PersonDocument => "CV & files",
        PersonPendingTargets.TravelHistory => "Travel history",
        PersonPendingTargets.EmployeeSalary => "Salaries",
        PersonPendingTargets.EmployeePositionHistory => "Position history",
        _ => targetKey
    };
}
