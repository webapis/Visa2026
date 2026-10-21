using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

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
    /// <summary>Applications on the ministry-review route (ApplicationProfileInstanceProgressRouteKind.ViaMinistries).</summary>
    ApplicationViaMinistry,
    /// <summary>Applications that go directly to migration (ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService).</summary>
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
    /// <summary>Invitation department, from the last position history. Empty on other categories.</summary>
    public string Department    { get; init; } = string.Empty;
    /// <summary>Work-permit specialty from the latest education. Empty on other categories.</summary>
    public string Specialty     { get; init; } = string.Empty;
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
    /// <summary>Optional 12th column (Active Invitation migration date and letter number).</summary>
    public string ColumnI       { get; init; } = string.Empty;
    /// <summary>Invitation ministry cells, in catalog header order. Used and Process still show every leg.</summary>
    public IReadOnlyList<string> MinistryCells { get; init; } = Array.Empty<string>();
    /// <summary>Active and Expired Invitation: date and number of the last approval leg that has either.</summary>
    public string LastApproval { get; init; } = string.Empty;
    /// <summary>Optional pill under <see cref="ColumnI"/> (Active Invitation validity extended).</summary>
    public string ColumnBadge   { get; init; } = string.Empty;
    /// <summary>Active Invitation remaining-time pill under the expiry date (<see cref="ColumnF"/>).</summary>
    public string ExpiresBadge  { get; init; } = string.Empty;
    /// <summary>Css class for <see cref="ExpiresBadge"/> (same buckets as Invitation Validity).</summary>
    public string ExpiresCssClass { get; init; } = string.Empty;
    /// <summary>Active and Expired Invitation NOT cell: geçerli gün sayı : N.</summary>
    public string RemainingDaysNote { get; init; } = string.Empty;
    /// <summary>Background intensity for <see cref="RemainingDaysNote"/>.</summary>
    public string RemainingDaysCss { get; init; } = string.Empty;
    /// <summary>Person to open when the preview row opens the dossier.</summary>
    public Guid?  PersonId      { get; init; }
    /// <summary>Invitation whose scanned copies open from the invitation-number cell.</summary>
    public Guid?  InvitationId  { get; init; }
    /// <summary>Visa issued from this invitation. Its number opens scanned visa copies.</summary>
    public Guid?  VisaId        { get; init; }
    public string VisaNumber    { get; init; } = string.Empty;
    public string VisaIssuedDate { get; init; } = string.Empty;
    public string VisaPlace     { get; init; } = string.Empty;
    public string VisaStartDate { get; init; } = string.Empty;
    public string VisaEndDate   { get; init; } = string.Empty;
    /// <summary>Work permit header whose scanned copies open from the permit-number cell.</summary>
    public Guid?  WorkPermitHeaderId { get; init; }
    public string WorkPermitStart { get; init; } = string.Empty;
    public string WorkPermitEnd   { get; init; } = string.Empty;
    /// <summary>Every valid work permit on this person row, newest start date first.</summary>
    public IReadOnlyList<WorkPermitBundleSlot> WorkPermits { get; init; } = Array.Empty<WorkPermitBundleSlot>();
    /// <summary>Sanaw sheet cells, without the screen row number.</summary>
    public IReadOnlyList<string> SanawCells { get; init; } = Array.Empty<string>();
    /// <summary>Çalışma izni listesi cells, without the screen row number.</summary>
    public IReadOnlyList<string> CalismaCells { get; init; } = Array.Empty<string>();
    /// <summary>uzatılanlar cells, without the screen row number.</summary>
    public IReadOnlyList<string> UzatilanlarCells { get; init; } = Array.Empty<string>();
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