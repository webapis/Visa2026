namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selector ids for <c>Passport_ListView</c> toolbar actions.
/// Nested on Person detail (Passports tab) uses <c>person-employee-tab-passports-new</c>.
/// </summary>
internal static class PassportListViewE2eActionHooks
{
    public const string ListViewId = "Passport_ListView";

    /// <summary>
    /// <c>New</c> on embedded Passports list while URL is a Person detail view.
    /// </summary>
    public const string PersonDetailPassportsNewTestId = "person-employee-tab-passports-new";
}
