using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ReportDashboard;

public static class ReportDashboardCatalog
{
    public static readonly ReportDashboardCategory[] Categories =
    [
        ReportDashboardCategory.VisaExtension,
        ReportDashboardCategory.Invitation,
        ReportDashboardCategory.Registration,
        ReportDashboardCategory.WorkPermit,
        ReportDashboardCategory.Travel,
        ReportDashboardCategory.BorderZone,
        ReportDashboardCategory.Passport
    ];

    // ---- Sub-reports per category ----------------------------------------

    public static IReadOnlyList<ReportDashboardSubReport> SubReports(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.VisaExtension => [
            new() { Key = "visa-state",   Label = "Visa State"         },
            new() { Key = "app-progress", Label = "Application Progress"},
            new() { Key = "by-category",  Label = "By Visa Category"   },
            new() { Key = "by-period",    Label = "By Visa Period"     },
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
            new() { Key = "by-validity", Label = "By Validity" },
            new() { Key = "by-status",   Label = "By Status"   },
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
        _ => [new() { Key = "default", Label = "Overview" }]
    };

    public static string DefaultSubReport(ReportDashboardCategory category) =>
        SubReports(category).Count > 0 ? SubReports(category)[0].Key : "default";

    // ---- Labels ----------------------------------------------------------

    public static string CategoryLabel(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.VisaExtension => "Visa",
        ReportDashboardCategory.Invitation    => "Invitation",
        ReportDashboardCategory.Registration  => "Registration",
        ReportDashboardCategory.WorkPermit    => "Work Permit",
        ReportDashboardCategory.Travel        => "Travel",
        ReportDashboardCategory.BorderZone    => "Border Zone",
        ReportDashboardCategory.Passport      => "Passport",
        _ => category.ToString()
    };

    public static string PersonTypeLabel(ReportDashboardPersonType personType) => personType switch
    {
        ReportDashboardPersonType.Employees        => "Employees",
        ReportDashboardPersonType.FamilyMembers    => "Family Members",
        ReportDashboardPersonType.TemporaryVisitors=> "Temporary Visitors",
        _ => personType.ToString()
    };

    public static PersonRecordRole ToPersonRole(ReportDashboardPersonType personType) => personType switch
    {
        ReportDashboardPersonType.Employees        => PersonRecordRole.Employee,
        ReportDashboardPersonType.FamilyMembers    => PersonRecordRole.FamilyMember,
        _ => PersonRecordRole.TemporaryVisitor
    };

    public static string PersonRoleCriteria(ReportDashboardPersonType personType) => personType switch
    {
        ReportDashboardPersonType.Employees        => PersonRoleHelper.EmployeeCriteria,
        ReportDashboardPersonType.FamilyMembers    => PersonRoleHelper.FamilyMemberCriteria,
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
        ReportDashboardCategory.VisaExtension => "VisaExtensionStatus_ListView",
        ReportDashboardCategory.Invitation    => "InvitationItem_ListView",
        ReportDashboardCategory.Registration  => "AddressOfResidence_ListView",
        ReportDashboardCategory.WorkPermit    => "WorkPermitItem_ListView",
        ReportDashboardCategory.Travel        => "ApplicationItem_ListView",
        ReportDashboardCategory.BorderZone    => "BorderZoneItem_ListView",
        ReportDashboardCategory.Passport      => "Passport_ListView",
        _ => "Person_ListView"
    };

    public static Type ListViewType(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.VisaExtension => typeof(VisaExtensionStatus),
        ReportDashboardCategory.Invitation    => typeof(InvitationItem),
        ReportDashboardCategory.Registration  => typeof(AddressOfResidence),
        ReportDashboardCategory.WorkPermit    => typeof(WorkPermitItem),
        ReportDashboardCategory.Travel        => typeof(ApplicationItem),
        ReportDashboardCategory.BorderZone    => typeof(BorderZoneItem),
        ReportDashboardCategory.Passport      => typeof(Passport),
        _ => typeof(Person)
    };

    // ---- Table headers (sub-report-aware) --------------------------------

    public static string[] TableHeaders(ReportDashboardCategory category, string? subReport = null) =>
        (category, subReport) switch
        {
            // Categorical: last column = grouping dimension; ColumnA = passport # or identifier
            (ReportDashboardCategory.Passport, "by-type")         => ["Name", "Project", "Passport #",  "Expiry", "Type"],
            (ReportDashboardCategory.Passport, "by-citizenship")   => ["Name", "Project", "Passport #",  "Expiry", "Citizenship"],
            (ReportDashboardCategory.Registration, "by-region")    => ["Name", "Project", "Address",     "Expiry", "Region"],
            (ReportDashboardCategory.BorderZone, "by-zone")        => ["Name", "Project", "BZ Number",   "Valid Until", "Zone"],
            (ReportDashboardCategory.VisaExtension, "visa-state")   => ["Name", "Project", "Visa #",   "Expiry",      "Visa State"     ],
            (ReportDashboardCategory.VisaExtension, "app-progress") => ["Name", "Project", "App #",    "App Date",    "Progress State" ],
            (ReportDashboardCategory.VisaExtension, "by-category")  => ["Name", "Project", "Visa #",   "Expiry",      "Visa Category"  ],
            (ReportDashboardCategory.VisaExtension, "by-period")    => ["Name", "Project", "Visa #",   "Expiry",      "Days Remaining" ],
            (ReportDashboardCategory.Invitation, "by-month")       => ["Name", "Project", "Month",       "Issue Date",      "Status"],
            (ReportDashboardCategory.Travel, "by-month")           => ["Name", "Project", "App #",       "Travel Date",     "Month"],
            (ReportDashboardCategory.WorkPermit, "by-status")      => ["Name", "Project", "WP Number",   "Expiry", "Status"],
            (ReportDashboardCategory.Invitation, "issued-inv")   => ["Name", "Project", "Invitation #", "Expiry",    "Validity"      ],
            (ReportDashboardCategory.Invitation, "app-progress") => ["Name", "Project", "App #",        "App Date",  "Progress State"],
            _ => DefaultTableHeaders(category)
        };

    private static string[] DefaultTableHeaders(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.VisaExtension => ["Name", "Project", "Current Expiry",  "Requested Until", "Status"],
        ReportDashboardCategory.Invitation    => ["Name", "Project", "Invitation #",    "Issue Date",      "Status"],
        ReportDashboardCategory.Registration  => ["Name", "Project", "Address",         "Expiry",          "Status"],
        ReportDashboardCategory.WorkPermit    => ["Name", "Project", "WP Number",       "Expiry",          "Status"],
        ReportDashboardCategory.Travel        => ["Name", "Project", "App #",           "Travel Date",     "Status"],
        ReportDashboardCategory.BorderZone    => ["Name", "Project", "BZ Number",       "Valid Until",     "Status"],
        ReportDashboardCategory.Passport      => ["Name", "Project", "Passport #",      "Expiry",          "Validity"],
        _ => ["Name", "Project", "Info", "Date", "Status"]
    };

    // ---- Criteria builder ------------------------------------------------

    public static string BuildListCriteria(
        ReportDashboardPersonType personType,
        ReportDashboardCategory category,
        string? projectKey,
        string? statusLabel)
    {
        var roleCriteria = category == ReportDashboardCategory.VisaExtension
            || category == ReportDashboardCategory.Passport
            || category == ReportDashboardCategory.Registration
                ? $"Person is not null And [{PersonRolePath(category)}] = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,{ToPersonRole(personType)}#"
                : category == ReportDashboardCategory.Invitation
                    || category == ReportDashboardCategory.WorkPermit
                    || category == ReportDashboardCategory.BorderZone
                    || category == ReportDashboardCategory.Travel
                    ? $"Person is not null And [Person.PersonRole] = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,{ToPersonRole(personType)}#"
                    : PersonRoleCriteria(personType);

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            var projectCriteria = category switch
            {
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
                ReportDashboardCategory.Registration or ReportDashboardCategory.Passport =>
                    $"[Person.ProjectContract.Name] = '{Escape(projectKey)}' Or [Person.ProjectContract.NameTm] = '{Escape(projectKey)}'",
                _ => "True"
            };
            roleCriteria = $"({roleCriteria}) And ({projectCriteria})";
        }

        if (!string.IsNullOrWhiteSpace(statusLabel) && category == ReportDashboardCategory.VisaExtension)
            roleCriteria = $"({roleCriteria}) And [CurrentState.Name] = '{Escape(statusLabel)}'";

        return roleCriteria;
    }

    private static string PersonRolePath(ReportDashboardCategory category) => "Person.PersonRole";

    private static string Escape(string value) => value.Replace("'", "''");
}