using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Ministry sanaw "Hünäri we bilimi" prints <c>Çaga</c> for a child dependent
/// instead of education level + institution.
/// People &amp; links and document copies also skip requiring an Education BO.
/// </summary>
public static class ChildDependentEducationCaption
{
    public const string Text = "Çaga";

    public static bool Applies(Person? person)
    {
        if (person == null || person.IsEmployee)
            return false;

        if (IsCagaMaritalStatus(person.MaritalStatus))
            return true;

        return person.DateOfBirth != default && person.Age < 18;
    }

    public static string? OverrideOrNull(Person? person) =>
        Applies(person) ? Text : null;

    private static bool IsCagaMaritalStatus(MaritalStatus? status)
    {
        if (status == null)
            return false;

        return EqualsIgnoreCase(status.LocalizationKey, "Minor")
            || EqualsIgnoreCase(status.Name, Text)
            || EqualsIgnoreCase(status.NameTm, Text)
            || EqualsIgnoreCase(status.Code, Text);
    }

    private static bool EqualsIgnoreCase(string? value, string expected) =>
        !string.IsNullOrWhiteSpace(value)
        && string.Equals(value.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
