namespace Visa2026.Module;

/// <summary>Person roster ListView ids and incomplete-filter criteria for Employees / Family Members.</summary>
public static class PersonRosterListViewIds
{
    public const string Employees = "Person_ListView_Employees";
    public const string EmployeesIncomplete = "Person_ListView_Employees_Incomplete";
    public const string FamilyMembers = "Person_ListView_FamilyMembers";
    public const string FamilyMembersIncomplete = "Person_ListView_FamilyMembers_Incomplete";
    public const string TemporaryVisitors = "Person_ListView_TemporaryVisitors";

    /// <summary>Employees and Family Members show only this column beside Dossier, wide enough for a full name on one line.</summary>
    public const int FullNameColumnWidth = 560;

    public const string NavEmployees = "Employees";
    public const string NavFamilyMembers = "FamilyMembers";
    public const string NavAll = "All";
    public const string NavIncomplete = "Incomplete";

    public static string EmployeesIncompleteCriteria =>
        PersonRoleHelper.EmployeeCriteria + " And IsDataIncomplete = True";

    public static string FamilyMembersIncompleteCriteria =>
        PersonRoleHelper.FamilyMemberCriteria + " And IsDataIncomplete = True";

    public static bool IsEmployeeRoster(string? listViewId) =>
        listViewId is Employees or EmployeesIncomplete;

    public static bool IsFamilyRoster(string? listViewId) =>
        listViewId is FamilyMembers or FamilyMembersIncomplete;

    public static bool IsIntakeWizardList(string? listViewId) =>
        IsEmployeeRoster(listViewId) || IsFamilyRoster(listViewId);

    public static bool IsIncompleteRoster(string? listViewId) =>
        listViewId is EmployeesIncomplete or FamilyMembersIncomplete;
}