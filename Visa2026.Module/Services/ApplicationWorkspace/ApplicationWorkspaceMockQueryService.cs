using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Prototype mock — hard-coded rows from docs/prototypes/application-detail-m2m.html.
/// Replace with <see cref="ApplicationWorkspaceQueryService"/> when domain M2M ships.
/// </summary>
public sealed class ApplicationWorkspaceMockQueryService : IApplicationWorkspaceQueryService
{

    private static readonly IReadOnlyList<string> LinkContext =
    [
        "Person",
        "Passport",
        "Visa",
        "AddressOfResidence",
        "WorkPermitItem",
        "InvitationItem",
        "BorderZoneItem",
        "Education",
        "EmployeeSalary",
        "EmployeePositionHistory",
        "MedicalRecord",
        "TravelHistory",
        "RejectionItem",
    ];

    public ApplicationWorkspaceSnapshot Load(IObjectSpace objectSpace, Guid applicationId) =>
        new()
        {
            ApplicationId = applicationId == Guid.Empty ? Guid.Parse("11111111-1111-1111-1111-111111111111") : applicationId,
            Header = new ApplicationWorkspaceHeader
            {
                ApplicationNumber = "12/-7010",
                ApplicationDate = "01.08.2026",
                Urgency = "Normal",
                ProgressStep = 3,
                ProgressTotalSteps = 6,
                SlaDaysElapsed = 5,
                SlaDaysTotal = 10,
            },
            ProgressHistory =
            [
                new() { State = "Office preparation", Date = "01.08.2026", Description = "Draft package" },
                new() { State = "Submitted (ministry)", Date = "03.08.2026", Description = "Türkmenenergo" },
                new() { State = "Approved (ministry)", Date = "05.08.2026", Description = "Letter on file" },
                new() { State = "Submitted (migration)", Date = "06.08.2026", Description = "AS538188" },
            ],
            Profile = new ApplicationWorkspaceProfileSummary
            {
                ProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Invitation + WP (employee)",
                Code = "INV_WP_EMP",
                Chips =
                [
                    "Related to: Issuance",
                    "Via ministry",
                    "For: Employee",
                    "Produces: Invitation, Work permit",
                ],
            },
            ProfileRail = Array.Empty<ApplicationWorkspaceProfileRailItem>(),
            LinkContextItems = LinkContext,
            Tabs = BuildTabs(),
            IsPrototypeMock = true,
        };

    private static IReadOnlyList<ApplicationWorkspaceTab> BuildTabs() =>
    [
        Tab("person", "Person", ["Person", "Role", "Personal №", "From view"],
            [
                ["Berdiýew A.A.", "Employee", "I-123456", "vw_app_persons"],
                ["Berdiýewa G.A.", "Family member", "I-654321", "vw_app_persons"],
            ],
            sqlViewHint: "vw_app_persons"),
        Tab("passport", "Passport", ["Person", "Passport №", "Issued", "Expires"],
            [
                ["Berdiýew A.A.", "A-998877", "12.01.2022", "12.01.2032"],
                ["Berdiýewa G.A.", "A-112233", "03.05.2021", "03.05.2031"],
            ]),
        Tab("visa", "Visa", ["Person", "Visa №", "Type", "Valid to"],
            [["Berdiýew A.A.", "V-44001", "WP", "01.02.2027"]]),
        Tab("education", "Education", ["Person", "Institution", "Level", "Year"],
            [["Berdiýew A.A.", "TDU", "Bachelor", "2014"]]),
        Tab("address", "Address", ["Person", "City", "Address"],
            [["Berdiýew A.A.", "Aşgabat", "…"]]),
        Tab("wp", "Work permit", ["Person", "WP item", "Location", "Valid to"],
            [["Berdiýew A.A.", "WP-7788", "Aşgabat", "01.02.2027"]]),
        Tab("inv", "Invitation", ["Person", "Invitation item", "Status"],
            [["Berdiýew A.A.", "INV-2201", "Current"]]),
        Tab("bz", "Border zone", ["Person", "Border zone item", "Zones"],
            emptyMessage: "No linked border-zone items"),
        Tab("position", "Position", ["Person", "Position", "From"],
            [["Berdiýew A.A.", "Engineer", "01.03.2023"]]),
        Tab("salary", "Salary", ["Person", "Amount", "Currency"],
            [["Berdiýew A.A.", "…", "TMT"]]),
        Tab("medical", "Medical", ["Person", "Record", "Valid to"],
            [["Berdiýew A.A.", "MED-91", "01.01.2027"]]),
        Tab("travel", "Travel history", ["Person", "Kind", "Date", "Check point"],
            [["Berdiýew A.A.", "External arrival", "12.07.2026", "Howdan"]]),
        Tab("rejection", "Rejection", ["Person", "Rejection item", "Status"],
            emptyMessage: "No current rejection items"),
    ];

    private static ApplicationWorkspaceTab Tab(
        string key,
        string label,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<string>>? rows = null,
        string? emptyMessage = null,
        string? sqlViewHint = null) =>
        new()
        {
            Key = key,
            Label = label,
            Columns = columns,
            Rows = rows ?? Array.Empty<IReadOnlyList<string>>(),
            EmptyMessage = emptyMessage,
            SqlViewHint = sqlViewHint,
        };
}
