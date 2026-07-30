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
    /// Application (direct migration) On Process (A) / Process Complete promote via <see cref="RealSubReports"/>.
    /// Application (via ministry) promotes one sub-report at a time via <see cref="RealSubReports"/>.
    /// </summary>
    private static readonly HashSet<(ReportDashboardCategory Category, string SubReport)> RealSubReports =
    [
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryInvitationOnProcessKey),
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryInvitationOnProcessVKey),
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryVisaExtOnProcessKey),
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryVisaExtOnProcessVKey),
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryOtherOnProcessKey),
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryInvitationCompletedKey),
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryInvitationCompletedVKey),
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryVisaExtCompletedKey),
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryVisaExtCompletedVKey),
        (ReportDashboardCategory.ApplicationViaMinistry,
            ReportDashboardCatalog.AppViaMinistryOtherCompletedKey),
        (ReportDashboardCategory.ApplicationDirectMigration,
            ReportDashboardCatalog.AppDirectOnProcessAKey),
        (ReportDashboardCategory.ApplicationDirectMigration,
            ReportDashboardCatalog.AppDirectProcessCompleteKey),
        (ReportDashboardCategory.Passport, "by-validity"),
        (ReportDashboardCategory.Passport, "by-type"),
        (ReportDashboardCategory.Passport, "by-citizenship"),
        (ReportDashboardCategory.WorkPermit, "active-by-project"),
        (ReportDashboardCategory.WorkPermit, "by-days-remaining"),
        (ReportDashboardCategory.WorkPermit, "extension-result"),
        (ReportDashboardCategory.Invitation, "ready-by-project"),
        (ReportDashboardCategory.Invitation, "ready-by-period-category"),
        (ReportDashboardCategory.Invitation, "in-process"),
        (ReportDashboardCategory.Invitation, "in-process-by-period-category-type"),
        (ReportDashboardCategory.Invitation, "process-result"),
        (ReportDashboardCategory.Invitation, "process-result-by-period-category-type"),
        (ReportDashboardCategory.Invitation, "used"),
        (ReportDashboardCategory.Invitation, "used-by-period-category-type"),
        (ReportDashboardCategory.Invitation, "valid-until"),
        (ReportDashboardCategory.VisaExtension, "active-by-project"),
        (ReportDashboardCategory.VisaExtension, "by-period-category-type"),
        (ReportDashboardCategory.VisaExtension, "extension-required"),
        (ReportDashboardCategory.VisaExtension, "on-extension"),
        (ReportDashboardCategory.VisaExtension, "on-extension-by-period-category-type"),
        (ReportDashboardCategory.VisaExtension, "by-days-remaining"),
        (ReportDashboardCategory.VisaExtension, "extension-result"),
        (ReportDashboardCategory.VisaExtension, "extension-result-by-period-category-type"),
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
        (ReportDashboardCategory.IncompletePersons, "by-missing-area"),
        (ReportDashboardCategory.PersonSearch, ReportDashboardCatalog.PersonSearchByNameKey),
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
        if (category == ReportDashboardCategory.Registration)
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
        bool validVisaPersonsOnly = true,
        string? searchTerm = null)
    {
        if (category == ReportDashboardCategory.Registration)
        {
            return _real.LoadPanel(
                objectSpace, personType, category, projectKey, dateRangeMonths, subReport,
                includeArchivedPersons, oneLastValidVisaPerPerson, oneLastValidWorkPermitPerPerson,
                includeCompletedApplicationProcesses, includeCancelledApplicationProcesses,
                validVisaPersonsOnly, searchTerm);
        }

        if (category == ReportDashboardCategory.PersonSearch
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
        {
            subReport = ReportDashboardCatalog.PersonSearchByNameKey;
        }

        if (category == ReportDashboardCategory.ApplicationViaMinistry
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
        {
            subReport = ReportDashboardCatalog.AppViaMinistryInvitationOnProcessKey;
        }

        if (category == ReportDashboardCategory.ApplicationDirectMigration
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)
                || subReport == ReportDashboardCatalog.ApplicationStatusSubReportKey))
        {
            subReport = ReportDashboardCatalog.AppDirectOnProcessAKey;
        }

        var key = (category, subReport);
        if (RealSubReports.Contains(key))
        {
            return _real.LoadPanel(
                objectSpace, personType, category, projectKey, dateRangeMonths, subReport,
                includeArchivedPersons, oneLastValidVisaPerPerson, oneLastValidWorkPermitPerPerson,
                includeCompletedApplicationProcesses, includeCancelledApplicationProcesses,
                validVisaPersonsOnly, searchTerm);
        }
        // Default sub-report key for Passport is "by-validity"
        if (category == ReportDashboardCategory.Passport
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "by-validity");
        if (category == ReportDashboardCategory.WorkPermit
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)))
            key = (category, "active-by-project");
        if (category == ReportDashboardCategory.WorkPermit
            && subReport == "by-validity")
            key = (category, "by-days-remaining");
        if (category == ReportDashboardCategory.VisaExtension
            && (subReport == "default" || string.IsNullOrWhiteSpace(subReport)
                || subReport is "app-progress" or "visa-state"))
            key = (category, "active-by-project");
        if (category == ReportDashboardCategory.VisaExtension
            && subReport is "by-category" or "by-type" or "by-period")
            key = (category, "by-period-category-type");
        if (category == ReportDashboardCategory.VisaExtension
            && subReport == "extension-required-by-period-category-type")
            key = (category, "extension-required");
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
        if (category == ReportDashboardCategory.Invitation && subReport == "expired")
            key = (category, "valid-until");
        if (category == ReportDashboardCategory.Invitation && subReport == "rejected-by-project")
            key = (category, "process-result");
        if (category == ReportDashboardCategory.Invitation && subReport == "rejected-by-period-category-type")
            key = (category, "process-result-by-period-category-type");

        IReportDashboardQueryService service = RealSubReports.Contains(key) ? _real : _mock;
        return service.LoadPanel(
            objectSpace, personType, category, projectKey, dateRangeMonths, subReport,
            includeArchivedPersons, oneLastValidVisaPerPerson, oneLastValidWorkPermitPerPerson,
            includeCompletedApplicationProcesses, includeCancelledApplicationProcesses,
            validVisaPersonsOnly, searchTerm);
    }
}
