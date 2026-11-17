namespace Visa2026.Module;

/// <summary>Person detail view identifiers (default + role-specific layouts in Blazor <c>Model.xafml</c>).</summary>
public static class PersonDetailViewIds
{
    public const string Default = "Person_DetailView";
    /// <summary>Employee intake wizard surface (fresh id so user ModelDifference for the old classic Employee view cannot win).</summary>
    public const string Employee = "Person_DetailView_EmployeeIntake";
    /// <summary>Family member intake wizard surface (fresh id; same reason as <see cref="Employee"/>).</summary>
    public const string FamilyMember = "Person_DetailView_FamilyMemberIntake";
    public const string TemporaryVisitor = "Person_DetailView_TemporaryVisitor";
}