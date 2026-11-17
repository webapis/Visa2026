namespace Visa2026.Module;

/// <summary>Person detail view identifiers (default + role-specific layouts in Blazor <c>Model.xafml</c>).</summary>
public static class PersonDetailViewIds
{
    public const string Default = "Person_DetailView";

    /// <summary>Native Employee DetailView (classic XAF form).</summary>
    public const string Employee = "Person_DetailView_Employee";

    /// <summary>Native Family member DetailView (classic XAF form).</summary>
    public const string FamilyMember = "Person_DetailView_FamilyMember";

    /// <summary>
    /// Optional Person-bound intake surface. Create/edit via wizard uses
    /// <see cref="BusinessObjects.PersonIntake.PersonIntakeWizardViewIds"/> instead.
    /// Kept so model patches do not rewrite the native Employee view.
    /// </summary>
    public const string EmployeeIntake = "Person_DetailView_EmployeeIntake";

    /// <summary>Optional Person-bound family intake surface (see <see cref="EmployeeIntake"/>).</summary>
    public const string FamilyMemberIntake = "Person_DetailView_FamilyMemberIntake";

    public const string TemporaryVisitor = "Person_DetailView_TemporaryVisitor";
}
