namespace Visa2026.Module.Services.WordReports;

/// <summary>Where the Resminamalar catalog is shown and which reports it includes.</summary>
public enum WordReportPackageScope
{
    /// <summary>Application detail — letters and application-root user templates.</summary>
    Application = 0,

    /// <summary>ApplicationItem list — item tables and per-item user templates for selected rows.</summary>
    ApplicationItem = 1
}
