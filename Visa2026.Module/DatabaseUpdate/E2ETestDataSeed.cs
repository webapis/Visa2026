namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Stable identities for EasyTest / Visa2026EasyTest database runs.</summary>
public static class E2ETestDataSeed
{
    public const string PersonFirstName = "E2E";
    public const string PersonLastName = "Applicant";
    public const string PersonPersonalNumber = "E2E-TEST-001";
    public const string PassportNumber = "E2E-PASS-001";

    public static string PersonFullName => $"{PersonFirstName} {PersonLastName}";
}

/// <summary>Stable values for employee-create E2E (E2E-010). Lookup display text matches English UI + seeded catalogs.</summary>
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

/// <summary>Officer logon for E2E flows that mirror UiScenario (empty password in dev).</summary>
public static class E2ETestLoginValues
{
    public const string StandardUserName = "standarduser";
    public const string StandardUserPassword = "";

    /// <summary>Blazor route for <c>Person_ListView_Employees</c> (UiScenarioRunner <c>goto</c> path).</summary>
    public const string EmployeesListViewPath = "Person_ListView_Employees";

    /// <summary>Expected detail view id after New on the employees list.</summary>
    public const string EmployeeDetailViewPath = "Person_DetailView_Employee";

    /// <summary>EasyTest sidebar navigation item path (fallback only — prefer <see cref="EmployeesListViewPath"/> URL).</summary>
    public const string EmployeesNavigationPath = "People.Employees";
}
