using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ReportDashboard;

public static class ReportDashboardCatalog
{
    public static readonly ReportDashboardCategory[] Categories =
    [
        ReportDashboardCategory.Application,
        ReportDashboardCategory.VisaExtension,
        ReportDashboardCategory.Invitation,
        ReportDashboardCategory.Registration,
        ReportDashboardCategory.WorkPermit,
        ReportDashboardCategory.Travel,
        ReportDashboardCategory.AddressOfResidence,
        ReportDashboardCategory.BorderZone,
        ReportDashboardCategory.Passport,
        ReportDashboardCategory.Education,
        ReportDashboardCategory.PositionHistory,
        ReportDashboardCategory.Subcontractor,
        ReportDashboardCategory.MedicalRecord
    ];

    /// <summary>
    /// Categories that expose an Include archived toggle (Person.IsArchived on the SQL view / loader).
    /// </summary>
    public static bool SupportsIncludeArchivedPersons(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.WorkPermit
            or ReportDashboardCategory.Education
            or ReportDashboardCategory.AddressOfResidence
            or ReportDashboardCategory.Subcontractor
            or ReportDashboardCategory.MedicalRecord;

    /// <summary>
    /// When checked (default), include only persons with at least one valid visa
    /// (not cancelled, ExpirationDate on or after today).
    /// </summary>
    public static bool SupportsValidVisaPersonsOnly(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.WorkPermit
            or ReportDashboardCategory.Travel
            or ReportDashboardCategory.BorderZone
            or ReportDashboardCategory.Passport
            or ReportDashboardCategory.Education
            or ReportDashboardCategory.PositionHistory
            or ReportDashboardCategory.AddressOfResidence
            or ReportDashboardCategory.Subcontractor
            or ReportDashboardCategory.MedicalRecord;

    /// <summary>
    /// Visa category: toggle to count one last valid visa per person (latest expiry) vs all valid visas.
    /// Applies to active-by-project / by-period-category-type / by-days-remaining.
    /// </summary>
    public static bool SupportsOneLastValidVisaPerPerson(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.VisaExtension;

    /// <summary>
    /// Work Permit: toggle to count one last valid work permit per person (latest expiry).
    /// Applies to active-by-project / by-days-remaining.
    /// </summary>
    public static bool SupportsOneLastValidWorkPermitPerPerson(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.WorkPermit;

    /// <summary>
    /// Application: toggles to include PROCESS_ISSUED / PROCESS_CANCELLED (latest progress).
    /// Shared on Application Status; default exclude both.
    /// </summary>
    public static bool SupportsIncludeCompletedApplicationProcesses(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.Application;

    public static bool SupportsIncludeCancelledApplicationProcesses(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.Application;

    /// <summary>
    /// Application Status sub-report: chart buckets use combined
    /// <c>State · Ministry depth · Approval leg · Migration SLA</c>.
    /// </summary>
    public const string ApplicationStatusSubReportKey = "app-status";

    /// <summary>
    /// Categories that show a local "Last N months" filter (not a global top-bar control).
    /// Education / Passport: ApplicationItem usage filtered by Application.ApplicationDate.
    /// </summary>
    public static bool SupportsCategoryDateRange(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.Education
            or ReportDashboardCategory.Passport
            or ReportDashboardCategory.PositionHistory
            or ReportDashboardCategory.AddressOfResidence
            or ReportDashboardCategory.MedicalRecord;

    /// <summary>Default Last-N months for category-local date filters.</summary>
    public const int DefaultCategoryDateRangeMonths = 9;

    /// <summary>
    /// Sub-reports that emit one row per valid visa (persons may appear more than once).
    /// </summary>
    public static bool SubReportCountsValidVisas(string subReport) =>
        subReport is "active-by-project"
            or "by-period-category-type"
            or "by-category" or "by-type" or "by-period" // legacy keys remapped
            or "by-days-remaining";

    /// <summary>
    /// Sub-reports that emit one row per valid work permit (persons may appear more than once).
    /// </summary>
    public static bool SubReportCountsValidWorkPermits(string subReport) =>
        subReport is "active-by-project"
            or "by-days-remaining"
            or "by-validity"; // legacy alias for WorkPermit Validity

    /// <summary>
    /// WorkPermit Extension (P) / Extension Result (P): apps of these types with CurrentWorkPermitItem.
    /// Extension excludes terminal + review rejects; Result includes only those outcome codes.
    /// </summary>
    public static readonly string[] WorkPermitExtensionApplicationTypeNames =
    [
        "App_WP_Ext",
        "App_Visa_and_WP_Ext",
    ];

    /// <summary>
    /// Progress codes for WorkPermit Extension Result (P)
    /// (Issued / Cancelled / Rejected + 1st/2nd Review Rejected).
    /// </summary>
    public static readonly string[] WorkPermitExtensionResultStateCodes =
    [
        ApplicationProgressStateCodes.ProcessIssued,
        ApplicationProgressStateCodes.ProcessCancelled,
        ApplicationProgressStateCodes.ProcessRejected,
        ApplicationProgressStateCodes.Review1Rejected,
        ApplicationProgressStateCodes.Review2Rejected,
    ];

    // ---- Sub-reports per category ----------------------------------------

    public const string RegistrationExpiringStateSubReportKey = "expiring-state";
    public const string RegistrationCheckInByProjectSubReportKey = "check-in-by-project";
    public const string RegistrationCheckInByPeriodCategoryTypeSubReportKey = "check-in-by-period-category-type";
    public const string RegistrationCheckInByCitySubReportKey = "check-in-by-city";
    public const string RegistrationToBeCheckedInSubReportKey = "to-be-checked-in";
    public const string RegistrationToBeCheckedOutSubReportKey = "to-be-checked-out";
    public const string RegistrationOnProcessSubReportKey = "on-process";

    /// <summary>
    /// Last registration app types that count for Expiring State / Active Registered (not Check-Out).
    /// </summary>
    public static readonly string[] RegistrationExpiringStateApplicationTypeNames =
    [
        "App_Reg_Check_In",
        "App_Reg_Check_In_Internal",
        "App_Reg_ext",
        "App_Reg_Info_Change_Address",
        "App_Reg_Info_Change_Passport",
        "App_Reg_Info_Change_Visa",
    ];

    /// <summary>
    /// All registration ApplicationType names for On process (unfinished apps).
    /// </summary>
    public static readonly string[] RegistrationOnProcessApplicationTypeNames =
    [
        "App_Reg_Check_In",
        "App_Reg_Check_In_Internal",
        "App_Reg_ext",
        "App_Reg_Info_Change_Address",
        "App_Reg_Info_Change_Passport",
        "App_Reg_Info_Change_Visa",
        "App_Reg_Check_Out",
        "App_Reg_Check_Out_Internal",
    ];

    public static bool IsRegistrationExpiringStateSubReport(string? subReport) =>
        string.Equals(subReport, RegistrationExpiringStateSubReportKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsRegistrationCheckInByProjectSubReport(string? subReport) =>
        string.Equals(subReport, RegistrationCheckInByProjectSubReportKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsRegistrationCheckInByPeriodCategoryTypeSubReport(string? subReport) =>
        string.Equals(subReport, RegistrationCheckInByPeriodCategoryTypeSubReportKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsRegistrationCheckInByCitySubReport(string? subReport) =>
        string.Equals(subReport, RegistrationCheckInByCitySubReportKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check-in population tabs (active reg types, one last visa/person): Project (P)/(V) and City.
    /// </summary>
    public static bool IsRegistrationCheckInPopulationSubReport(string? subReport) =>
        IsRegistrationCheckInByProjectSubReport(subReport)
        || IsRegistrationCheckInByPeriodCategoryTypeSubReport(subReport)
        || IsRegistrationCheckInByCitySubReport(subReport);

    public static bool IsRegistrationToBeCheckedInSubReport(string? subReport) =>
        string.Equals(subReport, RegistrationToBeCheckedInSubReportKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsRegistrationToBeCheckedOutSubReport(string? subReport) =>
        string.Equals(subReport, RegistrationToBeCheckedOutSubReportKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsRegistrationOnProcessSubReport(string? subReport) =>
        string.Equals(subReport, RegistrationOnProcessSubReportKey, StringComparison.OrdinalIgnoreCase);

    private static bool IsRegistrationPinnedSubReport(string? subReport) =>
        IsRegistrationCheckInByCitySubReport(subReport)
        || IsRegistrationCheckInByProjectSubReport(subReport)
        || IsRegistrationCheckInByPeriodCategoryTypeSubReport(subReport)
        || IsRegistrationExpiringStateSubReport(subReport)
        || IsRegistrationToBeCheckedInSubReport(subReport)
        || IsRegistrationToBeCheckedOutSubReport(subReport)
        || IsRegistrationOnProcessSubReport(subReport);

    /// <summary>
    /// Fixed Expiring State chart buckets (days + weeks + months). Always shown, including zero counts.
    /// </summary>
    public static readonly IReadOnlyList<(string Label, string CssClass)> RegistrationExpiringStateBuckets =
    [
        ("Expired", "st-expiring"),
        ("< 7 days", "st-expiring"),
        ("< 14 days", "st-expiring"),
        ("< 1 month", "st-pending"),
        ("< 3 months", "st-pending"),
        ("< 6 months", "st-approved"),
        ("≥ 6 months", "st-approved"),
    ];

    public static int RegistrationExpiringStateBucketSortKey(string? label) => label switch
    {
        "Expired" => 0,
        "< 7 days" => 1,
        "< 14 days" => 2,
        "< 1 month" => 3,
        "< 3 months" => 4,
        "< 6 months" => 5,
        "≥ 6 months" => 6,
        _ => 99
    };

    /// <summary>
    /// Fixed To Be Checked In buckets (days since latest ExternalArrival). Always shown, including zeros.
    /// </summary>
    public static readonly IReadOnlyList<(string Label, string CssClass)> RegistrationToBeCheckedInBuckets =
    [
        ("< 1 week", "st-expiring"),
        ("< 2 weeks", "st-expiring"),
        ("< 3 weeks", "st-pending"),
        ("< 4 weeks", "st-pending"),
        ("< 1 month", "st-pending"),
        ("≥ 1 month", "st-approved"),
    ];

    public static int RegistrationToBeCheckedInBucketSortKey(string? label) => label switch
    {
        "< 1 week" => 1,
        "< 2 weeks" => 2,
        "< 3 weeks" => 3,
        "< 4 weeks" => 4,
        "< 1 month" => 5,
        "≥ 1 month" => 6,
        _ => 99
    };

    /// <summary>
    /// Fixed To Be Checked Out buckets (days until visa expiry within 1 week, plus Expired). Always shown, including zeros.
    /// </summary>
    public static readonly IReadOnlyList<(string Label, string CssClass)> RegistrationToBeCheckedOutBuckets =
    [
        ("Expired", "st-expiring"),
        ("< 1 day", "st-expiring"),
        ("< 2 days", "st-expiring"),
        ("< 3 days", "st-expiring"),
        ("< 4 days", "st-pending"),
        ("< 5 days", "st-pending"),
        ("< 6 days", "st-pending"),
        ("< 7 days", "st-approved"),
    ];

    public static int RegistrationToBeCheckedOutBucketSortKey(string? label) => label switch
    {
        "Expired" => 0,
        "< 1 day" => 1,
        "< 2 days" => 2,
        "< 3 days" => 3,
        "< 4 days" => 4,
        "< 5 days" => 5,
        "< 6 days" => 6,
        "< 7 days" => 7,
        _ => 99
    };

    /// <summary>
    /// Sub-report tabs for UI. Registration pins Active Registered (C)/(P)/(V), Expiring State,
    /// To Be Checked In/Out, On process.
    /// </summary>
    public static IReadOnlyList<ReportDashboardSubReport> OrderedSubReports(
        ReportDashboardCategory category,
        IReadOnlyDictionary<string, int>? counts = null)
    {
        var list = SubReports(category);
        if (category != ReportDashboardCategory.Registration || counts == null || counts.Count == 0)
            return list;

        var pinned = list.Where(s => IsRegistrationPinnedSubReport(s.Key)).ToList();
        var rest = list
            .Where(s => !IsRegistrationPinnedSubReport(s.Key))
            .OrderByDescending(s => counts.TryGetValue(s.Key, out var n) ? n : 0)
            .ThenBy(s => s.Label, StringComparer.OrdinalIgnoreCase);

        return pinned.Concat(rest).ToList();
    }

    public static IReadOnlyList<ReportDashboardSubReport> SubReports(ReportDashboardCategory category) =>
        LocalizeSubReportList(category, RawSubReports(category));

    private static IReadOnlyList<ReportDashboardSubReport> LocalizeSubReportList(
        ReportDashboardCategory category,
        IReadOnlyList<ReportDashboardSubReport> source)
    {
        if (source.Count == 0) return source;
        var list = new List<ReportDashboardSubReport>(source.Count);
        foreach (var s in source)
        {
            list.Add(new ReportDashboardSubReport
            {
                Key = s.Key,
                Label = ReportDashboardLocalization.SubReport(category, s.Key, s.Label)
            });
        }
        return list;
    }

    private static IReadOnlyList<ReportDashboardSubReport> RawSubReports(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.Application => [
            new() { Key = ApplicationStatusSubReportKey, Label = "Application Status" },
        ],
        ReportDashboardCategory.VisaExtension => [
            new() { Key = "active-by-project", Label = "Active Visa (P)" },
            new() { Key = "by-period-category-type", Label = "Active Visa (V)" },
            new() { Key = "extension-required", Label = "Extension Required" },
            new() { Key = "on-extension", Label = "Visa On Extension (P)" },
            new() { Key = "on-extension-by-period-category-type", Label = "Visa On Extension (V)" },
            new() { Key = "by-days-remaining", Label = "Visa Validity" },
            new() { Key = "extension-result", Label = "Extension Result (P)" },
            new() { Key = "extension-result-by-period-category-type", Label = "Extension Result (V)" },
        ],
        ReportDashboardCategory.Invitation => [
            new() { Key = "ready-by-project", Label = "Active Invitation (P)" },
            new() { Key = "ready-by-period-category", Label = "Active Invitation (V)" },
            new() { Key = "in-process", Label = "Invitation Process (P)" },
            new() { Key = "in-process-by-period-category-type", Label = "Invitation Process (V)" },
            new() { Key = "process-result", Label = "Process Result (P)" },
            new() { Key = "process-result-by-period-category-type", Label = "Process Result (V)" },
            new() { Key = "used", Label = "Used (P)" },
            new() { Key = "used-by-period-category-type", Label = "Used (V)" },
            new() { Key = "valid-until", Label = "Invitation Validity" },
        ],
        ReportDashboardCategory.Registration =>
        [
            new() { Key = RegistrationCheckInByCitySubReportKey, Label = "Active Registered (C)" },
            new() { Key = RegistrationCheckInByProjectSubReportKey, Label = "Active Registered (P)" },
            new() { Key = RegistrationCheckInByPeriodCategoryTypeSubReportKey, Label = "Active Registered (V)" },
            new() { Key = RegistrationExpiringStateSubReportKey, Label = "Expiring State" },
            new() { Key = RegistrationToBeCheckedInSubReportKey, Label = "To Be Checked In" },
            new() { Key = RegistrationToBeCheckedOutSubReportKey, Label = "To Be Checked Out" },
            new() { Key = RegistrationOnProcessSubReportKey, Label = "On process" },
        ],
        ReportDashboardCategory.WorkPermit => [
            new() { Key = "active-by-project", Label = "Active WorkPermit (P)" },
            new() { Key = "on-extension", Label = "WorkPermit Extension (P)" },
            new() { Key = "extension-result", Label = "Extension Result (P)" },
            new() { Key = "by-days-remaining", Label = "WorkPermit Validity" },
            new() { Key = "by-status",         Label = "By Status"         },
        ],
        ReportDashboardCategory.Travel => [
            new() { Key = "by-month",  Label = "By Month"  },
            new() { Key = "by-status", Label = "By Status" },
        ],
        ReportDashboardCategory.AddressOfResidence => [
            new() { Key = "by-validity",      Label = "By Private House Validity" },
            new() { Key = "by-region",        Label = "By Region"     },
            new() { Key = "by-city",          Label = "By City"       },
            new() { Key = "by-address-type",  Label = "Address Type"  },
            new() { Key = "by-address",       Label = "By Address"    },
        ],
        ReportDashboardCategory.BorderZone => [
            new() { Key = "by-validity", Label = "By Validity" },
            new() { Key = "by-zone",     Label = "By Zone"     },
        ],
        ReportDashboardCategory.Passport => [
            new() { Key = "by-validity",    Label = "By Validity"    },
            new() { Key = "by-type",        Label = "By Type"        },
            new() { Key = "by-citizenship", Label = "By Citizenship" },
        ],
        ReportDashboardCategory.Education => [
            new() { Key = "by-level",     Label = "By Level"     },
            new() { Key = "by-country",   Label = "By Country"   },
            new() { Key = "by-specialty", Label = "By Speciality" },
        ],
        ReportDashboardCategory.PositionHistory => [
            new() { Key = "by-position",          Label = "Position (visa reports)" },
            new() { Key = "by-actual-position",   Label = "Position (actual / company)" },
        ],
        ReportDashboardCategory.Subcontractor => [
            new() { Key = "by-company", Label = "By Company" },
        ],
        ReportDashboardCategory.MedicalRecord => [
            new() { Key = "by-validity", Label = "By Validity" },
        ],
        _ => [new() { Key = "default", Label = "Overview" }]
    };

    public static string DefaultSubReport(ReportDashboardCategory category) =>
        SubReports(category).Count > 0 ? SubReports(category)[0].Key : "default";

    // ---- Labels ----------------------------------------------------------

    public static string CategoryLabel(ReportDashboardCategory category) =>
        ReportDashboardLocalization.Category(category);

    public static string PersonTypeLabel(ReportDashboardPersonType personType) =>
        ReportDashboardLocalization.PersonType(personType);

    /// <summary>True when the dashboard should not filter by a single <see cref="PersonRecordRole"/>.</summary>
    public static bool IsAllPersonTypes(ReportDashboardPersonType personType) =>
        personType == ReportDashboardPersonType.All;

    /// <summary>Single-role tabs only. Throws for <see cref="ReportDashboardPersonType.All"/>.</summary>
    public static PersonRecordRole ToPersonRole(ReportDashboardPersonType personType) => personType switch
    {
        ReportDashboardPersonType.Employees         => PersonRecordRole.Employee,
        ReportDashboardPersonType.FamilyMembers     => PersonRecordRole.FamilyMember,
        ReportDashboardPersonType.TemporaryVisitors => PersonRecordRole.TemporaryVisitor,
        _ => throw new ArgumentOutOfRangeException(nameof(personType), personType,
            "All person types has no single PersonRecordRole.")
    };

    public static PersonRecordRole? TryGetPersonRole(ReportDashboardPersonType personType) =>
        IsAllPersonTypes(personType) ? null : ToPersonRole(personType);

    public static string PersonRoleCriteria(ReportDashboardPersonType personType) => personType switch
    {
        ReportDashboardPersonType.All =>
            $"({PersonRoleHelper.EmployeeCriteria}) Or ({PersonRoleHelper.FamilyMemberCriteria}) Or ({PersonRoleHelper.TemporaryVisitorCriteria})",
        ReportDashboardPersonType.Employees         => PersonRoleHelper.EmployeeCriteria,
        ReportDashboardPersonType.FamilyMembers     => PersonRoleHelper.FamilyMemberCriteria,
        ReportDashboardPersonType.TemporaryVisitors => PersonRoleHelper.TemporaryVisitorCriteria,
        _ => PersonRoleHelper.TemporaryVisitorCriteria
    };

    // ---- Excel / ListView ------------------------------------------------

    /// <summary>
    /// Excel template name fragment for the category (and optional sub-report).
    /// Sub-report overrides fall back to the category default when unset.
    /// </summary>
    public static string? ExcelTemplateNameHint(
        ReportDashboardCategory category, string? subReport = null)
    {
        // Future: distinct templates per Visa/WorkPermit sub-report.
        _ = subReport;
        return category switch
        {
            ReportDashboardCategory.VisaExtension => "433_gurlusyk_uzt",
            ReportDashboardCategory.WorkPermit => "433-ek_uzt",
            _ => null
        };
    }

    /// <summary>
    /// Extension Required — <see cref="VwRdVisaExtensionRequired"/>.
    /// </summary>
    public static bool UsesVisaExtensionRequiredListView(string? subReport) =>
        subReport is "extension-required"
            or "extension-required-by-period-category-type";

    /// <summary>
    /// Visa Validity — <see cref="VwRdVisaByDaysRemaining"/>.
    /// </summary>
    public static bool UsesVisaByDaysRemainingListView(string? subReport) =>
        subReport is "by-days-remaining";

    /// <summary>
    /// Active Visa (P) — <see cref="VwRdVisaActiveByProject"/>.
    /// </summary>
    public static bool UsesVisaActiveByProjectListView(string? subReport) =>
        subReport is "active-by-project";

    /// <summary>
    /// Active Visa (V) (+ legacy by-category/by-type/by-period) —
    /// <see cref="VwRdVisaActiveByPeriodCategoryType"/>.
    /// </summary>
    public static bool UsesVisaActiveByPeriodCategoryTypeListView(string? subReport) =>
        subReport is "by-period-category-type"
            or "by-category" or "by-type" or "by-period";

    /// <summary>
    /// Either Active Visa dedicated ListView (P or V).
    /// </summary>
    public static bool UsesVisaActiveListView(string? subReport) =>
        UsesVisaActiveByProjectListView(subReport)
        || UsesVisaActiveByPeriodCategoryTypeListView(subReport);

    /// <summary>
    /// On Extension / Extension Result dedicated views (population baked into SQL wrappers).
    /// </summary>
    public static bool UsesVisaAppProgressDedicatedListView(string? subReport) =>
        subReport is "on-extension"
            or "on-extension-by-period-category-type"
            or "extension-result"
            or "extension-result-by-period-category-type"
            or "app-progress";

    /// <summary>Resolve ListView id + type for the active category / sub-report.</summary>
    public static (string ListViewId, Type ListViewType) ResolveListViewTarget(
        ReportDashboardCategory category, string? subReport = null)
    {
        if (category == ReportDashboardCategory.VisaExtension)
        {
            if (UsesVisaActiveByProjectListView(subReport))
                return ("VwRdVisaActiveByProject_ListView", typeof(VwRdVisaActiveByProject));
            if (UsesVisaActiveByPeriodCategoryTypeListView(subReport))
                return ("VwRdVisaActiveByPeriodCategoryType_ListView", typeof(VwRdVisaActiveByPeriodCategoryType));
            if (UsesVisaExtensionRequiredListView(subReport))
                return ("VwRdVisaExtensionRequired_ListView", typeof(VwRdVisaExtensionRequired));
            if (UsesVisaByDaysRemainingListView(subReport))
                return ("VwRdVisaByDaysRemaining_ListView", typeof(VwRdVisaByDaysRemaining));
            return subReport switch
            {
                "on-extension" or "app-progress" =>
                    ("VwRdVisaOnExtension_ListView", typeof(VwRdVisaOnExtension)),
                "on-extension-by-period-category-type" =>
                    ("VwRdVisaOnExtensionByPeriodCategoryType_ListView",
                        typeof(VwRdVisaOnExtensionByPeriodCategoryType)),
                "extension-result" =>
                    ("VwRdVisaExtensionResult_ListView", typeof(VwRdVisaExtensionResult)),
                "extension-result-by-period-category-type" =>
                    ("VwRdVisaExtensionResultByPeriodCategoryType_ListView",
                        typeof(VwRdVisaExtensionResultByPeriodCategoryType)),
                _ => (ListViewId(category), ListViewType(category))
            };
        }

        return (ListViewId(category), ListViewType(category));
    }

    public static string ListViewId(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.Application   => "Application_ListView",
        ReportDashboardCategory.VisaExtension => "VisaExtensionStatus_ListView",
        ReportDashboardCategory.Invitation    => "InvitationItem_ListView",
        ReportDashboardCategory.Registration  => "ApplicationItem_ListView",
        ReportDashboardCategory.WorkPermit    => "WorkPermitItem_ListView",
        ReportDashboardCategory.Travel        => "ApplicationItem_ListView",
        ReportDashboardCategory.AddressOfResidence => "AddressOfResidence_ListView",
        ReportDashboardCategory.BorderZone       => "BorderZoneItem_ListView",
        ReportDashboardCategory.Passport         => "Passport_ListView",
        ReportDashboardCategory.Education        => "Education_ListView",
        ReportDashboardCategory.PositionHistory  => "EmployeePositionHistory_ListView",
        ReportDashboardCategory.Subcontractor    => "Person_ListView",
        ReportDashboardCategory.MedicalRecord   => "MedicalRecord_ListView",
        _ => "Person_ListView"
    };

    public static Type ListViewType(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.Application      => typeof(Application),
        ReportDashboardCategory.VisaExtension    => typeof(VisaExtensionStatus),
        ReportDashboardCategory.Invitation       => typeof(InvitationItem),
        ReportDashboardCategory.Registration     => typeof(ApplicationItem),
        ReportDashboardCategory.WorkPermit       => typeof(WorkPermitItem),
        ReportDashboardCategory.Travel           => typeof(ApplicationItem),
        ReportDashboardCategory.AddressOfResidence => typeof(AddressOfResidence),
        ReportDashboardCategory.BorderZone       => typeof(BorderZoneItem),
        ReportDashboardCategory.Passport         => typeof(Passport),
        ReportDashboardCategory.Education        => typeof(Education),
        ReportDashboardCategory.PositionHistory  => typeof(EmployeePositionHistory),
        ReportDashboardCategory.Subcontractor    => typeof(Person),
        ReportDashboardCategory.MedicalRecord   => typeof(MedicalRecord),
        _ => typeof(Person)
    };

    // ---- Table headers (sub-report-aware) --------------------------------

    public static string[] TableHeaders(ReportDashboardCategory category, string? subReport = null) =>
        ReportDashboardLocalization.Headers(EnglishTableHeaders(category, subReport));

    private static string[] EnglishTableHeaders(ReportDashboardCategory category, string? subReport = null) =>
        (category, subReport) switch
        {
            (ReportDashboardCategory.Application, _) => ["Name", "Project", "App #", "App Date", "State"],
            // Categorical: last column = grouping dimension; ColumnA = passport # or identifier
            (ReportDashboardCategory.Passport, "by-type")         => ["Name", "Project", "Passport #",  "Expiry", "Type"],
            (ReportDashboardCategory.Passport, "by-citizenship")   => ["Name", "Project", "Passport #",  "Expiry", "Citizenship"],
            (ReportDashboardCategory.Registration, "check-in-by-project") =>
                ["Name", "Project", "Visa #", "Expiry", "Project"],
            (ReportDashboardCategory.Registration, "check-in-by-period-category-type") =>
                ["Name", "Project", "Visa #", "Expiry", "Period · Category · Type"],
            (ReportDashboardCategory.Registration, "check-in-by-city") => ["Name", "Project", "Visa #", "Expiry", "City"],
            (ReportDashboardCategory.Registration, "expiring-state") => ["Name", "Project", "Visa #", "Expiry", "Days Remaining"],
            (ReportDashboardCategory.Registration, "to-be-checked-in") => ["Name", "Project", "Visa #", "Entry", "Days Since Entry"],
            (ReportDashboardCategory.Registration, "to-be-checked-out") => ["Name", "Project", "Visa #", "Expiry", "Days Remaining"],
            (ReportDashboardCategory.Registration, "on-process") =>
                ["Name", "Project", "App #", "App Date", "Application Type · Process State"],
            (ReportDashboardCategory.Registration, _) => ["Name", "Project", "Visa #", "Expiry", "Process State"],
            (ReportDashboardCategory.BorderZone, "by-zone")        => ["Name", "Project", "BZ Number",   "Valid Until", "Zone"],
            (ReportDashboardCategory.VisaExtension, "active-by-project") =>
                ["Name", "Project", "Passport #", "Visa #", "Expiry", "Days Remaining", "Project"],
            (ReportDashboardCategory.VisaExtension, "by-period-category-type") =>
                ["Name", "Project", "Passport #", "Visa #", "Expiry", "Days Remaining", "Period · Category · Type"],
            (ReportDashboardCategory.VisaExtension, "extension-required") =>
                ["Name", "Project", "Passport #", "Visa #", "Expiry", "Days Remaining", "Status"],
            (ReportDashboardCategory.VisaExtension, "on-extension") =>
                ["Name", "Project", "Passport #", "App #", "App Date", "Days Remaining", "Project · State"],
            (ReportDashboardCategory.VisaExtension, "on-extension-by-period-category-type") =>
                ["Name", "Project", "Passport #", "App #", "App Date", "Days Remaining", "Period · Category · Type · State"],
            (ReportDashboardCategory.VisaExtension, "by-days-remaining") =>
                ["Name", "Project", "Passport #", "Visa #", "Expiry", "Days Remaining", "Status"],
            (ReportDashboardCategory.VisaExtension, "extension-result") =>
                ["Name", "Project", "Passport #", "App #", "App Date", "Project · State"],
            (ReportDashboardCategory.VisaExtension, "extension-result-by-period-category-type") =>
                ["Name", "Project", "Passport #", "App #", "App Date", "Period · Category · Type · State"],
            (ReportDashboardCategory.Travel, "by-month")           => ["Name", "Project", "App #",       "Travel Date",     "Month"],
            (ReportDashboardCategory.AddressOfResidence, "by-validity")     => ["Name", "Project", "Address", "Expiry", "Validity"],
            (ReportDashboardCategory.AddressOfResidence, "by-region")       => ["Name", "Project", "Address", "Expiry", "Region"],
            (ReportDashboardCategory.AddressOfResidence, "by-city")         => ["Name", "Project", "Address", "Expiry", "City"],
            (ReportDashboardCategory.AddressOfResidence, "by-address-type") => ["Name", "Project", "Address", "Expiry", "Address Type"],
            (ReportDashboardCategory.AddressOfResidence, "by-address")      => ["Name", "Project", "Region · City", "Expiry", "Address"],
            (ReportDashboardCategory.WorkPermit, "active-by-project")  => ["Name", "Project", "WP Number",   "Expiry", "Project"],
            (ReportDashboardCategory.WorkPermit, "on-extension") => ["Name", "Project", "App #", "App Date", "Project · State"],
            (ReportDashboardCategory.WorkPermit, "extension-result") => ["Name", "Project", "App #", "App Date", "Project · State"],
            (ReportDashboardCategory.WorkPermit, "by-status")         => ["Name", "Project", "WP Number",   "Expiry", "Status"],
            (ReportDashboardCategory.WorkPermit, "by-days-remaining") => ["Name", "Project", "WP Number",   "Expiry", "Days Remaining"],
            (ReportDashboardCategory.Invitation, "ready-by-project") => ["Name", "Project", "Invitation #", "Expiry", "Project"],
            (ReportDashboardCategory.Invitation, "ready-by-period-category") => ["Name", "Project", "Invitation #", "Expiry", "Period · Category · Type"],
            (ReportDashboardCategory.Invitation, "in-process") => ["Name", "Project", "App #", "App Date", "Project · State"],
            (ReportDashboardCategory.Invitation, "in-process-by-period-category-type") =>
                ["Name", "Project", "App #", "App Date", "Period · Category · Type · State"],
            (ReportDashboardCategory.Invitation, "process-result") => ["Name", "Project", "App #", "App Date", "Project · State"],
            (ReportDashboardCategory.Invitation, "process-result-by-period-category-type") =>
                ["Name", "Project", "App #", "App Date", "Period · Category · Type · State"],
            // Legacy keys (remapped in LoadInvitation)
            (ReportDashboardCategory.Invitation, "rejected-by-project") => ["Name", "Project", "App #", "App Date", "Project · State"],
            (ReportDashboardCategory.Invitation, "rejected-by-period-category-type") =>
                ["Name", "Project", "App #", "App Date", "Period · Category · Type · State"],
            (ReportDashboardCategory.Invitation, "used") => ["Name", "Project", "Invitation #", "Issued", "Project"],
            (ReportDashboardCategory.Invitation, "used-by-period-category-type") =>
                ["Name", "Project", "Invitation #", "Issued", "Period · Category · Type"],
            (ReportDashboardCategory.Invitation, "valid-until") => ["Name", "Project", "Invitation #", "Expiry", "Valid Until"],
            (ReportDashboardCategory.Invitation, "expired") => ["Name", "Project", "Invitation #", "Expiry", "Valid Until"],
            (ReportDashboardCategory.Invitation, "issued-inv") => ["Name", "Project", "Invitation #", "Expiry", "Project"],
            (ReportDashboardCategory.Education, "by-level")     => ["Name", "Project", "Institution", "Grad Year", "Level"],
            (ReportDashboardCategory.Education, "by-country")   => ["Name", "Project", "Institution", "Grad Year", "Country"],
            (ReportDashboardCategory.Education, "by-specialty") => ["Name", "Project", "Institution", "Grad Year", "Speciality"],
            (ReportDashboardCategory.PositionHistory, "by-position")        => ["Name", "Project", "Visa Position", "Start", "Visa Position"],
            (ReportDashboardCategory.PositionHistory, "by-actual-position") => ["Name", "Project", "Visa Position", "Start", "Actual Position"],
            (ReportDashboardCategory.Subcontractor, "by-company")    => ["Name", "Project", "Role", "Hire Date", "Company"],
            (ReportDashboardCategory.MedicalRecord, "by-validity")   => ["Name", "Project", "Document #", "Expiry", "Validity"],
            _ => DefaultTableHeaders(category)
        };

    private static string[] DefaultTableHeaders(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.Application      => ["Name", "Project", "App #",           "App Date",        "State"],
        ReportDashboardCategory.VisaExtension    => ["Name", "Project", "Current Expiry",  "Requested Until", "Status"],
        ReportDashboardCategory.Invitation       => ["Name", "Project", "Invitation #",    "Issue Date",      "Status"],
        ReportDashboardCategory.Registration     => ["Name", "Project", "Visa #",          "Expiry",          "Process State"],
        ReportDashboardCategory.WorkPermit       => ["Name", "Project", "WP Number",       "Expiry",          "Status"],
        ReportDashboardCategory.Travel           => ["Name", "Project", "App #",           "Travel Date",     "Status"],
        ReportDashboardCategory.AddressOfResidence => ["Name", "Project", "Address",         "Expiry",          "Validity"],
        ReportDashboardCategory.BorderZone       => ["Name", "Project", "BZ Number",       "Valid Until",     "Status"],
        ReportDashboardCategory.Passport         => ["Name", "Project", "Passport #",      "Expiry",          "Validity"],
        ReportDashboardCategory.Education        => ["Name", "Project", "Institution",     "Grad Year",       "Level"],
        ReportDashboardCategory.PositionHistory  => ["Name", "Project", "Position",        "Start",           "Status"],
        ReportDashboardCategory.Subcontractor    => ["Name", "Project", "Role",            "Hire Date",       "Company"],
        ReportDashboardCategory.MedicalRecord   => ["Name", "Project", "Document #",      "Expiry",          "Validity"],
        _ => ["Name", "Project", "Info", "Date", "Status"]
    };

    // ---- Criteria builder ------------------------------------------------

    public static string BuildListCriteria(
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string? projectKey,
        string? statusLabel,
        bool includeArchivedPersons = false,
        string? subReport = null,
        bool oneLastValidVisaPerPerson = false)
    {
        var usesVisaActive = category == ReportDashboardCategory.VisaExtension
            && UsesVisaActiveListView(subReport);
        var usesExtRequired = category == ReportDashboardCategory.VisaExtension
            && UsesVisaExtensionRequiredListView(subReport);
        var usesByDays = category == ReportDashboardCategory.VisaExtension
            && UsesVisaByDaysRemainingListView(subReport);
        var usesAppProgressDedicated = category == ReportDashboardCategory.VisaExtension
            && UsesVisaAppProgressDedicatedListView(subReport);
        var usesRdVisaRow = usesVisaActive || usesExtRequired || usesByDays;

        string roleCriteria;
        if (usesAppProgressDedicated)
        {
            // Population (on-extension vs extension-result) is baked into dedicated SQL views.
            roleCriteria = IsAllPersonTypes(personType)
                ? "[IsArchived] = False"
                : $"[IsArchived] = False And [PersonRoleCode] = {(int)ToPersonRole(personType)}";
        }
        else if (usesRdVisaRow)
        {
            roleCriteria = IsAllPersonTypes(personType)
                ? "[IsArchived] = False"
                : $"[IsArchived] = False And [PersonRoleCode] = {(int)ToPersonRole(personType)}";
            if ((usesVisaActive || usesByDays) && oneLastValidVisaPerPerson)
                roleCriteria = $"({roleCriteria}) And [IsOneLastValidPerPerson] = True";
        }
        else if (IsAllPersonTypes(personType))
        {
            roleCriteria = category is ReportDashboardCategory.VisaExtension
                or ReportDashboardCategory.Passport
                or ReportDashboardCategory.Registration
                or ReportDashboardCategory.Invitation
                or ReportDashboardCategory.WorkPermit
                or ReportDashboardCategory.BorderZone
                or ReportDashboardCategory.Travel
                or ReportDashboardCategory.AddressOfResidence
                or ReportDashboardCategory.Education
                or ReportDashboardCategory.PositionHistory
                or ReportDashboardCategory.MedicalRecord
                ? "Person is not null"
                : category is ReportDashboardCategory.Application
                ? "True"
                : PersonRoleCriteria(personType);
        }
        else
        {
            roleCriteria = category == ReportDashboardCategory.VisaExtension
                || category == ReportDashboardCategory.Passport
                || category == ReportDashboardCategory.Registration
                || category == ReportDashboardCategory.Education
                || category == ReportDashboardCategory.PositionHistory
                || category == ReportDashboardCategory.AddressOfResidence
                || category == ReportDashboardCategory.MedicalRecord
                    ? $"Person is not null And [{PersonRolePath(category)}] = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,{ToPersonRole(personType)}#"
                    : category == ReportDashboardCategory.Invitation
                        || category == ReportDashboardCategory.WorkPermit
                        || category == ReportDashboardCategory.BorderZone
                        || category == ReportDashboardCategory.Travel
                        ? $"Person is not null And [Person.PersonRole] = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,{ToPersonRole(personType)}#"
                        : category == ReportDashboardCategory.Application
                        ? "True"
                        : PersonRoleCriteria(personType);
        }

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            var projectCriteria = usesAppProgressDedicated || usesRdVisaRow
                ? $"[ProjectName] = '{Escape(projectKey)}' Or [ProjectNameTm] = '{Escape(projectKey)}' Or [ProjectNameRaw] = '{Escape(projectKey)}'"
                : category switch
                {
                    ReportDashboardCategory.Application =>
                        $"[ProjectContract.Name] = '{Escape(projectKey)}' Or [ProjectContract.NameTm] = '{Escape(projectKey)}'",
                    ReportDashboardCategory.Subcontractor =>
                        $"[ProjectContract.Name] = '{Escape(projectKey)}' Or [ProjectContract.NameTm] = '{Escape(projectKey)}'",
                    ReportDashboardCategory.VisaExtension =>
                        $"[Application.ProjectContract.Name] = '{Escape(projectKey)}' Or [Application.ProjectContract.NameTm] = '{Escape(projectKey)}'",
                    ReportDashboardCategory.Invitation =>
                        $"[Invitation.Application.ProjectContract.Name] = '{Escape(projectKey)}' Or [Invitation.Application.ProjectContract.NameTm] = '{Escape(projectKey)}'",
                    ReportDashboardCategory.WorkPermit =>
                        $"[WorkPermit.Application.ProjectContract.Name] = '{Escape(projectKey)}' Or [WorkPermit.Application.ProjectContract.NameTm] = '{Escape(projectKey)}'",
                    ReportDashboardCategory.BorderZone =>
                        $"[BorderZone.Application.ProjectContract.Name] = '{Escape(projectKey)}' Or [BorderZone.Application.ProjectContract.NameTm] = '{Escape(projectKey)}'",
                    ReportDashboardCategory.Travel =>
                        $"[Application.ProjectContract.Name] = '{Escape(projectKey)}' Or [Application.ProjectContract.NameTm] = '{Escape(projectKey)}'",
                    ReportDashboardCategory.Registration
                        or ReportDashboardCategory.Passport
                        or ReportDashboardCategory.Education
                        or ReportDashboardCategory.PositionHistory
                        or ReportDashboardCategory.AddressOfResidence
                        or ReportDashboardCategory.MedicalRecord =>
                        $"[Person.ProjectContract.Name] = '{Escape(projectKey)}' Or [Person.ProjectContract.NameTm] = '{Escape(projectKey)}'",
                    _ => "True"
                };
            roleCriteria = $"({roleCriteria}) And ({projectCriteria})";
        }

        if (!string.IsNullOrWhiteSpace(statusLabel))
        {
            if (usesAppProgressDedicated || usesVisaActive)
            {
                // Dedicated Active / OnExtension / ExtensionResult views expose StatusLabel.
                roleCriteria = $"({roleCriteria}) And [StatusLabel] = '{Escape(statusLabel)}'";
            }
            else if (usesExtRequired)
            {
                var mil = BuildMilestoneExpirationCriteria(statusLabel);
                if (!string.IsNullOrEmpty(mil))
                    roleCriteria = $"({roleCriteria}) And ({mil})";
            }
            else if (usesByDays)
            {
                roleCriteria = $"({roleCriteria}) And [StatusLabel] = '{Escape(statusLabel)}'";
            }
            else if (category == ReportDashboardCategory.VisaExtension)
            {
                var extStatus = BuildVisaExtensionStatusCriteria(subReport, statusLabel);
                if (!string.IsNullOrEmpty(extStatus))
                    roleCriteria = $"({roleCriteria}) And ({extStatus})";
            }
            else if (category == ReportDashboardCategory.Subcontractor)
            {
                var companyCriteria = string.Equals(statusLabel, "Unassigned", StringComparison.OrdinalIgnoreCase)
                    ? "[Subcontractor] is null"
                    : $"[Subcontractor.NameTm] = '{Escape(statusLabel)}' Or [Subcontractor.Name] = '{Escape(statusLabel)}'";
                roleCriteria = $"({roleCriteria}) And ({companyCriteria})";
            }
        }

        if (!includeArchivedPersons && SupportsIncludeArchivedPersons(category))
        {
            var archivedCriteria = category switch
            {
                ReportDashboardCategory.WorkPermit =>
                    "[Person.IsArchived] = False",
                ReportDashboardCategory.Education =>
                    "[Person.IsArchived] = False",
                ReportDashboardCategory.AddressOfResidence =>
                    "[Person.IsArchived] = False",
                ReportDashboardCategory.Subcontractor =>
                    "[IsArchived] = False",
                ReportDashboardCategory.MedicalRecord =>
                    "[Person.IsArchived] = False",
                _ => "True"
            };
            roleCriteria = $"({roleCriteria}) And ({archivedCriteria})";
        }

        // Passport active sub-reports exclude archived persons.
        if (category == ReportDashboardCategory.Passport && !includeArchivedPersons)
            roleCriteria = $"({roleCriteria}) And ([Person.IsArchived] = False)";

        return roleCriteria;
    }

    /// <summary>
    /// Population filter for legacy shared <see cref="VwRdVisaAppProgress"/> (base view).
    /// Dedicated On Extension / Extension Result wrappers bake population into SQL — do not use for those BOs.
    /// </summary>
    public static string BuildVisaAppProgressPopulationCriteria(string? subReport)
    {
        var issued = ApplicationProgressStateCodes.ProcessIssued;
        var cancelled = ApplicationProgressStateCodes.ProcessCancelled;
        var rejected = ApplicationProgressStateCodes.ProcessRejected;
        var terminal =
            $"([ProgressStateCode] In ('{issued}', '{cancelled}', '{rejected}') Or EndsWith([ProgressStateCode], '_REVIEW_REJECTED'))";
        if (subReport is "extension-result" or "extension-result-by-period-category-type")
            return terminal;

        // Visa On Extension: in-flight only (null / empty allowed); exclude terminal outcomes.
        return $"([ProgressStateCode] Is Null Or [ProgressStateCode] = '' Or Not ({terminal}))";
    }


    /// <summary>Status filter when Open ListView targets legacy <see cref="VwRdVisaAppProgress"/>.</summary>
    private static string? BuildVisaAppProgressStatusCriteria(string? subReport, string statusLabel)
    {
        var parts = statusLabel.Split(" · ", StringSplitOptions.None);
        if (parts.Length >= 2)
        {
            var state = Escape(parts[^1].Trim());
            var stateCrit =
                $"[CurrentState.Name] = '{state}' Or [ProgressStateLabel] = '{state}' Or StartsWith([ProgressStateLabel], '{state}')";
            if (subReport is "on-extension" or "extension-result")
            {
                var projectPart = parts[0].Trim();
                if (string.Equals(projectPart, "(No project)", StringComparison.OrdinalIgnoreCase))
                    return $"([ProjectName] = '' Or [ProjectName] Is Null) And ({stateCrit})";
                var project = Escape(projectPart);
                return $"([ProjectName] = '{project}' Or [ProjectNameTm] = '{project}') And ({stateCrit})";
            }

            // V variants: Period · Category · Type · State — match process state (last segment).
            return stateCrit;
        }

        var s = Escape(statusLabel);
        return $"[CurrentState.Name] = '{s}' Or [ProgressStateLabel] = '{s}'";
    }

    /// <summary>Legacy Status filter for shared <see cref="VwRdVisaByPeriod"/> (prefer StatusLabel on dedicated Active views).</summary>
    private static string? BuildVisaByPeriodStatusCriteria(string? subReport, string statusLabel)
    {
        if (subReport is "active-by-project")
        {
            if (string.Equals(statusLabel, "(No project)", StringComparison.OrdinalIgnoreCase))
                return "[ProjectName] = '' Or [ProjectName] Is Null";
            return $"[ProjectName] = '{Escape(statusLabel)}' Or [ProjectNameTm] = '{Escape(statusLabel)}'";
        }

        // Active Visa (V): Status = Period · Category · Type — filter by period label (first segment).
        var parts = statusLabel.Split(" · ", StringSplitOptions.None);
        if (parts.Length >= 1)
        {
            var period = Escape(parts[0].Trim());
            return $"[PeriodLabel] = '{period}'";
        }

        return null;
    }

    /// <summary>Status filter when Open ListView targets <see cref="VisaExtensionStatus"/> (nav fallback).</summary>
    private static string? BuildVisaExtensionStatusCriteria(string? subReport, string statusLabel)
    {
        var parts = statusLabel.Split(" · ", StringSplitOptions.None);
        if (parts.Length >= 2)
        {
            var state = Escape(parts[^1].Trim());
            if (subReport is "on-extension" or "extension-result")
            {
                var project = Escape(parts[0].Trim());
                return $"([Application.ProjectContract.Name] = '{project}' Or [Application.ProjectContract.NameTm] = '{project}') And [CurrentState.Name] = '{state}'";
            }

            // V variants: Period · Category · Type · State — match process state (last segment).
            return $"[CurrentState.Name] = '{state}'";
        }

        return $"[CurrentState.Name] = '{Escape(statusLabel)}'";
    }

    /// <summary>
    /// Approximate ExpirationDate window for Extension Required milestone labels ("0 days", "7 days", …).
    /// </summary>
    private static string? BuildMilestoneExpirationCriteria(string statusLabel)
    {
        if (!TryParseDaysLabel(statusLabel, out var milestone))
            return null;

        // Snap bounds mirror ReportDashboardQueryService.SnapDaysRemainingToMilestone.
        int[] milestones = [0, 7, 14, 30, 60, 90, 180, 365];
        if (milestone == 0)
        {
            return "[ExpirationDate] >= LocalDateTimeToday() And [ExpirationDate] < AddDays(LocalDateTimeToday(), 1)";
        }

        var idx = Array.IndexOf(milestones, milestone);
        if (idx < 1)
            return null;

        var lo = idx == 1
            ? 1
            : (milestones[idx - 1] + milestone) / 2 + 1;
        var hiExclusive = idx == milestones.Length - 1
            ? 10000
            : (milestone + milestones[idx + 1]) / 2 + 1;

        return $"[ExpirationDate] >= AddDays(LocalDateTimeToday(), {lo}) And [ExpirationDate] < AddDays(LocalDateTimeToday(), {hiExclusive})";
    }

    private static bool TryParseDaysLabel(string label, out int days)
    {
        days = 0;
        if (string.IsNullOrWhiteSpace(label))
            return false;
        var s = label.Trim();
        if (s.EndsWith(" days", StringComparison.OrdinalIgnoreCase))
            s = s[..^5].Trim();
        else if (s.EndsWith(" day", StringComparison.OrdinalIgnoreCase))
            s = s[..^4].Trim();
        return int.TryParse(s, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out days);
    }

    private static string PersonRolePath(ReportDashboardCategory category) => "Person.PersonRole";

    private static string Escape(string value) => value.Replace("'", "''");
}
