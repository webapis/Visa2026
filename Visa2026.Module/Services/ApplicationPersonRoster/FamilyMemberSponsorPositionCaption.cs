using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Family-member Wezipesi: sponsor current position + sponsor name + relationship.
/// Dependents do not own <see cref="EmployeePositionHistory"/>.
/// </summary>
public static class FamilyMemberSponsorPositionCaption
{
    public static bool UsesSponsorPosition(Person? person) =>
        person != null && !person.IsEmployee;

    public static string FormatTm(Person? person)
    {
        if (person == null)
            return string.Empty;

        if (!UsesSponsorPosition(person))
            return PersonCurrentItems.GetCurrentPositionHistory(person)?.Position?.NameTm ?? string.Empty;

        var emp = person.SponsoringEmployee;
        if (emp == null)
            return string.Empty;

        var pos = PersonCurrentItems.GetCurrentPositionHistory(emp)?.Position?.NameTm ?? string.Empty;
        var name = emp.FullName ?? string.Empty;
        var rel = person.Relationship?.NameTm ?? string.Empty;
        return $"{pos} {name}-\u0148 {rel}".Trim();
    }

    public static bool IsComplete(Person? person)
    {
        if (person == null || !UsesSponsorPosition(person))
            return true;

        var emp = person.SponsoringEmployee;
        if (emp == null)
            return false;

        var pos = PersonCurrentItems.GetCurrentPositionHistory(emp)?.Position;
        return pos != null && !string.IsNullOrWhiteSpace(person.Relationship?.NameTm);
    }
}