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
        (ReportDashboardCategory.Application, "by-progress"),
        (ReportDashboardCategory.Application, "by-type"),
        (ReportDashboardCategory.Passport, "by-validity"),
        (ReportDashboardCategory.Passport, "by-type"),
        (ReportDashboardCategory.Passport, "by-citizenship"),
        (ReportDashboardCategory.WorkPermit, "by-days-remaining"),
        (ReportDashboardCategory.VisaExtension, "app-progress"),
        (ReportDashboardCategory.VisaExtension, "visa-state"),
        (ReportDashboardCategory.VisaExtension, "by-category"),
        (ReportDashboardCategory.VisaExtension, "by-type"),
        (ReportDashboardCategory.VisaExtension, "by-period"),
        (ReportDashboardCategory.VisaExtension, "by-days-remaining"),
        (ReportDashboardCategory.Education, "by-level"),
        (ReportDashboardCategory.Education, "by-country"),
        (ReportDashboardCategory.Education, "by-specialty"),
        (ReportDashboardCategory.PositionHistory, "by-status"),
        (ReportDashboardCategory.PositionHistory, "by-position"),
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
    /// Project chips + person-type tabs from SQL views; category sidebar counts stay on mock.
    /// </summary>
    public ReportDashboardSnapshot LoadSnapshot(
        IObjectSpace objectSpace,
        int dateRangeMonths = 6,
        ReportDashboardPersonType personType = ReportDashboardPersonType.All)
    {
        var mock = _mock.LoadSnapshot(objectSpace, dateRangeMonths, personType);
        var real = _real.LoadSnapshot(objectSpace, dateRangeMonths, personType);
        return new ReportDashboardSnapshot
        {
            Projects = real.Projects.Count > 1 ? real.Projects : mock.Projects,
            CategoryCounts = mock.CategoryCounts,
            PersonRoleCounts = real.PersonRoleCounts.Count > 0 ? real.PersonRoleCounts : mock.PersonRoleCounts
        };
    }

    public ReportDashboardPanelData LoadPanel(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string projectKey,
        int dateRangeMonths = 6,
        string subReport = "default",
        bool includeArchivedPersons = false,
        bool oneLastValidVisaPerPerson = false,
        bool oneLastValidWorkPermitPerPerson = false,
        bool includeCompletedApplicationProcesses = false,
        bool includeCancelledApplicationProcesses = false)
    {
        var key = (category, subReport);
        // Default sub-report key for Passport is "by-validity"
        if (category == ReportDashboardCategory.Passport
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-validity");
        if (category == ReportDashboardCategory.WorkPermit
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport) || subReport == "by-validity"))
            key = (category, "by-days-remaining");
        if (category == ReportDashboardCategory.VisaExtension
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "visa-state");
        if (category == ReportDashboardCategory.Application
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-progress");
        if (category == ReportDashboardCategory.Education
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-level");
        if (category == ReportDashboardCategory.PositionHistory
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-status");

        IReportDashboardQueryService service = RealSubReports.Contains(key) ? _real : _mock;
        return service.LoadPanel(
            objectSpace, personType, category, projectKey, dateRangeMonths, subReport,
            includeArchivedPersons, oneLastValidVisaPerPerson, oneLastValidWorkPermitPerPerson,
            includeCompletedApplicationProcesses, includeCancelledApplicationProcesses);
    }
}