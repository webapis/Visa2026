namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Stable <see cref="PersonPendingItem.TargetKey"/> values and the XAF criteria that
/// skip <c>[RuleRequiredField]</c> while that key is pending.
/// </summary>
public static class PersonPendingTargets
{
    public const string DateOfBirth = "Person.DateOfBirth";
    public const string Gender = "Person.Gender";
    public const string PersonalNumber = "Person.PersonalNumber";
    public const string BirthPlace = "Person.BirthPlace";
    public const string CountryOfBirth = "Person.CountryOfBirth";
    public const string Nationality = "Person.Nationality";
    public const string MaritalStatus = "Person.MaritalStatus";
    public const string ForeignAddress = "Person.ForeignAddress";
    public const string ForeignAddressCountry = "Person.ForeignAddressCountry";
    public const string ProjectContract = "Person.ProjectContract";
    public const string Subcontractor = "Person.Subcontractor";
    public const string Photo = "Person.Photo";

    public const string FirstName = "Person.FirstName";
    public const string LastName = "Person.LastName";
    public const string MiddleName = "Person.MiddleName";
    public const string Age = "Person.Age";
    public const string VisaApplicationFamilyMembersText = "Person.VisaApplicationFamilyMembersText";
    public const string SponsoringEmployee = "Person.SponsoringEmployee";
    public const string Relationship = "Person.Relationship";

    public const string Passport = "Passport";
    public const string Education = "Education";
    public const string MedicalRecord = "MedicalRecord";
    public const string AddressOfResidence = "AddressOfResidence";
    public const string PersonDocument = "PersonDocument";
    public const string EmployeePositionHistory = "EmployeePositionHistory";
    public const string TravelHistory = "TravelHistory";
    public const string WorkDuty = "WorkDuty";
    public const string EmployeeSalary = "EmployeeSalary";
    public const string PersonFamilyRelationDocument = "PersonFamilyRelationDocument";

    public const string LegacyPrefix = "Legacy.";

    public const string UnlessPendingDateOfBirth = "PendingItems[TargetKey = 'Person.DateOfBirth'].Count() = 0";
    public const string UnlessPendingGender = "PendingItems[TargetKey = 'Person.Gender'].Count() = 0";
    public const string UnlessPendingPersonalNumber = "PendingItems[TargetKey = 'Person.PersonalNumber'].Count() = 0";
    public const string UnlessPendingBirthPlace = "PendingItems[TargetKey = 'Person.BirthPlace'].Count() = 0";
    public const string UnlessPendingCountryOfBirth = "PendingItems[TargetKey = 'Person.CountryOfBirth'].Count() = 0";
    public const string UnlessPendingNationality = "PendingItems[TargetKey = 'Person.Nationality'].Count() = 0";
    public const string UnlessPendingProjectContract = "PendingItems[TargetKey = 'Person.ProjectContract'].Count() = 0";
    public const string UnlessPendingSubcontractor = "PendingItems[TargetKey = 'Person.Subcontractor'].Count() = 0";

    public const string EmployeeUnlessPendingMaritalStatus =
        PersonRoleHelper.EmployeeCriteria + " And PendingItems[TargetKey = 'Person.MaritalStatus'].Count() = 0";

    public const string EmployeeUnlessPendingForeignAddress =
        PersonRoleHelper.EmployeeCriteria + " And PendingItems[TargetKey = 'Person.ForeignAddress'].Count() = 0";

    public const string EmployeeUnlessPendingForeignAddressCountry =
        PersonRoleHelper.EmployeeCriteria + " And PendingItems[TargetKey = 'Person.ForeignAddressCountry'].Count() = 0";
}
