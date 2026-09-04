namespace Visa2026.Module.BusinessObjects;

/// <summary>Root Business Object for user-defined Word report templates.</summary>
public enum UserReportBoType
{
    /// <summary>Application-level reports (cover letters, headers).</summary>
    ApplicationProfileInstance = 0,

    /// <summary>
    /// Per-roster-person reports (sanawy / registration lines). Value retained for seeded templates.
    /// Merge root is hydrated from Person + instance ResolvedLinks (skip-navigation People).
    /// </summary>
    ApplicationItem = 1, // roster merge line (ApplicationRosterMergeLine); keep enum name for stored templates

    /// <summary>Person-centric reports.</summary>
    Person = 2
}