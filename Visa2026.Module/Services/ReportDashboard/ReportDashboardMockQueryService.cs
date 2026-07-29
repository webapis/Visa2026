using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Prototype mock with hard-coded data for all categories x sub-reports.
/// Swap to ReportDashboardQueryService in Startup.cs when UX is finalised.
/// </summary>
public sealed class ReportDashboardMockQueryService : IReportDashboardQueryService
{
    private static readonly List<ReportDashboardProjectChip> Projects =
    [
        new() { Key = "All",              Label = "All",              Count = 147 },
        new() { Key = "Gurlusyk UZT",     Label = "Gurlusyk UZT",    Count = 63  },
        new() { Key = "Gaz Stansiasy",    Label = "Gaz Stansiasy",   Count = 35  },
        new() { Key = "Seismiki Barlag",  Label = "Seismiki Barlag", Count = 28  },
        new() { Key = "Elektrik Stansia", Label = "Elektrik Stansia",Count = 21  },
    ];

    private static readonly Dictionary<(ReportDashboardPersonType, ReportDashboardCategory), int> Counts = new()
    {
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.ApplicationViaMinistry)] = 70,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.ApplicationDirectMigration)] = 28,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.VisaExtension)] = 63,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.Invitation)]    = 45,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.Registration)]  = 89,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.WorkPermit)]    = 71,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.Travel)]        = 28,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.AddressOfResidence)] = 84,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.BorderZone)]    = 18,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.Passport)]         = 147,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.Education)]        = 92,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.PositionHistory)]  = 85,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.Subcontractor)]    = 85,
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.MedicalRecord)]    = 78,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.ApplicationViaMinistry)] = 20,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.ApplicationDirectMigration)] = 8,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.VisaExtension)] = 31,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.Invitation)]    = 22,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.Registration)]  = 52,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.WorkPermit)]    = 0,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.Travel)]        = 14,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.AddressOfResidence)] = 48,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.BorderZone)]    = 0,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.Passport)]         = 82,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.Education)]        = 40,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.PositionHistory)]  = 0,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.Subcontractor)]    = 52,
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.MedicalRecord)]    = 45,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.ApplicationViaMinistry)] = 7,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.ApplicationDirectMigration)] = 3,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.VisaExtension)] = 12,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.Invitation)]    = 8,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.Registration)]  = 4,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.WorkPermit)]    = 0,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.Travel)]        = 6,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.AddressOfResidence)] = 3,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.BorderZone)]    = 0,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.Passport)]         = 12,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.Education)]        = 3,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.PositionHistory)]  = 0,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.Subcontractor)]    = 8,
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.MedicalRecord)]    = 4,
        // All = Employees + Family Members + Temporary Visitors
        [(ReportDashboardPersonType.All, ReportDashboardCategory.ApplicationViaMinistry)] = 97,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.ApplicationDirectMigration)] = 39,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.VisaExtension)]    = 106,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.Invitation)]       = 75,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.Registration)]     = 145,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.WorkPermit)]       = 71,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.Travel)]           = 48,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.AddressOfResidence)] = 135,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.BorderZone)]       = 18,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.Passport)]         = 241,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.Education)]        = 135,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.PositionHistory)]  = 85,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.Subcontractor)]    = 145,
        [(ReportDashboardPersonType.All, ReportDashboardCategory.MedicalRecord)]    = 127,
    };

    public ReportDashboardSnapshot LoadSnapshot(
        IObjectSpace objectSpace,
        int dateRangeMonths = 6,
        ReportDashboardPersonType personType = ReportDashboardPersonType.All)
    {
        _ = objectSpace;
        _ = dateRangeMonths;
        _ = personType;
        return new() {
            Projects = Projects,
            CategoryCounts = Counts,
            PersonRoleCounts = new Dictionary<ReportDashboardPersonType, int>
            {
                [ReportDashboardPersonType.All] = 241,
                [ReportDashboardPersonType.Employees] = 147,
                [ReportDashboardPersonType.FamilyMembers] = 82,
                [ReportDashboardPersonType.TemporaryVisitors] = 12,
            }
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
        _ = objectSpace;
        _ = personType;
        _ = projectKey;
        _ = dateRangeMonths;
        _ = includeCompletedApplicationProcesses;
        _ = includeCancelledApplicationProcesses;
        return new ReportDashboardSubReportListing
        {
            SubReports = ReportDashboardCatalog.SubReports(category),
            Counts = new Dictionary<string, int>(StringComparer.Ordinal)
        };
    }

    public ReportDashboardPanelData LoadPanel(
        IObjectSpace _,
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
        // Include-archived toggle is for WP/Education/etc.
        if (includeArchivedPersons) { /* no-op */ }
        if (validVisaPersonsOnly) { /* no-op */ }
        // Application completed/cancelled filters are real-view only (Hybrid).
        if (includeCompletedApplicationProcesses || includeCancelledApplicationProcesses) { /* no-op */ }
        var applyOneLastVisa = oneLastValidVisaPerPerson
            && category == ReportDashboardCategory.VisaExtension
            && ReportDashboardCatalog.SubReportCountsValidVisas(subReport);
        var applyOneLastWp = oneLastValidWorkPermitPerPerson
            && category == ReportDashboardCategory.WorkPermit
            && ReportDashboardCatalog.SubReportCountsValidWorkPermits(subReport);
        return (category, subReport) switch
        {
            // Application (via ministry) — On Process / Completed by Invitation / Visa Extension / Other
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryInvitationOnProcessKey) =>
                Build(personType, category, subReport, AppViaMinistryInvitationOnProcessByProject(), projectKey,
                    subReportLabel: "Invitation on Process (P)"),
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryInvitationOnProcessVKey) =>
                Build(personType, category, subReport, AppViaMinistryInvitationOnProcessByPeriodCategoryType(), projectKey,
                    subReportLabel: "Invitation on Process (V)"),
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryVisaExtOnProcessKey) =>
                Build(personType, category, subReport, AppViaMinistryVisaExtOnProcessByProject(), projectKey,
                    subReportLabel: "Visa Extension on Process (P)"),
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryVisaExtOnProcessVKey) =>
                Build(personType, category, subReport, AppViaMinistryVisaExtOnProcessByPeriodCategoryType(), projectKey,
                    subReportLabel: "Visa Extension on Process (V)"),
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryOtherOnProcessKey) =>
                Build(personType, category, subReport, AppViaMinistryOtherOnProcessByProject(), projectKey,
                    subReportLabel: "Other on Process (P)"),
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryInvitationCompletedKey) =>
                Build(personType, category, subReport, AppViaMinistryInvitationCompletedByProject(), projectKey,
                    subReportLabel: "Invitation Completed (P)"),
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryInvitationCompletedVKey) =>
                Build(personType, category, subReport, AppViaMinistryInvitationCompletedByPeriodCategoryType(), projectKey,
                    subReportLabel: "Invitation Completed (V)"),
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryVisaExtCompletedKey) =>
                Build(personType, category, subReport, AppViaMinistryVisaExtCompletedByProject(), projectKey,
                    subReportLabel: "Visa Extension Completed (P)"),
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryVisaExtCompletedVKey) =>
                Build(personType, category, subReport, AppViaMinistryVisaExtCompletedByPeriodCategoryType(), projectKey,
                    subReportLabel: "Visa Extension Completed (V)"),
            (ReportDashboardCategory.ApplicationViaMinistry, ReportDashboardCatalog.AppViaMinistryOtherCompletedKey) =>
                Build(personType, category, subReport, AppViaMinistryOtherCompletedByProject(), projectKey,
                    subReportLabel: "Other Process Completed (P)"),
            (ReportDashboardCategory.ApplicationViaMinistry, _) =>
                Build(personType, category, ReportDashboardCatalog.AppViaMinistryInvitationOnProcessKey,
                    AppViaMinistryInvitationOnProcessByProject(), projectKey,
                    subReportLabel: "Invitation on Process (P)"),
            (ReportDashboardCategory.ApplicationDirectMigration, _) => Build(
                personType, category, subReport, ApplicationByStatusDirectMigration(), projectKey,
                subReportLabel: "Application Status"),
            // Visa (formerly Visa Extension)
            (ReportDashboardCategory.VisaExtension, "on-extension") => Build(personType, category, subReport, VisaOnExtensionByProject(), projectKey),
            (ReportDashboardCategory.VisaExtension, "app-progress") => Build(personType, category, "on-extension", VisaOnExtensionByProject(), projectKey),
            (ReportDashboardCategory.VisaExtension, "on-extension-by-period-category-type") =>
                Build(personType, category, subReport, VisaOnExtensionByPeriodCategoryType(), projectKey),
            (ReportDashboardCategory.VisaExtension, "active-by-project") =>
                Build(personType, category, subReport, VisaActiveByProject(), projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, "by-period-category-type") => Build(personType, category, subReport, VisaByPeriodCategoryType(), projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, "extension-required") =>
                BuildExtensionRequiredByDays(personType, category, subReport, VisaExtensionRequiredByDays(), projectKey),
            // Legacy key → same days-remaining panel (tab removed; (P)/(V) no longer apply).
            (ReportDashboardCategory.VisaExtension, "extension-required-by-period-category-type") =>
                BuildExtensionRequiredByDays(personType, category, "extension-required", VisaExtensionRequiredByDays(), projectKey),
            (ReportDashboardCategory.VisaExtension, "by-category") => Build(personType, category, "by-period-category-type", VisaByPeriodCategoryType(), projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, "by-type") => Build(personType, category, "by-period-category-type", VisaByPeriodCategoryType(), projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, "by-period") => Build(personType, category, "by-period-category-type", VisaByPeriodCategoryType(), projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, "by-days-remaining") => Build(personType, category, subReport, VisaByDaysRemaining(), projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, "extension-result") =>
                Build(personType, category, subReport, VisaExtensionResultByProject(), projectKey),
            (ReportDashboardCategory.VisaExtension, "extension-result-by-period-category-type") =>
                Build(personType, category, subReport, VisaExtensionResultByPeriodCategoryType(), projectKey),
            (ReportDashboardCategory.VisaExtension, "visa-state") =>
                Build(personType, category, "active-by-project", VisaActiveByProject(), projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, _) =>
                Build(personType, category, "active-by-project", VisaActiveByProject(), projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            // Invitation (legacy issued-inv → Ready by Project)
            (ReportDashboardCategory.Invitation, "ready-by-period-category") => Build(personType, category, subReport, InvitationReadyByPeriodCategory(), projectKey),
            (ReportDashboardCategory.Invitation, "in-process") => Build(personType, category, subReport, InvitationInProcessByProject(), projectKey),
            (ReportDashboardCategory.Invitation, "in-process-by-period-category-type") =>
                Build(personType, category, subReport, InvitationInProcessByPeriodCategoryType(), projectKey),
            (ReportDashboardCategory.Invitation, "process-result") =>
                Build(personType, category, subReport, InvitationProcessResultByProject(), projectKey),
            (ReportDashboardCategory.Invitation, "process-result-by-period-category-type") =>
                Build(personType, category, subReport, InvitationProcessResultByPeriodCategoryType(), projectKey),
            (ReportDashboardCategory.Invitation, "rejected-by-project") =>
                Build(personType, category, "process-result", InvitationProcessResultByProject(), projectKey),
            (ReportDashboardCategory.Invitation, "rejected-by-period-category-type") =>
                Build(personType, category, "process-result-by-period-category-type", InvitationProcessResultByPeriodCategoryType(), projectKey),
            (ReportDashboardCategory.Invitation, "used") => Build(personType, category, subReport, InvitationUsed(), projectKey),
            (ReportDashboardCategory.Invitation, "used-by-period-category-type") =>
                Build(personType, category, subReport, InvitationUsedByPeriodCategoryType(), projectKey),
            (ReportDashboardCategory.Invitation, "valid-until") => Build(personType, category, subReport, InvitationValidUntil(), projectKey),
            (ReportDashboardCategory.Invitation, "expired") => Build(personType, category, subReport, InvitationValidUntil(), projectKey),
            (ReportDashboardCategory.Invitation, _) => Build(personType, category, subReport, InvitationReadyByProject(), projectKey),
            // Registration
            (ReportDashboardCategory.Registration, "check-in-by-project") =>
                Build(personType, category, subReport, RegistrationCheckInByProject(), projectKey),
            (ReportDashboardCategory.Registration, "check-in-by-period-category-type") =>
                Build(personType, category, subReport, RegistrationCheckInByPeriodCategoryType(), projectKey),
            (ReportDashboardCategory.Registration, "check-in-by-city") =>
                Build(personType, category, subReport, RegistrationCheckInByCity(), projectKey),
            (ReportDashboardCategory.Registration, "on-process") =>
                Build(personType, category, subReport, RegistrationOnProcess(), projectKey),
            (ReportDashboardCategory.Registration, _) => Build(personType, category, subReport, RegistrationByApplicationType(subReport), projectKey),
            // Work Permit
            (ReportDashboardCategory.WorkPermit, "active-by-project") =>
                Build(personType, category, subReport, WorkPermitActiveByProject(), projectKey, excelConfigured: true, oneLastValidVisaPerPerson: applyOneLastWp),
            (ReportDashboardCategory.WorkPermit, "on-extension") =>
                Build(personType, category, subReport, WorkPermitOnExtensionByProject(), projectKey, excelConfigured: true),
            (ReportDashboardCategory.WorkPermit, "extension-result") =>
                Build(personType, category, subReport, WorkPermitExtensionResultByProject(), projectKey, excelConfigured: true),
            (ReportDashboardCategory.WorkPermit, "by-status")    => Build(personType, category, subReport, WorkPermitByStatus(),   projectKey, excelConfigured: true),
            (ReportDashboardCategory.WorkPermit, _)              => Build(personType, category, subReport, WorkPermitByDaysRemaining(), projectKey, excelConfigured: true, oneLastValidVisaPerPerson: applyOneLastWp),
            // Travel
            (ReportDashboardCategory.Travel, "by-status")        => Build(personType, category, subReport, TravelByStatus(), projectKey),
            (ReportDashboardCategory.Travel, _)                  => Build(personType, category, subReport, TravelByMonth(),  projectKey),
            // Address of Residence
            (ReportDashboardCategory.AddressOfResidence, "by-region") => Build(personType, category, subReport, AddressByRegion(), projectKey),
            (ReportDashboardCategory.AddressOfResidence, "by-city")   => Build(personType, category, subReport, AddressByCity(), projectKey),
            (ReportDashboardCategory.AddressOfResidence, "by-address-type") => Build(personType, category, subReport, AddressByAddressType(), projectKey),
            (ReportDashboardCategory.AddressOfResidence, "by-address") => Build(personType, category, subReport, AddressByAddress(), projectKey),
            (ReportDashboardCategory.AddressOfResidence, _)           => Build(personType, category, subReport, AddressByValidity(), projectKey),
            // Border Zone
            (ReportDashboardCategory.BorderZone, "by-zone")      => Build(personType, category, subReport, BorderZoneByZone(),     projectKey),
            (ReportDashboardCategory.BorderZone, _)              => Build(personType, category, subReport, BorderZoneByValidity(), projectKey),
            // Passport (fully implemented)
            (ReportDashboardCategory.Passport, "by-type")        => Build(personType, category, subReport, PassportByType(),        projectKey),
            (ReportDashboardCategory.Passport, "by-citizenship")  => Build(personType, category, subReport, PassportByCitizenship(), projectKey),
            (ReportDashboardCategory.Passport, _)                => Build(personType, category, subReport, PassportByValidity(),    projectKey),
            // Education (mock until vw_rd_education)
            (ReportDashboardCategory.Education, "by-country")   => Build(personType, category, subReport, EducationByCountry(),   projectKey),
            (ReportDashboardCategory.Education, "by-specialty") => Build(personType, category, subReport, EducationBySpecialty(), projectKey),
            (ReportDashboardCategory.Education, _)              => Build(personType, category, subReport, EducationByLevel(),     projectKey),
            // Position History (mock until vw_rd_position_history)
            (ReportDashboardCategory.PositionHistory, "by-actual-position") => Build(personType, category, subReport, PositionHistoryByActualPosition(), projectKey),
            (ReportDashboardCategory.PositionHistory, _)             => Build(personType, category, subReport, PositionHistoryByPosition(), projectKey),
            // Subcontractor (Person.Subcontractor — By Company)
            (ReportDashboardCategory.Subcontractor, _) => Build(personType, category, subReport, SubcontractorByCompany(), projectKey),
            // Medical Records
            (ReportDashboardCategory.MedicalRecord, _) => Build(personType, category, subReport, MedicalRecordByValidity(), projectKey),
            _ => Build(personType, category, subReport, [], projectKey)
        };
    }

    // ===== Visa (Visa Extension) — sub-reports ============================

    /// On Extension By Project: Status = Project · ProcessState (Issued excluded)
    private static List<ReportDashboardPreviewRow> VisaOnExtensionByProject() =>
    [
        R6("Mehmet Yilmaz",       "Gurlusyk UZT",     "APP-2026-0101", "Jan 08, 2026", "42", "Gurlusyk UZT · Being Prepared", "st-cat-1"),
        R6("Viktor Petrov",       "Gurlusyk UZT",     "APP-2026-0112", "Jan 14, 2026", "55", "Gurlusyk UZT · Being Prepared", "st-cat-1"),
        R6("John Smith",          "Seismiki Barlag",  "APP-2026-0120", "Jan 20, 2026", "18", "Seismiki Barlag · Processing", "st-cat-2"),
        R6("Kemal Aydin",         "Gaz Stansiasy",    "APP-2026-0131", "Jan 28, 2026", "88", "Gaz Stansiasy · 1st Review Approved", "st-cat-3"),
        R6("Alina Makarova",      "Gurlusyk UZT",     "APP-2026-0144", "Feb 02, 2026", "120", "Gurlusyk UZT · 2nd Review Started", "st-cat-1"),
        R6("Hans Muller",         "Gaz Stansiasy",    "APP-2026-0162", "Feb 12, 2026", "65", "Gaz Stansiasy · Being Prepared", "st-cat-3"),
        R6("Oleg Kovalev",        "Seismiki Barlag",  "APP-2026-0171", "Feb 18, 2026", "22", "Seismiki Barlag · 1st Review Rejected", "st-cat-2"),
        R6("Leyli Annagurbanowa", "Elektrik Stansia", "APP-2026-0180", "Feb 22, 2026", "9", "Elektrik Stansia · Process Cancelled", "st-cat-4"),
        R6("Serdar Geldiyew",     "Gurlusyk UZT",     "APP-2026-0191", "Mar 01, 2026", "100", "Gurlusyk UZT · Cleared agreement - Energetika", "st-cat-5"),
        R6("Kemal Aydin",         "Merkez ofis",      "APP-2026-0195", "Mar 04, 2026", "70", "Merkez ofis · Processing", "st-cat-2"),
    ];

    /// On Extension By Period · Category · Type: Status = Period · Category · Type · ProcessState (Issued excluded)
    private static List<ReportDashboardPreviewRow> VisaOnExtensionByPeriodCategoryType() =>
    [
        R6("Mehmet Yilmaz",       "Gurlusyk UZT",     "APP-2026-0101", "Jan 08, 2026", "42", "6 months · Multiple entry · WP — Work visa · Being Prepared", "st-cat-1"),
        R6("Viktor Petrov",       "Gurlusyk UZT",     "APP-2026-0112", "Jan 14, 2026", "55", "6 months · Multiple entry · WP — Work visa · Being Prepared", "st-cat-1"),
        R6("John Smith",          "Seismiki Barlag",  "APP-2026-0120", "Jan 20, 2026", "18", "6 months · Multiple entry · WP — Work visa · Processing", "st-cat-2"),
        R6("Kemal Aydin",         "Gaz Stansiasy",    "APP-2026-0131", "Jan 28, 2026", "88", "6 months · Multiple entry · FM — Family · 1st Review Approved", "st-cat-3"),
        R6("Alina Makarova",      "Gurlusyk UZT",     "APP-2026-0144", "Feb 02, 2026", "120", "6 months · Multiple entry · FM — Family · 2nd Review Started", "st-cat-1"),
        R6("Hans Muller",         "Gaz Stansiasy",    "APP-2026-0162", "Feb 12, 2026", "65", "3 months · Single entry · WP — Work visa · Being Prepared", "st-cat-3"),
        R6("Oleg Kovalev",        "Seismiki Barlag",  "APP-2026-0171", "Feb 18, 2026", "22", "6 months · Multiple entry · WP — Work visa · 1st Review Rejected", "st-cat-2"),
        R6("Leyli Annagurbanowa", "Elektrik Stansia", "APP-2026-0180", "Feb 22, 2026", "9", "6 months · Multiple entry · FM — Family · Process Cancelled", "st-cat-4"),
        R6("Serdar Geldiyew",     "Gurlusyk UZT",     "APP-2026-0191", "Mar 01, 2026", "100", "6 months · Multiple entry · WP — Work visa · Cleared agreement - Energetika", "st-cat-5"),
        R6("Kemal Aydin",         "Merkez ofis",      "APP-2026-0195", "Mar 04, 2026", "70", "6 months · Multiple entry · FM — Family · Processing", "st-cat-2"),
    ];

    /// Extension Result (P): terminal outcomes; Status = Project · ProcessState
    private static List<ReportDashboardPreviewRow> VisaExtensionResultByProject() =>
    [
        R6p("Mehmet Yilmaz",       "Gurlusyk UZT",     "APP-2025-2101", "Oct 08, 2025", "Gurlusyk UZT · Process Issued", "st-approved"),
        R6p("Viktor Petrov",       "Gurlusyk UZT",     "APP-2025-2112", "Oct 14, 2025", "Gurlusyk UZT · Process Issued", "st-approved"),
        R6p("John Smith",          "Seismiki Barlag",  "APP-2025-2120", "Oct 20, 2025", "Seismiki Barlag · Process Issued", "st-approved"),
        R6p("Kemal Aydin",         "Gaz Stansiasy",    "APP-2025-2131", "Nov 02, 2025", "Gaz Stansiasy · Process Issued", "st-approved"),
        R6p("Alina Makarova",      "Gurlusyk UZT",     "APP-2025-2144", "Nov 12, 2025", "Gurlusyk UZT · Process Issued", "st-approved"),
        R6p("Hans Muller",         "Gaz Stansiasy",    "APP-2025-2162", "Nov 22, 2025", "Gaz Stansiasy · Process Cancelled", "st-expiring"),
        R6p("Oleg Kovalev",        "Seismiki Barlag",  "APP-2025-2171", "Dec 01, 2025", "Seismiki Barlag · Process Rejected", "st-expiring"),
        R6p("Leyli Annagurbanowa", "Elektrik Stansia", "APP-2025-2180", "Dec 08, 2025", "Elektrik Stansia · Process Cancelled", "st-expiring"),
        R6p("Serdar Geldiyew",     "Gurlusyk UZT",     "APP-2025-2191", "Dec 15, 2025", "Gurlusyk UZT · Process Issued", "st-approved"),
        R6p("Kemal Aydin",         "Merkez ofis",      "APP-2025-2195", "Dec 20, 2025", "Merkez ofis · Process Rejected", "st-expiring"),
    ];

    /// Extension Result (V): terminal outcomes; Status = Period · Category · Type · ProcessState
    private static List<ReportDashboardPreviewRow> VisaExtensionResultByPeriodCategoryType() =>
    [
        R6p("Mehmet Yilmaz",       "Gurlusyk UZT",     "APP-2025-2101", "Oct 08, 2025", "6 months · Multiple entry · WP — Work visa · Process Issued", "st-approved"),
        R6p("Viktor Petrov",       "Gurlusyk UZT",     "APP-2025-2112", "Oct 14, 2025", "6 months · Multiple entry · WP — Work visa · Process Issued", "st-approved"),
        R6p("John Smith",          "Seismiki Barlag",  "APP-2025-2120", "Oct 20, 2025", "6 months · Multiple entry · WP — Work visa · Process Issued", "st-approved"),
        R6p("Kemal Aydin",         "Gaz Stansiasy",    "APP-2025-2131", "Nov 02, 2025", "6 months · Multiple entry · FM — Family · Process Issued", "st-approved"),
        R6p("Alina Makarova",      "Gurlusyk UZT",     "APP-2025-2144", "Nov 12, 2025", "6 months · Multiple entry · FM — Family · Process Issued", "st-approved"),
        R6p("Hans Muller",         "Gaz Stansiasy",    "APP-2025-2162", "Nov 22, 2025", "3 months · Single entry · WP — Work visa · Process Cancelled", "st-expiring"),
        R6p("Oleg Kovalev",        "Seismiki Barlag",  "APP-2025-2171", "Dec 01, 2025", "6 months · Multiple entry · WP — Work visa · Process Rejected", "st-expiring"),
        R6p("Leyli Annagurbanowa", "Elektrik Stansia", "APP-2025-2180", "Dec 08, 2025", "6 months · Multiple entry · FM — Family · Process Cancelled", "st-expiring"),
        R6p("Serdar Geldiyew",     "Gurlusyk UZT",     "APP-2025-2191", "Dec 15, 2025", "6 months · Multiple entry · WP — Work visa · Process Issued", "st-approved"),
        R6p("Kemal Aydin",         "Merkez ofis",      "APP-2025-2195", "Dec 20, 2025", "6 months · Multiple entry · FM — Family · Process Rejected", "st-expiring"),
    ];

    /// Visa State: where each person's visa extension process stands
    private static List<ReportDashboardPreviewRow> VisaByState() =>
    [
        // ColumnA = visa#, ColumnB = expiry, Status = visa state label
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "V-2025-1012", "Oct 12, 2026", "Extension Started",      "st-pending"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "V-2025-1031", "Mar 15, 2027", "Extension Started",      "st-pending"),
        R("John Smith",          "Seismiki Barlag",  "V-2025-0904", "Jan 04, 2026", "Extension to be Started","st-expiring"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "V-2025-1130", "Nov 30, 2026", "Extension Started",      "st-pending"),
        R("Alina Makarova",      "Gurlusyk UZT",     "V-2025-1018", "Feb 18, 2027", "Extension Not Required", "st-approved"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "V-2025-0801", "Sep 01, 2025", "Extension Rejected",     "st-expiring"),
        R("Hans Muller",         "Gaz Stansiasy",    "V-2025-0722", "Apr 22, 2026", "Extension Started",      "st-pending"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "V-2025-0610", "May 10, 2026", "Extension to be Started","st-expiring"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "V-2025-1201", "Dec 01, 2026", "Extension Not Required", "st-approved"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "V-2025-0614", "Jun 14, 2026", "Extension Cancelled",    "st-expiring"),
    ];

    // ===== Application (via ministry) =====================================
    // Process segment = ApplicationProgress.StatusListLabel style
    // (LookupCatalogStrings application-state + " - {ministry}" when applicable).

    private static List<ReportDashboardPreviewRow> AppViaMinistryInvitationOnProcessByProject() =>
    [
        R9("Leyli Annagurbanowa", "Elektrik Stansia", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0301", "Mar 02, 2026",
            "Elektrik Stansia · At office", "st-pending"),
        R9("Cary Durdyyew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0312", "Mar 08, 2026",
            "Gurlusyk UZT · Sent for agreement - Türkmenergo", "st-pending"),
        R9("Mehmet Yilmaz", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0320", "Mar 15, 2026",
            "Gurlusyk UZT · Cleared agreement - Energetika", "st-approved"),
        R9("Viktor Petrov", "Gaz Stansiasy", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0334", "Mar 22, 2026",
            "Gaz Stansiasy · Sent for agreement - Gurluşyk", "st-pending"),
        R9("Kemal Aydin", "Seismiki Barlag", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0341", "Apr 01, 2026",
            "Seismiki Barlag · Processing", "st-pending"),
    ];

    private static List<ReportDashboardPreviewRow> AppViaMinistryInvitationOnProcessByPeriodCategoryType() =>
    [
        R9("Leyli Annagurbanowa", "Elektrik Stansia", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0301", "Mar 02, 2026",
            "1 month · Double entry · BS1 — Business · At office", "st-pending"),
        R9("Cary Durdyyew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0312", "Mar 08, 2026",
            "6 months · Multiple entry · WP — Work visa · Sent for agreement - Türkmenergo", "st-pending"),
        R9("Mehmet Yilmaz", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0320", "Mar 15, 2026",
            "6 months · Multiple entry · WP — Work visa · Cleared agreement - Energetika", "st-approved"),
        R9("Viktor Petrov", "Gaz Stansiasy", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0334", "Mar 22, 2026",
            "3 months · Multiple entry · WP — Work visa · Sent for agreement - Gurluşyk", "st-pending"),
        R9("Kemal Aydin", "Seismiki Barlag", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0341", "Apr 01, 2026",
            "1 month · Single entry · BS1 — Business · Processing", "st-pending"),
    ];

    private static List<ReportDashboardPreviewRow> AppViaMinistryVisaExtOnProcessByProject() =>
    [
        R9("Hans Muller", "Gaz Stansiasy", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0402", "Feb 10, 2026",
            "Gaz Stansiasy · At office", "st-pending"),
        R9("Alina Makarova", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0415", "Feb 18, 2026",
            "Gurlusyk UZT · Sent for agreement - Türkmenergo", "st-pending"),
        R9("Oleg Kovalev", "Seismiki Barlag", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0428", "Mar 05, 2026",
            "Seismiki Barlag · Cleared agreement - Energetika", "st-approved"),
        R9("Serdar Geldiyew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0439", "Mar 19, 2026",
            "Gurlusyk UZT · Processing", "st-pending"),
    ];

    private static List<ReportDashboardPreviewRow> AppViaMinistryVisaExtOnProcessByPeriodCategoryType() =>
    [
        R9("Hans Muller", "Gaz Stansiasy", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0402", "Feb 10, 2026",
            "6 months · Multiple entry · WP — Work visa · At office", "st-pending"),
        R9("Alina Makarova", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0415", "Feb 18, 2026",
            "1 year · Double entry · FM — Family · Sent for agreement - Türkmenergo", "st-pending"),
        R9("Oleg Kovalev", "Seismiki Barlag", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0428", "Mar 05, 2026",
            "3 months · Multiple entry · BS1 — Business · Cleared agreement - Energetika", "st-approved"),
        R9("Serdar Geldiyew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2026-0439", "Mar 19, 2026",
            "6 months · Multiple entry · WP — Work visa · Processing", "st-pending"),
    ];

    private static List<ReportDashboardPreviewRow> AppViaMinistryOtherOnProcessByProject() =>
    [
        R7("John Smith", "Seismiki Barlag", "Inžener", "Çakylyk we RW", "APP-2026-0501", "Jan 20, 2026",
            "Seismiki Barlag · At office", "st-pending"),
        R7("Bayrammyrat Rejepow", "Elektrik Stansia", "Inžener", "Çakylyk we RW", "APP-2026-0514", "Feb 03, 2026",
            "Elektrik Stansia · Sent for agreement - Türkmengaz", "st-pending"),
        R7("Annaguly Hojayew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "APP-2026-0527", "Feb 25, 2026",
            "Gurlusyk UZT · Processing", "st-pending"),
    ];

    private static List<ReportDashboardPreviewRow> AppViaMinistryInvitationCompletedByProject() =>
    [
        R9("Kemal Aydin", "Gaz Stansiasy", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2025-9012", "Nov 12, 2025",
            "Gaz Stansiasy · Issued", "st-approved"),
        R9("Hans Muller", "Gaz Stansiasy", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2025-9018", "Nov 28, 2025",
            "Gaz Stansiasy · Rejected", "st-expiring"),
        R9("Leyli Annagurbanowa", "Elektrik Stansia", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2025-9030", "Dec 10, 2025",
            "Elektrik Stansia · Cancelled", "st-expiring"),
        R9("Cary Durdyyew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2025-9044", "Dec 22, 2025",
            "Gurlusyk UZT · Not received from ministry", "st-expiring"),
    ];

    private static List<ReportDashboardPreviewRow> AppViaMinistryInvitationCompletedByPeriodCategoryType() =>
    [
        R9("Kemal Aydin", "Gaz Stansiasy", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2025-9012", "Nov 12, 2025",
            "6 months · Multiple entry · WP — Work visa · Issued", "st-approved"),
        R9("Hans Muller", "Gaz Stansiasy", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2025-9018", "Nov 28, 2025",
            "6 months · Multiple entry · WP — Work visa · Rejected", "st-expiring"),
        R9("Leyli Annagurbanowa", "Elektrik Stansia", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2025-9030", "Dec 10, 2025",
            "1 month · Double entry · BS1 — Business · Cancelled", "st-expiring"),
        R9("Cary Durdyyew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "APP-2025-9044", "Dec 22, 2025",
            "6 months · Multiple entry · WP — Work visa · Not received from ministry", "st-expiring"),
    ];

    private static List<ReportDashboardPreviewRow> AppViaMinistryVisaExtCompletedByProject() =>
    [
        R11("Alina Makarova", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "A1711001", "A1802001", "APP-2025-9101", "Oct 05, 2025",
            "Gurlusyk UZT · Issued", "st-approved"),
        R11("Oleg Kovalev", "Seismiki Barlag", "Inžener", "Çakylyk we RW", "6 months", "WP", "A1711002", "", "APP-2025-9115", "Oct 18, 2025",
            "Seismiki Barlag · Rejected", "st-expiring"),
        R11("Serdar Geldiyew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "A1711003", "A1802003", "APP-2025-9128", "Nov 02, 2025",
            "Gurlusyk UZT · Not received from ministry", "st-expiring"),
    ];

    private static List<ReportDashboardPreviewRow> AppViaMinistryVisaExtCompletedByPeriodCategoryType() =>
    [
        R11("Alina Makarova", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "A1711001", "A1802001", "APP-2025-9101", "Oct 05, 2025",
            "1 year · Double entry · FM — Family · Issued", "st-approved"),
        R11("Oleg Kovalev", "Seismiki Barlag", "Inžener", "Çakylyk we RW", "6 months", "WP", "A1711002", "", "APP-2025-9115", "Oct 18, 2025",
            "3 months · Multiple entry · BS1 — Business · Rejected", "st-expiring"),
        R11("Serdar Geldiyew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "6 months", "WP", "A1711003", "A1802003", "APP-2025-9128", "Nov 02, 2025",
            "6 months · Multiple entry · WP — Work visa · Not received from ministry", "st-expiring"),
    ];

    private static List<ReportDashboardPreviewRow> AppViaMinistryOtherCompletedByProject() =>
    [
        R7("John Smith", "Seismiki Barlag", "Inžener", "Çakylyk we RW", "APP-2025-9201", "Sep 14, 2025",
            "Seismiki Barlag · Issued", "st-approved"),
        R7("Bayrammyrat Rejepow", "Elektrik Stansia", "Inžener", "Çakylyk we RW", "APP-2025-9216", "Sep 30, 2025",
            "Elektrik Stansia · Cancelled", "st-expiring"),
        R7("Annaguly Hojayew", "Gurlusyk UZT", "Inžener", "Çakylyk we RW", "APP-2025-9230", "Oct 20, 2025",
            "Gurlusyk UZT · Rejected", "st-expiring"),
    ];

    private static List<ReportDashboardPreviewRow> ApplicationByStatusDirectMigration() =>
    [
        R("John Smith", "Seismiki Barlag", "APP-2026-0150", "Feb 14, 2026",
            "Process Started · no ministry review · — · On track · 2/7", "st-pending"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "APP-2026-0173", "Mar 10, 2026",
            "Process Rejected · no ministry review · — · —", "st-expiring"),
        R("Cary Durdyyew", "Merkez ofis", "APP-2026-0210", "Apr 20, 2026",
            "At office · no ministry review · — · —", "st-pending"),
        R("Annaguly Hojayew", "Gurlusyk UZT", "APP-2026-0222", "May 02, 2026",
            "Issued · no ministry review · — · —", "st-approved"),
    ];

    // ===== Visa ===========================================================

    /// Active By Project: valid visas; Status = Project
    private static List<ReportDashboardPreviewRow> VisaActiveByProject() =>
    [
        R6("Mehmet Yilmaz",       "Gurlusyk UZT",     "V-2025-1012", "Sep 18, 2026", "52", "Gurlusyk UZT", "st-cat-1"),
        R6("Serdar Geldiyew",     "Gurlusyk UZT",     "V-2025-0614", "Oct 12, 2026", "76", "Gurlusyk UZT", "st-cat-1"),
        R6("Alina Makarova",      "Gurlusyk UZT",     "V-2025-1018", "Dec 01, 2026", "126", "Gurlusyk UZT", "st-cat-1"),
        R6("Viktor Petrov",       "Gurlusyk UZT",     "V-2025-1031", "Jan 25, 2027", "181", "Gurlusyk UZT", "st-cat-1"),
        R6("Hans Muller",         "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "64", "Gaz Stansiasy", "st-cat-2"),
        R6("Kemal Aydin",         "Gaz Stansiasy",    "V-2025-1130", "Nov 05, 2026", "100", "Gaz Stansiasy", "st-cat-2"),
        R6("John Smith",          "Seismiki Barlag",  "V-2025-0904", "Aug 04, 2026", "7", "Seismiki Barlag", "st-cat-3"),
        R6("Oleg Kovalev",        "Seismiki Barlag",  "V-2025-0610", "Aug 28, 2026", "31", "Seismiki Barlag", "st-cat-3"),
        R6("Bayrammyrat Rejepow", "Elektrik Stansia", "V-2025-0801", "Jul 20, 2026", "0", "Elektrik Stansia", "st-cat-4"),
        R6("Leyli Annagurbanowa", "Elektrik Stansia", "V-2025-1201", "Jan 10, 2027", "166", "Elektrik Stansia", "st-cat-4"),
    ];

    /// Active By Period · Category · Type (Invitation order): Status = Period · VisaCategory · VisaType
    private static List<ReportDashboardPreviewRow> VisaByPeriodCategoryType() =>
    [
        R6("Bayrammyrat Rejepow", "Elektrik Stansia", "V-2025-0801", "Jul 20, 2026", "0", "1 month · bir gezeklik · WP-Işçi Wiza", "st-cat-1"),
        R6("John Smith",          "Seismiki Barlag",  "V-2025-0904", "Aug 04, 2026", "7", "1 month · köp gezeklik · WP-Işçi Wiza", "st-cat-2"),
        R6("Oleg Kovalev",        "Seismiki Barlag",  "V-2025-0610", "Aug 28, 2026", "31", "3 months · köp gezeklik · BS1-İşerwürlik", "st-cat-3"),
        R6("Mehmet Yilmaz",       "Gurlusyk UZT",     "V-2025-1012", "Sep 18, 2026", "52", "6 months · köp gezeklik · WP-Işçi Wiza", "st-cat-4"),
        R6("Hans Muller",         "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "64", "6 months · köp gezeklik · WP-Işçi Wiza", "st-cat-4"),
        R6("Serdar Geldiyew",     "Gurlusyk UZT",     "V-2025-0614", "Oct 12, 2026", "76", "6 months · köp gezeklik · WP-Işçi Wiza", "st-cat-4"),
        R6("Kemal Aydin",         "Gaz Stansiasy",    "V-2025-1130", "Nov 05, 2026", "100", "6 months · köp gezeklik · WP-Işçi Wiza", "st-cat-4"),
        R6("Alina Makarova",      "Gurlusyk UZT",     "V-2025-1018", "Dec 01, 2026", "126", "6 months · iki gezeklik · FM-Maşgala", "st-cat-5"),
        R6("Leyli Annagurbanowa", "Elektrik Stansia", "V-2025-1201", "Jan 10, 2027", "166", "1 year · iki gezeklik · FM-Maşgala", "st-cat-5"),
        R6("Viktor Petrov",       "Gurlusyk UZT",     "V-2025-1031", "Jan 25, 2027", "181", "1 year · iki gezeklik · BS1-İşerwürlik", "st-cat-3"),
    ];

    /// Extension Required: Status = nearest milestone; ColumnC = exact days remaining.
    private static List<ReportDashboardPreviewRow> VisaExtensionRequiredByDays() =>
    [
        R6("Bayrammyrat Rejepow", "Elektrik Stansia", "V-2025-0801", "Jul 20, 2026", "5", "7 days", "st-cat-1"),
        R6("John Smith",          "Seismiki Barlag",  "V-2025-0904", "Aug 04, 2026", "28", "30 days", "st-cat-2"),
        R6("Oleg Kovalev",        "Seismiki Barlag",  "V-2025-0610", "Aug 28, 2026", "32", "30 days", "st-cat-2"),
        R6("Mehmet Yilmaz",       "Gurlusyk UZT",     "V-2025-1012", "Sep 18, 2026", "58", "60 days", "st-cat-3"),
        R6("Hans Muller",         "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "88", "90 days", "st-cat-4"),
        R6("Serdar Geldiyew",     "Gurlusyk UZT",     "V-2025-0614", "Oct 12, 2026", "95", "90 days", "st-cat-4"),
        R6("Kemal Aydin",         "Gaz Stansiasy",    "V-2025-1130", "Nov 05, 2026", "170", "180 days", "st-cat-5"),
        R6("Cary Durdyyew",       "Merkez ofis",      "V-2025-1144", "Nov 18, 2026", "185", "180 days", "st-cat-5"),
    ];

    private static ReportDashboardPreviewRow R6(
        string name, string project, string colA, string colB, string colC, string status, string css) =>
        new()
        {
            Name = name,
            Project = project,
            ColumnA = MockPassportNumber(name),
            ColumnB = colA,
            ColumnC = colB,
            ColumnD = colC,
            Status = status,
            StatusCssClass = css
        };

    /// <summary>Six-column Visa Extension Result rows (Passport # + App # + App Date + Status).</summary>
    private static ReportDashboardPreviewRow R6p(
        string name, string project, string colB, string colC, string status, string css) =>
        new()
        {
            Name = name,
            Project = project,
            ColumnA = MockPassportNumber(name),
            ColumnB = colB,
            ColumnC = colC,
            Status = status,
            StatusCssClass = css
        };

    private static string MockPassportNumber(string name) =>
        "P-" + (Math.Abs(name.GetHashCode()) % 100000).ToString("D5", System.Globalization.CultureInfo.InvariantCulture);

    /// By Days Remaining: closed days-to-expiry buckets on valid visas
    private static List<ReportDashboardPreviewRow> VisaByDaysRemaining() =>
    [
        R6("Bayrammyrat Rejepow", "Elektrik Stansia", "V-2025-0801", "Jul 20, 2026", "5", "< 10 days",  "st-expiring"),
        R6("John Smith",          "Seismiki Barlag",  "V-2025-0904", "Aug 04, 2026", "18", "< 1 month",  "st-expiring"),
        R6("Oleg Kovalev",        "Seismiki Barlag",  "V-2025-0610", "Aug 28, 2026", "25", "< 1 month",  "st-expiring"),
        R6("Mehmet Yilmaz",       "Gurlusyk UZT",     "V-2025-1012", "Sep 18, 2026", "52", "< 3 months", "st-pending"),
        R6("Hans Muller",         "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "64", "< 3 months", "st-pending"),
        R6("Serdar Geldiyew",     "Gurlusyk UZT",     "V-2025-0614", "Oct 12, 2026", "76", "< 3 months", "st-pending"),
        R6("Kemal Aydin",         "Gaz Stansiasy",    "V-2025-1130", "Nov 05, 2026", "100", "< 4 months", "st-approved"),
        R6("Alina Makarova",      "Gurlusyk UZT",     "V-2025-1018", "Dec 01, 2026", "126", "< 5 months", "st-approved"),
        R6("Leyli Annagurbanowa", "Elektrik Stansia", "V-2025-1201", "Jan 10, 2027", "166", "< 6 months", "st-approved"),
        R6("Viktor Petrov",       "Gurlusyk UZT",     "V-2025-1031", "Jan 25, 2027", "181", "≥ 6 months", "st-approved"),
    ];

    // ===== Invitation =====================================================

    /// Ready: valid + not used; chart Status = Project.
    private static List<ReportDashboardPreviewRow> InvitationReadyByProject() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "INV-2026-0071", "Sep 30, 2026", "Gurlusyk UZT",     "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "INV-2026-0058", "Oct 12, 2026", "Gurlusyk UZT",     "st-cat-1"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "INV-2026-0097", "Sep 01, 2026", "Gurlusyk UZT",     "st-cat-1"),
        R("Hans Muller",         "Gaz Stansiasy",    "INV-2026-0053", "Aug 22, 2026", "Gaz Stansiasy",    "st-cat-2"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "INV-2026-0110", "Nov 05, 2026", "Gaz Stansiasy",    "st-cat-2"),
        R("John Smith",          "Seismiki Barlag",  "INV-2026-0062", "Jul 28, 2026", "Seismiki Barlag",  "st-cat-3"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "INV-2026-0088", "Aug 05, 2026", "Seismiki Barlag",  "st-cat-3"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "INV-2026-0041", "Sep 18, 2026", "Elektrik Stansia", "st-cat-4"),
    ];

    /// Ready: valid + not used; chart Status = VisaPeriod · VisaCategory · VisaType.
    private static List<ReportDashboardPreviewRow> InvitationReadyByPeriodCategory() =>
    [
        R("John Smith",          "Seismiki Barlag",  "INV-2026-0062", "Jul 28, 2026", "3 months · bir gezeklik · BS-1", "st-cat-1"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "INV-2026-0041", "Sep 18, 2026", "3 months · iki gezeklik · BS-1", "st-cat-2"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "INV-2026-0088", "Aug 05, 2026", "3 months · köp gezeklik · BS-1", "st-cat-3"),
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "INV-2026-0071", "Sep 30, 2026", "6 months · köp gezeklik · WP", "st-cat-4"),
        R("Hans Muller",         "Gaz Stansiasy",    "INV-2026-0053", "Aug 22, 2026", "6 months · köp gezeklik · WP", "st-cat-4"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "INV-2026-0097", "Sep 01, 2026", "6 months · köp gezeklik · WP", "st-cat-4"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "INV-2026-0058", "Oct 12, 2026", "1 year · iki gezeklik · FM", "st-cat-5"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "INV-2026-0110", "Nov 05, 2026", "1 year · köp gezeklik · BS-1", "st-cat-6"),
    ];

    /// In process: invitation-issuing application created, invitation not issued yet; Status = Application Progress state.
    /// Invitation Process (P): Status = Project · ProcessState.
    private static List<ReportDashboardPreviewRow> InvitationInProcessByProject() =>
    [
        R("Leyli Annagurbanowa", "Elektrik Stansia", "APP-2026-0301", "Mar 02, 2026", "Elektrik Stansia · Being Prepared", "st-pending"),
        R("Cary Durdyyew",       "Gurlusyk UZT",     "APP-2026-0312", "Mar 08, 2026", "Gurlusyk UZT · Being Prepared", "st-pending"),
        R("Alina Makarova",      "Gurlusyk UZT",     "APP-2026-0320", "Mar 14, 2026", "Gurlusyk UZT · 1st Review Started", "st-pending"),
        R("Marat Atayew",        "Gaz Stansiasy",    "APP-2026-0331", "Mar 20, 2026", "Gaz Stansiasy · 1st Review Approved", "st-approved"),
        R("Elena Volkova",       "Seismiki Barlag",  "APP-2026-0344", "Apr 01, 2026", "Seismiki Barlag · 2nd Review Started", "st-pending"),
        R("Ahmet Demir",         "Gurlusyk UZT",     "APP-2026-0355", "Apr 10, 2026", "Gurlusyk UZT · Process Started", "st-pending"),
        R("Nina Sokolova",       "Elektrik Stansia", "APP-2026-0366", "Apr 18, 2026", "Elektrik Stansia · Process Started", "st-pending"),
        R("Pavel Orlov",         "Gaz Stansiasy",    "APP-2026-0377", "May 02, 2026", "Gaz Stansiasy · 2nd Review Approved", "st-approved"),
    ];

    /// Invitation Process (V): Status = Period · Category · Type · ProcessState.
    private static List<ReportDashboardPreviewRow> InvitationInProcessByPeriodCategoryType() =>
    [
        R("Leyli Annagurbanowa", "Elektrik Stansia", "APP-2026-0301", "Mar 02, 2026", "1 month · Double entry · BS1 — Business · Being Prepared", "st-pending"),
        R("Cary Durdyyew",       "Gurlusyk UZT",     "APP-2026-0312", "Mar 08, 2026", "6 months · Multiple entry · WP — Work visa · Being Prepared", "st-pending"),
        R("Alina Makarova",      "Gurlusyk UZT",     "APP-2026-0320", "Mar 14, 2026", "6 months · Multiple entry · WP — Work visa · 1st Review Started", "st-pending"),
        R("Marat Atayew",        "Gaz Stansiasy",    "APP-2026-0331", "Mar 20, 2026", "3 months · Multiple entry · WP — Work visa · 1st Review Approved", "st-approved"),
        R("Elena Volkova",       "Seismiki Barlag",  "APP-2026-0344", "Apr 01, 2026", "1 month · Double entry · BS1 — Business · 2nd Review Started", "st-pending"),
        R("Ahmet Demir",         "Gurlusyk UZT",     "APP-2026-0355", "Apr 10, 2026", "6 months · Multiple entry · WP — Work visa · Process Started", "st-pending"),
        R("Nina Sokolova",       "Elektrik Stansia", "APP-2026-0366", "Apr 18, 2026", "1 month · Double entry · BS1 — Business · Process Started", "st-pending"),
        R("Pavel Orlov",         "Gaz Stansiasy",    "APP-2026-0377", "May 02, 2026", "3 months · Multiple entry · WP — Work visa · 2nd Review Approved", "st-approved"),
    ];

    /// Process Result (P): Status = Project · ProcessState (terminal + 1st/2nd review rejected).
    private static List<ReportDashboardPreviewRow> InvitationProcessResultByProject() =>
    [
        R("Kemal Aydin",         "Gaz Stansiasy",    "APP-2026-0012", "Feb 12, 2026", "Gaz Stansiasy · Process Rejected", "st-expiring"),
        R("Hans Muller",         "Gaz Stansiasy",    "APP-2026-0018", "Feb 28, 2026", "Gaz Stansiasy · 1st Review Rejected", "st-expiring"),
        R("John Smith",          "Seismiki Barlag",  "APP-2026-0021", "Mar 05, 2026", "Seismiki Barlag · Process Cancelled", "st-expiring"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "APP-2026-0029", "Mar 19, 2026", "Elektrik Stansia · Process Issued", "st-approved"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "APP-2026-0034", "Apr 02, 2026", "Gurlusyk UZT · Process Issued", "st-approved"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "APP-2026-0040", "Apr 15, 2026", "Gurlusyk UZT · 2nd Review Rejected", "st-expiring"),
    ];

    /// Process Result (V): Status = Period · Category · Type · ProcessState.
    private static List<ReportDashboardPreviewRow> InvitationProcessResultByPeriodCategoryType() =>
    [
        R("Kemal Aydin",         "Gaz Stansiasy",    "APP-2026-0012", "Feb 12, 2026", "6 months · Multiple entry · WP — Work visa · Process Rejected", "st-expiring"),
        R("Hans Muller",         "Gaz Stansiasy",    "APP-2026-0018", "Feb 28, 2026", "6 months · Multiple entry · WP — Work visa · 1st Review Rejected", "st-expiring"),
        R("John Smith",          "Seismiki Barlag",  "APP-2026-0021", "Mar 05, 2026", "1 month · Double entry · BS1 — Business · Process Cancelled", "st-expiring"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "APP-2026-0029", "Mar 19, 2026", "1 month · Double entry · BS1 — Business · Process Issued", "st-approved"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "APP-2026-0034", "Apr 02, 2026", "6 months · Multiple entry · WP — Work visa · Process Issued", "st-approved"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "APP-2026-0040", "Apr 15, 2026", "3 months · Multiple entry · WP — Work visa · 2nd Review Rejected", "st-expiring"),
    ];

    /// Used (P): invitation items (visa issued from invitation line); Status = Project.
    private static List<ReportDashboardPreviewRow> InvitationUsed() =>
    [
        R("Leyli Annagur.",      "Elektrik Stansia", "INV-2025-0091", "Jan 10, 2026", "Elektrik Stansia", "st-cat-4"),
        R("Cary Durdyyew",       "Gurlusyk UZT",     "INV-2025-0095", "Jan 22, 2026", "Gurlusyk UZT",     "st-cat-1"),
        R("Alina Makarova",      "Gurlusyk UZT",     "INV-2025-0102", "Feb 04, 2026", "Gurlusyk UZT",     "st-cat-1"),
        R("Marat Atayew",        "Gaz Stansiasy",    "INV-2025-0111", "Feb 18, 2026", "Gaz Stansiasy",    "st-cat-2"),
        R("Elena Volkova",       "Seismiki Barlag",  "INV-2025-0120", "Mar 01, 2026", "Seismiki Barlag",  "st-cat-3"),
        R("Ahmet Demir",         "Gurlusyk UZT",     "INV-2025-0128", "Mar 12, 2026", "Gurlusyk UZT",     "st-cat-1"),
    ];

    /// Used (V): Status = Period · Category · Type.
    private static List<ReportDashboardPreviewRow> InvitationUsedByPeriodCategoryType() =>
    [
        R("Leyli Annagur.",      "Elektrik Stansia", "INV-2025-0091", "Jan 10, 2026", "1 month · Double entry · BS1 — Business", "st-cat-2"),
        R("Cary Durdyyew",       "Gurlusyk UZT",     "INV-2025-0095", "Jan 22, 2026", "6 months · Multiple entry · WP — Work visa", "st-cat-1"),
        R("Alina Makarova",      "Gurlusyk UZT",     "INV-2025-0102", "Feb 04, 2026", "6 months · Multiple entry · WP — Work visa", "st-cat-1"),
        R("Marat Atayew",        "Gaz Stansiasy",    "INV-2025-0111", "Feb 18, 2026", "6 months · Multiple entry · WP — Work visa", "st-cat-1"),
        R("Elena Volkova",       "Seismiki Barlag",  "INV-2025-0120", "Mar 01, 2026", "1 month · Double entry · BS1 — Business", "st-cat-2"),
        R("Ahmet Demir",         "Gurlusyk UZT",     "INV-2025-0128", "Mar 12, 2026", "3 months · Multiple entry · WP — Work visa", "st-cat-3"),
    ];

    /// Invitation Valid Until: remaining-time buckets (valid unused only).
    private static List<ReportDashboardPreviewRow> InvitationValidUntil() =>
    [
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "INV-2026-0041", "Jul 24, 2026", "< 1 day",    "st-expiring"),
        R("John Smith",          "Seismiki Barlag",  "INV-2026-0062", "Jul 28, 2026", "< 1 week",   "st-expiring"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "INV-2026-0088", "Aug 02, 2026", "< 2 weeks",  "st-pending"),
        R("Hans Muller",         "Gaz Stansiasy",    "INV-2026-0053", "Aug 10, 2026", "< 3 weeks",  "st-pending"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "INV-2026-0110", "Aug 18, 2026", "< 1 month",  "st-pending"),
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "INV-2026-0071", "Sep 05, 2026", "< 2 months", "st-approved"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "INV-2026-0058", "Oct 01, 2026", "< 3 months", "st-approved"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "INV-2026-0097", "Nov 20, 2026", "≥ 3 months", "st-approved"),
    ];

    // ===== Registration ===================================================

    /// Check in / Active Registered (P): Status = Project.
    private static List<ReportDashboardPreviewRow> RegistrationCheckInByProject() =>
    [
        R("Mehmet Yilmaz",    "Gurlusyk UZT",     "V-2025-1012", "Oct 15, 2026", "Gurlusyk UZT",     "st-cat-1"),
        R("Viktor Petrov",    "Gurlusyk UZT",     "V-2025-1031", "Mar 01, 2027", "Gurlusyk UZT",     "st-cat-1"),
        R("John Smith",       "Seismiki Barlag",  "V-2025-0904", "Jan 10, 2026", "Seismiki Barlag",  "st-cat-2"),
        R("Kemal Aydin",      "Gaz Stansiasy",    "V-2025-1130", "Dec 20, 2026", "Gaz Stansiasy",    "st-cat-3"),
        R("Hans Muller",      "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "Gaz Stansiasy",    "st-cat-3"),
        R("Alina Makarova",   "Gurlusyk UZT",     "V-2025-1018", "Apr 12, 2027", "Gurlusyk UZT",     "st-cat-1"),
    ];

    /// Active Registered (V): Status = Period · Category · Type
    /// (Period from Application; Category/Type from CurrentVisa).
    private static List<ReportDashboardPreviewRow> RegistrationCheckInByPeriodCategoryType() =>
    [
        R("Mehmet Yilmaz",    "Gurlusyk UZT",     "V-2025-1012", "Oct 15, 2026", "6 months · Multiple entry · WP — Work visa", "st-cat-1"),
        R("Viktor Petrov",    "Gurlusyk UZT",     "V-2025-1031", "Mar 01, 2027", "6 months · Multiple entry · WP — Work visa", "st-cat-1"),
        R("John Smith",       "Seismiki Barlag",  "V-2025-0904", "Jan 10, 2026", "1 month · Double entry · BS1 — Business", "st-cat-2"),
        R("Kemal Aydin",      "Gaz Stansiasy",    "V-2025-1130", "Dec 20, 2026", "3 months · Multiple entry · WP — Work visa", "st-cat-3"),
        R("Hans Muller",      "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "6 months · Multiple entry · WP — Work visa", "st-cat-1"),
        R("Alina Makarova",   "Gurlusyk UZT",     "V-2025-1018", "Apr 12, 2027", "6 months · Multiple entry · WP — Work visa", "st-cat-1"),
    ];

    /// Active Registered (C): Status = City.
    private static List<ReportDashboardPreviewRow> RegistrationCheckInByCity() =>
    [
        R("Mehmet Yilmaz",    "Gurlusyk UZT",     "V-2025-1012", "Oct 15, 2026", "Aşgabat",   "st-cat-1"),
        R("Viktor Petrov",    "Gurlusyk UZT",     "V-2025-1031", "Mar 01, 2027", "Aşgabat",   "st-cat-1"),
        R("John Smith",       "Seismiki Barlag",  "V-2025-0904", "Jan 10, 2026", "Mary",      "st-cat-2"),
        R("Kemal Aydin",      "Gaz Stansiasy",    "V-2025-1130", "Dec 20, 2026", "Balkanabat","st-cat-3"),
        R("Hans Muller",      "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "Balkanabat","st-cat-3"),
        R("Alina Makarova",   "Gurlusyk UZT",     "V-2025-1018", "Apr 12, 2027", "Aşgabat",   "st-cat-1"),
    ];

    /// On process: unfinished registration apps; Status = ApplicationType · ProcessState.
    private static List<ReportDashboardPreviewRow> RegistrationOnProcess() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "R-2026-0101", "Jan 10, 2026", "Check-In · At office",          "st-pending"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "R-2026-0102", "Jan 11, 2026", "Extension · Being Prepared",    "st-pending"),
        R("John Smith",          "Seismiki Barlag",  "R-2026-0103", "Jan 12, 2026", "Check-Out · At office",         "st-pending"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "R-2026-0104", "Jan 13, 2026", "Address Change · At migration", "st-cat-2"),
        R("Alina Makarova",      "Gurlusyk UZT",     "R-2026-0105", "Jan 14, 2026", "Check-In · At office",          "st-pending"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "R-2026-0106", "Jan 15, 2026", "Passport Change · At office",   "st-pending"),
    ];

    /// Registration ApplicationType tabs: ColumnA = visa #, ColumnB = expiry, Status = process state.
    private static List<ReportDashboardPreviewRow> RegistrationByApplicationType(string? _) =>
    [
        R("Mehmet Yilmaz",    "Gurlusyk UZT",     "V-2025-1012", "Oct 15, 2026", "Process Issued",   "st-approved"),
        R("Viktor Petrov",    "Gurlusyk UZT",     "V-2025-1031", "Mar 01, 2027", "Process Issued",   "st-approved"),
        R("John Smith",       "Seismiki Barlag",  "V-2025-0904", "Jan 10, 2026", "Process Started",  "st-pending"),
        R("Kemal Aydin",      "Gaz Stansiasy",    "V-2025-1130", "Dec 20, 2026", "Being Prepared",   "st-pending"),
        R("Hans Muller",      "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "Process Rejected", "st-expiring"),
        R("Alina Makarova",   "Gurlusyk UZT",     "V-2025-1018", "Apr 12, 2027", "Process Issued",   "st-approved"),
    ];
    // ===== Work Permit ====================================================

    /// Active WorkPermit (P): valid items; Status = Project
    private static List<ReportDashboardPreviewRow> WorkPermitActiveByProject() =>
    [
        R("Mehmet Yilmaz",  "Gurlusyk UZT",    "WP-2025-100142", "Jul 20, 2026", "Gurlusyk UZT",    "st-cat-1"),
        R("Viktor Petrov",  "Gurlusyk UZT",    "WP-2025-100155", "Aug 10, 2026", "Gurlusyk UZT",    "st-cat-1"),
        R("Alina Makarova", "Gurlusyk UZT",    "WP-2025-100191", "Jan 10, 2027", "Gurlusyk UZT",    "st-cat-1"),
        R("Cary Durdyyew",  "Gurlusyk UZT",    "WP-2025-100211", "Dec 31, 2027", "Gurlusyk UZT",    "st-cat-1"),
        R("Hans Muller",    "Gaz Stansiasy",   "WP-2025-100163", "Sep 30, 2026", "Gaz Stansiasy",   "st-cat-2"),
        R("Kemal Aydin",    "Gaz Stansiasy",   "WP-2025-100182", "Dec 20, 2026", "Gaz Stansiasy",   "st-cat-2"),
        R("John Smith",     "Seismiki Barlag", "WP-2025-100171", "Nov 15, 2026", "Seismiki Barlag", "st-cat-3"),
        R("Oleg Kovalev",   "Seismiki Barlag", "WP-2025-100204", "Jun 30, 2027", "Seismiki Barlag", "st-cat-3"),
    ];

    /// WorkPermit Extension (P): unfinished App_WP_Ext / App_Visa_and_WP_Ext;
    /// excludes Issued / Cancelled / Rejected / review rejects; Status = Project · ProcessState.
    private static List<ReportDashboardPreviewRow> WorkPermitOnExtensionByProject() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "APP-2026-0201", "Jan 09, 2026", "Gurlusyk UZT · Being Prepared", "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "APP-2026-0212", "Jan 15, 2026", "Gurlusyk UZT · Being Prepared", "st-cat-1"),
        R("John Smith",          "Seismiki Barlag",  "APP-2026-0220", "Jan 21, 2026", "Seismiki Barlag · Processing", "st-cat-2"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "APP-2026-0231", "Jan 29, 2026", "Gaz Stansiasy · 1st Review Approved", "st-cat-3"),
        R("Alina Makarova",      "Gurlusyk UZT",     "APP-2026-0244", "Feb 03, 2026", "Gurlusyk UZT · 2nd Review Started", "st-cat-1"),
        R("Hans Muller",         "Gaz Stansiasy",    "APP-2026-0262", "Feb 13, 2026", "Gaz Stansiasy · Being Prepared", "st-cat-3"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "APP-2026-0291", "Mar 02, 2026", "Gurlusyk UZT · Cleared agreement - Energetika", "st-cat-5"),
        R("Kemal Aydin",         "Merkez ofis",      "APP-2026-0295", "Mar 05, 2026", "Merkez ofis · Processing", "st-cat-2"),
    ];

    /// Extension Result (P): terminal + review rejects; Status = Project · ProcessState.
    private static List<ReportDashboardPreviewRow> WorkPermitExtensionResultByProject() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "APP-2025-2201", "Oct 09, 2025", "Gurlusyk UZT · Process Issued", "st-approved"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "APP-2025-2212", "Oct 15, 2025", "Gurlusyk UZT · Process Issued", "st-approved"),
        R("John Smith",          "Seismiki Barlag",  "APP-2025-2220", "Oct 21, 2025", "Seismiki Barlag · Process Issued", "st-approved"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "APP-2025-2231", "Nov 03, 2025", "Gaz Stansiasy · Process Issued", "st-approved"),
        R("Alina Makarova",      "Gurlusyk UZT",     "APP-2025-2244", "Nov 13, 2025", "Gurlusyk UZT · Process Issued", "st-approved"),
        R("Hans Muller",         "Gaz Stansiasy",    "APP-2025-2262", "Nov 23, 2025", "Gaz Stansiasy · Process Cancelled", "st-expiring"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "APP-2025-2271", "Dec 02, 2025", "Seismiki Barlag · Process Rejected", "st-expiring"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "APP-2025-2280", "Dec 09, 2025", "Elektrik Stansia · 1st Review Rejected", "st-expiring"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "APP-2025-2291", "Dec 16, 2025", "Gurlusyk UZT · Process Issued", "st-approved"),
        R("Kemal Aydin",         "Merkez ofis",      "APP-2025-2295", "Dec 21, 2025", "Merkez ofis · 2nd Review Rejected", "st-expiring"),
    ];

    private static List<ReportDashboardPreviewRow> WorkPermitByDaysRemaining() =>
    [
        R("Mehmet Yilmaz",  "Gurlusyk UZT",    "WP-2025-100142", "Jul 20, 2026", "< 10 days",  "st-expiring"),
        R("Viktor Petrov",  "Gurlusyk UZT",    "WP-2025-100155", "Aug 10, 2026", "< 1 month",  "st-expiring"),
        R("Hans Muller",    "Gaz Stansiasy",   "WP-2025-100163", "Sep 30, 2026", "< 3 months", "st-pending"),
        R("John Smith",     "Seismiki Barlag", "WP-2025-100171", "Nov 15, 2026", "< 4 months", "st-pending"),
        R("Kemal Aydin",    "Gaz Stansiasy",   "WP-2025-100182", "Dec 20, 2026", "< 5 months", "st-approved"),
        R("Alina Makarova", "Gurlusyk UZT",    "WP-2025-100191", "Jan 10, 2027", "< 6 months", "st-approved"),
        R("Oleg Kovalev",   "Seismiki Barlag", "WP-2025-100204", "Jun 30, 2027", "≥ 6 months", "st-approved"),
        R("Cary Durdyyew",  "Gurlusyk UZT",    "WP-2025-100211", "Dec 31, 2027", "≥ 6 months", "st-approved"),
    ];

    private static List<ReportDashboardPreviewRow> WorkPermitByStatus() =>
    [
        // Mirror Visa State: extension process buckets (WP# + expiry; Status drives chart).
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "WP-2025-100142", "Oct 31, 2026", "Extension Started",       "st-pending"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "WP-2025-100155", "Mar 31, 2027", "Extension Started",       "st-pending"),
        R("John Smith",          "Seismiki Barlag",  "WP-2025-100171", "Jan 04, 2026", "Extension to be Started", "st-expiring"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "WP-2025-100182", "Nov 30, 2026", "Extension Started",       "st-pending"),
        R("Alina Makarova",      "Gurlusyk UZT",     "WP-2025-100191", "Feb 18, 2027", "Extension Not Required",  "st-approved"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "WP-2025-100088", "Sep 01, 2025", "Extension Rejected",      "st-expiring"),
        R("Hans Muller",         "Gaz Stansiasy",    "WP-2025-100163", "Apr 22, 2026", "Extension Started",       "st-pending"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "WP-2025-100204", "May 10, 2026", "Extension to be Started", "st-expiring"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "WP-2025-100220", "Dec 01, 2026", "Extension Not Required",  "st-approved"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "WP-2025-100095", "Jun 14, 2026", "Extension Cancelled",     "st-expiring"),
    ];

    // ===== Travel =========================================================

    private static List<ReportDashboardPreviewRow> TravelByMonth() =>
    [
        // ColumnA = app#, ColumnB = travel date, Status = month label (drives chart buckets)
        R("Mehmet Yilmaz",  "Gurlusyk UZT",    "APP-2025-00341", "Jul 12, 2025", "Jul 2025", "st-cat-1"),
        R("Viktor Petrov",  "Gurlusyk UZT",    "APP-2025-00358", "Jul 20, 2025", "Jul 2025", "st-cat-1"),
        R("John Smith",     "Seismiki Barlag", "APP-2025-00372", "Aug 03, 2025", "Aug 2025", "st-cat-2"),
        R("Hans Muller",    "Gaz Stansiasy",   "APP-2025-00389", "Aug 17, 2025", "Aug 2025", "st-cat-2"),
        R("Alina Makarova", "Gurlusyk UZT",    "APP-2025-00401", "Sep 02, 2025", "Sep 2025", "st-cat-3"),
        R("Cary Durdyyew",  "Gurlusyk UZT",    "APP-2025-00415", "Sep 25, 2025", "Sep 2025", "st-cat-3"),
    ];

    private static List<ReportDashboardPreviewRow> TravelByStatus() =>
    [
        R("Mehmet Yilmaz",  "Gurlusyk UZT",    "APP-2025-00341", "Jul 12, 2025", "Approved", "st-approved"),
        R("Viktor Petrov",  "Gurlusyk UZT",    "APP-2025-00358", "Jul 20, 2025", "Approved", "st-approved"),
        R("John Smith",     "Seismiki Barlag", "APP-2025-00372", "Aug 03, 2025", "Approved", "st-approved"),
        R("Hans Muller",    "Gaz Stansiasy",   "APP-2025-00389", "Aug 17, 2025", "Approved", "st-approved"),
        R("Alina Makarova", "Gurlusyk UZT",    "APP-2025-00401", "Sep 02, 2025", "Approved", "st-approved"),
        R("Cary Durdyyew",  "Gurlusyk UZT",    "APP-2025-00415", "Sep 25, 2025", "Approved", "st-approved"),
    ];

    // ===== Address of Residence ===========================================

    private static List<ReportDashboardPreviewRow> AddressByValidity() =>
    [
        // Private House Validity states: ExpirationNotSet | Valid | Expiring | Expired
        R("Mehmet Yilmaz",  "Gurlusyk UZT",    "Ashgabat, Berkararlyk", "Dec 31, 2026", "Valid",             "st-approved"),
        R("Viktor Petrov",  "Gurlusyk UZT",    "Mary, Merkezi",         "",             "ExpirationNotSet",  "st-pending"),
        R("John Smith",     "Seismiki Barlag", "Balkanabat",            "Apr 02, 2026", "Expiring",          "st-expiring"),
        R("Hans Muller",    "Gaz Stansiasy",   "Dashoguz",              "Jan 10, 2025", "Expired",           "st-expiring"),
        R("Alina Makarova", "Gurlusyk UZT",    "Turkmenabat",           "Nov 20, 2026", "Valid",             "st-approved"),
        R("Cary Durdyyew",  "Gurlusyk UZT",    "Ashgabat, Kopetdag",    "May 01, 2026", "Expiring",          "st-expiring"),
    ];

    private static List<ReportDashboardPreviewRow> AddressByRegion() =>
    [
        R("Mehmet Yilmaz",  "Gurlusyk UZT",    "Ashgabat, Berkararlyk", "Dec 31, 2026", "Ashgabat", "st-cat-1"),
        R("Viktor Petrov",  "Gurlusyk UZT",    "Mary, Merkezi",         "Aug 15, 2026", "Mary",     "st-cat-2"),
        R("John Smith",     "Seismiki Barlag", "Balkanabat",            "Apr 02, 2026", "Balkan",   "st-cat-3"),
        R("Hans Muller",    "Gaz Stansiasy",   "Dashoguz",              "Jan 10, 2025", "Dashoguz", "st-cat-4"),
        R("Alina Makarova", "Gurlusyk UZT",    "Turkmenabat",           "Nov 20, 2026", "Lebap",    "st-cat-5"),
        R("Cary Durdyyew",  "Gurlusyk UZT",    "Ashgabat, Kopetdag",    "May 01, 2026", "Ashgabat", "st-cat-1"),
    ];

    private static List<ReportDashboardPreviewRow> AddressByCity() =>
    [
        R("Mehmet Yilmaz",  "Gurlusyk UZT",    "Ashgabat, Berkararlyk", "Dec 31, 2026", "Ashgabat",   "st-cat-1"),
        R("Viktor Petrov",  "Gurlusyk UZT",    "Mary, Merkezi",         "Aug 15, 2026", "Mary",       "st-cat-2"),
        R("John Smith",     "Seismiki Barlag", "Balkanabat",            "Apr 02, 2026", "Balkanabat", "st-cat-3"),
        R("Hans Muller",    "Gaz Stansiasy",   "Dashoguz",              "Jan 10, 2025", "Dashoguz",   "st-cat-4"),
        R("Alina Makarova", "Gurlusyk UZT",    "Turkmenabat",           "Nov 20, 2026", "Turkmenabat", "st-cat-5"),
        R("Cary Durdyyew",  "Gurlusyk UZT",    "Ashgabat, Kopetdag",    "May 01, 2026", "Ashgabat",   "st-cat-1"),
    ];

    private static List<ReportDashboardPreviewRow> AddressByAddressType() =>
    [
        R("Mehmet Yilmaz",  "Gurlusyk UZT",    "Ashgabat, Berkararlyk", "Dec 31, 2026", "Lodging",        "st-cat-1"),
        R("Viktor Petrov",  "Gurlusyk UZT",    "Mary, Merkezi",         "Aug 15, 2026", "Private House",  "st-cat-2"),
        R("John Smith",     "Seismiki Barlag", "Balkanabat",            "Apr 02, 2026", "Hotel",          "st-cat-3"),
        R("Hans Muller",    "Gaz Stansiasy",   "Dashoguz",              "Jan 10, 2025", "Hospital",       "st-cat-4"),
        R("Alina Makarova", "Gurlusyk UZT",    "Turkmenabat",           "Nov 20, 2026", "Other",          "st-cat-5"),
        R("Cary Durdyyew",  "Gurlusyk UZT",    "Ashgabat, Kopetdag",    "May 01, 2026", "Lodging",        "st-cat-1"),
    ];

    private static List<ReportDashboardPreviewRow> AddressByAddress() =>
    [
        // ColumnA = Region · City, Status = Region + City + FullAddress (chart buckets)
        R("Mehmet Yilmaz",  "Gurlusyk UZT",    "Ashgabat, Ashgabat", "Dec 31, 2026", "Ashgabat, Ashgabat, Berkararlyk etraby, 12", "st-cat-1"),
        R("Viktor Petrov",  "Gurlusyk UZT",    "Mary, Mary",         "Aug 15, 2026", "Mary, Mary, Merkezi, 5",                     "st-cat-2"),
        R("John Smith",     "Seismiki Barlag", "Balkan, Balkanabat", "Apr 02, 2026", "Balkan, Balkanabat, hotel Merkez",          "st-cat-3"),
        R("Hans Muller",    "Gaz Stansiasy",   "Dashoguz, Dashoguz", "Jan 10, 2025", "Dashoguz, Dashoguz, hospital",              "st-cat-4"),
        R("Alina Makarova", "Gurlusyk UZT",    "Lebap, Turkmenabat", "Nov 20, 2026", "Lebap, Turkmenabat, Lebap, 9",              "st-cat-5"),
        R("Cary Durdyyew",  "Gurlusyk UZT",    "Ashgabat, Ashgabat", "May 01, 2026", "Ashgabat, Ashgabat, Kopetdag etraby, 3",   "st-cat-1"),
    ];

    // ===== Border Zone ====================================================

    private static List<ReportDashboardPreviewRow> BorderZoneByValidity() =>
    [
        R("Mehmet Yilmaz", "Gurlusyk UZT",    "BZ-2025-0021", "Dec 31, 2026", "Valid (>90 days)",   "st-approved"),
        R("Viktor Petrov", "Gurlusyk UZT",    "BZ-2025-0022", "Dec 31, 2026", "Valid (>90 days)",   "st-approved"),
        R("Kemal Aydin",   "Gaz Stansiasy",   "BZ-2025-0023", "Aug 14, 2025", "Expired",            "st-expiring"),
        R("Hans Muller",   "Gaz Stansiasy",   "BZ-2025-0024", "Oct 01, 2025", "Expiring (<30 days)","st-expiring"),
        R("John Smith",    "Seismiki Barlag", "BZ-2025-0025", "Nov 30, 2026", "Valid (>90 days)",   "st-approved"),
    ];

    private static List<ReportDashboardPreviewRow> BorderZoneByZone() =>
    [
        // ColumnA = BZ number, ColumnB = valid until, Status = zone (drives chart buckets)
        R("Mehmet Yilmaz", "Gurlusyk UZT",    "BZ-2025-0021", "Dec 31, 2026", "Balkan Kenary", "st-cat-1"),
        R("Viktor Petrov", "Gurlusyk UZT",    "BZ-2025-0022", "Dec 31, 2026", "Balkan Kenary", "st-cat-1"),
        R("Kemal Aydin",   "Gaz Stansiasy",   "BZ-2025-0023", "Aug 14, 2025", "Mary Kenary",   "st-cat-2"),
        R("Hans Muller",   "Gaz Stansiasy",   "BZ-2025-0024", "Oct 01, 2025", "Mary Kenary",   "st-cat-2"),
        R("John Smith",    "Seismiki Barlag", "BZ-2025-0025", "Nov 30, 2026", "Balkan Kenary", "st-cat-1"),
    ];

    // ===== Passport =======================================================

    private static List<ReportDashboardPreviewRow> PassportByValidity() =>
    [
        R("Mehmet Yilmaz",    "Gurlusyk UZT",     "U12345678",  "Oct 18, 2027", "Valid (>90 days)",    "st-approved"),
        R("Viktor Petrov",    "Gurlusyk UZT",     "71 2344521", "Mar 05, 2028", "Valid (>90 days)",    "st-approved"),
        R("John Smith",       "Seismiki Barlag",  "GB1234567",  "Nov 22, 2025", "Expiring (<30 days)", "st-expiring"),
        R("Kemal Aydin",      "Gaz Stansiasy",    "U87654321",  "Jul 09, 2029", "Valid (>90 days)",    "st-approved"),
        R("Alina Makarova",   "Gurlusyk UZT",     "71 9876541", "Feb 17, 2027", "Valid (>90 days)",    "st-approved"),
        R("Bayram Rejepow",   "Elektrik Stansia", "TM1234001",  "May 30, 2025", "Expired",             "st-expiring"),
        R("Hans Muller",      "Gaz Stansiasy",    "C123456789", "Aug 12, 2026", "Valid (31-90 days)",  "st-pending"),
        R("Oleg Kovalev",     "Seismiki Barlag",  "EK1234567",  "Jan 04, 2025", "Expired",             "st-expiring"),
        R("Leyli Annagur.",   "Elektrik Stansia", "TM2345002",  "Sep 19, 2028", "Valid (>90 days)",    "st-approved"),
        R("Cary Durdyyew",    "Gurlusyk UZT",     "TM3456003",  "Apr 25, 2027", "Valid (>90 days)",    "st-approved"),
    ];

    private static List<ReportDashboardPreviewRow> PassportByType() =>
    [
        // ColumnA = passport#, ColumnB = expiry, Status = type (drives chart buckets)
        R("Mehmet Yilmaz",    "Gurlusyk UZT",     "U12345678",  "Oct 18, 2027", "Civil",       "st-cat-1"),
        R("Viktor Petrov",    "Gurlusyk UZT",     "71 2344521", "Mar 05, 2028", "Civil",       "st-cat-1"),
        R("John Smith",       "Seismiki Barlag",  "GB1234567",  "Nov 22, 2025", "Civil",       "st-cat-1"),
        R("Kemal Aydin",      "Gaz Stansiasy",    "U87654321",  "Jul 09, 2029", "Civil",       "st-cat-1"),
        R("Alina Makarova",   "Gurlusyk UZT",     "71 9876541", "Feb 17, 2027", "Service",     "st-cat-2"),
        R("Bayram Rejepow",   "Elektrik Stansia", "TM1234001",  "May 30, 2025", "Service",     "st-cat-2"),
        R("Hans Muller",      "Gaz Stansiasy",    "C123456789", "Aug 12, 2026", "UN Passport", "st-cat-3"),
        R("Oleg Kovalev",     "Seismiki Barlag",  "EK1234567",  "Jan 04, 2025", "UN Passport", "st-cat-3"),
        R("Leyli Annagur.",   "Elektrik Stansia", "TM2345002",  "Sep 19, 2028", "Service",     "st-cat-2"),
        R("Cary Durdyyew",    "Gurlusyk UZT",     "TM3456003",  "Apr 25, 2027", "Civil",       "st-cat-1"),
    ];

    private static List<ReportDashboardPreviewRow> PassportByCitizenship() =>
    [
        // ColumnA = passport#, ColumnB = expiry, Status = citizenship (drives chart buckets)
        R("Mehmet Yilmaz",    "Gurlusyk UZT",     "U12345678",  "Oct 18, 2027", "Turkiye",      "st-cat-1"),
        R("Viktor Petrov",    "Gurlusyk UZT",     "71 2344521", "Mar 05, 2028", "Russia",       "st-cat-2"),
        R("John Smith",       "Seismiki Barlag",  "GB1234567",  "Nov 22, 2025", "UK",           "st-cat-3"),
        R("Kemal Aydin",      "Gaz Stansiasy",    "U87654321",  "Jul 09, 2029", "Turkiye",      "st-cat-1"),
        R("Alina Makarova",   "Gurlusyk UZT",     "71 9876541", "Feb 17, 2027", "Russia",       "st-cat-2"),
        R("Bayram Rejepow",   "Elektrik Stansia", "TM1234001",  "May 30, 2025", "Turkmenistan", "st-cat-4"),
        R("Hans Muller",      "Gaz Stansiasy",    "C123456789", "Aug 12, 2026", "Germany",      "st-cat-5"),
        R("Oleg Kovalev",     "Seismiki Barlag",  "EK1234567",  "Jan 04, 2025", "Russia",       "st-cat-2"),
        R("Leyli Annagur.",   "Elektrik Stansia", "TM2345002",  "Sep 19, 2028", "Turkmenistan", "st-cat-4"),
        R("Cary Durdyyew",    "Gurlusyk UZT",     "TM3456003",  "Apr 25, 2027", "Turkmenistan", "st-cat-4"),
    ];

    // ===== Education ======================================================

    private static List<ReportDashboardPreviewRow> EducationByLevel() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "Istanbul Technical Univ.", "2012", "Bachelor",      "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "Moscow State Univ.",       "2008", "Master",        "st-cat-2"),
        R("John Smith",          "Seismiki Barlag",  "University of Leeds",      "2015", "Bachelor",      "st-cat-1"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "ODTU",                     "2010", "Bachelor",      "st-cat-1"),
        R("Alina Makarova",      "Gurlusyk UZT",     "SPbPU",                    "2014", "Specialist",    "st-cat-3"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "Turkmen Polytechnic",      "2006", "Bachelor",      "st-cat-1"),
        R("Hans Muller",         "Gaz Stansiasy",    "TU Munich",                "2009", "Master",        "st-cat-2"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "Bauman MSTU",              "2011", "PhD",           "st-cat-4"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "Magtymguly Univ.",         "2016", "Bachelor",      "st-cat-1"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "Istanbul University",      "2007", "College",       "st-cat-5"),
    ];

    private static List<ReportDashboardPreviewRow> EducationByCountry() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "Istanbul Technical Univ.", "2012", "Turkiye",      "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "Moscow State Univ.",       "2008", "Russia",       "st-cat-2"),
        R("John Smith",          "Seismiki Barlag",  "University of Leeds",      "2015", "UK",           "st-cat-3"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "ODTU",                     "2010", "Turkiye",      "st-cat-1"),
        R("Alina Makarova",      "Gurlusyk UZT",     "SPbPU",                    "2014", "Russia",       "st-cat-2"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "Turkmen Polytechnic",      "2006", "Turkmenistan", "st-cat-4"),
        R("Hans Muller",         "Gaz Stansiasy",    "TU Munich",                "2009", "Germany",      "st-cat-5"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "Bauman MSTU",              "2011", "Russia",       "st-cat-2"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "Magtymguly Univ.",         "2016", "Turkmenistan", "st-cat-4"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "Istanbul University",      "2007", "Turkiye",      "st-cat-1"),
    ];

    private static List<ReportDashboardPreviewRow> EducationBySpecialty() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "Istanbul Technical Univ.", "2012", "Civil Engineering",     "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "Moscow State Univ.",       "2008", "Mechanical Engineering","st-cat-2"),
        R("John Smith",          "Seismiki Barlag",  "University of Leeds",      "2015", "Geophysics",            "st-cat-3"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "ODTU",                     "2010", "Electrical Engineering","st-cat-4"),
        R("Alina Makarova",      "Gurlusyk UZT",     "SPbPU",                    "2014", "Civil Engineering",     "st-cat-1"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "Turkmen Polytechnic",      "2006", "Electrical Engineering","st-cat-4"),
        R("Hans Muller",         "Gaz Stansiasy",    "TU Munich",                "2009", "Mechanical Engineering","st-cat-2"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "Bauman MSTU",              "2011", "Geophysics",            "st-cat-3"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "Magtymguly Univ.",         "2016", "Accounting",            "st-cat-5"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "Istanbul University",      "2007", "Civil Engineering",     "st-cat-1"),
    ];

    // ===== Position History ===============================================

    private static List<ReportDashboardPreviewRow> PositionHistoryByPosition() =>
    [
        // Status = Position (visa reports)
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "Engineer",           "Jan 12, 2022", "Engineer",           "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "Site Supervisor",    "Mar 01, 2021", "Site Supervisor",    "st-cat-2"),
        R("John Smith",          "Seismiki Barlag",  "Geophysicist",       "Jun 15, 2023", "Geophysicist",       "st-cat-3"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "Electrical Eng.",    "Sep 01, 2020", "Electrical Eng.",    "st-cat-4"),
        R("Alina Makarova",      "Gurlusyk UZT",     "QA Specialist",      "Feb 10, 2019", "QA Specialist",      "st-cat-5"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "Technician",         "Apr 20, 2018", "Technician",         "st-cat-1"),
        R("Hans Muller",         "Gaz Stansiasy",    "Project Engineer",   "Nov 05, 2022", "Project Engineer",   "st-cat-2"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "Field Engineer",     "Jul 22, 2021", "Field Engineer",     "st-cat-3"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "Accountant",         "Jan 08, 2024", "Accountant",         "st-cat-4"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "Foreman",            "May 14, 2017", "Foreman",            "st-cat-5"),
    ];

    private static List<ReportDashboardPreviewRow> PositionHistoryByActualPosition() =>
    [
        // ColumnA = visa Position, Status = Position (actual / company)
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "Engineer",           "Jan 12, 2022", "Senior Field Engineer", "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "Site Supervisor",    "Mar 01, 2021", "Site Lead",             "st-cat-2"),
        R("John Smith",          "Seismiki Barlag",  "Geophysicist",       "Jun 15, 2023", "Survey Specialist",     "st-cat-3"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "Electrical Eng.",    "Sep 01, 2020", "E&I Engineer",          "st-cat-4"),
        R("Alina Makarova",      "Gurlusyk UZT",     "QA Specialist",      "Feb 10, 2019", "QA Lead",               "st-cat-5"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "Technician",         "Apr 20, 2018", "Plant Technician",      "st-cat-1"),
        R("Hans Muller",         "Gaz Stansiasy",    "Project Engineer",   "Nov 05, 2022", "Project Engineer",      "st-cat-2"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "Field Engineer",     "Jul 22, 2021", "Field Engineer",        "st-cat-3"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "Accountant",         "Jan 08, 2024", "Payroll Accountant",    "st-cat-4"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "Foreman",            "May 14, 2017", "Crew Foreman",          "st-cat-5"),
    ];

    // ===== Subcontractor ==================================================

    private static List<ReportDashboardPreviewRow> SubcontractorByCompany() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "Employee",           "Jan 12, 2022", "Calik Enerji",       "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "Employee",           "Mar 01, 2021", "Calik Enerji",       "st-cat-1"),
        R("John Smith",          "Seismiki Barlag",  "Employee",           "Jun 15, 2023", "Gap Insaat",         "st-cat-2"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "Employee",           "Sep 01, 2020", "Polimeks",           "st-cat-3"),
        R("Alina Makarova",      "Gurlusyk UZT",     "Family Member",      "Feb 10, 2019", "Calik Enerji",       "st-cat-1"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "Employee",           "Apr 20, 2018", "Ronesans",           "st-cat-4"),
        R("Hans Muller",         "Gaz Stansiasy",    "Employee",           "Nov 05, 2022", "Polimeks",           "st-cat-3"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "Employee",           "Jul 22, 2021", "Gap Insaat",         "st-cat-2"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "Family Member",      "Jan 08, 2024", "Ronesans",           "st-cat-4"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "Employee",           "May 14, 2017", "Unassigned",         "st-cat-5"),
    ];

    // ===== Medical Records ================================================

    private static List<ReportDashboardPreviewRow> MedicalRecordByValidity() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "MR-2025-1012", "Oct 12, 2026", "Approved",           "st-approved"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "MR-2025-1031", "Mar 15, 2027", "Approved",           "st-approved"),
        R("John Smith",          "Seismiki Barlag",  "MR-2025-0904", "Jan 04, 2026", "Expiring (<30 days)", "st-expiring"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "MR-2025-1130", "Nov 30, 2026", "Approved",           "st-approved"),
        R("Alina Makarova",      "Gurlusyk UZT",     "MR-2025-1018", "Feb 18, 2026", "Expiring Soon",      "st-expiring"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "MR-2024-0801", "Sep 01, 2025", "Expired",            "st-expiring"),
        R("Hans Muller",         "Gaz Stansiasy",    "MR-2025-0722", "Apr 22, 2026", "Expiring Soon",      "st-expiring"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "MR-2025-0610", "May 10, 2026", "Approved",           "st-approved"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "MR-2025-1201", "Dec 01, 2026", "Approved",           "st-approved"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "MR-2024-0614", "Jun 14, 2025", "Expired",            "st-expiring"),
    ];

    // ===== helpers ========================================================

    private static ReportDashboardPreviewRow R11(
        string name, string project, string position, string appType, string visaPeriod, string visaType,
        string visaOnExt, string issuedVisa, string appNum, string appDate, string status, string css) =>
        new()
        {
            Name = name,
            Project = project,
            ColumnA = position,
            ColumnB = appType,
            ColumnC = visaPeriod,
            ColumnD = visaType,
            ColumnE = visaOnExt,
            ColumnF = issuedVisa,
            ColumnG = appNum,
            ColumnH = appDate,
            Status = status,
            StatusCssClass = css
        };

    private static ReportDashboardPreviewRow R9(
        string name, string project, string position, string appType, string visaPeriod, string visaType,
        string appNum, string appDate, string status, string css) =>
        new()
        {
            Name = name,
            Project = project,
            ColumnA = position,
            ColumnB = appType,
            ColumnC = visaPeriod,
            ColumnD = visaType,
            ColumnE = appNum,
            ColumnF = appDate,
            Status = status,
            StatusCssClass = css
        };
    private static ReportDashboardPreviewRow R7(
        string name, string project, string position, string appType, string appNum, string appDate, string status, string css) =>
        new()
        {
            Name = name,
            Project = project,
            ColumnA = position,
            ColumnB = appType,
            ColumnC = appNum,
            ColumnD = appDate,
            Status = status,
            StatusCssClass = css
        };
    private static ReportDashboardPreviewRow R(
        string name, string project, string colA, string colB, string status, string css) =>
        new() { Name = name, Project = project, ColumnA = colA, ColumnB = colB, Status = status, StatusCssClass = css };

    private static ReportDashboardPanelData BuildExtensionRequiredByDays(
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string subReport,
        List<ReportDashboardPreviewRow> allRows,
        string projectKey)
    {
        static int DaysKey(string? label)
        {
            if (string.IsNullOrWhiteSpace(label)) return int.MaxValue;
            var sp = label.IndexOf(' ');
            return sp > 0 && int.TryParse(label.AsSpan(0, sp), out var n) ? n : int.MaxValue;
        }

        var rows = projectKey == "All"
            ? allRows.ToList()
            : allRows.FindAll(r => r.Project.Contains(projectKey, StringComparison.OrdinalIgnoreCase));

        rows = rows
            .OrderBy(r => DaysKey(r.Status))
            .ThenBy(r => r.ColumnB)
            .ThenBy(r => r.Name)
            .ToList();

        var buckets = rows
            .GroupBy(r => r.Status)
            .Select(g => new ReportDashboardStatusBucket
            {
                Label = g.Key,
                Count = g.Count(),
                CssClass = g.First().StatusCssClass
            })
            .OrderBy(b => DaysKey(b.Label))
            .ToList();

        var subLabel = ReportDashboardCatalog.SubReports(category)
            .FirstOrDefault(s => s.Key == subReport)?.Label ?? subReport;

        return new ReportDashboardPanelData
        {
            PersonType = personType,
            Category = category,
            SubReport = subReport,
            Title = ReportDashboardCatalog.CategoryLabel(category),
            Subtitle = $"{ReportDashboardCatalog.PersonTypeLabel(personType)} — {subLabel}",
            TableHeaders = ReportDashboardCatalog.TableHeaders(category, subReport),
            StatusBuckets = buckets,
            PreviewRows = rows,
            TotalCount = rows.Count,
            ExcelTemplateNameHint = ReportDashboardCatalog.ExcelTemplateNameHint(category, subReport),
            ExcelConfigured = false,
            ListViewId = ReportDashboardCatalog.ResolveListViewTarget(category, subReport).ListViewId
        };
    }

    private static ReportDashboardPanelData Build(
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string subReport,
        List<ReportDashboardPreviewRow> allRows,
        string projectKey,
        bool excelConfigured = false,
        bool oneLastValidVisaPerPerson = false,
        string? subReportLabel = null)
    {
        var rows = projectKey == "All"
            ? allRows
            : allRows.FindAll(r => r.Project.Contains(projectKey, StringComparison.OrdinalIgnoreCase));

        if (oneLastValidVisaPerPerson)
        {
            // Mock has no PersonOid; keep one row per person name (latest ColumnB / expiry text order).
            rows = rows
                .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(r => r.ColumnB).ThenByDescending(r => r.ColumnA).First())
                .ToList();
        }

        var buckets = rows
            .GroupBy(r => r.Status)
            .Select(g => new ReportDashboardStatusBucket
            {
                Label    = g.Key,
                Count    = g.Count(),
                CssClass = g.First().StatusCssClass
            })
            .OrderByDescending(b => b.Count)
            .ToList();

        var subLabel = !string.IsNullOrWhiteSpace(subReportLabel)
            ? subReportLabel!
            : ReportDashboardCatalog.SubReports(category)
                .FirstOrDefault(s => s.Key == subReport)?.Label ?? subReport;

        return new ReportDashboardPanelData
        {
            PersonType            = personType,
            Category              = category,
            SubReport             = subReport,
            Title                 = ReportDashboardCatalog.CategoryLabel(category),
            Subtitle              = $"{ReportDashboardCatalog.PersonTypeLabel(personType)} — {subLabel}",
            TableHeaders          = ReportDashboardCatalog.TableHeaders(category, subReport),
            StatusBuckets         = buckets,
            PreviewRows           = rows,
            TotalCount            = rows.Count,
            ExcelTemplateNameHint = ReportDashboardCatalog.ExcelTemplateNameHint(category, subReport),
            ExcelConfigured       = excelConfigured,
            ListViewId            = ReportDashboardCatalog.ResolveListViewTarget(category, subReport).ListViewId
        };
    }
}
