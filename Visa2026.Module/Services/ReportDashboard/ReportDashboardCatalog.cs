using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

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
        ReportDashboardCategory.BorderZone,
        ReportDashboardCategory.Passport,
        ReportDashboardCategory.Education,
        ReportDashboardCategory.PositionHistory
    ];

    /// <summary>
    /// Categories that expose an Include archived toggle (Person.IsArchived on the SQL view / loader).
    /// </summary>
    public static bool SupportsIncludeArchivedPersons(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.Passport
            or ReportDashboardCategory.WorkPermit
            or ReportDashboardCategory.Education;

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
    /// Shared across by-progress and by-type; default exclude both.
    /// </summary>
    public static bool SupportsIncludeCompletedApplicationProcesses(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.Application;

    public static bool SupportsIncludeCancelledApplicationProcesses(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.Application;

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

    public static IReadOnlyList<ReportDashboardSubReport> SubReports(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.Application => [
            new() { Key = "by-progress", Label = "By Progress" },
            new() { Key = "by-type",     Label = "By Type"     },
        ],
        ReportDashboardCategory.VisaExtension => [
            new() { Key = "visa-state",   Label = "Visa State"         },
            new() { Key = "app-progress", Label = "Application Progress"},
            new() { Key = "by-category",  Label = "By Visa Category"   },
            new() { Key = "by-type",      Label = "By Visa Type"       },
            new() { Key = "by-period",         Label = "By Visa Period"     },
            new() { Key = "by-days-remaining", Label = "By Days Remaining"  },
        ],
        ReportDashboardCategory.Invitation => [
            new() { Key = "issued-inv",   Label = "Issued Invitations"    },
            new() { Key = "app-progress", Label = "Application Progress"  },
        ],
        ReportDashboardCategory.Registration => [
            new() { Key = "by-validity", Label = "By Validity" },
            new() { Key = "by-region",   Label = "By Region"   },
        ],
        ReportDashboardCategory.WorkPermit => [
            new() { Key = "by-days-remaining", Label = "By Days Remaining" },
            new() { Key = "by-status",         Label = "By Status"         },
        ],
        ReportDashboardCategory.Travel => [
            new() { Key = "by-month",  Label = "By Month"  },
            new() { Key = "by-status", Label = "By Status" },
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
            new() { Key = "by-status",   Label = "By Status"   },
            new() { Key = "by-position", Label = "By Position" },
        ],
        _ => [new() { Key = "default", Label = "Overview" }]
    };

    public static string DefaultSubReport(ReportDashboardCategory category) =>
        SubReports(category).Count > 0 ? SubReports(category)[0].Key : "default";

    // ---- Labels ----------------------------------------------------------

    public static string CategoryLabel(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.Application   => "Application",
        ReportDashboardCategory.VisaExtension => "Visa",
        ReportDashboardCategory.Invitation    => "Invitation",
        ReportDashboardCategory.Registration  => "Registration",
        ReportDashboardCategory.WorkPermit    => "Work Permit",
        ReportDashboardCategory.Travel        => "Travel",
        ReportDashboardCategory.BorderZone       => "Border Zone",
        ReportDashboardCategory.Passport         => "Passport",
        ReportDashboardCategory.Education        => "Education",
        ReportDashboardCategory.PositionHistory  => "Position History",
        _ => category.ToString()
    };

    public static string PersonTypeLabel(ReportDashboardPersonType personType) => personType switch
    {
        ReportDashboardPersonType.All              => "All",
        ReportDashboardPersonType.Employees        => "Employees",
        ReportDashboardPersonType.FamilyMembers    => "Family Members",
        ReportDashboardPersonType.TemporaryVisitors=> "Temporary Visitors",
        _ => personType.ToString()
    };

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
        ReportDashboardCategory.Registration  => "AddressOfResidence_ListView",
        ReportDashboardCategory.WorkPermit    => "WorkPermitItem_ListView",
        ReportDashboardCategory.Travel        => "ApplicationItem_ListView",
        ReportDashboardCategory.BorderZone       => "BorderZoneItem_ListView",
        ReportDashboardCategory.Passport         => "Passport_ListView",
        ReportDashboardCategory.Education        => "Education_ListView",
        ReportDashboardCategory.PositionHistory  => "EmployeePositionHistory_ListView",
        _ => "Person_ListView"
    };

    public static Type ListViewType(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.Application      => typeof(Application),
        ReportDashboardCategory.VisaExtension    => typeof(VisaExtensionStatus),
        ReportDashboardCategory.Invitation       => typeof(InvitationItem),
        ReportDashboardCategory.Registration     => typeof(AddressOfResidence),
        ReportDashboardCategory.WorkPermit       => typeof(WorkPermitItem),
        ReportDashboardCategory.Travel           => typeof(ApplicationItem),
        ReportDashboardCategory.BorderZone       => typeof(BorderZoneItem),
        ReportDashboardCategory.Passport         => typeof(Passport),
        ReportDashboardCategory.Education        => typeof(Education),
        ReportDashboardCategory.PositionHistory  => typeof(EmployeePositionHistory),
        _ => typeof(Person)
    };

    // ---- Table headers (sub-report-aware) --------------------------------

    public static string[] TableHeaders(ReportDashboardCategory category, string? subReport = null) =>
        (category, subReport) switch
        {
            (ReportDashboardCategory.Application, "by-progress") => ["Name", "Project", "App #", "App Date", "Progress State"],
            (ReportDashboardCategory.Application, "by-type")     => ["Name", "Project", "App #", "App Date", "Application Type"],
            // Categorical: last column = grouping dimension; ColumnA = passport # or identifier
            (ReportDashboardCategory.Passport, "by-type")         => ["Name", "Project", "Passport #",  "Expiry", "Type"],
            (ReportDashboardCategory.Passport, "by-citizenship")   => ["Name", "Project", "Passport #",  "Expiry", "Citizenship"],
            (ReportDashboardCategory.Registration, "by-region")    => ["Name", "Project", "Address",     "Expiry", "Region"],
            (ReportDashboardCategory.BorderZone, "by-zone")        => ["Name", "Project", "BZ Number",   "Valid Until", "Zone"],
            (ReportDashboardCategory.VisaExtension, "visa-state")   => ["Name", "Project", "Visa #",   "Expiry",      "Visa State"     ],
            (ReportDashboardCategory.VisaExtension, "app-progress") => ["Name", "Project", "App #",    "App Date",    "Progress State" ],
            (ReportDashboardCategory.VisaExtension, "by-category")  => ["Name", "Project", "Visa #",   "Expiry",      "Visa Category" ],
            (ReportDashboardCategory.VisaExtension, "by-type")      => ["Name", "Project", "Visa #",   "Expiry",      "Visa Type"     ],
            (ReportDashboardCategory.VisaExtension, "by-period")         => ["Name", "Project", "Visa #",   "Expiry",      "Period" ],
            (ReportDashboardCategory.VisaExtension, "by-days-remaining") => ["Name", "Project", "Visa #",   "Expiry",      "Days Remaining" ],
            (ReportDashboardCategory.Invitation, "by-month")       => ["Name", "Project", "Month",       "Issue Date",      "Status"],
            (ReportDashboardCategory.Travel, "by-month")           => ["Name", "Project", "App #",       "Travel Date",     "Month"],
            (ReportDashboardCategory.WorkPermit, "by-status")         => ["Name", "Project", "WP Number",   "Expiry", "Status"],
            (ReportDashboardCategory.WorkPermit, "by-days-remaining") => ["Name", "Project", "WP Number",   "Expiry", "Days Remaining"],
            (ReportDashboardCategory.Invitation, "issued-inv")   => ["Name", "Project", "Invitation #", "Expiry",    "Validity"      ],
            (ReportDashboardCategory.Invitation, "app-progress") => ["Name", "Project", "App #",        "App Date",  "Progress State"],
            (ReportDashboardCategory.Education, "by-level")     => ["Name", "Project", "Institution", "Grad Year", "Level"],
            (ReportDashboardCategory.Education, "by-country")   => ["Name", "Project", "Institution", "Grad Year", "Country"],
            (ReportDashboardCategory.Education, "by-specialty") => ["Name", "Project", "Institution", "Grad Year", "Speciality"],
            (ReportDashboardCategory.PositionHistory, "by-status")   => ["Name", "Project", "Position", "Start", "Status"],
            (ReportDashboardCategory.PositionHistory, "by-position") => ["Name", "Project", "Position", "Start", "Position"],
            _ => DefaultTableHeaders(category)
        };

    private static string[] DefaultTableHeaders(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.Application      => ["Name", "Project", "App #",           "App Date",        "Status"],
        ReportDashboardCategory.VisaExtension    => ["Name", "Project", "Current Expiry",  "Requested Until", "Status"],
        ReportDashboardCategory.Invitation       => ["Name", "Project", "Invitation #",    "Issue Date",      "Status"],
        ReportDashboardCategory.Registration     => ["Name", "Project", "Address",         "Expiry",          "Status"],
        ReportDashboardCategory.WorkPermit       => ["Name", "Project", "WP Number",       "Expiry",          "Status"],
        ReportDashboardCategory.Travel           => ["Name", "Project", "App #",           "Travel Date",     "Status"],
        ReportDashboardCategory.BorderZone       => ["Name", "Project", "BZ Number",       "Valid Until",     "Status"],
        ReportDashboardCategory.Passport         => ["Name", "Project", "Passport #",      "Expiry",          "Validity"],
        ReportDashboardCategory.Education        => ["Name", "Project", "Institution",     "Grad Year",       "Level"],
        ReportDashboardCategory.PositionHistory  => ["Name", "Project", "Position",        "Start",           "Status"],
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
                or ReportDashboardCategory.Education
                or ReportDashboardCategory.PositionHistory
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
                    or ReportDashboardCategory.PositionHistory =>
                    $"[Person.ProjectContract.Name] = '{Escape(projectKey)}' Or [Person.ProjectContract.NameTm] = '{Escape(projectKey)}'",
                _ => "True"
            };
            roleCriteria = $"({roleCriteria}) And ({projectCriteria})";
        }

        if (!string.IsNullOrWhiteSpace(statusLabel) && category == ReportDashboardCategory.VisaExtension)
            roleCriteria = $"({roleCriteria}) And [CurrentState.Name] = '{Escape(statusLabel)}'";

        if (!includeArchivedPersons && SupportsIncludeArchivedPersons(category))
        {
            var archivedCriteria = category switch
            {
                ReportDashboardCategory.Passport =>
                    "[Person.IsArchived] = False",
                ReportDashboardCategory.WorkPermit =>
                    "[Person.IsArchived] = False",
                ReportDashboardCategory.Education =>
                    "[Person.IsArchived] = False",
                _ => "True"
            };
            roleCriteria = $"({roleCriteria}) And ({archivedCriteria})";
        }

        return roleCriteria;
    }

    private static string PersonRolePath(ReportDashboardCategory category) => "Person.PersonRole";

    private static string Escape(string value) => value.Replace("'", "''");
}