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
    public const string ProjectContractDisplay = "GT-15";
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

/// <summary>Officer logon for EasyTest E2E (empty password in dev).</summary>
public static class E2ETestLoginValues
{
    public const string StandardUserName = "standarduser";
    public const string StandardUserPassword = "";

    /// <summary>Blazor route for <c>Person_ListView_Employees</c>.</summary>
    public const string EmployeesListViewPath = "Person_ListView_Employees";

    /// <summary>Expected detail view id after New on the employees list.</summary>
    public const string EmployeeDetailViewPath = "Person_DetailView_Employee";

    /// <summary>EasyTest sidebar navigation item path (fallback only — prefer <see cref="EmployeesListViewPath"/> URL).</summary>
    public const string EmployeesNavigationPath = "People.Employees";
}
