using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.ReportDashboard;

public sealed class ReportDashboardRosterMember
{
    public Guid PersonId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PassportOrKey { get; init; } = string.Empty;
    /// <summary>Visa start (date) when known — used to classify date-filter adds.</summary>
    public DateTime? VisaStartOn { get; init; }
    /// <summary>Visa end (date) when known — used to classify date-filter removes/adds.</summary>
    public DateTime? VisaEndOn { get; init; }
}

public sealed class ReportDashboardRosterDiff
{
    public DateTime? PreviousCreatedOnUtc { get; init; }
    public DateTime? PreviousAsOfDate { get; init; }
    public DateTime? CurrentAsOfDate { get; init; }
    public Guid? PreviousExportId { get; init; }
    public string? PreviousFileName { get; init; }
    public int PreviousTotalCount { get; init; }
    public int CurrentTotalCount { get; init; }
    public Guid? CurrentExportId { get; set; }
    public string? CurrentFileName { get; set; }

    public IReadOnlyList<ReportDashboardRosterMember> DataAdded { get; init; } =
        Array.Empty<ReportDashboardRosterMember>();
    public IReadOnlyList<ReportDashboardRosterMember> DataRemoved { get; init; } =
        Array.Empty<ReportDashboardRosterMember>();
    public IReadOnlyList<ReportDashboardRosterMember> DateFilterAdded { get; init; } =
        Array.Empty<ReportDashboardRosterMember>();
    public IReadOnlyList<ReportDashboardRosterMember> DateFilterRemoved { get; init; } =
        Array.Empty<ReportDashboardRosterMember>();

    /// <summary>All adds (data + date/filter).</summary>
    public IReadOnlyList<ReportDashboardRosterMember> Added =>
        DataAdded.Concat(DateFilterAdded).ToList();

    /// <summary>All removes (data + date/filter).</summary>
    public IReadOnlyList<ReportDashboardRosterMember> Removed =>
        DataRemoved.Concat(DateFilterRemoved).ToList();

    public bool HasPrevious => PreviousCreatedOnUtc.HasValue || PreviousAsOfDate.HasValue;
    public int Delta => CurrentTotalCount - PreviousTotalCount;
    public bool HasMembershipMoves =>
        DataAdded.Count > 0 || DataRemoved.Count > 0
        || DateFilterAdded.Count > 0 || DateFilterRemoved.Count > 0;
    public bool HasDataMoves => DataAdded.Count > 0 || DataRemoved.Count > 0;
    public bool HasDateFilterMoves => DateFilterAdded.Count > 0 || DateFilterRemoved.Count > 0;
}
