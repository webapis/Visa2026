namespace Visa2026.Module.Localization;

/// <summary>Find employees UI strings (<c>EmployeeSearch.*</c> in <c>UiStrings.employee-search.json</c>).</summary>
public static class EmployeeSearchText
{
    public const string Prefix = "EmployeeSearch.";

    public static string Get(string key) => VisaUiMessages.Get(Prefix + key);

    public static string Format(string key, params object[] args) => VisaUiMessages.Format(Prefix + key, args);
}
