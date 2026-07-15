using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Gradual promotion: mock remains the default for UI appeal.
/// Promote one <c>(category, subReport)</c> at a time via <see cref="RealSubReports"/>.
/// Snapshot stays on mock until counts are promoted separately.
/// </summary>
public sealed class ReportDashboardHybridQueryService : IReportDashboardQueryService
{
    /// <summary>
    /// Sub-reports that load from SQL views / real EF queries.
    /// Add entries one at a time after verifying each view.
    /// </summary>
    private static readonly HashSet<(ReportDashboardCategory Category, string SubReport)> RealSubReports =
    [
        (ReportDashboardCategory.Passport, "by-validity"),
        (ReportDashboardCategory.Passport, "by-type"),
        (ReportDashboardCategory.Passport, "by-citizenship"),
    ];

    private readonly ReportDashboardQueryService _real;
    private readonly ReportDashboardMockQueryService _mock;

    public ReportDashboardHybridQueryService(
        ReportDashboardQueryService real,
        ReportDashboardMockQueryService mock)
    {
        _real = real;
        _mock = mock;
    }

    /// <summary>
    /// Keep overview counts / project chips on mock until snapshot view ships.
    /// </summary>
    public ReportDashboardSnapshot LoadSnapshot(IObjectSpace objectSpace, int dateRangeMonths = 6)
        => _mock.LoadSnapshot(objectSpace, dateRangeMonths);

    public ReportDashboardPanelData LoadPanel(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string projectKey,
        int dateRangeMonths = 6,
        string subReport = "default",
        bool includeArchivedPersons = false)
    {
        var key = (category, subReport);
        // Default sub-report key for Passport is "by-validity"
        if (category == ReportDashboardCategory.Passport
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-validity");

        IReportDashboardQueryService service = RealSubReports.Contains(key) ? _real : _mock;
        return service.LoadPanel(
            objectSpace, personType, category, projectKey, dateRangeMonths, subReport, includeArchivedPersons);
    }
}