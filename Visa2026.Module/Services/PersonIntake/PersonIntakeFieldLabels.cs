using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

public static class PersonIntakeFieldLabels
{
    public static string Label(string targetKey) => targetKey switch
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
        _ => targetKey
    };

    public static bool CanDefer(string targetKey) => targetKey switch
    {
        PersonPendingTargets.FirstName => false,
        PersonPendingTargets.LastName => false,
        PersonPendingTargets.Age => false,
        PersonPendingTargets.VisaApplicationFamilyMembersText => false,
        PersonPendingTargets.SponsoringEmployee => false,
        PersonPendingTargets.Relationship => false,
        _ => true
    };
}