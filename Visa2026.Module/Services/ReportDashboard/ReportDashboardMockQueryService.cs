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
        [(ReportDashboardPersonType.Employees, ReportDashboardCategory.Application)]   = 98,
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
        [(ReportDashboardPersonType.FamilyMembers, ReportDashboardCategory.Application)]   = 28,
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
        [(ReportDashboardPersonType.TemporaryVisitors, ReportDashboardCategory.Application)]   = 10,
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
        [(ReportDashboardPersonType.All, ReportDashboardCategory.Application)]      = 136,
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
            // Application — Application Status (combined state label)
            (ReportDashboardCategory.Application, _) => Build(
                personType, category, subReport, ApplicationByStatus(), projectKey,
                subReportLabel: "Application Status"),
            // Visa (formerly Visa Extension)
            (ReportDashboardCategory.VisaExtension, "by-category")  => Build(personType, category, subReport, VisaByCategory(),     projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, "by-type")      => Build(personType, category, subReport, VisaByType(),         projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, "by-period")         => Build(personType, category, subReport, VisaByPeriod(),         projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, "by-days-remaining") => Build(personType, category, subReport, VisaByDaysRemaining(), projectKey, oneLastValidVisaPerPerson: applyOneLastVisa),
            (ReportDashboardCategory.VisaExtension, _)                   => Build(personType, category, subReport, VisaByState(),        projectKey, excelConfigured: true),
            // Invitation
            (ReportDashboardCategory.Invitation, _)              => Build(personType, category, subReport, InvitationIssued(),        projectKey),
            // Registration
            (ReportDashboardCategory.Registration, "by-region")  => Build(personType, category, subReport, RegistrationByRegion(),   projectKey),
            (ReportDashboardCategory.Registration, _)            => Build(personType, category, subReport, RegistrationByValidity(), projectKey),
            // Work Permit
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

    // ===== Visa (Visa Extension) — 4 sub-reports ==========================

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

    // ===== Application ====================================================

    private static List<ReportDashboardPreviewRow> ApplicationByStatus() =>
    [
        R("Mehmet Yilmaz", "Gurlusyk UZT", "APP-2026-0101", "Jan 08, 2026",
            "Being Prepared · 1 ministry · Energetika · —", "st-pending"),
        R("Viktor Petrov", "Gurlusyk UZT", "APP-2026-0112", "Jan 12, 2026",
            "1st Review Started · 1 ministry · Energetika · On track · 3/10", "st-pending"),
        R("Kemal Aydin", "Gaz Stansiasy", "APP-2026-0125", "Jan 20, 2026",
            "1st Review Approved · 1 ministry · — · —", "st-approved"),
        R("Hans Muller", "Gaz Stansiasy", "APP-2026-0138", "Feb 02, 2026",
            "2nd Review Started · 2 ministries · Gurluşyk · —", "st-pending"),
        R("John Smith", "Seismiki Barlag", "APP-2026-0150", "Feb 14, 2026",
            "Process Started · None · — · On track · 2/7", "st-pending"),
        R("Alina Makarova", "Gurlusyk UZT", "APP-2026-0161", "Feb 28, 2026",
            "Process Issued · 1 ministry · Energetika · —", "st-approved"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "APP-2026-0173", "Mar 10, 2026",
            "Process Rejected · None · — · —", "st-expiring"),
        R("Leyli Annagur.", "Elektrik Stansia", "APP-2026-0184", "Mar 22, 2026",
            "Being Prepared · 1 ministry · — · —", "st-pending"),
        R("Oleg Kovalev", "Seismiki Barlag", "APP-2026-0196", "Apr 05, 2026",
            "1st Review Started · 1 ministry · — · —", "st-pending"),
        R("Serdar Geldiyew", "Gurlusyk UZT", "APP-2026-0208", "Apr 18, 2026",
            "2nd Review Approved · 2 ministries · Gurluşyk · —", "st-approved"),
    ];

    // ===== Visa ===========================================================

    /// By Visa Category: Status = VisaCategory only (not Visa State)
    private static List<ReportDashboardPreviewRow> VisaByCategory() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "V-2025-1012", "Oct 12, 2026", "köp gezeklik", "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "V-2025-1031", "Mar 15, 2027", "iki gezeklik", "st-cat-2"),
        R("John Smith",          "Seismiki Barlag",  "V-2025-0904", "Jan 04, 2026", "köp gezeklik", "st-cat-1"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "V-2025-1130", "Nov 30, 2026", "köp gezeklik", "st-cat-1"),
        R("Alina Makarova",      "Gurlusyk UZT",     "V-2025-1018", "Feb 18, 2027", "iki gezeklik", "st-cat-2"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "V-2025-0801", "Sep 01, 2025", "bir gezeklik", "st-cat-3"),
        R("Hans Muller",         "Gaz Stansiasy",    "V-2025-0722", "Apr 22, 2026", "köp gezeklik", "st-cat-1"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "V-2025-0610", "May 10, 2026", "köp gezeklik", "st-cat-1"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "V-2025-1201", "Dec 01, 2026", "iki gezeklik", "st-cat-2"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "V-2025-0614", "Jun 14, 2026", "köp gezeklik", "st-cat-1"),
    ];

    /// By Visa Type: Status = VisaType only (not Visa State)
    private static List<ReportDashboardPreviewRow> VisaByType() =>
    [
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "V-2025-1012", "Oct 12, 2026", "WP-Işçi Wiza",   "st-cat-1"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "V-2025-1031", "Mar 15, 2027", "BS1-İşerwürlik", "st-cat-2"),
        R("John Smith",          "Seismiki Barlag",  "V-2025-0904", "Jan 04, 2026", "WP-Işçi Wiza",   "st-cat-1"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "V-2025-1130", "Nov 30, 2026", "WP-Işçi Wiza",   "st-cat-1"),
        R("Alina Makarova",      "Gurlusyk UZT",     "V-2025-1018", "Feb 18, 2027", "FM-Maşgala",     "st-cat-3"),
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "V-2025-0801", "Sep 01, 2025", "WP-Işçi Wiza",   "st-cat-1"),
        R("Hans Muller",         "Gaz Stansiasy",    "V-2025-0722", "Apr 22, 2026", "WP-Işçi Wiza",   "st-cat-1"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "V-2025-0610", "May 10, 2026", "BS1-İşerwürlik", "st-cat-2"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "V-2025-1201", "Dec 01, 2026", "FM-Maşgala",     "st-cat-3"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "V-2025-0614", "Jun 14, 2026", "WP-Işçi Wiza",   "st-cat-1"),
    ];

    /// By Visa Period: nearest granted duration (Start→End → 1 month / 3 months / 6 months / 1 year)
    private static List<ReportDashboardPreviewRow> VisaByPeriod() =>
    [
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "V-2025-0801", "Jul 20, 2026", "1 month",  "st-cat-1"),
        R("John Smith",          "Seismiki Barlag",  "V-2025-0904", "Aug 04, 2026", "1 month",  "st-cat-1"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "V-2025-0610", "Aug 28, 2026", "3 months", "st-cat-2"),
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "V-2025-1012", "Sep 18, 2026", "6 months", "st-cat-3"),
        R("Hans Muller",         "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "6 months", "st-cat-3"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "V-2025-0614", "Oct 12, 2026", "6 months", "st-cat-3"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "V-2025-1130", "Nov 05, 2026", "6 months", "st-cat-3"),
        R("Alina Makarova",      "Gurlusyk UZT",     "V-2025-1018", "Dec 01, 2026", "6 months", "st-cat-3"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "V-2025-1201", "Jan 10, 2027", "1 year",   "st-cat-4"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "V-2025-1031", "Jan 25, 2027", "1 year",   "st-cat-4"),
    ];

    /// By Days Remaining: closed days-to-expiry buckets on valid visas
    private static List<ReportDashboardPreviewRow> VisaByDaysRemaining() =>
    [
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "V-2025-0801", "Jul 20, 2026", "< 10 days",  "st-expiring"),
        R("John Smith",          "Seismiki Barlag",  "V-2025-0904", "Aug 04, 2026", "< 1 month",  "st-expiring"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "V-2025-0610", "Aug 28, 2026", "< 1 month",  "st-expiring"),
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "V-2025-1012", "Sep 18, 2026", "< 3 months", "st-pending"),
        R("Hans Muller",         "Gaz Stansiasy",    "V-2025-0722", "Sep 30, 2026", "< 3 months", "st-pending"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "V-2025-0614", "Oct 12, 2026", "< 3 months", "st-pending"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "V-2025-1130", "Nov 05, 2026", "< 4 months", "st-approved"),
        R("Alina Makarova",      "Gurlusyk UZT",     "V-2025-1018", "Dec 01, 2026", "< 5 months", "st-approved"),
        R("Leyli Annagurbanowa", "Elektrik Stansia", "V-2025-1201", "Jan 10, 2027", "< 6 months", "st-approved"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "V-2025-1031", "Jan 25, 2027", "≥ 6 months", "st-approved"),
    ];

    // ===== Invitation =====================================================

    /// Issued Invitations: grouped by remaining validity (exclusive buckets)
    private static List<ReportDashboardPreviewRow> InvitationIssued() =>
    [
        // ColumnA = invitation#, ColumnB = expiry, Status = validity bucket (drives chart buckets)
        R("Bayrammyrat Rejepow", "Elektrik Stansia", "INV-2025-0041", "Jul 18, 2026", "Valid (<15 days)",  "st-expiring"),
        R("John Smith",          "Seismiki Barlag",  "INV-2025-0062", "Jul 28, 2026", "Valid (<30 days)",  "st-expiring"),
        R("Oleg Kovalev",        "Seismiki Barlag",  "INV-2025-0088", "Aug 05, 2026", "Valid (<30 days)",  "st-expiring"),
        R("Hans Muller",         "Gaz Stansiasy",    "INV-2025-0053", "Aug 22, 2026", "Valid (<60 days)",  "st-pending"),
        R("Serdar Geldiyew",     "Gurlusyk UZT",     "INV-2025-0097", "Sep 01, 2026", "Valid (<60 days)",  "st-pending"),
        R("Mehmet Yilmaz",       "Gurlusyk UZT",     "INV-2025-0071", "Sep 30, 2026", "Valid (<90 days)",  "st-pending"),
        R("Viktor Petrov",       "Gurlusyk UZT",     "INV-2025-0058", "Oct 12, 2026", "Valid (<90 days)",  "st-pending"),
        R("Kemal Aydin",         "Gaz Stansiasy",    "INV-2025-0079", "Jun 05, 2026", "Expired",           "st-expiring"),
        R("Alina Makarova",      "Gurlusyk UZT",     "INV-2025-0083", "May 15, 2026", "Expired",           "st-expiring"),
        R("Leyli Annagur.",      "Elektrik Stansia", "INV-2025-0091", "Apr 30, 2026", "Used",              "st-approved"),
        R("Cary Durdyyew",       "Gurlusyk UZT",     "INV-2025-0095", "Mar 20, 2026", "Used",              "st-approved"),
    ];

    // ===== Registration ===================================================

    private static List<ReportDashboardPreviewRow> RegistrationByValidity() =>
    [
        R("Mehmet Yilmaz",    "Gurlusyk UZT",     "Balkan welaýaty, Turkmenbasy", "Oct 15, 2026", "Approved",      "st-approved"),
        R("Viktor Petrov",    "Gurlusyk UZT",     "Ahal welaýaty, Asgabat",       "Mar 01, 2026", "Approved",      "st-approved"),
        R("John Smith",       "Seismiki Barlag",  "Ahal welaýaty, Asgabat",       "Jan 10, 2026", "Expiring Soon", "st-expiring"),
        R("Kemal Aydin",      "Gaz Stansiasy",    "Balkan welaýaty, Turkmenbasy", "Dec 20, 2026", "Approved",      "st-approved"),
        R("Hans Muller",      "Gaz Stansiasy",    "Balkan welaýaty, Turkmenbasy", "Sep 30, 2025", "Expiring Soon", "st-expiring"),
        R("Alina Makarova",   "Gurlusyk UZT",     "Ahal welaýaty, Asgabat",       "Apr 12, 2026", "Approved",      "st-approved"),
        R("Bayram Rejepow",   "Elektrik Stansia", "Mary welaýaty, Mary",           "Feb 08, 2026", "Pending",       "st-pending"),
        R("Oleg Kovalev",     "Seismiki Barlag",  "Balkan welaýaty, Hazar",        "Nov 25, 2026", "Approved",      "st-approved"),
        R("Leyli Annagur.",   "Elektrik Stansia", "Lebap welaýaty, Turkmenabat",  "Jul 05, 2026", "Approved",      "st-approved"),
        R("Cary Durdyyew",    "Gurlusyk UZT",     "Balkan welaýaty, Turkmenbasy", "Aug 19, 2026", "Approved",      "st-approved"),
    ];

    private static List<ReportDashboardPreviewRow> RegistrationByRegion() =>
    [
        // ColumnA = address, ColumnB = expiry, Status = region (drives chart buckets)
        R("Mehmet Yilmaz",    "Gurlusyk UZT",     "Balkan welaýaty, Turkmenbasy", "Oct 15, 2026", "Balkan",  "st-cat-1"),
        R("Viktor Petrov",    "Gurlusyk UZT",     "Ahal welaýaty, Asgabat",       "Mar 01, 2026", "Ahal",    "st-cat-2"),
        R("John Smith",       "Seismiki Barlag",  "Ahal welaýaty, Asgabat",       "Jan 10, 2026", "Ahal",    "st-cat-2"),
        R("Kemal Aydin",      "Gaz Stansiasy",    "Balkan welaýaty, Turkmenbasy", "Dec 20, 2026", "Balkan",  "st-cat-1"),
        R("Hans Muller",      "Gaz Stansiasy",    "Balkan welaýaty, Hazar",       "Sep 30, 2025", "Balkan",  "st-cat-1"),
        R("Alina Makarova",   "Gurlusyk UZT",     "Ahal welaýaty, Asgabat",       "Apr 12, 2026", "Ahal",    "st-cat-2"),
        R("Bayram Rejepow",   "Elektrik Stansia", "Mary welaýaty, Mary",           "Feb 08, 2026", "Mary",    "st-cat-3"),
        R("Oleg Kovalev",     "Seismiki Barlag",  "Balkan welaýaty, Hazar",        "Nov 25, 2026", "Balkan",  "st-cat-1"),
        R("Leyli Annagur.",   "Elektrik Stansia", "Lebap welaýaty, Turkmenabat",  "Jul 05, 2026", "Lebap",   "st-cat-4"),
        R("Cary Durdyyew",    "Gurlusyk UZT",     "Balkan welaýaty, Turkmenbasy", "Aug 19, 2026", "Balkan",  "st-cat-1"),
    ];

    // ===== Work Permit ====================================================

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

    private static ReportDashboardPreviewRow R(
        string name, string project, string colA, string colB, string status, string css) =>
        new() { Name = name, Project = project, ColumnA = colA, ColumnB = colB, Status = status, StatusCssClass = css };

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
            ExcelTemplateNameHint = ReportDashboardCatalog.ExcelTemplateNameHint(category),
            ExcelConfigured       = excelConfigured,
            ListViewId            = ReportDashboardCatalog.ListViewId(category)
        };
    }
}