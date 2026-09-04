namespace Visa2026.Module.Services.WordReports;

/// <summary>Where the Resminamalar catalog is shown and which reports it includes.</summary>
public enum WordReportPackageScope
{
    /// <summary>ApplicationProfileInstance detail — letters and application-root user templates.</summary>
    ApplicationProfileInstance = 0,

    /// <summary>
    /// Roster person rows (Person IDs on the instance) —
    /// item tables and per-person user templates for selected rows.
    /// </summary>
    RosterPerson = 1,

    /// <summary>Obsolete alias for <see cref="RosterPerson"/> (stored batches / callers).</summary>
    ApplicationRosterMergeLine = RosterPerson
}