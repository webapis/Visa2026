using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.ReportDashboard;

public enum ReportDashboardPersonType
{
    /// <summary>Employees + Family Members + Temporary Visitors combined.</summary>
    All,
    Employees,
    FamilyMembers,
    TemporaryVisitors
}

public enum ReportDashboardCategory
{
    /// <summary>Applications on the ministry-review route (ApplicationProgressRouteKind.ViaMinistries).</summary>
    ApplicationViaMinistry,
    /// <summary>Applications that go directly to migration (ApplicationProgressRouteKind.DirectToMigrationService).</summary>
    ApplicationDirectMigration,
    VisaExtension,
    Invitation,
    Registration,
    WorkPermit,
    Travel,
    AddressOfResidence,
    BorderZone,
    Passport,
    Education,
    PositionHistory,
    Subcontractor,
    MedicalRecord,
    IncompletePersons,
    /// <summary>Free-text person lookup; a result row opens that person's dossier.</summary>
    PersonSearch
}

/// <summary>A named report variant within a category (e.g. "By Type", "By Citizenship").</summary>
public sealed class ReportDashboardSubReport
{
    public string Key   { get; init; } = "default";
    public string Label { get; init; } = string.Empty;
}

/// <summary>
/// Sub-report tabs with optional counts (used when a category needs dynamic tab listing).
/// </summary>
public sealed class ReportDashboardSubReportListing
{
    public IReadOnlyList<ReportDashboardSubReport> SubReports { get; init; } =
        Array.Empty<ReportDashboardSubReport>();
    public IReadOnlyDictionary<string, int> Counts { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
}

public sealed class ReportDashboardProjectChip
{
    public string Key   { get; init; } = "All";
    public string Label { get; init; } = "All";
    public int    Count { get; init; }
}

public sealed class ReportDashboardStatusBucket
{
    public string Label    { get; init; } = string.Empty;
    public string CssClass { get; init; } = "st-pending";
    public int    Count    { get; init; }
}

public sealed class ReportDashboardPreviewRow
{
    public Guid?  RecordId      { get; init; }
    public string Name          { get; init; } = string.Empty;
    public string Project       { get; init; } = string.Empty;
    public string ColumnA       { get; init; } = string.Empty;
    public string ColumnB       { get; init; } = string.Empty;
    /// <summary>Optional 6th column (e.g. expiry / app date before Days Remaining or Status).</summary>
    public string ColumnC       { get; init; } = string.Empty;
    /// <summary>Optional 7th column (e.g. days remaining before Status on Visa previews).</summary>
    public string ColumnD       { get; init; } = string.Empty;
    /// <summary>Optional 8th column (e.g. App # when Visa Period/Type occupy C/D).</summary>
    public string ColumnE       { get; init; } = string.Empty;
    /// <summary>Optional 9th column (e.g. App Date when Visa Period/Type occupy C/D).</summary>
    public string ColumnF       { get; init; } = string.Empty;
    /// <summary>Optional 10th column (e.g. App # when Visa-on-extension / Issued Visa occupy E/F).</summary>
    public string ColumnG       { get; init; } = string.Empty;
    /// <summary>Optional 11th column (e.g. App Date when Visa-on-extension / Issued Visa occupy E/F).</summary>
    public string ColumnH       { get; init; } = string.Empty;
    public string Status        { get; init; } = string.Empty;
    public string StatusCssClass{ get; init; } = "st-pending";
}

public sealed class ReportDashboardPanelData
{
    public ReportDashboardPersonType   PersonType   { get; init; }
    public ReportDashboardCategory     Category     { get; init; }
    public string                      SubReport    { get; init; } = "default";
    public string                      Title        { get; init; } = string.Empty;
    public string                      Subtitle     { get; init; } = string.Empty;
    public IReadOnlyList<string>               TableHeaders  { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ReportDashboardStatusBucket> StatusBuckets { get; init; } = Array.Empty<ReportDashboardStatusBucket>();
    public IReadOnlyList<ReportDashboardPreviewRow>   PreviewRows   { get; init; } = Array.Empty<ReportDashboardPreviewRow>();
    /// <summary>Full matched-row count for the panel (may exceed <see cref="PreviewRows"/> length).</summary>
    public int    TotalCount          { get; init; }
    public string? ExcelTemplateNameHint { get; init; }
    public bool   ExcelConfigured     { get; init; }
    public string ListViewId          { get; init; } = string.Empty;
}

public sealed class ReportDashboardSnapshot
{
    public IReadOnlyList<ReportDashboardProjectChip> Projects { get; init; } = Array.Empty<ReportDashboardProjectChip>();
    public IReadOnlyDictionary<(ReportDashboardPersonType PersonType, ReportDashboardCategory Category), int> CategoryCounts { get; init; }
        = new Dictionary<(ReportDashboardPersonType, ReportDashboardCategory), int>();
    /// <summary>Non-archived people totals for All / Employees / Family / Temporary Visitors tabs.</summary>
    public IReadOnlyDictionary<ReportDashboardPersonType, int> PersonRoleCounts { get; init; }
        = new Dictionary<ReportDashboardPersonType, int>();
}