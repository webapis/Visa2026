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
        category is ReportDashboardCategory.Registration
            or ReportDashboardCategory.WorkPermit
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
    /// Applies to by-category / by-type / by-period / by-days-remaining.
    /// </summary>
    public static bool SupportsOneLastValidVisaPerPerson(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.VisaExtension;

    /// <summary>
    /// Work Permit: toggle to count one last valid work permit per person (latest expiry).
    /// Applies to by-days-remaining only.
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
        subReport is "by-category" or "by-type" or "by-period" or "by-days-remaining";

    /// <summary>
    /// Sub-reports that emit one row per valid work permit (persons may appear more than once).
    /// </summary>
    public static bool SubReportCountsValidWorkPermits(string subReport) =>
        subReport is "by-days-remaining";

    // ---- Sub-reports per category ----------------------------------------

    public const string RegistrationExpiringStateSubReportKey = "expiring-state";
    public const string RegistrationCheckInByCitySubReportKey = "check-in-by-city";
    public const string RegistrationToBeCheckedInSubReportKey = "to-be-checked-in";
    public const string RegistrationToBeCheckedOutSubReportKey = "to-be-checked-out";

    /// <summary>
    /// Last registration app types that count for Expiring State (not Check-Out).
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
    /// Registration category tabs = registration ApplicationType.Name keys with short labels.
    /// </summary>
    public static readonly IReadOnlyList<ReportDashboardSubReport> RegistrationApplicationTypeSubReports =
    [
        new() { Key = "App_Reg_Check_In",             Label = "Check-In" },
        new() { Key = "App_Reg_Check_In_Internal",    Label = "Check-In (Internal)" },
        new() { Key = "App_Reg_Check_Out",            Label = "Check-Out" },
        new() { Key = "App_Reg_Check_Out_Internal",   Label = "Check-Out (Internal)" },
        new() { Key = "App_Reg_ext",                  Label = "Extension" },
        new() { Key = "App_Reg_Info_Change_Address",  Label = "Address Change" },
        new() { Key = "App_Reg_Info_Change_Passport", Label = "Passport Change" },
        new() { Key = "App_Reg_Info_Change_Visa",     Label = "Visa Change" },
    ];

    public static bool IsRegistrationApplicationTypeSubReport(string? subReport) =>
        !string.IsNullOrWhiteSpace(subReport)
        && RegistrationApplicationTypeSubReports.Any(s =>
            string.Equals(s.Key, subReport, StringComparison.OrdinalIgnoreCase));

    public static bool IsRegistrationExpiringStateSubReport(string? subReport) =>
        string.Equals(subReport, RegistrationExpiringStateSubReportKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsRegistrationCheckInByCitySubReport(string? subReport) =>
        string.Equals(subReport, RegistrationCheckInByCitySubReportKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsRegistrationToBeCheckedInSubReport(string? subReport) =>
        string.Equals(subReport, RegistrationToBeCheckedInSubReportKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsRegistrationToBeCheckedOutSubReport(string? subReport) =>
        string.Equals(subReport, RegistrationToBeCheckedOutSubReportKey, StringComparison.OrdinalIgnoreCase);

    private static bool IsRegistrationPinnedSubReport(string? subReport) =>
        IsRegistrationCheckInByCitySubReport(subReport)
        || IsRegistrationExpiringStateSubReport(subReport)
        || IsRegistrationToBeCheckedInSubReport(subReport)
        || IsRegistrationToBeCheckedOutSubReport(subReport);

    /// <summary>
    /// Fixed Expiring State chart buckets (days + weeks + months). Always shown, including zero counts.
    /// </summary>
    public static readonly IReadOnlyList<(string Label, string CssClass)> RegistrationExpiringStateBuckets =
    [
        ("< 7 days", "st-expiring"),
        ("< 14 days", "st-expiring"),
        ("< 1 month", "st-pending"),
        ("< 3 months", "st-pending"),
        ("< 6 months", "st-approved"),
        ("≥ 6 months", "st-approved"),
    ];

    public static int RegistrationExpiringStateBucketSortKey(string? label) => label switch
    {
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
    /// Fixed To Be Checked Out buckets (days until visa expiry within 1 week). Always shown, including zeros.
    /// </summary>
    public static readonly IReadOnlyList<(string Label, string CssClass)> RegistrationToBeCheckedOutBuckets =
    [
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
    /// Sub-report tabs for UI. Registration pins Expiring State, To Be Checked In, To Be Checked Out; other tabs by count desc.
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
            new() { Key = "visa-state",   Label = "Visa State"         },
            new() { Key = "by-category",  Label = "By Visa Category"   },
            new() { Key = "by-type",      Label = "By Visa Type"       },
            new() { Key = "by-period",         Label = "By Visa Period"     },
            new() { Key = "by-days-remaining", Label = "By Days Remaining"  },
        ],
        ReportDashboardCategory.Invitation => [
            new() { Key = "ready-by-project",     Label = "Ready By Project" },
            new() { Key = "ready-by-period-category", Label = "Ready By Period · Category · Type" },
            new() { Key = "in-process",           Label = "Invitations In Process" },
            new() { Key = "rejected-by-project",  Label = "Invitations Rejected" },
            new() { Key = "used",                 Label = "Used Invitations" },
            new() { Key = "valid-until",          Label = "Invitation Valid Until" },
        ],
        ReportDashboardCategory.Registration =>
        [
            new() { Key = RegistrationCheckInByCitySubReportKey, Label = "Check in by City" },
            new() { Key = RegistrationExpiringStateSubReportKey, Label = "Expiring State" },
            new() { Key = RegistrationToBeCheckedInSubReportKey, Label = "To Be Checked In" },
            new() { Key = RegistrationToBeCheckedOutSubReportKey, Label = "To Be Checked Out" },
            ..RegistrationApplicationTypeSubReports,
        ],
        ReportDashboardCategory.WorkPermit => [
            new() { Key = "by-days-remaining", Label = "By Days Remaining" },
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

    public static string? ExcelTemplateNameHint(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.VisaExtension => "433_gurlusyk_uzt",
        ReportDashboardCategory.WorkPermit    => "433-ek_uzt",
        _ => null
    };

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
            (ReportDashboardCategory.Registration, "check-in-by-city") => ["Name", "Project", "Visa #", "Expiry", "City"],
            (ReportDashboardCategory.Registration, "expiring-state") => ["Name", "Project", "Visa #", "Expiry", "Days Remaining"],
            (ReportDashboardCategory.Registration, "to-be-checked-in") => ["Name", "Project", "Visa #", "Entry", "Days Since Entry"],
            (ReportDashboardCategory.Registration, "to-be-checked-out") => ["Name", "Project", "Visa #", "Expiry", "Days Remaining"],
            (ReportDashboardCategory.Registration, _) => ["Name", "Project", "Visa #", "Expiry", "Process State"],
            (ReportDashboardCategory.BorderZone, "by-zone")        => ["Name", "Project", "BZ Number",   "Valid Until", "Zone"],
            (ReportDashboardCategory.VisaExtension, "visa-state")   => ["Name", "Project", "Visa #",   "Expiry",      "Visa State"     ],
            (ReportDashboardCategory.VisaExtension, "by-category")  => ["Name", "Project", "Visa #",   "Expiry",      "Visa Category" ],
            (ReportDashboardCategory.VisaExtension, "by-type")      => ["Name", "Project", "Visa #",   "Expiry",      "Visa Type"     ],
            (ReportDashboardCategory.VisaExtension, "by-period")         => ["Name", "Project", "Visa #",   "Expiry",      "Period" ],
            (ReportDashboardCategory.VisaExtension, "by-days-remaining") => ["Name", "Project", "Visa #",   "Expiry",      "Days Remaining" ],
            (ReportDashboardCategory.Travel, "by-month")           => ["Name", "Project", "App #",       "Travel Date",     "Month"],
            (ReportDashboardCategory.AddressOfResidence, "by-validity")     => ["Name", "Project", "Address", "Expiry", "Validity"],
            (ReportDashboardCategory.AddressOfResidence, "by-region")       => ["Name", "Project", "Address", "Expiry", "Region"],
            (ReportDashboardCategory.AddressOfResidence, "by-city")         => ["Name", "Project", "Address", "Expiry", "City"],
            (ReportDashboardCategory.AddressOfResidence, "by-address-type") => ["Name", "Project", "Address", "Expiry", "Address Type"],
            (ReportDashboardCategory.AddressOfResidence, "by-address")      => ["Name", "Project", "Region · City", "Expiry", "Address"],
            (ReportDashboardCategory.WorkPermit, "by-status")         => ["Name", "Project", "WP Number",   "Expiry", "Status"],
            (ReportDashboardCategory.WorkPermit, "by-days-remaining") => ["Name", "Project", "WP Number",   "Expiry", "Days Remaining"],
            (ReportDashboardCategory.Invitation, "ready-by-project")    => ["Name", "Project", "Invitation #", "Expiry", "Project"],
            (ReportDashboardCategory.Invitation, "ready-by-period-category") => ["Name", "Project", "Invitation #", "Expiry", "Period · Category · Type"],
            (ReportDashboardCategory.Invitation, "in-process")          => ["Name", "Project", "App #", "App Date", "Process State"],
            (ReportDashboardCategory.Invitation, "rejected-by-project") => ["Name", "Project", "Rejection #", "Date", "Project"],
            (ReportDashboardCategory.Invitation, "used")                => ["Name", "Project", "Invitation #", "Issued", "Project"],
            (ReportDashboardCategory.Invitation, "valid-until")         => ["Name", "Project", "Invitation #", "Expiry", "Valid Until"],
            (ReportDashboardCategory.Invitation, "expired")             => ["Name", "Project", "Invitation #", "Expiry", "Valid Until"],
            (ReportDashboardCategory.Invitation, "issued-inv")          => ["Name", "Project", "Invitation #", "Expiry", "Project"],
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
        bool includeArchivedPersons = false)
    {
        string roleCriteria;
        if (IsAllPersonTypes(personType))
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
            var projectCriteria = category switch
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

        if (!string.IsNullOrWhiteSpace(statusLabel) && category == ReportDashboardCategory.VisaExtension)
            roleCriteria = $"({roleCriteria}) And [CurrentState.Name] = '{Escape(statusLabel)}'";

        if (!string.IsNullOrWhiteSpace(statusLabel) && category == ReportDashboardCategory.Subcontractor)
        {
            var companyCriteria = string.Equals(statusLabel, "Unassigned", StringComparison.OrdinalIgnoreCase)
                ? "[Subcontractor] is null"
                : $"[Subcontractor.NameTm] = '{Escape(statusLabel)}' Or [Subcontractor.Name] = '{Escape(statusLabel)}'";
            roleCriteria = $"({roleCriteria}) And ({companyCriteria})";
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

    private static string PersonRolePath(ReportDashboardCategory category) => "Person.PersonRole";

    private static string Escape(string value) => value.Replace("'", "''");
}
