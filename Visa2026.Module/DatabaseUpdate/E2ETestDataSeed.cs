namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Stable values for employee create in officer journey E2E-001. Lookup display text matches English UI + seeded catalogs.</summary>
public static class E2ETestEmployeeCreateValues
{
    public const string PersonalNumber = "E2E-EMP-010";
    public const string FirstName = "Ferdi";
    public const string LastName = "EmployeeCreate";
    public const string DateOfBirth = "22.05.1980";
    public const string BirthPlace = "Soma";
    public const string CountryDisplay = "Türkiye";
    public const string GenderDisplay = "Male";
    public const string MaritalStatusDisplay = "Single";
    public const string ForeignAddress = "E2E employee create foreign address";
    public const string ProjectContractDisplay = "14306 Mary";
    public const string SubcontractorDisplay = "Çalyk Enerji";

    public static string FullName => $"{FirstName} {LastName}";
}

/// <summary>English Blazor captions for <see cref="BusinessObjects.Person"/> employee detail (XAF title case).</summary>
public static class E2ETestPersonFieldCaptions
{
    public const string FirstName = "First Name";
    public const string LastName = "Last Name";
    public const string PersonalNumber = "Personal Number";
    public const string DateOfBirth = "Date Of Birth";
    public const string BirthPlace = "Birth Place";
    public const string CountryOfBirth = "Country Of Birth";
    public const string Gender = "Gender";
    public const string MaritalStatus = "Marital Status";
    public const string Nationality = "Nationality";
    public const string ForeignAddress = "Foreign Address";
    public const string ForeignAddressCountry = "Foreign Address Country";
    public const string ProjectContract = "Project Contract";
    public const string Subcontractor = "Company (Subcontractor)";
}

/// <summary>Stable values for add-passport step in officer journey E2E-001.</summary>
public static class E2ETestPassportCreateValues
{
    public const string PassportNumber = "E2E-PASS-020";
    public const string PassportTypeDisplay = "AML — Accredited national passport";
    public const string IssuedCountryDisplay = "Türkiye";
    public const string IssueDate = "15.01.2020";
    public const string ExpirationDate = "15.01.2030";
    public const string Authority = "E2E second passport authority";
}

/// <summary>English Blazor captions for <see cref="BusinessObjects.Passport"/> detail (required fields).</summary>
public static class E2ETestPassportFieldCaptions
{
    public const string PassportNumber = "Passport Number";
    public const string PassportType = "Passport Type";
    public const string IssueDate = "Issue Date";
    public const string ExpirationDate = "Expiration Date";
    public const string Authority = "Authority";
    public const string IssuedCountry = "Issued Country";
}

/// <summary>Stable values for nested Visa under Passport (E2E-003).</summary>
public static class E2ETestVisaCreateValues
{
    public const string VisaNumber = "E2E-VISA-030";
    public const string IssueDate = "15.01.2024";
    public const string StartDate = "15.01.2024";
    public const string ExpirationDate = "15.01.2025";
}

/// <summary>English Blazor captions for <see cref="BusinessObjects.Visa"/> detail (required visible fields).</summary>
public static class E2ETestVisaFieldCaptions
{
    public const string VisaNumber = "Visa Number";
    public const string IssueDate = "Issue Date";
    public const string StartDate = "Start Date";
    public const string ExpirationDate = "Expiration Date";
}

/// <summary>Stable values for Education nested create (E2E-002). Institution/Specialty NameTm from tenant catalog.</summary>
public static class E2ETestEducationCreateValues
{
    public const string InstitutionDisplay = "Adana liseýi";
    public const string SpecialtyDisplay = "Arhitektor";
    public const string UpdatedInstitutionDisplay = "Afşin Senagat hünärment okuwy";
}

/// <summary>English Blazor captions for <see cref="BusinessObjects.Education"/> detail.</summary>
public static class E2ETestEducationFieldCaptions
{
    public const string EducationInstitution = "Education Institution";
    public const string Specialty = "Specialty";
}

/// <summary>Stable values for AddressOfResidence Lodging create (E2E-004). Default Type is Lodging.</summary>
public static class E2ETestAddressCreateValues
{
    public const string RegionDisplay = "Aşgabat şäheri";
    public const string CityDisplay = "Aşgabat şäheri";
    public const string LodgingDisplay = "1932 (A.Garlyýew) köç. 70/1 UÝJ";
}

/// <summary>English Blazor captions for <see cref="BusinessObjects.AddressOfResidence"/> detail.</summary>
public static class E2ETestAddressFieldCaptions
{
    public const string Type = "Type";
    public const string Region = "Region";
    public const string City = "City";
    public const string Lodging = "Lodging";
    public const string FullAddress = "Full Address";
    public const string ExpirationDate = "Expiration Date";
}

/// <summary>Stable values for MedicalRecord create / update / delete (E2E-005).</summary>
public static class E2ETestMedicalRecordCreateValues
{
    public const string DocumentNumber = "E2E-MED-040";
    public const string UpdatedDocumentNumber = "E2E-MED-041";
}

/// <summary>English Blazor captions for <see cref="BusinessObjects.MedicalRecord"/> detail.</summary>
public static class E2ETestMedicalRecordFieldCaptions
{
    public const string DocumentNumber = "Document Number";
}

/// <summary>Stable values for EmployeePositionHistory (E2E-006). ActualPosition is EasyTest-DB seeded.</summary>
public static class E2ETestPositionHistoryCreateValues
{
    public const string PositionDisplay = "24 okly kran - mehanik";
    public const string ActualPositionDisplay = "E2E Actual Position";
}

/// <summary>English Blazor captions for <see cref="BusinessObjects.EmployeePositionHistory"/> detail.</summary>
public static class E2ETestPositionHistoryFieldCaptions
{
    public const string Position = "Position (visa reports)";
    public const string ActualPosition = "Position (actual / company)";
}

/// <summary>Stable values for WorkDuty (E2E-007).</summary>
public static class E2ETestWorkDutyCreateValues
{
    public const string Description = "E2E work duty purpose text";
}

/// <summary>English Blazor captions for <see cref="BusinessObjects.WorkDuty"/> detail.</summary>
public static class E2ETestWorkDutyFieldCaptions
{
    public const string Description = "Gelmeginiň Maksady";
}

/// <summary>Stable values for EmployeeSalary (E2E-008).</summary>
public static class E2ETestSalaryCreateValues
{
    public const string Amount = "5000";
    public const string CurrencyDisplay = "TMT";
}

/// <summary>English Blazor captions for <see cref="BusinessObjects.EmployeeSalary"/> detail.</summary>
public static class E2ETestSalaryFieldCaptions
{
    public const string Amount = "Amount";
    public const string Currency = "Currency";
}

/// <summary>Person detail layout tab captions / New toolbar title prefixes for nested lists.</summary>
public static class E2ETestPersonNestedUi
{
    public const string PassportsTab = "Passports";
    public const string PassportsNewTitle = "New Passport";
    public const string VisasTab = "Visas";
    public const string VisasNewTitle = "New Visa";
    public const string EducationsTab = "Educations";
    public const string EducationsNewTitle = "New Education";
    public const string AddressesTab = "Addresses Of Residence";
    public const string AddressesTabAlt = "AddressesOfResidence";
    public const string AddressesTabAlt2 = "Addresses of residence";
    public const string AddressesNewTitle = "New Address Of Residence";
    public const string MedicalRecordsTab = "Medical Records";
    public const string MedicalRecordsTabAlt = "MedicalRecords";
    public const string MedicalRecordsTabAlt2 = "Medical records";
    public const string MedicalRecordsNewTitle = "New Medical Record";
    public const string PositionHistoryTab = "Position History";
    public const string PositionHistoryTabAlt = "PositionHistory";
    public const string PositionHistoryNewTitle = "New Employee Position History";
    public const string WorkDutiesTab = "Work Duties";
    public const string WorkDutiesTabAlt = "WorkDuties";
    public const string WorkDutiesTabTm = "Gelmeginiň Maksady";
    public const string WorkDutiesNewTitle = "New Gelmeginiň Maksady";
    public const string WorkDutiesNewTitleAlt = "New Work Duty";
    public const string SalariesTab = "Salaries";
    public const string SalariesNewTitle = "New Employee Salary";
    public const string TravelHistoriesTab = "Travel Histories";
    public const string TravelHistoriesTabAlt = "TravelHistories";
    public const string TravelHistoriesTabAlt2 = "Travel histories";
    public const string TravelExternalArrivalNewTitle = "New External Arrival";
}

/// <summary>External Arrival create — defaults usually apply; fallback display strings from catalogs.</summary>
public static class E2ETestTravelCreateValues
{
    /// <summary>IsDefault CheckPoint in checkpoint.json.</summary>
    public const string CheckPointDisplay = "Aşgabat şäher howa menzilindäki MGP";

    /// <summary>Same default country used elsewhere in E2E.</summary>
    public const string CountryDisplay = E2ETestEmployeeCreateValues.CountryDisplay;
}

/// <summary>Officer logon for EasyTest E2E (empty password in dev).</summary>
public static class E2ETestLoginValues
{
    /// <summary>Must match <c>Updater</c> seed user name exactly (Postgres auth is case-sensitive).</summary>
    public const string StandardUserName = "StandardUser";
    public const string StandardUserPassword = "";

    /// <summary>Blazor route for <c>Person_ListView_Employees</c>.</summary>
    public const string EmployeesListViewPath = "Person_ListView_Employees";

    /// <summary>Expected detail view id after New on the employees list.</summary>
    public const string EmployeeDetailViewPath = "Person_DetailView_Employee";

    /// <summary>EasyTest sidebar navigation item path (fallback only — prefer <see cref="EmployeesListViewPath"/> URL).</summary>
    public const string EmployeesNavigationPath = "Employees";
}
