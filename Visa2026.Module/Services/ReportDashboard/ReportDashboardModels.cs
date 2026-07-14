using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.ReportDashboard;

public enum ReportDashboardPersonType
{
    Employees,
    FamilyMembers,
    TemporaryVisitors
}

public enum ReportDashboardCategory
{
    VisaExtension,
    Invitation,
    Registration,
    WorkPermit,
    Travel,
    BorderZone,
    Passport
}

/// <summary>A named report variant within a category (e.g. "By Type", "By Citizenship").</summary>
public sealed class ReportDashboardSubReport
{
    public string Key   { get; init; } = "default";
    public string Label { get; init; } = string.Empty;
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
    public int    TotalCount          => PreviewRows.Count;
    public string? ExcelTemplateNameHint { get; init; }
    public bool   ExcelConfigured     { get; init; }
    public string ListViewId          { get; init; } = string.Empty;
}

public sealed class ReportDashboardSnapshot
{
    public IReadOnlyList<ReportDashboardProjectChip> Projects { get; init; } = Array.Empty<ReportDashboardProjectChip>();
    public IReadOnlyDictionary<(ReportDashboardPersonType PersonType, ReportDashboardCategory Category), int> CategoryCounts { get; init; }
        = new Dictionary<(ReportDashboardPersonType, ReportDashboardCategory), int>();
}