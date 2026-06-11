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
