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
    /// Application category is always real (Application Status combined label).
    /// </summary>
    private static readonly HashSet<(ReportDashboardCategory Category, string SubReport)> RealSubReports =
    [
        (ReportDashboardCategory.Passport, "by-validity"),
        (ReportDashboardCategory.Passport, "by-type"),
        (ReportDashboardCategory.Passport, "by-citizenship"),
        (ReportDashboardCategory.WorkPermit, "by-days-remaining"),
        (ReportDashboardCategory.VisaExtension, "visa-state"),
        (ReportDashboardCategory.VisaExtension, "by-category"),
        (ReportDashboardCategory.VisaExtension, "by-type"),
        (ReportDashboardCategory.VisaExtension, "by-period"),
        (ReportDashboardCategory.VisaExtension, "by-days-remaining"),
        (ReportDashboardCategory.AddressOfResidence, "by-validity"),
        (ReportDashboardCategory.AddressOfResidence, "by-region"),
        (ReportDashboardCategory.AddressOfResidence, "by-city"),
        (ReportDashboardCategory.AddressOfResidence, "by-address-type"),
        (ReportDashboardCategory.AddressOfResidence, "by-address"),
        (ReportDashboardCategory.Education, "by-level"),
        (ReportDashboardCategory.Education, "by-country"),
        (ReportDashboardCategory.Education, "by-specialty"),
        (ReportDashboardCategory.PositionHistory, "by-position"),
        (ReportDashboardCategory.PositionHistory, "by-actual-position"),
        (ReportDashboardCategory.Subcontractor, "by-company"),
        (ReportDashboardCategory.MedicalRecord, "by-validity"),
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

    public ReportDashboardSubReportListing ListSubReports(
        IObjectSpace objectSpace,
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string projectKey,
        int dateRangeMonths = 6,
        bool includeCompletedApplicationProcesses = false,
        bool includeCancelledApplicationProcesses = false)
    {
        if (category == ReportDashboardCategory.Application
            || category == ReportDashboardCategory.Registration)
        {
            return _real.ListSubReports(
                objectSpace, personType, category, projectKey, dateRangeMonths,
                includeCompletedApplicationProcesses, includeCancelledApplicationProcesses);
        }

        return _mock.ListSubReports(
            objectSpace, personType, category, projectKey, dateRangeMonths,
            includeCompletedApplicationProcesses, includeCancelledApplicationProcesses);
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
        bool includeCancelledApplicationProcesses = false,
        bool validVisaPersonsOnly = true)
    {
        if (category == ReportDashboardCategory.Application
            || category == ReportDashboardCategory.Registration)
        {
            return _real.LoadPanel(
                objectSpace, personType, category, projectKey, dateRangeMonths, subReport,
                includeArchivedPersons, oneLastValidVisaPerPerson, oneLastValidWorkPermitPerPerson,
                includeCompletedApplicationProcesses, includeCancelledApplicationProcesses,
                validVisaPersonsOnly);
        }

        var key = (category, subReport);
        // Default sub-report key for Passport is "by-validity"
        if (category == ReportDashboardCategory.Passport
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-validity");
        if (category == ReportDashboardCategory.WorkPermit
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport) || subReport == "by-validity"))
            key = (category, "by-days-remaining");
        if (category == ReportDashboardCategory.VisaExtension
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport) || subReport == "app-progress"))
            key = (category, "visa-state");
        if (category == ReportDashboardCategory.Education
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-level");
        if (category == ReportDashboardCategory.PositionHistory
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport) || subReport == "by-status"))
            key = (category, "by-position");
        if (category == ReportDashboardCategory.Subcontractor
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-company");
        if (category == ReportDashboardCategory.MedicalRecord
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-validity");

        IReportDashboardQueryService service = RealSubReports.Contains(key) ? _real : _mock;
        return service.LoadPanel(
            objectSpace, personType, category, projectKey, dateRangeMonths, subReport,
            includeArchivedPersons, oneLastValidVisaPerPerson, oneLastValidWorkPermitPerPerson,
            includeCompletedApplicationProcesses, includeCancelledApplicationProcesses,
            validVisaPersonsOnly);
    }
}
